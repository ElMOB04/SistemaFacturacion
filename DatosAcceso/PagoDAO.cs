using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>
    /// Acceso a datos para Pagos (abonos a proveedores / Cuentas por Pagar).
    /// Transaccional: guarda el pago, reduce el saldo de la compra (la marca
    /// como Pagada si queda saldada) y reduce el saldo del proveedor.
    /// </summary>
    public class PagoDAO
    {
        /// <summary>Registra un pago a proveedor y actualiza los saldos. Devuelve el ID del pago.</summary>
        /// <param name="pago">Datos del pago (compra, proveedor, monto, forma de pago...).</param>
        /// <returns>El ID del pago recien registrado.</returns>
        public int Registrar(Pago pago)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                // Espejo del cobro, pero del lado de las Cuentas por Pagar: insertar el pago,
                // rebajar el saldo de la compra y rebajar la deuda con el proveedor van juntos
                // en una transaccion (todo o nada).
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Leemos el saldo real de la compra dentro de la transaccion.
                        // 'saldoCompra' es lo que aun se le debe al proveedor por esa compra.
                        decimal saldoCompra;
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT Saldo FROM dbo.Compras WHERE CompraID=@id", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", pago.CompraID);
                            object res = cmd.ExecuteScalar();
                            // res == null significa que la compra no existe.
                            if (res == null) throw new Exception("La compra no existe.");
                            saldoCompra = Convert.ToDecimal(res);
                        }

                        // Regla de negocio: no se puede pagar mas de lo que la compra debe.
                        if (pago.Monto > saldoCompra)
                            throw new Exception("El monto del pago (" + pago.Monto.ToString("N2") +
                                                ") no puede ser mayor que el saldo de la compra (" +
                                                saldoCompra.ToString("N2") + ").");

                        // 1) Insertar el pago y capturar su ID con SCOPE_IDENTITY().
                        int pagoID;
                        string sql = @"INSERT INTO dbo.Pagos (CompraID, ProveedorID, Fecha, Monto, FormaPago, Referencia, EmpleadoID)
                                       VALUES (@comp, @prov, @fecha, @monto, @forma, @ref, @emp);
                                       SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@comp", pago.CompraID);
                            cmd.Parameters.AddWithValue("@prov", pago.ProveedorID);
                            cmd.Parameters.AddWithValue("@fecha", pago.Fecha);
                            cmd.Parameters.AddWithValue("@monto", pago.Monto);
                            cmd.Parameters.AddWithValue("@forma", pago.FormaPago);
                            // Referencia y EmpleadoID son opcionales: null -> DBNull.Value (NULL en BD).
                            cmd.Parameters.AddWithValue("@ref", (object)pago.Referencia ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@emp", (object)pago.EmpleadoID ?? DBNull.Value);
                            pagoID = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2) Reducir el saldo de la compra y recalcular su estado. El CASE la
                        // deja en 'Pagada' cuando el saldo llega a 0 (o menos), o 'Pendiente'
                        // si todavia queda por pagar.
                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE dbo.Compras
                              SET Saldo = Saldo - @monto,
                                  Estado = CASE WHEN Saldo - @monto <= 0 THEN 'Pagada' ELSE 'Pendiente' END
                              WHERE CompraID = @comp;", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@monto", pago.Monto);
                            cmd.Parameters.AddWithValue("@comp", pago.CompraID);
                            cmd.ExecuteNonQuery();
                        }

                        // 3) Reducir el saldo (Cuentas por Pagar) del proveedor: al pagarle,
                        // nuestra deuda total con el baja en el mismo monto del pago.
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.Proveedores SET Saldo = Saldo - @monto WHERE ProveedorID = @prov", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@monto", pago.Monto);
                            cmd.Parameters.AddWithValue("@prov", pago.ProveedorID);
                            cmd.ExecuteNonQuery();
                        }

                        // Confirmamos los tres pasos como una sola operacion.
                        tran.Commit();
                        return pagoID;
                    }
                    catch
                    {
                        // Si algo falla, revertimos todo y relanzamos la excepcion.
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Lista el historial de pagos con nombre de proveedor y numero de documento.</summary>
        /// <returns>Historial de pagos, del mas reciente al mas antiguo.</returns>
        public List<Pago> Listar()
        {
            // JOIN con Compras y Proveedores para mostrar el documento y el nombre del
            // proveedor en pantalla en lugar de solo sus IDs.
            string sql = @"SELECT pg.PagoID, pg.CompraID, c.NumeroDocumento, pg.ProveedorID, pr.Nombre AS Proveedor,
                                  pg.Fecha, pg.Monto, pg.FormaPago, pg.Referencia, pg.EmpleadoID
                           FROM dbo.Pagos pg
                           INNER JOIN dbo.Compras c ON c.CompraID = pg.CompraID
                           INNER JOIN dbo.Proveedores pr ON pr.ProveedorID = pg.ProveedorID
                           ORDER BY pg.Fecha DESC, pg.PagoID DESC";
            DataTable dt = ConexionBD.EjecutarConsulta(sql);
            List<Pago> lista = new List<Pago>();
            // Mapeo en linea: cada fila del resultado se convierte en un objeto Pago.
            foreach (DataRow f in dt.Rows)
            {
                lista.Add(new Pago
                {
                    PagoID = Convert.ToInt32(f["PagoID"]),
                    CompraID = Convert.ToInt32(f["CompraID"]),
                    NumeroDocumento = f["NumeroDocumento"].ToString(),
                    ProveedorID = Convert.ToInt32(f["ProveedorID"]),
                    NombreProveedor = f["Proveedor"].ToString(),
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
