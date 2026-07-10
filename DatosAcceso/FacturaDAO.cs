using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>
    /// Acceso a datos para Facturas y su detalle. La creacion de una factura
    /// es una operacion transaccional: inserta el encabezado, las lineas,
    /// descuenta el inventario y actualiza el saldo del cliente de forma atomica.
    /// </summary>
    public class FacturaDAO
    {
        /// <summary>
        /// Inserta una factura completa (encabezado + detalle) dentro de una
        /// transaccion. Descuenta stock de los productos y aumenta el saldo
        /// (Cuentas por Cobrar) del cliente. Devuelve el ID de la factura.
        /// </summary>
        /// <param name="factura">Factura con su encabezado y su lista de Detalles.</param>
        /// <returns>El ID de la factura recien creada.</returns>
        public int Insertar(Factura factura)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                // Abrimos una transaccion: todos los pasos siguientes (encabezado, detalle,
                // ajuste de stock y saldo del cliente) forman una unidad atomica. O se
                // confirman TODOS juntos (Commit) o no se aplica NINGUNO (Rollback). Asi
                // nunca queda, por ejemplo, una factura sin detalle o un stock descuadrado.
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1) Insertar encabezado de la factura.
                        // Al final el SELECT SCOPE_IDENTITY() nos devuelve el ID generado,
                        // que necesitaremos para enlazar las lineas del detalle.
                        string sqlCab = @"INSERT INTO dbo.Facturas
                            (NumeroFactura, ClienteID, EmpleadoID, Fecha, Subtotal, Impuesto, Total, Saldo, TipoPago, Estado)
                            VALUES (@num, @cli, @emp, @fecha, @sub, @imp, @tot, @saldo, @tpago, @estado);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        // 'facturaID' guarda la clave del encabezado; es la variable que
                        // amarra todo el proceso (detalle, stock) a esta factura concreta.
                        int facturaID;
                        // Cada SqlCommand se crea con (sql, cn, tran) para que participe de
                        // la MISMA transaccion; si se omitiera 'tran' quedaria fuera de ella.
                        using (SqlCommand cmd = new SqlCommand(sqlCab, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@num", factura.NumeroFactura);
                            cmd.Parameters.AddWithValue("@cli", factura.ClienteID);
                            // EmpleadoID es opcional (int?): si viene null guardamos NULL en la BD.
                            cmd.Parameters.AddWithValue("@emp", (object)factura.EmpleadoID ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@fecha", factura.Fecha);
                            cmd.Parameters.AddWithValue("@sub", factura.Subtotal);
                            cmd.Parameters.AddWithValue("@imp", factura.Impuesto);
                            cmd.Parameters.AddWithValue("@tot", factura.Total);
                            cmd.Parameters.AddWithValue("@saldo", factura.Saldo);
                            cmd.Parameters.AddWithValue("@tpago", factura.TipoPago);
                            cmd.Parameters.AddWithValue("@estado", factura.Estado);
                            // ExecuteScalar recupera el SCOPE_IDENTITY(): el ID recien asignado.
                            facturaID = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2) Insertar cada linea del detalle y descontar inventario.
                        // 'd' es cada renglon de la factura (un producto/servicio, cantidad y precio).
                        foreach (DetalleFactura d in factura.Detalles)
                        {
                            // Insertamos la linea enlazandola al encabezado mediante @fac (= facturaID).
                            string sqlDet = @"INSERT INTO dbo.DetalleFactura
                                (FacturaID, ProductoID, Cantidad, PrecioUnitario, Importe)
                                VALUES (@fac, @prod, @cant, @precio, @imp);";
                            using (SqlCommand cmd = new SqlCommand(sqlDet, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@fac", facturaID);
                                cmd.Parameters.AddWithValue("@prod", d.ProductoID);
                                cmd.Parameters.AddWithValue("@cant", d.Cantidad);
                                cmd.Parameters.AddWithValue("@precio", d.PrecioUnitario);
                                cmd.Parameters.AddWithValue("@imp", d.Importe);
                                cmd.ExecuteNonQuery();
                            }

                            // Vender rebaja el inventario: Stock = Stock - cantidad. La condicion
                            // Tipo = 'Producto' asegura que a los servicios NO se les toca el stock.
                            string sqlStock = @"UPDATE dbo.Productos
                                                SET Stock = Stock - @cant
                                                WHERE ProductoID = @prod AND Tipo = 'Producto';";
                            using (SqlCommand cmd = new SqlCommand(sqlStock, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@cant", d.Cantidad);
                                cmd.Parameters.AddWithValue("@prod", d.ProductoID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3) Aumentar el saldo (Cuentas por Cobrar) del cliente.
                        // Al vender a credito, el cliente nos debe mas: sumamos el saldo pendiente
                        // de esta factura a su deuda acumulada.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Clientes SET Saldo = Saldo + @saldo WHERE ClienteID = @cli", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@saldo", factura.Saldo);
                            cmd.Parameters.AddWithValue("@cli", factura.ClienteID);
                            cmd.ExecuteNonQuery();
                        }

                        // Si llegamos aqui sin excepciones, confirmamos: los 4 pasos se hacen
                        // definitivos en la BD de una sola vez.
                        tran.Commit();
                        return facturaID;
                    }
                    catch
                    {
                        // Ante cualquier fallo revertimos TODO lo hecho en la transaccion,
                        // dejando la base como estaba antes de empezar, y relanzamos el error
                        // (throw) para que la capa superior se entere y lo maneje.
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Genera el proximo numero de factura correlativo (formato FAC-000001).</summary>
        /// <returns>Cadena tipo "FAC-000042" lista para usar como numero de factura.</returns>
        public string GenerarNumeroFactura()
        {
            // Tomamos el mayor FacturaID existente (ISNULL(...,0) para cuando la tabla
            // esta vacia) y le sumamos 1 para obtener el proximo correlativo.
            object max = ConexionBD.EjecutarEscalar(
                "SELECT ISNULL(MAX(FacturaID), 0) FROM dbo.Facturas");
            int siguiente = Convert.ToInt32(max) + 1;
            // "D6" formatea el numero con 6 digitos y ceros a la izquierda (ej. 42 -> 000042).
            return "FAC-" + siguiente.ToString("D6");
        }

        /// <summary>Lista las facturas (encabezados) con nombre de cliente y empleado.</summary>
        /// <param name="estado">Filtro opcional por estado (ej. "Pendiente", "Pagada"). Null trae todas.</param>
        /// <returns>Lista de encabezados de factura ordenados de mas reciente a mas antiguo.</returns>
        public List<Factura> Listar(string estado = null)
        {
            // INNER JOIN con Clientes: toda factura tiene cliente obligatorio.
            // LEFT JOIN con Empleados: el empleado es opcional, por eso usamos LEFT (para no
            // perder facturas sin empleado) e ISNULL(...) para mostrar "" en lugar de NULL.
            string sql = @"SELECT f.FacturaID, f.NumeroFactura, f.ClienteID, c.Nombre AS Cliente,
                                  f.EmpleadoID, ISNULL(e.Nombre + ' ' + e.Apellido, '') AS Empleado,
                                  f.Fecha, f.Subtotal, f.Impuesto, f.Total, f.Saldo, f.TipoPago, f.Estado
                           FROM dbo.Facturas f
                           INNER JOIN dbo.Clientes c ON c.ClienteID = f.ClienteID
                           LEFT JOIN dbo.Empleados e ON e.EmpleadoID = f.EmpleadoID
                           WHERE 1=1";
            List<SqlParameter> ps = new List<SqlParameter>();
            // Solo agregamos el filtro por estado si se pidio uno concreto.
            if (!string.IsNullOrEmpty(estado))
            {
                sql += " AND f.Estado = @estado";
                ps.Add(ConexionBD.Parametro("@estado", estado));
            }
            // Ordenamos por fecha descendente (y luego por ID) para ver primero lo mas nuevo.
            sql += " ORDER BY f.Fecha DESC, f.FacturaID DESC";

            DataTable dt = ConexionBD.EjecutarConsulta(sql, ps.ToArray());
            List<Factura> lista = new List<Factura>();
            foreach (DataRow f in dt.Rows)
                lista.Add(MapearCabecera(f));
            return lista;
        }

        /// <summary>Devuelve las facturas con saldo pendiente de un cliente (para cobros).</summary>
        /// <param name="clienteID">Cliente cuyas facturas por cobrar se quieren consultar.</param>
        /// <returns>Facturas con Saldo > 0 y no anuladas, ordenadas por fecha ascendente.</returns>
        public List<Factura> ListarPendientesPorCliente(int clienteID)
        {
            // Solo interesan las facturas que aun deben algo (Saldo > 0) y que no esten
            // anuladas. Se usan al registrar cobros para saber que puede pagar el cliente.
            string sql = @"SELECT f.FacturaID, f.NumeroFactura, f.ClienteID, c.Nombre AS Cliente,
                                  f.EmpleadoID, '' AS Empleado, f.Fecha, f.Subtotal, f.Impuesto,
                                  f.Total, f.Saldo, f.TipoPago, f.Estado
                           FROM dbo.Facturas f
                           INNER JOIN dbo.Clientes c ON c.ClienteID = f.ClienteID
                           WHERE f.ClienteID = @cli AND f.Saldo > 0 AND f.Estado <> 'Anulada'
                           ORDER BY f.Fecha";
            DataTable dt = ConexionBD.EjecutarConsulta(sql, ConexionBD.Parametro("@cli", clienteID));
            List<Factura> lista = new List<Factura>();
            foreach (DataRow f in dt.Rows)
                lista.Add(MapearCabecera(f));
            return lista;
        }

        /// <summary>Obtiene una factura con su detalle completo.</summary>
        /// <param name="facturaID">ID de la factura a cargar.</param>
        /// <returns>La factura con su lista de Detalles llena, o null si no existe.</returns>
        public Factura ObtenerConDetalle(int facturaID)
        {
            // Primera consulta: el encabezado (datos generales de la factura + cliente/empleado).
            string sqlCab = @"SELECT f.FacturaID, f.NumeroFactura, f.ClienteID, c.Nombre AS Cliente,
                                     f.EmpleadoID, ISNULL(e.Nombre + ' ' + e.Apellido,'') AS Empleado,
                                     f.Fecha, f.Subtotal, f.Impuesto, f.Total, f.Saldo, f.TipoPago, f.Estado
                              FROM dbo.Facturas f
                              INNER JOIN dbo.Clientes c ON c.ClienteID = f.ClienteID
                              LEFT JOIN dbo.Empleados e ON e.EmpleadoID = f.EmpleadoID
                              WHERE f.FacturaID = @id";
            DataTable dtCab = ConexionBD.EjecutarConsulta(sqlCab, ConexionBD.Parametro("@id", facturaID));
            // Si no existe el encabezado, no hay nada mas que cargar.
            if (dtCab.Rows.Count == 0) return null;

            // Mapeamos el encabezado a un objeto Factura (todavia sin lineas de detalle).
            Factura factura = MapearCabecera(dtCab.Rows[0]);

            // Segunda consulta: las lineas de la factura, con el nombre del producto (JOIN).
            string sqlDet = @"SELECT d.DetalleID, d.FacturaID, d.ProductoID, p.Nombre AS Producto,
                                     d.Cantidad, d.PrecioUnitario, d.Importe
                              FROM dbo.DetalleFactura d
                              INNER JOIN dbo.Productos p ON p.ProductoID = d.ProductoID
                              WHERE d.FacturaID = @id";
            DataTable dtDet = ConexionBD.EjecutarConsulta(sqlDet, ConexionBD.Parametro("@id", facturaID));
            // Cada fila de detalle se agrega a la coleccion Detalles de la factura.
            foreach (DataRow f in dtDet.Rows)
            {
                factura.Detalles.Add(new DetalleFactura
                {
                    DetalleID = Convert.ToInt32(f["DetalleID"]),
                    FacturaID = Convert.ToInt32(f["FacturaID"]),
                    ProductoID = Convert.ToInt32(f["ProductoID"]),
                    NombreProducto = f["Producto"].ToString(),
                    Cantidad = Convert.ToInt32(f["Cantidad"]),
                    PrecioUnitario = Convert.ToDecimal(f["PrecioUnitario"])
                });
            }
            return factura;
        }

        /// <summary>
        /// Anula una factura: marca el estado como 'Anulada', reduce el saldo
        /// del cliente y devuelve el inventario. Todo dentro de una transaccion.
        /// </summary>
        /// <param name="facturaID">ID de la factura que se desea anular.</param>
        public void Anular(int facturaID)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                // Anular tambien es atomico: revertir stock, ajustar saldo del cliente y
                // marcar la factura debe ocurrir todo junto o nada, para no descuadrar cuentas.
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Leemos el estado actual de la factura DENTRO de la transaccion.
                        // Guardamos en variables locales el cliente, el saldo pendiente y el
                        // estado, porque los necesitamos para revertir los movimientos.
                        int clienteID; decimal saldo; string estado;
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT ClienteID, Saldo, Estado FROM dbo.Facturas WHERE FacturaID=@id", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", facturaID);
                            // Usamos un DataReader porque leemos varias columnas de una sola fila.
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                // Si no hay fila, la factura no existe: abortamos.
                                if (!r.Read())
                                    throw new Exception("La factura no existe.");
                                clienteID = Convert.ToInt32(r["ClienteID"]);
                                saldo = Convert.ToDecimal(r["Saldo"]);
                                estado = r["Estado"].ToString();
                            }
                        }

                        // Regla de negocio: no tiene sentido anular dos veces.
                        if (estado == "Anulada")
                            throw new Exception("La factura ya se encuentra anulada.");

                        // Paso 1: devolver al inventario lo que se habia descontado al facturar.
                        // El UPDATE...FROM suma la cantidad de cada linea de vuelta al Stock del
                        // producto (solo Tipo = 'Producto'; los servicios no tienen inventario).
                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE p SET p.Stock = p.Stock + d.Cantidad
                              FROM dbo.Productos p
                              INNER JOIN dbo.DetalleFactura d ON d.ProductoID = p.ProductoID
                              WHERE d.FacturaID = @id AND p.Tipo = 'Producto';", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", facturaID);
                            cmd.ExecuteNonQuery();
                        }

                        // Paso 2: rebajar del saldo del cliente exactamente lo que aun debia
                        // por esta factura (el 'saldo' que leimos antes). Asi su deuda vuelve
                        // a como estaba antes de emitir la factura.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Clientes SET Saldo = Saldo - @saldo WHERE ClienteID = @cli", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@saldo", saldo);
                            cmd.Parameters.AddWithValue("@cli", clienteID);
                            cmd.ExecuteNonQuery();
                        }

                        // Paso 3: marcar la factura como 'Anulada' y poner su saldo en 0
                        // para que ya no figure como cuenta por cobrar.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Facturas SET Estado='Anulada', Saldo=0 WHERE FacturaID=@id", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", facturaID);
                            cmd.ExecuteNonQuery();
                        }

                        // Todo salio bien: confirmamos los tres pasos juntos.
                        tran.Commit();
                    }
                    catch
                    {
                        // Cualquier error revierte la anulacion completa y propaga la excepcion.
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Convierte una fila del encabezado de factura en un objeto Factura.</summary>
        /// <param name="f">Fila (DataRow) con las columnas del encabezado.</param>
        /// <returns>Factura con los datos generales (sin el detalle).</returns>
        private Factura MapearCabecera(DataRow f)
        {
            // Traduccion fila -> objeto del encabezado. Los importes van a decimal y el
            // EmpleadoID puede ser NULL (por eso el int? y el chequeo de DBNull).
            return new Factura
            {
                FacturaID = Convert.ToInt32(f["FacturaID"]),
                NumeroFactura = f["NumeroFactura"].ToString(),
                ClienteID = Convert.ToInt32(f["ClienteID"]),
                NombreCliente = f["Cliente"].ToString(),
                // EmpleadoID es opcional: si la columna viene NULL guardamos null (int?).
                EmpleadoID = f["EmpleadoID"] == DBNull.Value ? (int?)null : Convert.ToInt32(f["EmpleadoID"]),
                NombreEmpleado = f["Empleado"].ToString(),
                Fecha = Convert.ToDateTime(f["Fecha"]),
                Subtotal = Convert.ToDecimal(f["Subtotal"]),
                Impuesto = Convert.ToDecimal(f["Impuesto"]),
                Total = Convert.ToDecimal(f["Total"]),
                Saldo = Convert.ToDecimal(f["Saldo"]),
                TipoPago = f["TipoPago"].ToString(),
                Estado = f["Estado"].ToString()
            };
        }
    }
}
