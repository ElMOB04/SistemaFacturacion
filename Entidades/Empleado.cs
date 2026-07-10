using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>Representa un empleado de la empresa.</summary>
    public class Empleado
    {
        /// <summary>Identificador unico del empleado (clave primaria en la BD).</summary>
        public int EmpleadoID { get; set; }

        /// <summary>Nombre(s) de pila del empleado.</summary>
        public string Nombre { get; set; }

        /// <summary>Apellido(s) del empleado.</summary>
        public string Apellido { get; set; }

        /// <summary>Numero de cedula / documento de identidad. Deberia ser unico por empleado.</summary>
        public string Cedula { get; set; }

        /// <summary>Puesto o cargo que ocupa (por ejemplo "Cajero", "Vendedor", "Contador").</summary>
        public string Cargo { get; set; }

        /// <summary>Telefono de contacto del empleado.</summary>
        public string Telefono { get; set; }

        /// <summary>Correo electronico de contacto del empleado.</summary>
        public string Email { get; set; }

        /// <summary>
        /// Fecha en que el empleado ingreso a la empresa. Es anulable (DateTime?)
        /// porque puede desconocerse o quedar sin capturar al crear el registro.
        /// </summary>
        public DateTime? FechaIngreso { get; set; }

        /// <summary>
        /// Indica si el empleado sigue vigente. Si es false representa una baja
        /// logica (el registro se conserva por historial, pero ya no esta activo).
        /// </summary>
        public bool Activo { get; set; }

        /// <summary>
        /// Constructor por defecto. Marca al empleado como activo y usa la fecha de
        /// hoy como fecha de ingreso tentativa (el usuario puede cambiarla luego).
        /// </summary>
        public Empleado()
        {
            Activo = true;
            FechaIngreso = DateTime.Today;
        }

        /// <summary>
        /// Nombre completo del empleado (nombre + apellido).
        /// Concatena ambos campos con un espacio y aplica Trim para que, si falta
        /// alguno, no queden espacios sobrantes al inicio o al final.
        /// </summary>
        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }

        /// <summary>Muestra el nombre completo al representar el objeto como texto (listas, combos, etc.).</summary>
        public override string ToString()
        {
            return NombreCompleto;
        }
    }
}
