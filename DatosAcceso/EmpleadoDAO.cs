using System;
using System.Collections.Generic;
using System.Data;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>Acceso a datos para la tabla Empleados (CRUD).</summary>
    public class EmpleadoDAO
    {
        // Lista de columnas reutilizada en todas las consultas SELECT. Centralizarla
        // aqui evita repetir los nombres y mantiene consistencia entre metodos.
        private const string CamposBase =
            "EmpleadoID, Nombre, Apellido, Cedula, Cargo, Telefono, Email, FechaIngreso, Activo";

        /// <summary>Lista empleados. Si soloActivos es true, solo devuelve los activos.</summary>
        /// <param name="soloActivos">Cuando es true agrega el filtro Activo = 1.</param>
        /// <returns>Lista de empleados ordenada por nombre y apellido.</returns>
        public List<Empleado> Listar(bool soloActivos = false)
        {
            // Armamos el SELECT base y, segun el parametro, agregamos el filtro de activos.
            string sql = "SELECT " + CamposBase + " FROM dbo.Empleados";
            if (soloActivos) sql += " WHERE Activo = 1";
            sql += " ORDER BY Nombre, Apellido";

            DataTable dt = ConexionBD.EjecutarConsulta(sql);
            List<Empleado> lista = new List<Empleado>();
            // Convertimos cada fila devuelta en un objeto Empleado.
            foreach (DataRow f in dt.Rows)
                lista.Add(Mapear(f));
            return lista;
        }

        /// <summary>Busca un empleado por su ID. Devuelve null si no existe.</summary>
        /// <param name="id">Identificador del empleado.</param>
        public Empleado ObtenerPorId(int id)
        {
            // Consulta puntual por clave primaria con parametro @id.
            DataTable dt = ConexionBD.EjecutarConsulta(
                "SELECT " + CamposBase + " FROM dbo.Empleados WHERE EmpleadoID = @id",
                ConexionBD.Parametro("@id", id));
            // Si no hubo filas devolvemos null; si hubo, mapeamos la primera.
            return dt.Rows.Count == 0 ? null : Mapear(dt.Rows[0]);
        }

        /// <summary>Inserta un empleado nuevo y devuelve el ID generado.</summary>
        /// <param name="e">Datos del empleado a registrar.</param>
        /// <returns>ID autonumerico asignado por la base de datos.</returns>
        public int Insertar(Empleado e)
        {
            // INSERT + SCOPE_IDENTITY() para recuperar el ID recien creado en un solo viaje.
            string sql = @"INSERT INTO dbo.Empleados (Nombre, Apellido, Cedula, Cargo, Telefono, Email, FechaIngreso, Activo)
                           VALUES (@nom, @ape, @ced, @car, @tel, @email, @fecha, @act);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object id = ConexionBD.EjecutarEscalar(sql,
                ConexionBD.Parametro("@nom", e.Nombre),
                ConexionBD.Parametro("@ape", e.Apellido),
                ConexionBD.Parametro("@ced", e.Cedula),
                ConexionBD.Parametro("@car", e.Cargo),
                ConexionBD.Parametro("@tel", e.Telefono),
                ConexionBD.Parametro("@email", e.Email),
                // FechaIngreso es opcional (DateTime?): si es null enviamos DBNull.Value
                // para guardar NULL en la columna en vez de una fecha falsa.
                ConexionBD.Parametro("@fecha", (object)e.FechaIngreso ?? DBNull.Value),
                ConexionBD.Parametro("@act", e.Activo));
            return Convert.ToInt32(id);
        }

        /// <summary>Actualiza todos los datos de un empleado existente.</summary>
        /// <param name="e">Empleado con los valores nuevos (identificado por su EmpleadoID).</param>
        public void Actualizar(Empleado e)
        {
            // UPDATE que sobreescribe todos los campos del empleado localizado por @id.
            string sql = @"UPDATE dbo.Empleados SET
                              Nombre=@nom, Apellido=@ape, Cedula=@ced, Cargo=@car,
                              Telefono=@tel, Email=@email, FechaIngreso=@fecha, Activo=@act
                           WHERE EmpleadoID=@id";
            ConexionBD.EjecutarComando(sql,
                ConexionBD.Parametro("@nom", e.Nombre),
                ConexionBD.Parametro("@ape", e.Apellido),
                ConexionBD.Parametro("@ced", e.Cedula),
                ConexionBD.Parametro("@car", e.Cargo),
                ConexionBD.Parametro("@tel", e.Telefono),
                ConexionBD.Parametro("@email", e.Email),
                ConexionBD.Parametro("@fecha", (object)e.FechaIngreso ?? DBNull.Value),
                ConexionBD.Parametro("@act", e.Activo),
                ConexionBD.Parametro("@id", e.EmpleadoID));
        }

        /// <summary>Elimina un empleado por su ID.</summary>
        /// <param name="id">Identificador del empleado a borrar.</param>
        public void Eliminar(int id)
        {
            ConexionBD.EjecutarComando("DELETE FROM dbo.Empleados WHERE EmpleadoID=@id",
                ConexionBD.Parametro("@id", id));
        }

        /// <summary>Convierte una fila de datos en un objeto Empleado.</summary>
        /// <param name="f">Fila (DataRow) devuelta por la consulta.</param>
        /// <returns>Empleado con las columnas ya convertidas a sus tipos.</returns>
        private Empleado Mapear(DataRow f)
        {
            // Pasamos columna por columna del DataRow al objeto. Para los campos que la
            // BD permite NULL comprobamos DBNull.Value y sustituimos por "" (o null en
            // el caso de la fecha), evitando arrastrar valores nulos a la capa superior.
            return new Empleado
            {
                EmpleadoID = Convert.ToInt32(f["EmpleadoID"]),
                Nombre = f["Nombre"].ToString(),
                Apellido = f["Apellido"].ToString(),
                Cedula = f["Cedula"] == DBNull.Value ? "" : f["Cedula"].ToString(),
                Cargo = f["Cargo"] == DBNull.Value ? "" : f["Cargo"].ToString(),
                Telefono = f["Telefono"] == DBNull.Value ? "" : f["Telefono"].ToString(),
                Email = f["Email"] == DBNull.Value ? "" : f["Email"].ToString(),
                // FechaIngreso es DateTime? (admite ausencia): si la celda es NULL guardamos null.
                FechaIngreso = f["FechaIngreso"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(f["FechaIngreso"]),
                Activo = Convert.ToBoolean(f["Activo"])
            };
        }
    }
}
