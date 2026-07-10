using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa un cliente. El campo Saldo acumula las Cuentas por Cobrar
    /// (total pendiente que el cliente adeuda a la empresa).
    /// </summary>
    public class Cliente
    {
        /// <summary>Identificador unico del cliente (clave primaria en la BD).</summary>
        public int ClienteID { get; set; }

        /// <summary>Nombre o razon social del cliente.</summary>
        public string Nombre { get; set; }

        /// <summary>Documento de identificacion (cedula, RNC/RUC, pasaporte, etc.).</summary>
        public string Identificacion { get; set; }

        /// <summary>Telefono de contacto del cliente.</summary>
        public string Telefono { get; set; }

        /// <summary>Correo electronico de contacto del cliente.</summary>
        public string Email { get; set; }

        /// <summary>Direccion fisica del cliente (para facturacion o envio).</summary>
        public string Direccion { get; set; }

        /// <summary>
        /// Limite de credito autorizado al cliente, expresado en la moneda del sistema.
        /// Es el maximo que puede llegar a deber en ventas a credito. 0 significa "sin credito".
        /// </summary>
        public decimal LimiteCredito { get; set; }

        /// <summary>
        /// Saldo pendiente que el cliente adeuda (Cuentas por Cobrar), en la moneda del sistema.
        /// Sube cuando se factura a credito y baja cuando el cliente hace un cobro/abono.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>Indica si el cliente esta activo. false = baja logica (se conserva el historial).</summary>
        public bool Activo { get; set; }

        /// <summary>
        /// Constructor por defecto. Deja el cliente activo y sin deuda ni credito:
        /// LimiteCredito y Saldo arrancan en 0 (0m = literal decimal).
        /// </summary>
        public Cliente()
        {
            Activo = true;
            LimiteCredito = 0m;
            Saldo = 0m;
        }

        /// <summary>
        /// Credito disponible = LimiteCredito - Saldo.
        /// Es cuanto mas puede comprar a credito el cliente antes de topar su limite.
        /// Un valor negativo indica que ya se paso del limite autorizado.
        /// </summary>
        public decimal CreditoDisponible
        {
            get { return LimiteCredito - Saldo; }
        }

        /// <summary>Muestra el nombre del cliente al representar el objeto como texto (combos, listas, etc.).</summary>
        public override string ToString()
        {
            return Nombre;
        }
    }
}
