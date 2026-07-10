using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa un cobro (abono) recibido de un cliente sobre una factura.
    /// Reduce el Saldo de la factura y del cliente (Cuentas por Cobrar).
    /// </summary>
    public class Cobro
    {
        /// <summary>Identificador unico del cobro (clave primaria en la BD).</summary>
        public int CobroID { get; set; }

        /// <summary>Id de la factura sobre la que se aplica el cobro (clave foranea hacia Factura).</summary>
        public int FacturaID { get; set; }

        /// <summary>Numero de factura copiado para mostrar en pantalla y recibos.</summary>
        public string NumeroFactura { get; set; }   // solo para mostrar

        /// <summary>Id del cliente que realiza el cobro (clave foranea hacia Cliente).</summary>
        public int ClienteID { get; set; }

        /// <summary>Nombre del cliente copiado para mostrar en pantalla y recibos.</summary>
        public string NombreCliente { get; set; }   // solo para mostrar

        /// <summary>Fecha y hora en que se recibio el cobro.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Importe del abono recibido, en la moneda del sistema. Este monto es el que
        /// se descuenta del Saldo de la factura y del cliente.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>Forma de pago del cobro (por ejemplo "Efectivo", "Transferencia", "Tarjeta").</summary>
        public string FormaPago { get; set; }

        /// <summary>Referencia del pago (numero de transferencia, cheque, autorizacion, etc.). Opcional.</summary>
        public string Referencia { get; set; }

        /// <summary>
        /// Id del empleado que registro el cobro (clave foranea hacia Empleado).
        /// Es anulable (int?) porque puede registrarse sin asociar un empleado.
        /// </summary>
        public int? EmpleadoID { get; set; }

        /// <summary>
        /// Constructor por defecto. Usa la fecha/hora actual como fecha del cobro
        /// y "Efectivo" como forma de pago mas comun.
        /// </summary>
        public Cobro()
        {
            Fecha = DateTime.Now;
            FormaPago = "Efectivo";
        }
    }
}
