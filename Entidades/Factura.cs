using System;
using System.Collections.Generic;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa una factura de venta (encabezado + detalle).
    /// Genera una Cuenta por Cobrar mientras su Saldo sea mayor que cero.
    /// </summary>
    public class Factura
    {
        /// <summary>Identificador unico de la factura (clave primaria en la BD).</summary>
        public int FacturaID { get; set; }

        /// <summary>Numero de factura visible para el usuario (secuencial o segun formato de la empresa).</summary>
        public string NumeroFactura { get; set; }

        /// <summary>Id del cliente al que se le facturo (clave foranea hacia Cliente).</summary>
        public int ClienteID { get; set; }

        /// <summary>Nombre del cliente copiado para mostrar; evita cruzar la tabla Cliente al listar.</summary>
        public string NombreCliente { get; set; }    // solo para mostrar

        /// <summary>
        /// Id del empleado/vendedor que emitio la factura (clave foranea hacia Empleado).
        /// Es anulable (int?) porque la factura puede registrarse sin asociar un empleado.
        /// </summary>
        public int? EmpleadoID { get; set; }

        /// <summary>Nombre del empleado copiado para mostrar en pantalla y reportes.</summary>
        public string NombreEmpleado { get; set; }   // solo para mostrar

        /// <summary>Fecha y hora de emision de la factura.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>Suma de los importes de las lineas antes de impuestos, en la moneda del sistema.</summary>
        public decimal Subtotal { get; set; }

        /// <summary>Monto del impuesto (por ejemplo ITBIS/IVA) calculado sobre el Subtotal.</summary>
        public decimal Impuesto { get; set; }

        /// <summary>Total a pagar de la factura = Subtotal + Impuesto, en la moneda del sistema.</summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Saldo pendiente de cobro de esta factura. Arranca igual al Total y baja con cada cobro.
        /// Mientras sea mayor que cero, la factura forma parte de las Cuentas por Cobrar.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Forma de pago de la venta. Valores validos: "Contado" (se paga de inmediato)
        /// o "Credito" (queda pendiente y genera cuenta por cobrar).
        /// </summary>
        public string TipoPago { get; set; }         // "Contado" o "Credito"

        /// <summary>
        /// Estado de la factura. Valores validos: "Pendiente" (con saldo por cobrar),
        /// "Pagada" (saldo en cero) o "Anulada" (cancelada, no cuenta).
        /// </summary>
        public string Estado { get; set; }           // "Pendiente" / "Pagada" / "Anulada"

        /// <summary>Lista de lineas (renglones) que componen la factura. Ver DetalleFactura.</summary>
        public List<DetalleFactura> Detalles { get; set; }

        /// <summary>
        /// Constructor por defecto. Inicializa la factura con la fecha/hora actual,
        /// tipo de pago "Contado", estado "Pendiente" y una lista de detalles vacia
        /// (asi se puede empezar a agregar renglones sin riesgo de null).
        /// </summary>
        public Factura()
        {
            Fecha = DateTime.Now;
            TipoPago = "Contado";
            Estado = "Pendiente";
            Detalles = new List<DetalleFactura>();
        }

        /// <summary>
        /// Monto ya cobrado de la factura = Total - Saldo.
        /// Representa cuanto ha pagado el cliente hasta el momento (calculado, no se almacena).
        /// </summary>
        public decimal MontoCobrado
        {
            get { return Total - Saldo; }
        }
    }
}
