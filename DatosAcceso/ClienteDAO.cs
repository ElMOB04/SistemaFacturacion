using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>Acceso a datos para la tabla Clientes (CRUD + saldo).</summary>
    public class ClienteDAO
    {
        // Columnas comunes a todos los SELECT de clientes. Incluye LimiteCredito y Saldo,
        // datos clave para las Cuentas por Cobrar.
        private const string CamposBase =
            "ClienteID, Nombre, Identificacion, Telefono, Email, Direccion, LimiteCredito, Saldo, Activo";

        /// <summary>Lista clientes, opcionalmente filtrando por texto y solo activos.</summary>
        /// <param name="soloActivos">Si es true, solo devuelve clientes con Activo = 1.</param>
        /// <param name="filtro">Texto a buscar en Nombre o Identificacion (busqueda parcial).</param>
        /// <returns>Lista de clientes que cumplen los filtros, ordenada por nombre.</returns>
        public List<Cliente> Listar(bool soloActivos = false, string filtro = null)
        {
            // "WHERE 1=1" es un truco comun: siempre verdadero, permite ir agregando
            // condiciones con "AND ..." sin preocuparnos de si es la primera o no.
            string sql = "SELECT " + CamposBase + " FROM dbo.Clientes WHERE 1=1";
            // 'ps' acumula los parametros que realmente se usen segun los filtros activos.
            List<SqlParameter> ps = new List<SqlParameter>();
            if (soloActivos) sql += " AND Activo = 1";
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // Busqueda parcial con LIKE. El mismo @f sirve para Nombre e Identificacion,
                // y va parametrizado (con los % agregados en el valor) para evitar inyeccion.
                sql += " AND (Nombre LIKE @f OR Identificacion LIKE @f)";
                ps.Add(ConexionBD.Parametro("@f", "%" + filtro.Trim() + "%"));
            }
            sql += " ORDER BY Nombre";

            DataTable dt = ConexionBD.EjecutarConsulta(sql, ps.ToArray());
            List<Cliente> lista = new List<Cliente>();
            foreach (DataRow f in dt.Rows)
                lista.Add(Mapear(f));
            return lista;
        }

        /// <summary>Busca un cliente por su ID. Devuelve null si no existe.</summary>
        /// <param name="id">Identificador del cliente.</param>
        public Cliente ObtenerPorId(int id)
        {
            DataTable dt = ConexionBD.EjecutarConsulta(
                "SELECT " + CamposBase + " FROM dbo.Clientes WHERE ClienteID=@id",
                ConexionBD.Parametro("@id", id));
            return dt.Rows.Count == 0 ? null : Mapear(dt.Rows[0]);
        }

        /// <summary>Inserta un cliente nuevo (con su saldo inicial) y devuelve el ID generado.</summary>
        /// <param name="c">Datos del cliente a registrar.</param>
        /// <returns>ID autonumerico asignado por la base de datos.</returns>
        public int Insertar(Cliente c)
        {
            // INSERT con SCOPE_IDENTITY() para recuperar el ID del cliente recien creado.
            // Nota: aqui si se guarda el Saldo inicial (a diferencia del UPDATE).
            string sql = @"INSERT INTO dbo.Clientes (Nombre, Identificacion, Telefono, Email, Direccion, LimiteCredito, Saldo, Activo)
                           VALUES (@nom, @ident, @tel, @email, @dir, @limite, @saldo, @act);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object id = ConexionBD.EjecutarEscalar(sql,
                ConexionBD.Parametro("@nom", c.Nombre),
                ConexionBD.Parametro("@ident", c.Identificacion),
                ConexionBD.Parametro("@tel", c.Telefono),
                ConexionBD.Parametro("@email", c.Email),
                ConexionBD.Parametro("@dir", c.Direccion),
                ConexionBD.Parametro("@limite", c.LimiteCredito),
                ConexionBD.Parametro("@saldo", c.Saldo),
                ConexionBD.Parametro("@act", c.Activo));
            return Convert.ToInt32(id);
        }

        /// <summary>Actualiza los datos generales del cliente.</summary>
        /// <param name="c">Cliente con los valores nuevos (identificado por ClienteID).</param>
        public void Actualizar(Cliente c)
        {
            // IMPORTANTE: este UPDATE NO toca la columna Saldo a proposito. El saldo se
            // maneja solo a traves de las operaciones de facturas y cobros (movimientos
            // contables), no editandolo directamente desde el formulario de clientes.
            string sql = @"UPDATE dbo.Clientes SET
                              Nombre=@nom, Identificacion=@ident, Telefono=@tel, Email=@email,
                              Direccion=@dir, LimiteCredito=@limite, Activo=@act
                           WHERE ClienteID=@id";
            ConexionBD.EjecutarComando(sql,
                ConexionBD.Parametro("@nom", c.Nombre),
                ConexionBD.Parametro("@ident", c.Identificacion),
                ConexionBD.Parametro("@tel", c.Telefono),
                ConexionBD.Parametro("@email", c.Email),
                ConexionBD.Parametro("@dir", c.Direccion),
                ConexionBD.Parametro("@limite", c.LimiteCredito),
                ConexionBD.Parametro("@act", c.Activo),
                ConexionBD.Parametro("@id", c.ClienteID));
        }

        /// <summary>Elimina un cliente por su ID.</summary>
        /// <param name="id">Identificador del cliente a borrar.</param>
        public void Eliminar(int id)
        {
            ConexionBD.EjecutarComando("DELETE FROM dbo.Clientes WHERE ClienteID=@id",
                ConexionBD.Parametro("@id", id));
        }

        /// <summary>Convierte una fila de datos en un objeto Cliente.</summary>
        /// <param name="f">Fila (DataRow) devuelta por la consulta.</param>
        /// <returns>Cliente con las columnas ya convertidas a sus tipos.</returns>
        private Cliente Mapear(DataRow f)
        {
            // Traduccion fila -> objeto. Los campos de texto opcionales se protegen
            // contra NULL (DBNull) devolviendo "", y los importes se pasan a decimal.
            return new Cliente
            {
                ClienteID = Convert.ToInt32(f["ClienteID"]),
                Nombre = f["Nombre"].ToString(),
                Identificacion = f["Identificacion"] == DBNull.Value ? "" : f["Identificacion"].ToString(),
                Telefono = f["Telefono"] == DBNull.Value ? "" : f["Telefono"].ToString(),
                Email = f["Email"] == DBNull.Value ? "" : f["Email"].ToString(),
                Direccion = f["Direccion"] == DBNull.Value ? "" : f["Direccion"].ToString(),
                // LimiteCredito y Saldo son montos: se convierten a decimal para calculos exactos.
                LimiteCredito = Convert.ToDecimal(f["LimiteCredito"]),
                Saldo = Convert.ToDecimal(f["Saldo"]),
                Activo = Convert.ToBoolean(f["Activo"])
            };
        }
    }
}
