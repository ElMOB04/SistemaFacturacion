using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>Acceso a datos para la tabla Usuarios (CRUD + autenticacion).</summary>
    public class UsuarioDAO
    {
        /// <summary>Busca un usuario activo por su nombre de usuario. Devuelve null si no existe.</summary>
        /// <param name="nombreUsuario">Nombre de usuario (login) a buscar.</param>
        /// <returns>El usuario encontrado, o null si no existe.</returns>
        public Usuario ObtenerPorNombre(string nombreUsuario)
        {
            // Consulta que trae un usuario segun su login. El @nombre viaja como
            // parametro, nunca concatenado, para blindar contra inyeccion SQL.
            string sql = @"SELECT UsuarioID, NombreUsuario, Contrasena, NombreCompleto, Rol, Activo, FechaCreacion
                           FROM dbo.Usuarios WHERE NombreUsuario = @nombre";
            DataTable dt = ConexionBD.EjecutarConsulta(sql, ConexionBD.Parametro("@nombre", nombreUsuario));
            // Sin filas significa que no hay coincidencia: devolvemos null.
            if (dt.Rows.Count == 0) return null;
            // Convertimos la unica fila obtenida en un objeto Usuario.
            return Mapear(dt.Rows[0]);
        }

        /// <summary>Devuelve todos los usuarios registrados.</summary>
        /// <returns>Lista con todos los usuarios ordenados por nombre.</returns>
        public List<Usuario> ListarTodos()
        {
            // SELECT sin filtro: trae todos los usuarios ordenados alfabeticamente.
            string sql = @"SELECT UsuarioID, NombreUsuario, Contrasena, NombreCompleto, Rol, Activo, FechaCreacion
                           FROM dbo.Usuarios ORDER BY NombreUsuario";
            DataTable dt = ConexionBD.EjecutarConsulta(sql);
            // 'lista' acumula el resultado ya convertido a objetos del dominio.
            List<Usuario> lista = new List<Usuario>();
            // Recorremos cada fila de la tabla y la mapeamos a un Usuario.
            foreach (DataRow fila in dt.Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        /// <summary>Inserta un usuario nuevo y devuelve el ID generado.</summary>
        /// <param name="u">Usuario con los datos a guardar.</param>
        /// <returns>El ID autonumerico asignado por la base de datos.</returns>
        public int Insertar(Usuario u)
        {
            // INSERT del nuevo usuario. La FechaCreacion se pone con GETDATE() (fecha
            // del servidor). Tras insertar, SCOPE_IDENTITY() devuelve el ID recien
            // generado y lo casteamos a INT para retornarlo.
            string sql = @"INSERT INTO dbo.Usuarios (NombreUsuario, Contrasena, NombreCompleto, Rol, Activo, FechaCreacion)
                           VALUES (@nombre, @clave, @completo, @rol, @activo, GETDATE());
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object id = ConexionBD.EjecutarEscalar(sql,
                ConexionBD.Parametro("@nombre", u.NombreUsuario),
                ConexionBD.Parametro("@clave", u.Contrasena),
                ConexionBD.Parametro("@completo", u.NombreCompleto),
                ConexionBD.Parametro("@rol", u.Rol),
                ConexionBD.Parametro("@activo", u.Activo));
            // EjecutarEscalar retorna un object; lo convertimos al int que espera el llamador.
            return Convert.ToInt32(id);
        }

        /// <summary>Actualiza los datos de un usuario. Si la contrasena viene vacia, no se modifica.</summary>
        /// <param name="u">Usuario con los datos actualizados. Si Contrasena esta vacia, se conserva la anterior.</param>
        public void Actualizar(Usuario u)
        {
            // 'cambiarClave' decide si tocamos o no la contrasena. Si el usuario dejo
            // el campo en blanco, no queremos sobrescribir la clave existente.
            bool cambiarClave = !string.IsNullOrEmpty(u.Contrasena);
            // Armamos el UPDATE de forma condicional: solo agregamos "Contrasena = @clave"
            // cuando de verdad hay una clave nueva. El resto de campos siempre se actualiza.
            string sql = @"UPDATE dbo.Usuarios SET
                              NombreUsuario = @nombre,
                              NombreCompleto = @completo,
                              Rol = @rol,
                              Activo = @activo"
                        + (cambiarClave ? ", Contrasena = @clave" : "")
                        + " WHERE UsuarioID = @id";

            // Construimos la lista de parametros de forma dinamica porque la cantidad
            // varia: '@clave' solo existe si decidimos cambiar la contrasena.
            List<SqlParameter> ps = new List<SqlParameter>
            {
                ConexionBD.Parametro("@nombre", u.NombreUsuario),
                ConexionBD.Parametro("@completo", u.NombreCompleto),
                ConexionBD.Parametro("@rol", u.Rol),
                ConexionBD.Parametro("@activo", u.Activo),
                ConexionBD.Parametro("@id", u.UsuarioID)
            };
            // Agregamos el parametro de la clave solo si el SQL lo incluye,
            // para que numero de @ y numero de parametros siempre coincidan.
            if (cambiarClave)
                ps.Add(ConexionBD.Parametro("@clave", u.Contrasena));

            ConexionBD.EjecutarComando(sql, ps.ToArray());
        }

        /// <summary>Elimina un usuario por su ID.</summary>
        /// <param name="usuarioID">Identificador del usuario a borrar.</param>
        public void Eliminar(int usuarioID)
        {
            // DELETE directo por clave primaria; el @id parametrizado evita inyeccion.
            ConexionBD.EjecutarComando("DELETE FROM dbo.Usuarios WHERE UsuarioID = @id",
                ConexionBD.Parametro("@id", usuarioID));
        }

        /// <summary>Convierte una fila de datos en un objeto Usuario.</summary>
        /// <param name="f">Fila (DataRow) proveniente de la consulta.</param>
        /// <returns>Objeto Usuario con los valores de la fila ya tipados.</returns>
        private Usuario Mapear(DataRow f)
        {
            // Aqui traducimos cada columna del DataRow a la propiedad correspondiente,
            // convirtiendo el tipo (Convert.ToXxx) porque las celdas llegan como object.
            return new Usuario
            {
                UsuarioID = Convert.ToInt32(f["UsuarioID"]),
                NombreUsuario = f["NombreUsuario"].ToString(),
                Contrasena = f["Contrasena"].ToString(),
                // NombreCompleto puede venir NULL en la BD; si es asi usamos cadena vacia
                // para no arrastrar un null que rompa la interfaz de usuario.
                NombreCompleto = f["NombreCompleto"] == DBNull.Value ? "" : f["NombreCompleto"].ToString(),
                Rol = f["Rol"].ToString(),
                Activo = Convert.ToBoolean(f["Activo"]),
                FechaCreacion = Convert.ToDateTime(f["FechaCreacion"])
            };
        }
    }
}
