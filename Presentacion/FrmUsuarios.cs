using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Mantenimiento (CRUD) de Usuarios del sistema. Solo accesible para el rol
    /// Administrador. Gestiona quien puede entrar al sistema, su rol y su estado.
    /// La contrasena tiene un manejo especial: al editar, si se deja vacia NO se cambia.
    /// </summary>
    public class FrmUsuarios : Form
    {
        // ---------- Controles (campos miembro) ----------
        private DataGridView grid; // tabla con el listado de usuarios
        private TextBox txtUsuario, txtNombre, txtContrasena; // login, nombre completo y contrasena
        private ComboBox cboRol;                          // desplegable: "Administrador" o "Usuario"
        private CheckBox chkActivo;                       // usuario habilitado o deshabilitado
        private Button btnNuevo, btnGuardar, btnEliminar; // botones del CRUD

        private readonly UsuarioNegocio _negocio = new UsuarioNegocio(); // puente a la capa de negocio

        // ID del usuario elegido. 0 = registro nuevo (Crear); distinto de 0 = edicion (Actualizar).
        private int _idSeleccionado = 0;

        /// <summary>Constructor: titulo, construccion de la interfaz y carga inicial del listado.</summary>
        public FrmUsuarios()
        {
            Text = "Usuarios";
            ConstruirUI();
            Cargar();
        }

        /// <summary>Construye por codigo la interfaz: titulo, panel de datos (con el combo Rol y la nota de la contrasena), barra con Eliminar y tabla.</summary>
        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Gestion de Usuarios",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel panelForm = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(14), BackColor = Color.White };
            int y = 10; // contador vertical para apilar los controles
            panelForm.Controls.Add(Etiqueta("Nombre de usuario:", ref y));
            txtUsuario = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Nombre completo:", ref y));
            txtNombre = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Contrasena:", ref y));
            txtContrasena = Caja(ref y, panelForm);
            txtContrasena.UseSystemPasswordChar = true; // oculta la contrasena con asteriscos
            // Nota aclaratoria para el usuario: al editar, dejar la contrasena vacia significa "no la cambies".
            panelForm.Controls.Add(new Label
            {
                Text = "(Al editar, deje la contrasena vacia para no cambiarla)",
                Location = new Point(14, y), AutoSize = false, Size = new Size(276, 30),
                ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F)
            });
            y += 34;
            panelForm.Controls.Add(Etiqueta("Rol:", ref y));
            // Combo Rol: solo permite elegir de la lista (DropDownList) entre Administrador o Usuario.
            cboRol = new ComboBox { Location = new Point(14, y), Width = 276, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRol.Items.AddRange(new object[] { "Administrador", "Usuario" });
            cboRol.SelectedIndex = 1; // por defecto "Usuario" (indice 1), el rol con menos privilegios
            panelForm.Controls.Add(cboRol);
            y += 34;

            chkActivo = new CheckBox { Text = "Activo", Location = new Point(14, y), Checked = true, AutoSize = true };
            panelForm.Controls.Add(chkActivo);
            y += 36;

            // Boton Guardar (verde): crea o actualiza el usuario segun _idSeleccionado.
            btnGuardar = new Button { Text = "Guardar", Location = new Point(14, y), Width = 140 };
            UI.EstiloBoton(btnGuardar, UI.ColorExito);
            btnGuardar.Click += (s, e) => Guardar();
            panelForm.Controls.Add(btnGuardar);

            // Boton Nuevo (azul): limpia el formulario para dar de alta uno nuevo.
            btnNuevo = new Button { Text = "Nuevo", Location = new Point(162, y), Width = 128 };
            UI.EstiloBoton(btnNuevo, UI.ColorSecundario);
            btnNuevo.Click += (s, e) => LimpiarFormulario();
            panelForm.Controls.Add(btnNuevo);
            Controls.Add(panelForm);

            // Barra superior: solo el boton Eliminar (este mantenimiento no tiene buscador).
            Panel panelTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 8) };
            btnEliminar = new Button { Text = "Eliminar", Location = new Point(12, 8), Width = 120 };
            UI.EstiloBoton(btnEliminar, UI.ColorPeligro);
            btnEliminar.Click += (s, e) => Eliminar();
            panelTop.Controls.Add(btnEliminar);
            Controls.Add(panelTop);

            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false; // columnas definidas a mano (nota: la contrasena NUNCA se muestra en la tabla)
            grid.Columns.Add(Col("UsuarioID", "ID", 50));
            grid.Columns.Add(Col("NombreUsuario", "Usuario", 130));
            grid.Columns.Add(Col("NombreCompleto", "Nombre completo", 200));
            grid.Columns.Add(Col("Rol", "Rol", 120));
            grid.Columns.Add(Col("Activo", "Activo", 60));
            grid.SelectionChanged += (s, e) => MostrarSeleccion(); // al elegir fila, vuelca el usuario al formulario
            Controls.Add(grid);
            grid.BringToFront();
        }

        // ----- Helpers de construccion (etiquetas, cajas y columnas reutilizables) -----
        private Label Etiqueta(string texto, ref int y)
        {
            Label l = new Label { Text = texto, Location = new Point(14, y), AutoSize = true, ForeColor = UI.ColorTexto };
            y += 20;
            return l;
        }
        private TextBox Caja(ref int y, Panel p)
        {
            TextBox t = new TextBox { Location = new Point(14, y), Width = 276 };
            p.Controls.Add(t);
            y += 32;
            return t;
        }
        private DataGridViewTextBoxColumn Col(string prop, string titulo, int ancho)
        {
            return new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = ancho };
        }

        /// <summary>Carga la tabla con todos los usuarios del sistema. Muestra error si la consulta falla.</summary>
        private void Cargar()
        {
            try { grid.DataSource = _negocio.Listar(); }
            catch (Exception ex) { UI.Error("No se pudieron cargar los usuarios.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Al seleccionar una fila, vuelca ese usuario al formulario y guarda su ID (modo
        /// edicion). La contrasena se deja en blanco a proposito: por seguridad nunca se
        /// recupera la existente, y dejarla vacia indica que no se cambiara al guardar.
        /// </summary>
        private void MostrarSeleccion()
        {
            if (grid.CurrentRow == null || !(grid.CurrentRow.DataBoundItem is Usuario)) return; // fila no valida
            Usuario u = (Usuario)grid.CurrentRow.DataBoundItem; // usuario de la fila seleccionada
            _idSeleccionado = u.UsuarioID; // se recuerda su ID -> el proximo Guardar sera una actualizacion
            txtUsuario.Text = u.NombreUsuario;
            txtNombre.Text = u.NombreCompleto;
            txtContrasena.Text = ""; // se vacia: si el admin no la escribe, se conservara la actual
            cboRol.SelectedItem = u.Rol; // posiciona el combo en el rol del usuario
            chkActivo.Checked = u.Activo;
        }

        /// <summary>Vacia el formulario y pone _idSeleccionado en 0 para dar de alta un usuario nuevo.</summary>
        private void LimpiarFormulario()
        {
            _idSeleccionado = 0; // 0 = modo crear
            txtUsuario.Text = txtNombre.Text = txtContrasena.Text = "";
            cboRol.SelectedIndex = 1; // vuelve a "Usuario"
            chkActivo.Checked = true;
            txtUsuario.Focus();
        }

        /// <summary>
        /// Guarda el usuario: arma la entidad y llama a Crear o Actualizar segun
        /// _idSeleccionado. La contrasena se pasa aparte; la capa de negocio se encarga
        /// de encriptarla, y si al editar viene vacia, mantiene la contrasena anterior.
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Se arma la entidad con los datos del formulario (sin la contrasena, que va por separado).
                Usuario u = new Usuario
                {
                    UsuarioID = _idSeleccionado,
                    NombreUsuario = txtUsuario.Text.Trim(),
                    NombreCompleto = txtNombre.Text.Trim(),
                    Rol = cboRol.SelectedItem.ToString(),
                    Activo = chkActivo.Checked
                };
                // ID 0 = alta (se exige contrasena); ID distinto de 0 = modificacion (contrasena opcional).
                if (_idSeleccionado == 0) { _negocio.Crear(u, txtContrasena.Text); UI.Info("Usuario creado correctamente."); }
                else { _negocio.Actualizar(u, txtContrasena.Text); UI.Info("Usuario actualizado correctamente."); }
                LimpiarFormulario();
                Cargar();
            }
            catch (NegocioException nex) { UI.Advertencia(nex.Message); } // regla de negocio: advertencia amigable
            catch (Exception ex) { UI.Error("No se pudo guardar el usuario.\n\n" + ex.Message); } // fallo tecnico: error
        }

        /// <summary>
        /// Elimina el usuario seleccionado. Ademas de validar la seleccion y confirmar,
        /// impide una accion peligrosa: que el usuario borre su propia cuenta (con la que
        /// esta logueado en ese momento).
        /// </summary>
        private void Eliminar()
        {
            if (_idSeleccionado == 0) { UI.Advertencia("Seleccione un usuario de la tabla."); return; } // nada elegido
            // Proteccion: no dejar que alguien se elimine a si mismo mientras tiene la sesion abierta.
            if (Sesion.UsuarioActual != null && Sesion.UsuarioActual.UsuarioID == _idSeleccionado)
            {
                UI.Advertencia("No puede eliminar el usuario con el que ha iniciado sesion.");
                return;
            }
            if (!UI.Confirmar("Esta seguro de eliminar el usuario seleccionado?")) return; // usuario cancelo
            try
            {
                _negocio.Eliminar(_idSeleccionado);
                UI.Info("Usuario eliminado.");
                LimpiarFormulario();
                Cargar();
            }
            catch (Exception ex) { UI.Error("No se pudo eliminar el usuario.\n\n" + ex.Message); }
        }
    }
}
