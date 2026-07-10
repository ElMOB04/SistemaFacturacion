using System;
using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>
    /// Reglas de negocio para las Compras a proveedores (Cuentas por Pagar).
    /// Calcula subtotal/impuesto/total y siempre registra la compra a credito
    /// (Saldo = Total), quedando pendiente hasta que se paguen.
    /// </summary>
    public class CompraNegocio
    {
        // Acceso a datos de compras.
        private readonly CompraDAO _dao = new CompraDAO();

        /// <summary>Calcula subtotal, impuesto y total de la compra.</summary>
        /// <param name="compra">Compra con sus lineas cargadas; se le rellenan los totales.</param>
        public void CalcularTotales(Compra compra)
        {
            // 'subtotal' acumula el importe (cantidad * costo) de cada linea, sin ITBIS.
            decimal subtotal = 0m;
            foreach (DetalleCompra d in compra.Detalles)
                subtotal += d.Importe;

            compra.Subtotal = subtotal;
            // Mismo criterio que en ventas: ITBIS 18% redondeado a 2 decimales.
            compra.Impuesto = Math.Round(subtotal * Configuracion.TasaImpuesto, 2);
            compra.Total = compra.Subtotal + compra.Impuesto;
            // A diferencia de la factura, la compra SIEMPRE nace a credito: queda
            // debiendo el total completo al proveedor (cuenta por pagar).
            compra.Saldo = compra.Total;      // se registra como cuenta por pagar
            compra.Estado = "Pendiente";      // se salda despues con los pagos
        }

        /// <summary>Valida y registra una compra. Devuelve el ID generado.</summary>
        /// <param name="compra">Compra a crear (proveedor, documento y detalles).</param>
        public int CrearCompra(Compra compra)
        {
            // --- Validaciones de cabecera ---
            if (compra.ProveedorID <= 0)
                throw new NegocioException("Debe seleccionar un proveedor.");
            // El numero de documento (factura del proveedor) es obligatorio para trazabilidad.
            Validaciones.RequerirTexto(compra.NumeroDocumento, "Numero de documento");
            if (compra.Detalles == null || compra.Detalles.Count == 0)
                throw new NegocioException("La compra debe tener al menos un producto.");

            // --- Validaciones de cada linea ---
            foreach (DetalleCompra d in compra.Detalles)
            {
                if (d.Cantidad <= 0)
                    throw new NegocioException("La cantidad debe ser mayor que cero en todas las lineas.");
                // En compras no hay tope de stock (justo lo estamos ingresando),
                // pero el costo no puede ser negativo.
                if (d.CostoUnitario < 0)
                    throw new NegocioException("El costo unitario no puede ser negativo.");
            }

            // Calcula totales y deja la compra como pendiente de pago, luego persiste.
            CalcularTotales(compra);
            return _dao.Insertar(compra);
        }

        /// <summary>Lista compras, opcionalmente filtradas por estado.</summary>
        public List<Compra> Listar(string estado = null)
        {
            return _dao.Listar(estado);
        }

        /// <summary>Devuelve las compras aun con saldo de un proveedor (para pagarlas).</summary>
        public List<Compra> ListarPendientesPorProveedor(int proveedorID)
        {
            return _dao.ListarPendientesPorProveedor(proveedorID);
        }

        /// <summary>Trae una compra junto con todas sus lineas de detalle.</summary>
        public Compra ObtenerConDetalle(int compraID)
        {
            return _dao.ObtenerConDetalle(compraID);
        }
    }
}
