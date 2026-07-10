using System.Drawing;
using System.Windows.Forms;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Utilidades de interfaz: paleta de colores, fuentes y ayudas para mostrar
    /// mensajes y dar formato uniforme a las tablas (DataGridView).
    /// </summary>
    public static class UI
    {
        // ---------- Paleta de colores de la aplicacion ----------
        // Se definen una sola vez aqui (readonly) para que TODOS los formularios usen
        // exactamente los mismos colores y la interfaz se vea uniforme y consistente.
        public static readonly Color ColorPrimario = Color.FromArgb(33, 64, 95);    // azul oscuro: barras, cabeceras y encabezados de tabla
        public static readonly Color ColorSecundario = Color.FromArgb(52, 152, 219); // azul: botones de accion secundaria (Nuevo) y fila seleccionada
        public static readonly Color ColorFondo = Color.FromArgb(236, 240, 241);     // gris muy claro: fondo general de los formularios
        public static readonly Color ColorTexto = Color.FromArgb(44, 62, 80);        // gris azulado oscuro: color de las etiquetas y textos
        public static readonly Color ColorExito = Color.FromArgb(39, 174, 96);       // verde: boton Guardar (accion positiva)
        public static readonly Color ColorPeligro = Color.FromArgb(192, 57, 43);     // rojo: boton Eliminar y mensajes de error

        // ---------- Fuentes reutilizables ----------
        public static readonly Font FuenteNormal = new Font("Segoe UI", 9.5F);              // texto general de la aplicacion
        public static readonly Font FuenteTitulo = new Font("Segoe UI Semibold", 14F);      // titulos de cada pantalla (ej. "Gestion de Clientes")
        public static readonly Font FuenteBoton = new Font("Segoe UI", 9.5F, FontStyle.Bold); // texto en negrita de los botones

        /// <summary>
        /// Da estilo "plano" (flat) y moderno a un boton de accion: sin borde, con el
        /// color de fondo indicado, texto blanco en negrita y cursor de mano al pasar.
        /// </summary>
        /// <param name="btn">Boton al que se le aplica el estilo.</param>
        /// <param name="fondo">Color de fondo del boton (ej. verde=Guardar, rojo=Eliminar).</param>
        public static void EstiloBoton(Button btn, Color fondo)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = fondo;
            btn.ForeColor = Color.White;
            btn.Font = FuenteBoton;
            btn.Cursor = Cursors.Hand;
            btn.Height = 34;
        }

        /// <summary>
        /// Aplica un formato uniforme y legible a una tabla (DataGridView): encabezado
        /// azul, filas alternadas, seleccion de fila completa y solo lectura. Se llama
        /// desde cada mantenimiento para que todas las tablas luzcan igual.
        /// </summary>
        /// <param name="g">La tabla a la que se le da formato.</param>
        public static void EstiloGrid(DataGridView g)
        {
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = ColorPrimario;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 32;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 250);
            g.DefaultCellStyle.SelectionBackColor = ColorSecundario;
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.RowTemplate.Height = 28;
            g.Font = FuenteNormal;
        }

        // ---------- Ayudas para mostrar mensajes al usuario ----------
        // Envuelven MessageBox para no repetir en cada formulario el icono y los botones.

        /// <summary>Muestra un mensaje informativo (icono de informacion, boton OK).</summary>
        public static void Info(string mensaje, string titulo = "Informacion")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Muestra un mensaje de error (icono rojo). Se usa para fallos tecnicos, ej. sin conexion a la base de datos.</summary>
        public static void Error(string mensaje, string titulo = "Error")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>Muestra una advertencia amigable (icono de atencion). Se usa para las reglas de negocio (NegocioException).</summary>
        public static void Advertencia(string mensaje, string titulo = "Atencion")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Pregunta Si/No al usuario y devuelve true solo si pulso "Si".
        /// Se usa para confirmar acciones delicadas como eliminar o cerrar sesion.
        /// </summary>
        public static bool Confirmar(string mensaje, string titulo = "Confirmar")
        {
            return MessageBox.Show(mensaje, titulo, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
