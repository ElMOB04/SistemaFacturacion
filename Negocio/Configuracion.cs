namespace SistemaFacturacion.Negocio
{
    /// <summary>
    /// Constantes de configuracion del negocio, centralizadas en un solo lugar
    /// para poder cambiarlas facilmente (tasa de impuesto, simbolo de moneda, etc.).
    /// </summary>
    public static class Configuracion
    {
        /// <summary>Tasa de impuesto aplicada a las ventas y compras (ITBIS 18%).</summary>
        // Se guarda como 0.18 (18% expresado en fraccion) para multiplicarlo
        // directamente por el subtotal. La 'm' final indica que es un literal
        // decimal (no double), lo cual evita errores de redondeo con dinero.
        public const decimal TasaImpuesto = 0.18m;

        /// <summary>Simbolo de moneda usado en la interfaz.</summary>
        public const string SimboloMoneda = "RD$";

        /// <summary>Nombre de la empresa mostrado en la aplicacion.</summary>
        public const string NombreEmpresa = "Comercial El Progreso, SRL";
    }
}
