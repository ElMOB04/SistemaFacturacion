using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>
    /// Guarda el usuario autenticado durante la sesion de la aplicacion,
    /// para mostrarlo en pantalla y controlar el acceso a modulos restringidos.
    /// </summary>
    public static class Sesion
    {
        /// <summary>
        /// Usuario que inicio sesion. Es estatico para que este disponible desde
        /// cualquier formulario mientras la aplicacion siga abierta. Es null
        /// mientras nadie se ha autenticado.
        /// </summary>
        public static Usuario UsuarioActual { get; set; }

        /// <summary>
        /// Indica si el usuario en sesion tiene permisos de administrador.
        /// Comprueba primero que exista sesion (UsuarioActual != null) para no
        /// provocar una excepcion de referencia nula.
        /// </summary>
        public static bool EsAdministrador
        {
            get { return UsuarioActual != null && UsuarioActual.EsAdministrador; }
        }
    }
}
