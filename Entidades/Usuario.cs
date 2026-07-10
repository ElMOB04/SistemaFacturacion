using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>
    /// Representa un usuario del sistema (control de acceso / login).
    /// La contrasena nunca se guarda en texto plano: se almacena su hash SHA-256.
    /// </summary>
    public class Usuario
    {
        /// <summary>Identificador unico del usuario (clave primaria, autonumerico en la BD).</summary>
        public int UsuarioID { get; set; }

        /// <summary>Nombre de acceso con el que el usuario inicia sesion. Debe ser unico.</summary>
        public string NombreUsuario { get; set; }

        /// <summary>
        /// Contrasena del usuario ya cifrada. Aqui NO viaja el texto plano:
        /// se guarda el hash SHA-256 calculado a partir de la clave que escribio el usuario.
        /// </summary>
        public string Contrasena { get; set; }      // hash SHA-256

        /// <summary>Nombre y apellidos reales de la persona, para mostrar en pantalla y reportes.</summary>
        public string NombreCompleto { get; set; }

        /// <summary>
        /// Rol de permisos del usuario. Valores validos: "Administrador" (acceso total)
        /// o "Usuario" (acceso limitado). Se usa para habilitar/ocultar opciones del menu.
        /// </summary>
        public string Rol { get; set; }             // "Administrador" o "Usuario"

        /// <summary>
        /// Indica si la cuenta esta habilitada. Si es false, el usuario existe pero
        /// no puede iniciar sesion (baja logica, sin borrar el registro).
        /// </summary>
        public bool Activo { get; set; }

        /// <summary>Fecha y hora en que se creo la cuenta de usuario.</summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Constructor por defecto. Deja el usuario con los valores mas seguros/comunes:
        /// rol "Usuario" (el menos privilegiado) y cuenta activa.
        /// </summary>
        public Usuario()
        {
            Rol = "Usuario";
            Activo = true;
        }

        /// <summary>
        /// Indica si el usuario tiene privilegios de administrador.
        /// Compara el campo Rol contra "Administrador" ignorando mayusculas/minusculas.
        /// </summary>
        public bool EsAdministrador
        {
            get { return string.Equals(Rol, "Administrador", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Devuelve el nombre de acceso como texto. Util para mostrar el objeto
        /// directamente en ComboBox, ListBox, mensajes, etc.
        /// </summary>
        public override string ToString()
        {
            return NombreUsuario;
        }
    }
}
