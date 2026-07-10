using System;
using System.Collections.Generic;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa una compra a proveedor (encabezado + detalle).
    /// Genera una Cuenta por Pagar mientras su Saldo sea mayor que cero.
    /// </summary>
    public class Compra
    {
        /// <summary>Identificador unico de la compra (clave primaria en la BD).</summary>
        public int CompraID { get; set; }

        /// <summary>Numero del documento de compra (factura del proveedor, conduce, etc.).</summary>
        public string NumeroDocumento { get; set; }

        /// <summary>Id del proveedor al que se le compro (clave foranea hacia Proveedor).</summary>
        public int ProveedorID { get; set; }

        /// <summary>Nombre del proveedor copiado para mostrar; evita cruzar la tabla Proveedor al listar.</summary>
        public string NombreProveedor { get; set; }   // solo para mostrar

        /// <summary>Fecha y hora de la compra.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>Suma de los importes de las lineas antes de impuestos, en la moneda del sistema.</summary>
        public decimal Subtotal { get; set; }

        /// <summary>Monto del impuesto (por ejemplo ITBIS/IVA) calculado sobre el Subtotal.</summary>
        public decimal Impuesto { get; set; }

        /// <summary>Total a pagar de la compra = Subtotal + Impuesto, en la moneda del sistema.</summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Saldo pendiente de pago de esta compra. Arranca igual al Total y baja con cada pago.
        /// Mientras sea mayor que cero, la compra forma parte de las Cuentas por Pagar.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Estado de la compra. Valores validos: "Pendiente" (con saldo por pagar),
        /// "Pagada" (saldo en cero) o "Anulada" (cancelada, no cuenta).
        /// </summary>
        public string Estado { get; set; }            // "Pendiente" / "Pagada" / "Anulada"

        /// <summary>Lista de lineas (renglones) que componen la compra. Ver DetalleCompra.</summary>
        public List<DetalleCompra> Detalles { get; set; }

        /// <summary>
        /// Constructor por defecto. Inicializa la compra con la fecha/hora actual,
        /// estado "Pendiente" y una lista de detalles vacia (para agregar renglones sin null).
        /// </summary>
        public Compra()
        {
            Fecha = DateTime.Now;
            Estado = "Pendiente";
            Detalles = new List<DetalleCompra>();
        }

        /// <summary>
        /// Monto ya pagado de la compra = Total - Saldo.
        /// Representa cuanto se le ha abonado al proveedor (calculado, no se almacena).
        /// </summary>
        public decimal MontoPagado
        {
            get { return Total - Saldo; }
        }
    }
}
