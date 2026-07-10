using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>
    /// Acceso a datos para Compras y su detalle (Cuentas por Pagar).
    /// La creacion de una compra es transaccional: inserta encabezado y lineas,
    /// AUMENTA el inventario y aumenta el saldo del proveedor.
    /// </summary>
    public class CompraDAO
    {
        /// <summary>Inserta una compra completa dentro de una transaccion. Devuelve el ID.</summary>
        /// <param name="compra">Compra con su encabezado y su lista de Detalles.</param>
        /// <returns>El ID de la compra recien creada.</returns>
        public int Insertar(Compra compra)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                // La compra es el reflejo de la factura pero al reves: aqui el inventario SUBE
                // y se genera una deuda con el proveedor (Cuentas por Pagar). Todo el proceso
                // (encabezado, detalle, stock, saldo del proveedor) va en una sola transaccion.
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1) Encabezado de la compra. SCOPE_IDENTITY() devuelve el ID generado.
                        string sqlCab = @"INSERT INTO dbo.Compras
                            (NumeroDocumento, ProveedorID, Fecha, Subtotal, Impuesto, Total, Saldo, Estado)
                            VALUES (@num, @prov, @fecha, @sub, @imp, @tot, @saldo, @estado);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        // 'compraID' amarra las lineas del detalle a este encabezado.
                        int compraID;
                        using (SqlCommand cmd = new SqlCommand(sqlCab, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@num", compra.NumeroDocumento);
                            cmd.Parameters.AddWithValue("@prov", compra.ProveedorID);
                            cmd.Parameters.AddWithValue("@fecha", compra.Fecha);
                            cmd.Parameters.AddWithValue("@sub", compra.Subtotal);
                            cmd.Parameters.AddWithValue("@imp", compra.Impuesto);
                            cmd.Parameters.AddWithValue("@tot", compra.Total);
                            cmd.Parameters.AddWithValue("@saldo", compra.Saldo);
                            cmd.Parameters.AddWithValue("@estado", compra.Estado);
                            compraID = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2) Insertar cada linea del detalle y aumentar el inventario.
                        // 'd' es cada renglon comprado (producto, cantidad y costo).
                        foreach (DetalleCompra d in compra.Detalles)
                        {
                            // Linea enlazada al encabezado por @comp (= compraID).
                            string sqlDet = @"INSERT INTO dbo.DetalleCompra
                                (CompraID, ProductoID, Cantidad, CostoUnitario, Importe)
                                VALUES (@comp, @prod, @cant, @costo, @imp);";
                            using (SqlCommand cmd = new SqlCommand(sqlDet, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@comp", compraID);
                                cmd.Parameters.AddWithValue("@prod", d.ProductoID);
                                cmd.Parameters.AddWithValue("@cant", d.Cantidad);
                                cmd.Parameters.AddWithValue("@costo", d.CostoUnitario);
                                cmd.Parameters.AddWithValue("@imp", d.Importe);
                                cmd.ExecuteNonQuery();
                            }

                            // Comprar SUMA existencias: Stock = Stock + cantidad. Igual que en la
                            // venta, solo aplica a Tipo = 'Producto' (los servicios no llevan stock).
                            using (SqlCommand cmd = new SqlCommand(
                                @"UPDATE dbo.Productos SET Stock = Stock + @cant
                                  WHERE ProductoID = @prod AND Tipo = 'Producto';", cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@cant", d.Cantidad);
                                cmd.Parameters.AddWithValue("@prod", d.ProductoID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3) Aumentar el saldo (Cuentas por Pagar) del proveedor: comprar a
                        // credito incrementa lo que le debemos al proveedor.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Proveedores SET Saldo = Saldo + @saldo WHERE ProveedorID = @prov", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@saldo", compra.Saldo);
                            cmd.Parameters.AddWithValue("@prov", compra.ProveedorID);
                            cmd.ExecuteNonQuery();
                        }

                        // Confirmamos toda la compra de una sola vez.
                        tran.Commit();
                        return compraID;
                    }
                    catch
                    {
                        // Ante cualquier error deshacemos todo y propagamos la excepcion.
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Lista las compras (encabezados) con el nombre del proveedor.</summary>
        /// <param name="estado">Filtro opcional por estado. Null trae todas las compras.</param>
        /// <returns>Lista de compras ordenadas de mas reciente a mas antigua.</returns>
        public List<Compra> Listar(string estado = null)
        {
            // JOIN con Proveedores para mostrar su nombre. "WHERE 1=1" facilita el filtro opcional.
            string sql = @"SELECT c.CompraID, c.NumeroDocumento, c.ProveedorID, p.Nombre AS Proveedor,
                                  c.Fecha, c.Subtotal, c.Impuesto, c.Total, c.Saldo, c.Estado
                           FROM dbo.Compras c
                           INNER JOIN dbo.Proveedores p ON p.ProveedorID = c.ProveedorID
                           WHERE 1=1";
            List<SqlParameter> ps = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(estado))
            {
                sql += " AND c.Estado = @estado";
                ps.Add(ConexionBD.Parametro("@estado", estado));
            }
            sql += " ORDER BY c.Fecha DESC, c.CompraID DESC";

            DataTable dt = ConexionBD.EjecutarConsulta(sql, ps.ToArray());
            List<Compra> lista = new List<Compra>();
            foreach (DataRow f in dt.Rows)
                lista.Add(MapearCabecera(f));
            return lista;
        }

        /// <summary>Compras con saldo pendiente de un proveedor (para registrar pagos).</summary>
        /// <param name="proveedorID">Proveedor cuyas compras por pagar se consultan.</param>
        /// <returns>Compras con Saldo > 0 y no anuladas, ordenadas por fecha ascendente.</returns>
        public List<Compra> ListarPendientesPorProveedor(int proveedorID)
        {
            // Solo las compras que aun debemos (Saldo > 0) y no anuladas: sirven para elegir
            // a cual aplicar un pago al proveedor.
            string sql = @"SELECT c.CompraID, c.NumeroDocumento, c.ProveedorID, p.Nombre AS Proveedor,
                                  c.Fecha, c.Subtotal, c.Impuesto, c.Total, c.Saldo, c.Estado
                           FROM dbo.Compras c
                           INNER JOIN dbo.Proveedores p ON p.ProveedorID = c.ProveedorID
                           WHERE c.ProveedorID = @prov AND c.Saldo > 0 AND c.Estado <> 'Anulada'
                           ORDER BY c.Fecha";
            DataTable dt = ConexionBD.EjecutarConsulta(sql, ConexionBD.Parametro("@prov", proveedorID));
            List<Compra> lista = new List<Compra>();
            foreach (DataRow f in dt.Rows)
                lista.Add(MapearCabecera(f));
            return lista;
        }

        /// <summary>Obtiene una compra con su detalle completo.</summary>
        /// <param name="compraID">ID de la compra a cargar.</param>
        /// <returns>La compra con su lista de Detalles, o null si no existe.</returns>
        public Compra ObtenerConDetalle(int compraID)
        {
            // Primera consulta: el encabezado de la compra con el nombre del proveedor.
            string sqlCab = @"SELECT c.CompraID, c.NumeroDocumento, c.ProveedorID, p.Nombre AS Proveedor,
                                     c.Fecha, c.Subtotal, c.Impuesto, c.Total, c.Saldo, c.Estado
                              FROM dbo.Compras c
                              INNER JOIN dbo.Proveedores p ON p.ProveedorID = c.ProveedorID
                              WHERE c.CompraID = @id";
            DataTable dtCab = ConexionBD.EjecutarConsulta(sqlCab, ConexionBD.Parametro("@id", compraID));
            if (dtCab.Rows.Count == 0) return null;

            // Encabezado a objeto; luego le agregamos las lineas.
            Compra compra = MapearCabecera(dtCab.Rows[0]);
            // Segunda consulta: las lineas de la compra, con el nombre del producto (JOIN).
            string sqlDet = @"SELECT d.DetalleID, d.CompraID, d.ProductoID, pr.Nombre AS Producto,
                                     d.Cantidad, d.CostoUnitario, d.Importe
                              FROM dbo.DetalleCompra d
                              INNER JOIN dbo.Productos pr ON pr.ProductoID = d.ProductoID
                              WHERE d.CompraID = @id";
            DataTable dtDet = ConexionBD.EjecutarConsulta(sqlDet, ConexionBD.Parametro("@id", compraID));
            // Cada fila se convierte en un DetalleCompra y se agrega a la coleccion.
            foreach (DataRow f in dtDet.Rows)
            {
                compra.Detalles.Add(new DetalleCompra
                {
                    DetalleID = Convert.ToInt32(f["DetalleID"]),
                    CompraID = Convert.ToInt32(f["CompraID"]),
                    ProductoID = Convert.ToInt32(f["ProductoID"]),
                    NombreProducto = f["Producto"].ToString(),
                    Cantidad = Convert.ToInt32(f["Cantidad"]),
                    CostoUnitario = Convert.ToDecimal(f["CostoUnitario"])
                });
            }
            return compra;
        }

        /// <summary>Convierte una fila del encabezado de compra en un objeto Compra.</summary>
        /// <param name="f">Fila (DataRow) con las columnas del encabezado.</param>
        /// <returns>Compra con los datos generales (sin el detalle).</returns>
        private Compra MapearCabecera(DataRow f)
        {
            // Traduccion fila -> objeto: IDs a int, importes a decimal, textos directos.
            return new Compra
            {
                CompraID = Convert.ToInt32(f["CompraID"]),
                NumeroDocumento = f["NumeroDocumento"].ToString(),
                ProveedorID = Convert.ToInt32(f["ProveedorID"]),
                NombreProveedor = f["Proveedor"].ToString(),
                Fecha = Convert.ToDateTime(f["Fecha"]),
                Subtotal = Convert.ToDecimal(f["Subtotal"]),
                Impuesto = Convert.ToDecimal(f["Impuesto"]),
                Total = Convert.ToDecimal(f["Total"]),
                Saldo = Convert.ToDecimal(f["Saldo"]),
                Estado = f["Estado"].ToString()
            };
        }
    }
}
