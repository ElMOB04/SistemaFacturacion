using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>Representa un producto o servicio que la empresa vende.</summary>
    public class Producto
    {
        /// <summary>Identificador unico del producto (clave primaria en la BD).</summary>
        public int ProductoID { get; set; }

        /// <summary>Codigo interno o de barras del producto. Suele ser unico y sirve para buscarlo rapido.</summary>
        public string Codigo { get; set; }

        /// <summary>Nombre corto del producto o servicio.</summary>
        public string Nombre { get; set; }

        /// <summary>Descripcion mas detallada (opcional) del producto o servicio.</summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// Tipo del articulo. Valores validos: "Producto" (maneja inventario) o
        /// "Servicio" (no maneja stock). Ver la propiedad EsServicio.
        /// </summary>
        public string Tipo { get; set; }        // "Producto" o "Servicio"

        /// <summary>Precio de venta al cliente, en la moneda del sistema.</summary>
        public decimal Precio { get; set; }

        /// <summary>
        /// Costo de adquisicion del producto, en la moneda del sistema.
        /// La diferencia Precio - Costo es el margen de ganancia.
        /// </summary>
        public decimal Costo { get; set; }

        /// <summary>
        /// Existencia disponible en inventario (unidades). Solo tiene sentido para
        /// articulos de tipo "Producto"; en los servicios se ignora.
        /// </summary>
        public int Stock { get; set; }

        /// <summary>Indica si el producto esta activo. false = baja logica (se conserva el historial).</summary>
        public bool Activo { get; set; }

        /// <summary>
        /// Constructor por defecto. Asume que el articulo es un "Producto" (lo mas comun)
        /// y lo deja activo.
        /// </summary>
        public Producto()
        {
            Tipo = "Producto";
            Activo = true;
        }

        /// <summary>
        /// Los servicios no manejan inventario (stock).
        /// Devuelve true cuando el campo Tipo es "Servicio", ignorando mayusculas/minusculas.
        /// </summary>
        public bool EsServicio
        {
            get { return string.Equals(Tipo, "Servicio", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>Muestra el nombre del producto al representar el objeto como texto (combos, listas, etc.).</summary>
        public override string ToString()
        {
            return Nombre;
        }
    }
}
