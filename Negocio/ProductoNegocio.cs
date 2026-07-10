using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para Productos/Servicios (validaciones + CRUD).</summary>
    public class ProductoNegocio
    {
        // Acceso a datos de productos y servicios.
        private readonly ProductoDAO _dao = new ProductoDAO();

        /// <summary>Lista productos/servicios con filtro opcional por texto y por estado activo.</summary>
        public List<Producto> Listar(bool soloActivos = false, string filtro = null)
        {
            return _dao.Listar(soloActivos, filtro);
        }

        /// <summary>Devuelve un producto por su ID (o null si no existe).</summary>
        public Producto ObtenerPorId(int id)
        {
            return _dao.ObtenerPorId(id);
        }

        /// <summary>Valida y crea el producto. Devuelve el ID generado.</summary>
        public int Crear(Producto p)
        {
            Validar(p);
            return _dao.Insertar(p);
        }

        /// <summary>Valida y guarda los cambios de un producto existente.</summary>
        public void Actualizar(Producto p)
        {
            Validar(p);
            _dao.Actualizar(p);
        }

        /// <summary>Elimina un producto por su ID.</summary>
        public void Eliminar(int id)
        {
            _dao.Eliminar(id);
        }

        // Reglas del producto:
        //  - Nombre obligatorio.
        //  - Precio (venta) y Costo (compra) no pueden ser negativos.
        //  - El manejo de Stock depende de si es un bien fisico o un servicio.
        private void Validar(Producto p)
        {
            Validaciones.RequerirTexto(p.Nombre, "Nombre");
            Validaciones.RequerirNoNegativo(p.Precio, "Precio");
            Validaciones.RequerirNoNegativo(p.Costo, "Costo");
            // Los servicios no manejan inventario
            // Si es servicio, forzamos Stock = 0 (no tiene sentido inventariarlo);
            // si es un producto fisico, exigimos un stock valido (>= 0).
            if (p.EsServicio) p.Stock = 0;
            else Validaciones.RequerirNoNegativo(p.Stock, "Stock");
        }
    }
}
