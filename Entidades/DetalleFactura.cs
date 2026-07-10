using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>Representa una linea (renglon) de una factura de venta.</summary>
    public class DetalleFactura
    {
        /// <summary>Identificador unico de la linea de detalle (clave primaria en la BD).</summary>
        public int DetalleID { get; set; }

        /// <summary>Id de la factura a la que pertenece esta linea (clave foranea hacia Factura).</summary>
        public int FacturaID { get; set; }

        /// <summary>Id del producto vendido en esta linea (clave foranea hacia Producto).</summary>
        public int ProductoID { get; set; }

        /// <summary>
        /// Nombre del producto copiado al momento de la venta. Es solo para mostrar en pantalla;
        /// no se consulta la tabla Producto cada vez y ademas conserva el nombre historico.
        /// </summary>
        public string NombreProducto { get; set; }   // solo para mostrar en pantalla

        /// <summary>Cantidad de unidades vendidas en esta linea.</summary>
        public int Cantidad { get; set; }

        /// <summary>
        /// Precio por unidad aplicado en esta venta, en la moneda del sistema.
        /// Se guarda en el detalle porque el precio del producto puede cambiar con el tiempo.
        /// </summary>
        public decimal PrecioUnitario { get; set; }

        /// <summary>
        /// Importe de la linea = Cantidad * PrecioUnitario (calculado).
        /// Es el subtotal de este renglon; no se almacena, se recalcula al leerlo.
        /// </summary>
        public decimal Importe
        {
            get { return Cantidad * PrecioUnitario; }
        }
    }
}
