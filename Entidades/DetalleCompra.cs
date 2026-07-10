using System;

namespace SistemaFacturacion.Entidades
{
    /// <summary>Representa una linea (renglon) de una compra a proveedor.</summary>
    public class DetalleCompra
    {
        /// <summary>Identificador unico de la linea de detalle (clave primaria en la BD).</summary>
        public int DetalleID { get; set; }

        /// <summary>Id de la compra a la que pertenece esta linea (clave foranea hacia Compra).</summary>
        public int CompraID { get; set; }

        /// <summary>Id del producto comprado en esta linea (clave foranea hacia Producto).</summary>
        public int ProductoID { get; set; }

        /// <summary>
        /// Nombre del producto copiado al momento de la compra. Es solo para mostrar;
        /// conserva el nombre historico y evita cruzar la tabla Producto al listar.
        /// </summary>
        public string NombreProducto { get; set; }   // solo para mostrar

        /// <summary>Cantidad de unidades compradas en esta linea.</summary>
        public int Cantidad { get; set; }

        /// <summary>
        /// Costo por unidad pagado al proveedor en esta compra, en la moneda del sistema.
        /// Se guarda en el detalle porque el costo puede variar de una compra a otra.
        /// </summary>
        public decimal CostoUnitario { get; set; }

        /// <summary>
        /// Importe de la linea = Cantidad * CostoUnitario (calculado).
        /// Es el subtotal de este renglon; no se almacena, se recalcula al leerlo.
        /// </summary>
        public decimal Importe
        {
            get { return Cantidad * CostoUnitario; }
        }
    }
}
