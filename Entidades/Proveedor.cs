using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa un proveedor. El campo Saldo acumula las Cuentas por Pagar
    /// (total pendiente que la empresa adeuda al proveedor).
    /// </summary>
    public class Proveedor
    {
        /// <summary>Identificador unico del proveedor (clave primaria en la BD).</summary>
        public int ProveedorID { get; set; }

        /// <summary>Nombre o razon social del proveedor.</summary>
        public string Nombre { get; set; }

        /// <summary>Registro Nacional del Contribuyente del proveedor (identificacion fiscal).</summary>
        public string RNC { get; set; }

        /// <summary>Telefono de contacto del proveedor.</summary>
        public string Telefono { get; set; }

        /// <summary>Correo electronico de contacto del proveedor.</summary>
        public string Email { get; set; }

        /// <summary>Direccion fisica del proveedor.</summary>
        public string Direccion { get; set; }

        /// <summary>
        /// Saldo pendiente que la empresa le debe al proveedor (Cuentas por Pagar),
        /// en la moneda del sistema. Sube al comprar a credito y baja al registrar un pago/abono.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>Indica si el proveedor esta activo. false = baja logica (se conserva el historial).</summary>
        public bool Activo { get; set; }

        /// <summary>
        /// Constructor por defecto. Deja el proveedor activo y sin deuda pendiente
        /// (Saldo en 0; 0m es un literal decimal).
        /// </summary>
        public Proveedor()
        {
            Activo = true;
            Saldo = 0m;
        }

        /// <summary>Muestra el nombre del proveedor al representar el objeto como texto (combos, listas, etc.).</summary>
        public override string ToString()
        {
            return Nombre;
        }
    }
}
