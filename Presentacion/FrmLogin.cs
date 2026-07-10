using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Formulario de inicio de sesion. Es la primera ventana que ve el usuario:
    /// pide usuario y contrasena, los valida contra la capa de negocio y, si son
    /// correctos, guarda al usuario en la Sesion y devuelve DialogResult.OK para
    /// que el programa continue abriendo la ventana principal.
    /// </summary>
    public class FrmLogin : Form
    {
        // ---------- Controles (campos miembro) ----------
        private TextBox txtUsuario;      // caja donde se escribe el nombre de usuario
        private TextBox txtContrasena;   // caja de la contrasena (se muestra con asteriscos)
        private Button btnEntrar;        // boton que dispara la autenticacion
        private Label lblMensaje;        // etiqueta roja donde se muestra el error de login (usuario/clave incorrectos)

        // Puente hacia la capa de Negocio: aqui vive la logica de autenticacion.
        // Es readonly porque siempre usamos la misma instancia durante la vida del formulario.
        private readonly UsuarioNegocio _usuarioNegocio = new UsuarioNegocio();

        /// <summary>Constructor: arma la interfaz al crear el formulario.</summary>
        public FrmLogin()
        {
            InicializarComponentes();
        }

        /// <summary>
        /// Construye por codigo toda la interfaz del login: tamano de la ventana,
        /// la cabecera azul con el titulo, las cajas de usuario/contrasena, el boton
        /// Entrar y las etiquetas de mensaje y ayuda.
        /// </summary>
        private void InicializarComponentes()
        {
            Text = "Inicio de Sesion";
            Size = new Size(400, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = UI.FuenteNormal;

            // Banda superior con el nombre del sistema
            Panel cabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = UI.ColorPrimario
            };
            Label lblTitulo = new Label
            {
                Text = "Sistema de Facturacion",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 16F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            Label lblSub = new Label
            {
                Text = "Cuentas por Cobrar y por Pagar",
                ForeColor = Color.FromArgb(200, 220, 240),
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = false,
                TextAlign = ContentAlignment.BottomCenter,
                Dock = DockStyle.Bottom,
                Height = 30
            };
            cabecera.Controls.Add(lblTitulo);
            cabecera.Controls.Add(lblSub);
            Controls.Add(cabecera);

            Label lblU = new Label { Text = "Usuario:", Location = new Point(50, 160), AutoSize = true, ForeColor = UI.ColorTexto };
            txtUsuario = new TextBox { Location = new Point(50, 182), Width = 290, Font = new Font("Segoe UI", 11F) };

            Label lblC = new Label { Text = "Contrasena:", Location = new Point(50, 225), AutoSize = true, ForeColor = UI.ColorTexto };
            txtContrasena = new TextBox { Location = new Point(50, 247), Width = 290, Font = new Font("Segoe UI", 11F), UseSystemPasswordChar = true };
            // Comodidad: si el usuario pulsa Enter estando en la contrasena, intenta entrar directamente.
            txtContrasena.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Entrar(); };

            btnEntrar = new Button { Text = "Entrar", Location = new Point(50, 300), Width = 290 };
            UI.EstiloBoton(btnEntrar, UI.ColorSecundario);
            btnEntrar.Click += (s, e) => Entrar(); // al hacer clic, se ejecuta la validacion del login

            lblMensaje = new Label
            {
                Location = new Point(50, 345),
                Size = new Size(290, 40),
                ForeColor = UI.ColorPeligro,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblAyuda = new Label
            {
                Text = "Usuario: admin   /   Contrasena: admin123",
                Location = new Point(50, 395),
                Size = new Size(290, 20),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F)
            };

            Controls.Add(lblU);
            Controls.Add(txtUsuario);
            Controls.Add(lblC);
            Controls.Add(txtContrasena);
            Controls.Add(btnEntrar);
            Controls.Add(lblMensaje);
            Controls.Add(lblAyuda);

            // AcceptButton: hace que la tecla Enter, este donde este el foco, active "Entrar".
            AcceptButton = btnEntrar;
        }

        /// <summary>
        /// Intenta iniciar sesion con lo escrito en las cajas. Si la autenticacion es
        /// correcta guarda al usuario en la sesion y cierra el formulario con OK; si
        /// falla, distingue entre un error de credenciales (advertencia amable en la
        /// etiqueta) y un fallo tecnico de conexion (mensaje de error detallado).
        /// </summary>
        private void Entrar()
        {
            lblMensaje.Text = ""; // se limpia cualquier mensaje de error anterior antes de reintentar
            try
            {
                // La capa de negocio valida usuario y contrasena. Si no coinciden, lanza NegocioException.
                Usuario u = _usuarioNegocio.Autenticar(txtUsuario.Text, txtContrasena.Text);
                Sesion.UsuarioActual = u;      // se recuerda quien inicio sesion (disponible en todo el sistema)
                DialogResult = DialogResult.OK; // avisa al Program.cs que el login fue exitoso
                Close();                        // cierra el login para dar paso a la ventana principal
            }
            catch (NegocioException nex)
            {
                // Error esperado de reglas de negocio (ej. "Usuario o contrasena incorrectos").
                // No es un fallo del programa, asi que se muestra suavemente en la etiqueta roja.
                lblMensaje.Text = nex.Message;
            }
            catch (Exception ex)
            {
                // Cualquier otro error es tecnico (tipicamente no hay conexion a SQL Server):
                // se muestra un cuadro de error con la pista para solucionarlo.
                UI.Error("No se pudo conectar con la base de datos.\n\nDetalle: " + ex.Message +
                         "\n\nVerifique que SQL Server este instalado y que se hayan ejecutado los scripts de la carpeta Database.",
                         "Error de conexion");
            }
        }
    }
}
