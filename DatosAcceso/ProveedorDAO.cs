using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>Acceso a datos para la tabla Proveedores (CRUD + saldo).</summary>
    public class ProveedorDAO
    {
        // Columnas comunes a los SELECT de proveedores. Incluye Saldo, base de las
        // Cuentas por Pagar (lo que la empresa le debe al proveedor).
        private const string CamposBase =
            "ProveedorID, Nombre, RNC, Telefono, Email, Direccion, Saldo, Activo";

        /// <summary>Lista proveedores, opcionalmente filtrando por texto y solo activos.</summary>
        /// <param name="soloActivos">Si es true, solo devuelve proveedores con Activo = 1.</param>
        /// <param name="filtro">Texto a buscar en Nombre o RNC (busqueda parcial).</param>
        /// <returns>Lista de proveedores que cumplen los filtros, ordenada por nombre.</returns>
        public List<Proveedor> Listar(bool soloActivos = false, string filtro = null)
        {
            // "WHERE 1=1" facilita concatenar condiciones opcionales con "AND".
            string sql = "SELECT " + CamposBase + " FROM dbo.Proveedores WHERE 1=1";
            List<SqlParameter> ps = new List<SqlParameter>();
            if (soloActivos) sql += " AND Activo = 1";
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // Busqueda parcial parametrizada por Nombre o RNC.
                sql += " AND (Nombre LIKE @f OR RNC LIKE @f)";
                ps.Add(ConexionBD.Parametro("@f", "%" + filtro.Trim() + "%"));
            }
            sql += " ORDER BY Nombre";

            DataTable dt = ConexionBD.EjecutarConsulta(sql, ps.ToArray());
            List<Proveedor> lista = new List<Proveedor>();
            foreach (DataRow f in dt.Rows)
                lista.Add(Mapear(f));
            return lista;
        }

        /// <summary>Busca un proveedor por su ID. Devuelve null si no existe.</summary>
        /// <param name="id">Identificador del proveedor.</param>
        public Proveedor ObtenerPorId(int id)
        {
            DataTable dt = ConexionBD.EjecutarConsulta(
                "SELECT " + CamposBase + " FROM dbo.Proveedores WHERE ProveedorID=@id",
                ConexionBD.Parametro("@id", id));
            return dt.Rows.Count == 0 ? null : Mapear(dt.Rows[0]);
        }

        /// <summary>Inserta un proveedor nuevo (con su saldo inicial) y devuelve el ID generado.</summary>
        /// <param name="p">Datos del proveedor a registrar.</param>
        /// <returns>ID autonumerico asignado por la base de datos.</returns>
        public int Insertar(Proveedor p)
        {
            // INSERT + SCOPE_IDENTITY() para obtener el ID del proveedor recien creado.
            string sql = @"INSERT INTO dbo.Proveedores (Nombre, RNC, Telefono, Email, Direccion, Saldo, Activo)
                           VALUES (@nom, @rnc, @tel, @email, @dir, @saldo, @act);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object id = ConexionBD.EjecutarEscalar(sql,
                ConexionBD.Parametro("@nom", p.Nombre),
                ConexionBD.Parametro("@rnc", p.RNC),
                ConexionBD.Parametro("@tel", p.Telefono),
                ConexionBD.Parametro("@email", p.Email),
                ConexionBD.Parametro("@dir", p.Direccion),
                ConexionBD.Parametro("@saldo", p.Saldo),
                ConexionBD.Parametro("@act", p.Activo));
            return Convert.ToInt32(id);
        }

        /// <summary>Actualiza los datos generales del proveedor.</summary>
        /// <param name="p">Proveedor con los valores nuevos (identificado por ProveedorID).</param>
        public void Actualizar(Proveedor p)
        {
            // Igual que en clientes, este UPDATE NO modifica el Saldo: ese valor solo
            // cambia con las compras y los pagos, nunca por edicion manual del proveedor.
            string sql = @"UPDATE dbo.Proveedores SET
                              Nombre=@nom, RNC=@rnc, Telefono=@tel, Email=@email,
                              Direccion=@dir, Activo=@act
                           WHERE ProveedorID=@id";
            ConexionBD.EjecutarComando(sql,
                ConexionBD.Parametro("@nom", p.Nombre),
                ConexionBD.Parametro("@rnc", p.RNC),
                ConexionBD.Parametro("@tel", p.Telefono),
                ConexionBD.Parametro("@email", p.Email),
                ConexionBD.Parametro("@dir", p.Direccion),
                ConexionBD.Parametro("@act", p.Activo),
                ConexionBD.Parametro("@id", p.ProveedorID));
        }

        /// <summary>Elimina un proveedor por su ID.</summary>
        /// <param name="id">Identificador del proveedor a borrar.</param>
        public void Eliminar(int id)
        {
            ConexionBD.EjecutarComando("DELETE FROM dbo.Proveedores WHERE ProveedorID=@id",
                ConexionBD.Parametro("@id", id));
        }

        /// <summary>Convierte una fila de datos en un objeto Proveedor.</summary>
        /// <param name="f">Fila (DataRow) devuelta por la consulta.</param>
        /// <returns>Proveedor con las columnas ya convertidas a sus tipos.</returns>
        private Proveedor Mapear(DataRow f)
        {
            // Fila -> objeto: los textos opcionales se blindan contra NULL con "" y el Saldo se pasa a decimal.
            return new Proveedor
            {
                ProveedorID = Convert.ToInt32(f["ProveedorID"]),
                Nombre = f["Nombre"].ToString(),
                RNC = f["RNC"] == DBNull.Value ? "" : f["RNC"].ToString(),
                Telefono = f["Telefono"] == DBNull.Value ? "" : f["Telefono"].ToString(),
                Email = f["Email"] == DBNull.Value ? "" : f["Email"].ToString(),
                Direccion = f["Direccion"] == DBNull.Value ? "" : f["Direccion"].ToString(),
                Saldo = Convert.ToDecimal(f["Saldo"]),
                Activo = Convert.ToBoolean(f["Activo"])
            };
        }
    }
}
