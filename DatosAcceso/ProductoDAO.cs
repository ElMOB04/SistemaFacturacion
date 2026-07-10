using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.DatosAcceso
{
    /// <summary>Acceso a datos para la tabla Productos (CRUD + inventario).</summary>
    public class ProductoDAO
    {
        // Columnas comunes a los SELECT de productos. 'Tipo' distingue entre "Producto"
        // (maneja inventario/Stock) y "Servicio" (no descuenta stock). 'Stock' es la
        // existencia actual que suben las compras y bajan las facturas.
        private const string CamposBase =
            "ProductoID, Codigo, Nombre, Descripcion, Tipo, Precio, Costo, Stock, Activo";

        /// <summary>Lista productos, opcionalmente filtrando por texto y solo activos.</summary>
        /// <param name="soloActivos">Si es true, solo devuelve productos con Activo = 1.</param>
        /// <param name="filtro">Texto a buscar en Codigo o Nombre (busqueda parcial).</param>
        /// <returns>Lista de productos que cumplen los filtros, ordenada por nombre.</returns>
        public List<Producto> Listar(bool soloActivos = false, string filtro = null)
        {
            // "WHERE 1=1" para ir sumando condiciones opcionales comodamente.
            string sql = "SELECT " + CamposBase + " FROM dbo.Productos WHERE 1=1";
            List<SqlParameter> ps = new List<SqlParameter>();
            if (soloActivos) sql += " AND Activo = 1";
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // Busqueda parcial parametrizada por Codigo o Nombre.
                sql += " AND (Codigo LIKE @f OR Nombre LIKE @f)";
                ps.Add(ConexionBD.Parametro("@f", "%" + filtro.Trim() + "%"));
            }
            sql += " ORDER BY Nombre";

            DataTable dt = ConexionBD.EjecutarConsulta(sql, ps.ToArray());
            List<Producto> lista = new List<Producto>();
            foreach (DataRow f in dt.Rows)
                lista.Add(Mapear(f));
            return lista;
        }

        /// <summary>Busca un producto por su ID. Devuelve null si no existe.</summary>
        /// <param name="id">Identificador del producto.</param>
        public Producto ObtenerPorId(int id)
        {
            DataTable dt = ConexionBD.EjecutarConsulta(
                "SELECT " + CamposBase + " FROM dbo.Productos WHERE ProductoID=@id",
                ConexionBD.Parametro("@id", id));
            return dt.Rows.Count == 0 ? null : Mapear(dt.Rows[0]);
        }

        /// <summary>Inserta un producto nuevo (con su stock inicial) y devuelve el ID generado.</summary>
        /// <param name="p">Datos del producto a registrar.</param>
        /// <returns>ID autonumerico asignado por la base de datos.</returns>
        public int Insertar(Producto p)
        {
            // INSERT + SCOPE_IDENTITY() para recuperar el ID del producto recien creado.
            string sql = @"INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Tipo, Precio, Costo, Stock, Activo)
                           VALUES (@cod, @nom, @desc, @tipo, @precio, @costo, @stock, @act);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object id = ConexionBD.EjecutarEscalar(sql,
                ConexionBD.Parametro("@cod", p.Codigo),
                ConexionBD.Parametro("@nom", p.Nombre),
                ConexionBD.Parametro("@desc", p.Descripcion),
                ConexionBD.Parametro("@tipo", p.Tipo),
                ConexionBD.Parametro("@precio", p.Precio),
                ConexionBD.Parametro("@costo", p.Costo),
                ConexionBD.Parametro("@stock", p.Stock),
                ConexionBD.Parametro("@act", p.Activo));
            return Convert.ToInt32(id);
        }

        /// <summary>Actualiza todos los datos de un producto existente.</summary>
        /// <param name="p">Producto con los valores nuevos (identificado por ProductoID).</param>
        public void Actualizar(Producto p)
        {
            // A diferencia de clientes/proveedores, aqui SI se actualiza el Stock, ya que
            // este metodo se usa para correcciones manuales del inventario desde el
            // mantenimiento de productos. Los movimientos automaticos (facturas/compras)
            // ajustan el Stock por su cuenta dentro de sus propias transacciones.
            string sql = @"UPDATE dbo.Productos SET
                              Codigo=@cod, Nombre=@nom, Descripcion=@desc, Tipo=@tipo,
                              Precio=@precio, Costo=@costo, Stock=@stock, Activo=@act
                           WHERE ProductoID=@id";
            ConexionBD.EjecutarComando(sql,
                ConexionBD.Parametro("@cod", p.Codigo),
                ConexionBD.Parametro("@nom", p.Nombre),
                ConexionBD.Parametro("@desc", p.Descripcion),
                ConexionBD.Parametro("@tipo", p.Tipo),
                ConexionBD.Parametro("@precio", p.Precio),
                ConexionBD.Parametro("@costo", p.Costo),
                ConexionBD.Parametro("@stock", p.Stock),
                ConexionBD.Parametro("@act", p.Activo),
                ConexionBD.Parametro("@id", p.ProductoID));
        }

        /// <summary>Elimina un producto por su ID.</summary>
        /// <param name="id">Identificador del producto a borrar.</param>
        public void Eliminar(int id)
        {
            ConexionBD.EjecutarComando("DELETE FROM dbo.Productos WHERE ProductoID=@id",
                ConexionBD.Parametro("@id", id));
        }

        /// <summary>Convierte una fila de datos en un objeto Producto.</summary>
        /// <param name="f">Fila (DataRow) devuelta por la consulta.</param>
        /// <returns>Producto con las columnas ya convertidas a sus tipos.</returns>
        private Producto Mapear(DataRow f)
        {
            // Fila -> objeto. Codigo y Descripcion se protegen contra NULL; Precio/Costo
            // como decimal (dinero) y Stock como int (unidades enteras en inventario).
            return new Producto
            {
                ProductoID = Convert.ToInt32(f["ProductoID"]),
                Codigo = f["Codigo"] == DBNull.Value ? "" : f["Codigo"].ToString(),
                Nombre = f["Nombre"].ToString(),
                Descripcion = f["Descripcion"] == DBNull.Value ? "" : f["Descripcion"].ToString(),
                Tipo = f["Tipo"].ToString(),
                Precio = Convert.ToDecimal(f["Precio"]),
                Costo = Convert.ToDecimal(f["Costo"]),
                Stock = Convert.ToInt32(f["Stock"]),
                Activo = Convert.ToBoolean(f["Activo"])
            };
        }
    }
}
