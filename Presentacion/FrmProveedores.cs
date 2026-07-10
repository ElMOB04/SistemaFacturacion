using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Mantenimiento (CRUD) de Proveedores. Mismo patron que el resto de mantenimientos:
    /// tabla con el listado, panel derecho para los datos y buscador arriba. Aqui se
    /// registran las empresas a las que se les compra (cuentas por pagar).
    /// </summary>
    public class FrmProveedores : Form
    {
        // ---------- Controles (campos miembro) ----------
        private DataGridView grid; // tabla con el listado de proveedores
        private TextBox txtBuscar, txtNombre, txtRNC, txtTelefono, txtEmail, txtDireccion, txtSaldo; // buscador + datos del proveedor
        private CheckBox chkActivo;                       // proveedor activo o dado de baja
        private Button btnNuevo, btnGuardar, btnEliminar; // botones del CRUD

        private readonly ProveedorNegocio _negocio = new ProveedorNegocio(); // puente a la capa de negocio

        // ID del proveedor elegido en la tabla. 0 = registro nuevo (Crear); distinto de 0 = edicion (Actualizar).
        private int _idSeleccionado = 0;

        /// <summary>Constructor: titulo, construccion de la interfaz y carga inicial del listado.</summary>
        public FrmProveedores()
        {
            Text = "Proveedores";
            ConstruirUI();
            Cargar();
        }

        /// <summary>Construye por codigo la interfaz: titulo, panel de datos, buscador y tabla.</summary>
        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Gestion de Proveedores",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel panelForm = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(14), BackColor = Color.White };
            int y = 10; // contador vertical: los helpers apilan cada control debajo del anterior
            panelForm.Controls.Add(Etiqueta("Nombre / Razon social:", ref y));
            txtNombre = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("RNC:", ref y));
            txtRNC = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Telefono:", ref y));
            txtTelefono = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Correo electronico:", ref y));
            txtEmail = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Direccion:", ref y));
            txtDireccion = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Saldo por pagar (solo lectura):", ref y));
            txtSaldo = Caja(ref y, panelForm);
            // Saldo por pagar: lo calcula el sistema (compras menos pagos), por eso es solo lectura.
            txtSaldo.ReadOnly = true;
            txtSaldo.BackColor = Color.FromArgb(240, 240, 240);

            chkActivo = new CheckBox { Text = "Activo", Location = new Point(14, y), Checked = true, AutoSize = true };
            panelForm.Controls.Add(chkActivo);
            y += 36;

            // Boton Guardar (verde): crea o actualiza el proveedor segun _idSeleccionado.
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

            Panel panelTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 8) };
            Label lblB = new Label { Text = "Buscar:", Location = new Point(12, 14), AutoSize = true };
            txtBuscar = new TextBox { Location = new Point(70, 11), Width = 240 };
            txtBuscar.TextChanged += (s, e) => Cargar(); // busqueda en vivo: recarga al escribir
            btnEliminar = new Button { Text = "Eliminar", Location = new Point(330, 8), Width = 120 };
            UI.EstiloBoton(btnEliminar, UI.ColorPeligro);
            btnEliminar.Click += (s, e) => Eliminar();
            panelTop.Controls.Add(lblB);
            panelTop.Controls.Add(txtBuscar);
            panelTop.Controls.Add(btnEliminar);
            Controls.Add(panelTop);

            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false; // columnas definidas manualmente, enlazadas a la entidad Proveedor
            grid.Columns.Add(Col("ProveedorID", "ID", 50));
            grid.Columns.Add(Col("Nombre", "Nombre", 200));
            grid.Columns.Add(Col("RNC", "RNC", 110));
            grid.Columns.Add(Col("Telefono", "Telefono", 110));
            grid.Columns.Add(ColMoneda("Saldo", "Saldo por pagar"));
            grid.Columns.Add(Col("Activo", "Activo", 60));
            grid.SelectionChanged += (s, e) => MostrarSeleccion(); // al elegir fila, vuelca el proveedor al formulario
            Controls.Add(grid);
            grid.BringToFront();
        }

        // ----- Helpers de construccion (crean etiquetas, cajas y columnas reutilizables) -----
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
        private DataGridViewTextBoxColumn ColMoneda(string prop, string titulo)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = 120 };
            c.DefaultCellStyle.Format = "N2";
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }

        /// <summary>Carga la tabla con los proveedores, aplicando el filtro del buscador. Muestra error si la consulta falla.</summary>
        private void Cargar()
        {
            try { grid.DataSource = _negocio.Listar(false, txtBuscar != null ? txtBuscar.Text : null); }
            catch (Exception ex) { UI.Error("No se pudieron cargar los proveedores.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Al seleccionar una fila, vuelca ese proveedor al formulario y guarda su ID
        /// en _idSeleccionado (pasando a modo edicion).
        /// </summary>
        private void MostrarSeleccion()
        {
            if (grid.CurrentRow == null || !(grid.CurrentRow.DataBoundItem is Proveedor)) return; // fila no valida: salir
            Proveedor p = (Proveedor)grid.CurrentRow.DataBoundItem; // proveedor de la fila seleccionada
            _idSeleccionado = p.ProveedorID; // se recuerda su ID -> el proximo Guardar sera una actualizacion
            txtNombre.Text = p.Nombre;
            txtRNC.Text = p.RNC;
            txtTelefono.Text = p.Telefono;
            txtEmail.Text = p.Email;
            txtDireccion.Text = p.Direccion;
            txtSaldo.Text = p.Saldo.ToString("N2");
            chkActivo.Checked = p.Activo;
        }

        /// <summary>Vacia el formulario y pone _idSeleccionado en 0 para dar de alta un proveedor nuevo.</summary>
        private void LimpiarFormulario()
        {
            _idSeleccionado = 0; // 0 = modo crear
            txtNombre.Text = txtRNC.Text = txtTelefono.Text = txtEmail.Text = txtDireccion.Text = "";
            txtSaldo.Text = "0.00";
            chkActivo.Checked = true;
            txtNombre.Focus();
        }

        /// <summary>
        /// Guarda el proveedor: arma la entidad con los datos del formulario y llama a
        /// Crear o Actualizar segun _idSeleccionado. Separa los errores de negocio
        /// (advertencia) de los tecnicos (error).
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Se arma la entidad con lo escrito; Trim() elimina espacios sobrantes.
                Proveedor p = new Proveedor
                {
                    ProveedorID = _idSeleccionado,
                    Nombre = txtNombre.Text.Trim(),
                    RNC = txtRNC.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    Activo = chkActivo.Checked
                };
                // ID 0 = alta (Crear); ID distinto de 0 = modificacion (Actualizar).
                if (_idSeleccionado == 0) { _negocio.Crear(p); UI.Info("Proveedor registrado correctamente."); }
                else { _negocio.Actualizar(p); UI.Info("Proveedor actualizado correctamente."); }
                LimpiarFormulario();
                Cargar();
            }
            catch (NegocioException nex) { UI.Advertencia(nex.Message); } // regla de negocio: advertencia amigable
            catch (Exception ex) { UI.Error("No se pudo guardar el proveedor.\n\n" + ex.Message); } // fallo tecnico: error
        }

        /// <summary>
        /// Elimina el proveedor seleccionado tras validar que haya uno elegido y pedir
        /// confirmacion. Si tiene compras asociadas, el borrado fallara y se avisa.
        /// </summary>
        private void Eliminar()
        {
            if (_idSeleccionado == 0) { UI.Advertencia("Seleccione un proveedor de la tabla."); return; } // nada seleccionado
            if (!UI.Confirmar("Esta seguro de eliminar el proveedor seleccionado?")) return; // el usuario cancelo
            try
            {
                _negocio.Eliminar(_idSeleccionado);
                UI.Info("Proveedor eliminado.");
                LimpiarFormulario();
                Cargar();
            }
            catch (Exception ex) { UI.Error("No se pudo eliminar. Es posible que tenga compras asociadas.\n\n" + ex.Message); }
        }
    }
}
