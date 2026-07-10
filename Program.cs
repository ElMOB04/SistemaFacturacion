using System;
using System.Windows.Forms;
using SistemaFacturacion.Presentacion;

namespace SistemaFacturacion
{
    /// <summary>
    /// Punto de entrada de la aplicacion. Aqui arranca todo: se configura el
    /// entorno de Windows Forms, se instala un "paracaidas" para errores no
    /// controlados y se muestra primero el login antes de abrir el sistema.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Metodo principal. El atributo [STAThread] es obligatorio en Windows
        /// Forms: indica que el hilo principal usa el modelo de apartamento
        /// "single-thread", requerido por muchos controles (portapapeles,
        /// dialogos, etc.).
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Habilita los estilos visuales modernos de Windows (botones, bordes,
            // etc.) para que la app no se vea con la apariencia antigua.
            Application.EnableVisualStyles();
            // Ajuste de compatibilidad recomendado para el renderizado de texto.
            Application.SetCompatibleTextRenderingDefault(false);

            // Captura global de errores no controlados para no cerrar la app abruptamente.
            // Si en cualquier parte de la interfaz se escapa una excepcion sin
            // atrapar, en lugar de que Windows cierre el programa de golpe,
            // mostramos un mensaje amigable con el detalle del error.
            Application.ThreadException += (s, e) =>
                MessageBox.Show("Ocurrio un error inesperado:\n\n" + e.Exception.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Primero se muestra el login; si es exitoso, se abre el formulario principal.
            // El "using" garantiza que el formulario de login libere sus recursos
            // (Dispose) al cerrarse, aunque el usuario cancele.
            using (FrmLogin login = new FrmLogin())
            {
                // ShowDialog() bloquea hasta que el login se cierra y devuelve el
                // resultado. Solo si el usuario se autentico correctamente
                // (DialogResult.OK) levantamos la ventana principal del sistema.
                if (login.ShowDialog() == DialogResult.OK)
                {
                    // Application.Run mantiene viva la aplicacion mientras la
                    // ventana principal siga abierta (bucle de mensajes).
                    Application.Run(new FrmPrincipal());
                }
            }
        }
    }
}
