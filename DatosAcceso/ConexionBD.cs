using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>
    /// Clase central de acceso a datos (ADO.NET). Encapsula la conexion a
    /// SQL Server y ofrece metodos reutilizables para ejecutar consultas y
    /// comandos con parametros, evitando la duplicacion de codigo en los DAO
    /// y protegiendo contra inyeccion SQL.
    /// </summary>
    public static class ConexionBD
    {
        /// <summary>
        /// Devuelve la cadena de conexion definida en App.config
        /// (seccion connectionStrings, nombre "SistemaFacturacionDB").
        /// </summary>
        public static string CadenaConexion
        {
            get
            {
                // Buscamos en App.config la entrada de conexion por su nombre.
                // 'cfg' contiene servidor, base de datos, credenciales, etc.
                ConnectionStringSettings cfg =
                    ConfigurationManager.ConnectionStrings["SistemaFacturacionDB"];

                // Si la entrada no existe o esta vacia avisamos con un error claro,
                // asi evitamos fallos confusos mas adelante al abrir la conexion.
                if (cfg == null || string.IsNullOrEmpty(cfg.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "No se encontro la cadena de conexion 'SistemaFacturacionDB' en el archivo App.config.");
                }
                return cfg.ConnectionString;
            }
        }

        /// <summary>Crea y devuelve una nueva conexion SQL (sin abrir).</summary>
        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(CadenaConexion);
        }

        /// <summary>
        /// Prueba la conexion con la base de datos. Devuelve true si se pudo
        /// abrir correctamente; de lo contrario lanza una excepcion.
        /// </summary>
        public static bool ProbarConexion()
        {
            using (SqlConnection cn = ObtenerConexion())
            {
                cn.Open();
                return cn.State == ConnectionState.Open;
            }
        }

        /// <summary>Utilidad para crear un parametro SQL de forma corta.</summary>
        /// <param name="nombre">Nombre del parametro tal cual aparece en la consulta (ej. "@id").</param>
        /// <param name="valor">Valor a asignar. Si es null se convierte a DBNull.Value (NULL de SQL).</param>
        /// <returns>El parametro SQL ya listo para agregarse a un comando.</returns>
        public static SqlParameter Parametro(string nombre, object valor)
        {
            // Usar parametros (@nombre) en lugar de concatenar texto es la defensa
            // principal contra la inyeccion SQL: el valor viaja aparte y nunca se
            // interpreta como parte de la sentencia.
            // El operador ?? traduce un null de C# al NULL de la base de datos (DBNull.Value),
            // porque ADO.NET no acepta null directo como valor de parametro.
            return new SqlParameter(nombre, valor ?? DBNull.Value);
        }

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve los resultados en un DataTable.
        /// </summary>
        /// <param name="sql">Texto de la consulta SELECT (puede llevar parametros @).</param>
        /// <param name="parametros">Parametros opcionales que reemplazan a los @ de la consulta.</param>
        /// <returns>Una tabla en memoria con todas las filas devueltas.</returns>
        public static DataTable EjecutarConsulta(string sql, params SqlParameter[] parametros)
        {
            // 'tabla' es el contenedor en memoria donde volcaremos el resultado.
            DataTable tabla = new DataTable();

            // 'using' garantiza que la conexion y el comando se liberen (Dispose)
            // aunque ocurra una excepcion, evitando fugas de recursos.
            using (SqlConnection cn = ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                // Adjuntamos los parametros al comando (si los hay).
                if (parametros != null)
                    cmd.Parameters.AddRange(parametros);

                // El SqlDataAdapter abre la conexion, ejecuta el SELECT y rellena
                // la tabla por si solo; no hace falta llamar a cn.Open() manualmente.
                using (SqlDataAdapter adaptador = new SqlDataAdapter(cmd))
                {
                    adaptador.Fill(tabla);
                }
            }
            return tabla;
        }

        /// <summary>
        /// Ejecuta un comando INSERT / UPDATE / DELETE y devuelve el numero
        /// de filas afectadas.
        /// </summary>
        /// <param name="sql">Sentencia INSERT, UPDATE o DELETE (puede llevar parametros @).</param>
        /// <param name="parametros">Parametros opcionales de la sentencia.</param>
        /// <returns>Numero de filas afectadas por la operacion.</returns>
        public static int EjecutarComando(string sql, params SqlParameter[] parametros)
        {
            using (SqlConnection cn = ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                if (parametros != null)
                    cmd.Parameters.AddRange(parametros);

                // Aqui si abrimos la conexion a mano porque ExecuteNonQuery no lo hace.
                cn.Open();
                // ExecuteNonQuery se usa cuando no esperamos filas de vuelta,
                // solo cuantos registros se insertaron/modificaron/borraron.
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta un comando que devuelve un unico valor (por ejemplo
        /// SELECT COUNT(*) o SCOPE_IDENTITY()).
        /// </summary>
        /// <param name="sql">Consulta que retorna un solo valor (ej. un COUNT o el nuevo ID).</param>
        /// <param name="parametros">Parametros opcionales de la consulta.</param>
        /// <returns>El valor de la primera columna de la primera fila.</returns>
        public static object EjecutarEscalar(string sql, params SqlParameter[] parametros)
        {
            using (SqlConnection cn = ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                if (parametros != null)
                    cmd.Parameters.AddRange(parametros);

                cn.Open();
                // ExecuteScalar devuelve solo el primer valor del resultado; ideal para
                // recuperar el ID recien generado (SCOPE_IDENTITY) o un total agregado.
                return cmd.ExecuteScalar();
            }
        }
    }
}
