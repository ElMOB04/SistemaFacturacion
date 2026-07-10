using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>
    /// Acceso a datos para Cobros (abonos de clientes a las Cuentas por Cobrar).
    /// El registro de un cobro es transaccional: guarda el abono, reduce el saldo
    /// de la factura (y la marca como Pagada si queda saldada) y reduce el saldo
    /// del cliente.
    /// </summary>
    public class CobroDAO
    {
        /// <summary>Registra un cobro y actualiza los saldos. Devuelve el ID del cobro.</summary>
        /// <param name="cobro">Datos del cobro (factura, cliente, monto, forma de pago...).</param>
        /// <returns>El ID del cobro recien registrado.</returns>
        public int Registrar(Cobro cobro)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                // Transaccion: insertar el cobro, rebajar el saldo de la factura y rebajar
                // el saldo del cliente deben ir juntos. Si algo falla, se revierte todo para
                // que los importes cobrados nunca queden a medias.
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Primero validamos el saldo real de la factura leyendolo DENTRO de la
                        // transaccion (dato fresco). 'saldoFactura' es lo que aun se debe.
                        decimal saldoFactura;
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT Saldo FROM dbo.Facturas WHERE FacturaID=@id", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", cobro.FacturaID);
                            object res = cmd.ExecuteScalar();
                            // res == null significa que la factura no existe.
                            if (res == null) throw new Exception("La factura no existe.");
                            saldoFactura = Convert.ToDecimal(res);
                        }

                        // Regla de negocio: no se puede cobrar mas de lo que la factura debe.
                        if (cobro.Monto > saldoFactura)
                            throw new Exception("El monto del cobro (" + cobro.Monto.ToString("N2") +
                                                ") no puede ser mayor que el saldo de la factura (" +
                                                saldoFactura.ToString("N2") + ").");

                        // 1) Insertar el cobro y capturar su ID con SCOPE_IDENTITY().
                        int cobroID;
                        string sql = @"INSERT INTO dbo.Cobros (FacturaID, ClienteID, Fecha, Monto, FormaPago, Referencia, EmpleadoID)
                                       VALUES (@fac, @cli, @fecha, @monto, @forma, @ref, @emp);
                                       SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@fac", cobro.FacturaID);
                            cmd.Parameters.AddWithValue("@cli", cobro.ClienteID);
                            cmd.Parameters.AddWithValue("@fecha", cobro.Fecha);
                            cmd.Parameters.AddWithValue("@monto", cobro.Monto);
                            cmd.Parameters.AddWithValue("@forma", cobro.FormaPago);
                            // Referencia y EmpleadoID son opcionales: null -> DBNull.Value (NULL en BD).
                            cmd.Parameters.AddWithValue("@ref", (object)cobro.Referencia ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@emp", (object)cobro.EmpleadoID ?? DBNull.Value);
                            cobroID = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2) Reducir el saldo de la factura y recalcular su estado.
                        // El CASE deja el Estado en 'Pagada' cuando el saldo llega a 0 (o menos)
                        // tras el abono, o 'Pendiente' si todavia queda algo por cobrar.
                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE dbo.Facturas
                              SET Saldo = Saldo - @monto,
                                  Estado = CASE WHEN Saldo - @monto <= 0 THEN 'Pagada' ELSE 'Pendiente' END
                              WHERE FacturaID = @fac;", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@monto", cobro.Monto);
                            cmd.Parameters.AddWithValue("@fac", cobro.FacturaID);
                            cmd.ExecuteNonQuery();
                        }

                        // 3) Reducir el saldo (Cuentas por Cobrar) del cliente: si nos paga,
                        // su deuda total baja en el mismo monto del cobro.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Clientes SET Saldo = Saldo - @monto WHERE ClienteID = @cli", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@monto", cobro.Monto);
                            cmd.Parameters.AddWithValue("@cli", cobro.ClienteID);
                            cmd.ExecuteNonQuery();
                        }

                        // Confirmamos los tres pasos como una sola operacion.
                        tran.Commit();
                        return cobroID;
                    }
                    catch
                    {
                        // Si algo falla (validacion, error de BD...) revertimos todo y relanzamos.
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Lista el historial de cobros con nombre de cliente y numero de factura.</summary>
        /// <returns>Historial de cobros, del mas reciente al mas antiguo.</returns>
        public List<Cobro> Listar()
        {
            // JOIN con Facturas y Clientes para mostrar el numero de factura y el nombre
            // del cliente en la pantalla, en vez de solo sus IDs.
            string sql = @"SELECT co.CobroID, co.FacturaID, f.NumeroFactura, co.ClienteID, cl.Nombre AS Cliente,
                                  co.Fecha, co.Monto, co.FormaPago, co.Referencia, co.EmpleadoID
                           FROM dbo.Cobros co
                           INNER JOIN dbo.Facturas f ON f.FacturaID = co.FacturaID
                           INNER JOIN dbo.Clientes cl ON cl.ClienteID = co.ClienteID
                           ORDER BY co.Fecha DESC, co.CobroID DESC";
            DataTable dt = ConexionBD.EjecutarConsulta(sql);
            List<Cobro> lista = new List<Cobro>();
            // Aqui el mapeo se hace en linea (sin metodo Mapear aparte): cada fila -> un Cobro.
            foreach (DataRow f in dt.Rows)
            {
                lista.Add(new Cobro
                {
                    CobroID = Convert.ToInt32(f["CobroID"]),
                    FacturaID = Convert.ToInt32(f["FacturaID"]),
                    NumeroFactura = f["NumeroFactura"].ToString(),
                    ClienteID = Convert.ToInt32(f["ClienteID"]),
                    NombreCliente = f["Cliente"].ToString(),
                    Fecha = Convert.ToDateTime(f["Fecha"]),
                    Monto = Convert.ToDecimal(f["Monto"]),
                    FormaPago = f["FormaPago"].ToString(),
                    // Campos opcionales: Referencia NULL -> "" y EmpleadoID NULL -> null (int?).
                    Referencia = f["Referencia"] == DBNull.Value ? "" : f["Referencia"].ToString(),
                    EmpleadoID = f["EmpleadoID"] == DBNull.Value ? (int?)null : Convert.ToInt32(f["EmpleadoID"])
                });
            }
            return lista;
        }
    }
}
