using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Formulario principal (contenedor MDI). Ofrece el menu de navegacion a
    /// todos los modulos del sistema y controla el acceso segun el rol.
    /// </summary>
    public class FrmPrincipal : Form
    {
        // ---------- Controles (campos miembro) ----------
        private MenuStrip menu;                  // barra de menu superior con todos los modulos
        private StatusStrip barraEstado;         // barra inferior de estado
        private ToolStripStatusLabel lblUsuario; // muestra el nombre y rol del usuario que inicio sesion
        private ToolStripStatusLabel lblFecha;   // muestra la fecha y hora actual (reloj vivo)
        private Timer timerReloj;                // temporizador que actualiza la hora cada segundo

        /// <summary>Constructor: arma la interfaz del contenedor principal.</summary>
        public FrmPrincipal()
        {
            InicializarComponentes();
        }

        /// <summary>
        /// Construye la ventana principal: la configura como contenedor MDI, crea el
        /// menu con sus opciones (Archivo, Mantenimiento, Cuentas por Cobrar/Pagar,
        /// Ayuda), la barra de estado con el reloj y el mensaje de bienvenida de fondo.
        /// </summary>
        private void InicializarComponentes()
        {
            Text = "Sistema de Facturacion - " + Configuracion.NombreEmpresa;
            WindowState = FormWindowState.Maximized; // arranca maximizada para aprovechar la pantalla
            IsMdiContainer = true;                    // clave: permite alojar los demas formularios dentro (ventanas hijas MDI)
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            // ---------------- Menu principal ----------------
            menu = new MenuStrip { BackColor = UI.ColorPrimario, ForeColor = Color.White, Padding = new Padding(6, 2, 0, 2) };

            // Menu Archivo: cerrar la sesion actual o salir por completo de la aplicacion.
            ToolStripMenuItem miArchivo = new ToolStripMenuItem("Archivo");
            miArchivo.DropDownItems.Add(Item("Cerrar sesion", (s, e) => CerrarSesion()));
            miArchivo.DropDownItems.Add(new ToolStripSeparator());
            miArchivo.DropDownItems.Add(Item("Salir", (s, e) => Application.Exit()));

            // Menu Mantenimiento: abre los catalogos base (CRUD). Cada opcion crea una
            // instancia del formulario correspondiente y lo abre como ventana hija MDI.
            ToolStripMenuItem miMant = new ToolStripMenuItem("Mantenimiento");
            miMant.DropDownItems.Add(Item("Clientes", (s, e) => Abrir(new FrmClientes())));
            miMant.DropDownItems.Add(Item("Proveedores", (s, e) => Abrir(new FrmProveedores())));
            miMant.DropDownItems.Add(Item("Productos y Servicios", (s, e) => Abrir(new FrmProductos())));
            miMant.DropDownItems.Add(Item("Empleados", (s, e) => Abrir(new FrmEmpleados())));
            // Usuarios pasa por AbrirUsuarios() porque requiere validar que el rol sea Administrador.
            ToolStripMenuItem miUsuarios = Item("Usuarios (solo Admin)", (s, e) => AbrirUsuarios());
            miMant.DropDownItems.Add(miUsuarios);

            // Menu Cuentas por Cobrar: facturacion a clientes y registro de sus cobros.
            ToolStripMenuItem miCxC = new ToolStripMenuItem("Cuentas por Cobrar");
            miCxC.DropDownItems.Add(Item("Facturacion", (s, e) => Abrir(new FrmFacturas())));
            miCxC.DropDownItems.Add(Item("Cobros", (s, e) => Abrir(new FrmCobros())));

            // Menu Cuentas por Pagar: compras a proveedores y los pagos que se les hacen.
            ToolStripMenuItem miCxP = new ToolStripMenuItem("Cuentas por Pagar");
            miCxP.DropDownItems.Add(Item("Compras", (s, e) => Abrir(new FrmCompras())));
            miCxP.DropDownItems.Add(Item("Pagos", (s, e) => Abrir(new FrmPagos())));

            // Menu Ayuda: informacion "Acerca de" del proyecto.
            ToolStripMenuItem miAyuda = new ToolStripMenuItem("Ayuda");
            miAyuda.DropDownItems.Add(Item("Acerca de...", (s, e) => MostrarAcercaDe()));

            menu.Items.Add(miArchivo);
            menu.Items.Add(miMant);
            menu.Items.Add(miCxC);
            menu.Items.Add(miCxP);
            menu.Items.Add(miAyuda);
            MainMenuStrip = menu;
            Controls.Add(menu);

            // ---------------- Barra de estado ----------------
            barraEstado = new StatusStrip { BackColor = UI.ColorPrimario, ForeColor = Color.White };
            // Se leen el nombre y el rol del usuario logueado; con el operador ternario se
            // evita un fallo si por alguna razon no hubiera sesion (se dejarian en blanco).
            string nombre = Sesion.UsuarioActual != null ? Sesion.UsuarioActual.NombreCompleto : "";
            string rol = Sesion.UsuarioActual != null ? Sesion.UsuarioActual.Rol : "";
            lblUsuario = new ToolStripStatusLabel("Usuario: " + nombre + "  (" + rol + ")") { ForeColor = Color.White };
            lblFecha = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.White };
            barraEstado.Items.Add(lblUsuario);
            barraEstado.Items.Add(lblFecha);
            Controls.Add(barraEstado);

            // Reloj de la barra de estado: cada 1000 ms (1 segundo) el Tick refresca la
            // etiqueta con la fecha y hora actuales, dando la sensacion de reloj en vivo.
            timerReloj = new Timer { Interval = 1000 };
            timerReloj.Tick += (s, e) => lblFecha.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy  hh:mm:ss tt");
            timerReloj.Start();

            // Titulo de bienvenida en el fondo
            Label lblBienvenida = new Label
            {
                Text = "Bienvenido al Sistema de Facturacion\nSeleccione una opcion del menu superior",
                Font = new Font("Segoe UI Light", 20F),
                ForeColor = Color.FromArgb(160, 170, 180),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblBienvenida);
            lblBienvenida.SendToBack(); // se manda al fondo para que no tape el menu ni las ventanas hijas
        }

        /// <summary>
        /// Fabrica un elemento de menu (ToolStripMenuItem) con su texto y le engancha
        /// la accion que se ejecutara al hacer clic. Evita repetir codigo por cada opcion.
        /// </summary>
        private ToolStripMenuItem Item(string texto, EventHandler accion)
        {
            ToolStripMenuItem mi = new ToolStripMenuItem(texto) { ForeColor = UI.ColorTexto };
            mi.Click += accion;
            return mi;
        }

        /// <summary>Abre un formulario como hijo MDI, evitando duplicados.</summary>
        private void Abrir(Form formulario)
        {
            // Se busca entre las ventanas hijas ya abiertas una del mismo tipo (ej. FrmClientes).
            // Si ya existe, no se abre otra: se trae al frente la existente y se descarta la nueva.
            Form existente = MdiChildren.FirstOrDefault(f => f.GetType() == formulario.GetType());
            if (existente != null)
            {
                existente.Activate();   // pone el foco en la ventana que ya estaba abierta
                formulario.Dispose();   // libera la instancia recien creada que ya no se usara
                return;
            }
            // No estaba abierta: se enlaza como hija de esta ventana principal y se muestra maximizada.
            formulario.MdiParent = this;
            formulario.WindowState = FormWindowState.Maximized;
            formulario.Show();
        }

        /// <summary>
        /// Abre el mantenimiento de Usuarios, pero solo si el usuario actual es
        /// Administrador. Si no lo es, muestra una advertencia y no hace nada.
        /// </summary>
        private void AbrirUsuarios()
        {
            if (!Sesion.EsAdministrador)
            {
                UI.Advertencia("Solo un usuario Administrador puede gestionar los usuarios del sistema.");
                return;
            }
            Abrir(new FrmUsuarios());
        }

        /// <summary>
        /// Cierra la sesion actual: pide confirmacion, olvida al usuario y reinicia la
        /// aplicacion para volver a mostrar la pantalla de login.
        /// </summary>
        private void CerrarSesion()
        {
            if (!UI.Confirmar("Desea cerrar la sesion actual?")) return; // si dice "No", no hace nada
            Sesion.UsuarioActual = null;  // se borra el usuario en memoria
            Application.Restart();        // se reinicia el programa, lo que vuelve a lanzar el login
        }

        /// <summary>Muestra el cuadro "Acerca de" con los datos del proyecto y la tecnologia usada.</summary>
        private void MostrarAcercaDe()
        {
            UI.Info(
                "Sistema de Facturacion y Cuentas por Cobrar / Pagar\n\n" +
                "Proyecto Final de Programacion\n" +
                "Tecnologia: C# - Windows Forms - ADO.NET - SQL Server\n" +
                "Arquitectura en 3 capas (Presentacion / Negocio / Datos)\n\n" +
                Configuracion.NombreEmpresa,
                "Acerca de");
        }
    }
}
