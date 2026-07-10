using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa un pago (abono) realizado a un proveedor sobre una compra.
    /// Reduce el Saldo de la compra y del proveedor (Cuentas por Pagar).
    /// </summary>
    public class Pago
    {
        /// <summary>Identificador unico del pago (clave primaria en la BD).</summary>
        public int PagoID { get; set; }

        /// <summary>Id de la compra sobre la que se aplica el pago (clave foranea hacia Compra).</summary>
        public int CompraID { get; set; }

        /// <summary>Numero de documento de la compra copiado para mostrar en pantalla y comprobantes.</summary>
        public string NumeroDocumento { get; set; }   // solo para mostrar

        /// <summary>Id del proveedor al que se le paga (clave foranea hacia Proveedor).</summary>
        public int ProveedorID { get; set; }

        /// <summary>Nombre del proveedor copiado para mostrar en pantalla y comprobantes.</summary>
        public string NombreProveedor { get; set; }   // solo para mostrar

        /// <summary>Fecha y hora en que se realizo el pago.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Importe del abono pagado al proveedor, en la moneda del sistema. Este monto es
        /// el que se descuenta del Saldo de la compra y del proveedor.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>Forma de pago (por ejemplo "Efectivo", "Transferencia", "Cheque").</summary>
        public string FormaPago { get; set; }

        /// <summary>Referencia del pago (numero de transferencia, cheque, autorizacion, etc.). Opcional.</summary>
        public string Referencia { get; set; }

        /// <summary>
        /// Id del empleado que registro el pago (clave foranea hacia Empleado).
        /// Es anulable (int?) porque puede registrarse sin asociar un empleado.
        /// </summary>
        public int? EmpleadoID { get; set; }

        /// <summary>
        /// Constructor por defecto. Usa la fecha/hora actual como fecha del pago
        /// y "Efectivo" como forma de pago mas comun.
        /// </summary>
        public Pago()
        {
            Fecha = DateTime.Now;
            FormaPago = "Efectivo";
        }
    }
}
