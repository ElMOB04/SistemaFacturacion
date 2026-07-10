using System;
using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para Usuarios (autenticacion y CRUD).</summary>
    public class UsuarioNegocio
    {
        // Puerta de acceso a la base de datos para la tabla de usuarios.
        // 'readonly' = se asigna una sola vez y no se reemplaza durante la vida del objeto.
        private readonly UsuarioDAO _dao = new UsuarioDAO();

        /// <summary>
        /// Autentica un usuario. Devuelve el objeto Usuario si las credenciales
        /// son correctas y esta activo; lanza NegocioException en caso contrario.
        /// </summary>
        /// <param name="nombreUsuario">Nombre de usuario tal como se escribio en el login.</param>
        /// <param name="contrasena">Contrasena en texto plano ingresada por el usuario.</param>
        /// <returns>El usuario autenticado, listo para guardarse en la Sesion.</returns>
        public Usuario Autenticar(string nombreUsuario, string contrasena)
        {
            // 1) Ambos campos son obligatorios; si faltan ni siquiera vamos a la BD.
            Validaciones.RequerirTexto(nombreUsuario, "Usuario");
            Validaciones.RequerirTexto(contrasena, "Contrasena");

            // 2) Buscamos el usuario por nombre (Trim quita espacios accidentales).
            //    'u' sera null si no existe ningun usuario con ese nombre.
            Usuario u = _dao.ObtenerPorNombre(nombreUsuario.Trim());
            if (u == null)
                // Mensaje generico a proposito: no revelamos si fallo el usuario
                // o la contrasena, para no ayudar a un atacante.
                throw new NegocioException("Usuario o contrasena incorrectos.");
            if (!u.Activo)
                throw new NegocioException("El usuario esta inactivo. Contacte al administrador.");

            // 3) Nunca comparamos contrasenas en claro: calculamos el hash de lo
            //    que se escribio y lo comparamos con el hash guardado en la BD.
            string hash = Seguridad.CalcularHashSHA256(contrasena);
            //    OrdinalIgnoreCase porque el hash es hexadecimal y no distingue mayus/minus.
            if (!string.Equals(hash, u.Contrasena, StringComparison.OrdinalIgnoreCase))
                throw new NegocioException("Usuario o contrasena incorrectos.");

            // 4) Credenciales validas: devolvemos el usuario al llamador.
            return u;
        }

        /// <summary>Devuelve todos los usuarios registrados (para el mantenimiento).</summary>
        public List<Usuario> Listar()
        {
            return _dao.ListarTodos();
        }

        /// <summary>Crea un usuario nuevo. La contrasena se guarda como hash.</summary>
        /// <param name="u">Datos del usuario a crear (el campo Contrasena se rellena aqui).</param>
        /// <param name="contrasenaPlana">Contrasena en claro escrita en el formulario.</param>
        /// <returns>El ID generado para el nuevo usuario.</returns>
        public int Crear(Usuario u, string contrasenaPlana)
        {
            // Reglas minimas: nombre y contrasena obligatorios, y largo minimo.
            Validaciones.RequerirTexto(u.NombreUsuario, "Nombre de usuario");
            Validaciones.RequerirTexto(contrasenaPlana, "Contrasena");
            if (contrasenaPlana.Length < 4)
                throw new NegocioException("La contrasena debe tener al menos 4 caracteres.");

            // Evitamos nombres de usuario duplicados consultando primero la BD.
            if (_dao.ObtenerPorNombre(u.NombreUsuario.Trim()) != null)
                throw new NegocioException("Ya existe un usuario con ese nombre.");

            u.NombreUsuario = u.NombreUsuario.Trim();
            // Guardamos SIEMPRE el hash, nunca la contrasena original.
            u.Contrasena = Seguridad.CalcularHashSHA256(contrasenaPlana);
            return _dao.Insertar(u);
        }

        /// <summary>
        /// Actualiza un usuario. Si contrasenaPlana viene vacia, no se cambia la
        /// contrasena existente.
        /// </summary>
        /// <param name="u">Usuario con los datos ya modificados.</param>
        /// <param name="contrasenaPlana">Nueva contrasena; si viene vacia se conserva la actual.</param>
        public void Actualizar(Usuario u, string contrasenaPlana)
        {
            Validaciones.RequerirTexto(u.NombreUsuario, "Nombre de usuario");
            u.NombreUsuario = u.NombreUsuario.Trim();

            // Caso 1: el usuario escribio una contrasena nueva -> la validamos y
            // la volvemos a hashear para guardarla.
            if (!string.IsNullOrEmpty(contrasenaPlana))
            {
                if (contrasenaPlana.Length < 4)
                    throw new NegocioException("La contrasena debe tener al menos 4 caracteres.");
                u.Contrasena = Seguridad.CalcularHashSHA256(contrasenaPlana);
            }
            else
            {
                // Caso 2: dejo el campo en blanco -> ponemos Contrasena en null y,
                // por convencion, el DAO interpreta null como "no tocar la contrasena".
                u.Contrasena = null; // el DAO no modificara la contrasena
            }
            _dao.Actualizar(u);
        }

        /// <summary>Elimina un usuario por su identificador.</summary>
        /// <param name="usuarioID">ID del usuario a borrar.</param>
        public void Eliminar(int usuarioID)
        {
            _dao.Eliminar(usuarioID);
        }
    }
}
