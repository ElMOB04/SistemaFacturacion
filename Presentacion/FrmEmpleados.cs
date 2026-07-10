using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Mantenimiento (CRUD) de Empleados. Incluye un selector de fecha (DateTimePicker)
    /// para la fecha de ingreso. A diferencia de otros mantenimientos, aqui no hay caja
    /// de busqueda: se lista siempre la totalidad de empleados.
    /// </summary>
    public class FrmEmpleados : Form
    {
        // ---------- Controles (campos miembro) ----------
        private DataGridView grid; // tabla con el listado de empleados
        private TextBox txtNombre, txtApellido, txtCedula, txtCargo, txtTelefono, txtEmail; // datos del empleado
        private DateTimePicker dtpIngreso;                // selector de la fecha de ingreso
        private CheckBox chkActivo;                       // empleado activo o dado de baja
        private Button btnNuevo, btnGuardar, btnEliminar; // botones del CRUD

        private readonly EmpleadoNegocio _negocio = new EmpleadoNegocio(); // puente a la capa de negocio

        // ID del empleado elegido. 0 = registro nuevo (Crear); distinto de 0 = edicion (Actualizar).
        private int _idSeleccionado = 0;

        /// <summary>Constructor: titulo, construccion de la interfaz y carga inicial del listado.</summary>
        public FrmEmpleados()
        {
            Text = "Empleados";
            ConstruirUI();
            Cargar();
        }

        /// <summary>Construye por codigo la interfaz: titulo, panel de datos (con el selector de fecha), barra con Eliminar y tabla.</summary>
        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Gestion de Empleados",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel panelForm = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(14), BackColor = Color.White };
            int y = 10; // contador vertical para apilar los controles
            panelForm.Controls.Add(Etiqueta("Nombre:", ref y));
            txtNombre = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Apellido:", ref y));
            txtApellido = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Cedula:", ref y));
            txtCedula = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Cargo:", ref y));
            txtCargo = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Telefono:", ref y));
            txtTelefono = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Correo electronico:", ref y));
            txtEmail = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Fecha de ingreso:", ref y));
            dtpIngreso = new DateTimePicker { Location = new Point(14, y), Width = 276, Format = DateTimePickerFormat.Short };
            panelForm.Controls.Add(dtpIngreso);
            y += 34;

            chkActivo = new CheckBox { Text = "Activo", Location = new Point(14, y), Checked = true, AutoSize = true };
            panelForm.Controls.Add(chkActivo);
            y += 36;

            // Boton Guardar (verde): crea o actualiza el empleado segun _idSeleccionado.
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

            // Barra superior: aqui solo lleva el boton Eliminar (este mantenimiento no tiene buscador).
            Panel panelTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 8) };
            btnEliminar = new Button { Text = "Eliminar", Location = new Point(12, 8), Width = 120 };
            UI.EstiloBoton(btnEliminar, UI.ColorPeligro);
            btnEliminar.Click += (s, e) => Eliminar();
            panelTop.Controls.Add(btnEliminar);
            Controls.Add(panelTop);

            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false; // columnas definidas a mano, enlazadas a la entidad Empleado
            grid.Columns.Add(Col("EmpleadoID", "ID", 50));
            grid.Columns.Add(Col("Nombre", "Nombre", 120));
            grid.Columns.Add(Col("Apellido", "Apellido", 120));
            grid.Columns.Add(Col("Cedula", "Cedula", 120));
            grid.Columns.Add(Col("Cargo", "Cargo", 120));
            grid.Columns.Add(Col("Telefono", "Telefono", 110));
            grid.Columns.Add(Col("Activo", "Activo", 60));
            grid.SelectionChanged += (s, e) => MostrarSeleccion(); // al elegir fila, vuelca el empleado al formulario
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

        /// <summary>Carga la tabla con todos los empleados. El parametro false indica que se traigan tambien los inactivos.</summary>
        private void Cargar()
        {
            try { grid.DataSource = _negocio.Listar(false); }
            catch (Exception ex) { UI.Error("No se pudieron cargar los empleados.\n\n" + ex.Message); }
        }

        /// <summary>Al seleccionar una fila, vuelca ese empleado al formulario y guarda su ID (modo edicion).</summary>
        private void MostrarSeleccion()
        {
            if (grid.CurrentRow == null || !(grid.CurrentRow.DataBoundItem is Empleado)) return; // fila no valida
            Empleado e = (Empleado)grid.CurrentRow.DataBoundItem; // empleado de la fila seleccionada
            _idSeleccionado = e.EmpleadoID; // se recuerda su ID -> el proximo Guardar sera una actualizacion
            txtNombre.Text = e.Nombre;
            txtApellido.Text = e.Apellido;
            txtCedula.Text = e.Cedula;
            txtCargo.Text = e.Cargo;
            txtTelefono.Text = e.Telefono;
            txtEmail.Text = e.Email;
            // FechaIngreso es opcional (nullable): si viene vacia (null), se muestra la fecha de hoy con ?? .
            dtpIngreso.Value = e.FechaIngreso ?? DateTime.Today;
            chkActivo.Checked = e.Activo;
        }

        /// <summary>Vacia el formulario y pone _idSeleccionado en 0 para dar de alta un empleado nuevo.</summary>
        private void LimpiarFormulario()
        {
            _idSeleccionado = 0; // 0 = modo crear
            txtNombre.Text = txtApellido.Text = txtCedula.Text = txtCargo.Text = txtTelefono.Text = txtEmail.Text = "";
            dtpIngreso.Value = DateTime.Today; // la fecha de ingreso arranca en hoy
            chkActivo.Checked = true;
            txtNombre.Focus();
        }

        /// <summary>
        /// Guarda el empleado: arma la entidad con los datos del formulario (tomando la
        /// fecha del selector) y llama a Crear o Actualizar segun _idSeleccionado.
        /// Separa errores de negocio (advertencia) de los tecnicos (error).
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Se arma la entidad con lo escrito; Trim() quita espacios sobrantes.
                Empleado e = new Empleado
                {
                    EmpleadoID = _idSeleccionado,
                    Nombre = txtNombre.Text.Trim(),
                    Apellido = txtApellido.Text.Trim(),
                    Cedula = txtCedula.Text.Trim(),
                    Cargo = txtCargo.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    FechaIngreso = dtpIngreso.Value.Date, // .Date toma solo la fecha, sin la hora
                    Activo = chkActivo.Checked
                };
                // ID 0 = alta; ID distinto de 0 = modificacion.
                if (_idSeleccionado == 0) { _negocio.Crear(e); UI.Info("Empleado registrado correctamente."); }
                else { _negocio.Actualizar(e); UI.Info("Empleado actualizado correctamente."); }
                LimpiarFormulario();
                Cargar();
            }
            catch (NegocioException nex) { UI.Advertencia(nex.Message); } // regla de negocio: advertencia amigable
            catch (Exception ex) { UI.Error("No se pudo guardar el empleado.\n\n" + ex.Message); } // fallo tecnico: error
        }

        /// <summary>Elimina el empleado seleccionado tras validar la seleccion y confirmar. Avisa si tiene movimientos ligados.</summary>
        private void Eliminar()
        {
            if (_idSeleccionado == 0) { UI.Advertencia("Seleccione un empleado de la tabla."); return; } // nada elegido
            if (!UI.Confirmar("Esta seguro de eliminar el empleado seleccionado?")) return; // usuario cancelo
            try
            {
                _negocio.Eliminar(_idSeleccionado);
                UI.Info("Empleado eliminado.");
                LimpiarFormulario();
                Cargar();
            }
            catch (Exception ex) { UI.Error("No se pudo eliminar. Es posible que tenga movimientos asociados.\n\n" + ex.Message); }
        }
    }
}
