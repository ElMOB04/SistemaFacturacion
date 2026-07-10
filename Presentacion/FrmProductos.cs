using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Mantenimiento (CRUD) de Productos y Servicios. Ademas de los datos base, maneja
    /// precio, costo y existencia (stock), y un combo para distinguir si es Producto o
    /// Servicio. Mismo patron visual que los demas mantenimientos.
    /// </summary>
    public class FrmProductos : Form
    {
        // ---------- Controles (campos miembro) ----------
        private DataGridView grid; // tabla con el listado de productos/servicios
        private TextBox txtBuscar, txtCodigo, txtNombre, txtDescripcion, txtPrecio, txtCosto, txtStock; // buscador + datos
        private ComboBox cboTipo;                         // desplegable: "Producto" o "Servicio"
        private CheckBox chkActivo;                       // producto activo o descontinuado
        private Button btnNuevo, btnGuardar, btnEliminar; // botones del CRUD

        private readonly ProductoNegocio _negocio = new ProductoNegocio(); // puente a la capa de negocio

        // ID del producto elegido. 0 = registro nuevo (Crear); distinto de 0 = edicion (Actualizar).
        private int _idSeleccionado = 0;

        /// <summary>Constructor: titulo, construccion de la interfaz y carga inicial del listado.</summary>
        public FrmProductos()
        {
            Text = "Productos y Servicios";
            ConstruirUI();
            Cargar();
        }

        /// <summary>Construye por codigo la interfaz: titulo, panel de datos (con el combo Tipo), buscador y tabla.</summary>
        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Gestion de Productos y Servicios",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel panelForm = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(14), BackColor = Color.White };
            int y = 10; // contador vertical para ir apilando los controles
            panelForm.Controls.Add(Etiqueta("Codigo:", ref y));
            txtCodigo = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Nombre:", ref y));
            txtNombre = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Descripcion:", ref y));
            txtDescripcion = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Tipo:", ref y));
            // Combo Tipo: solo permite elegir de la lista (DropDownList) entre Producto o Servicio.
            cboTipo = new ComboBox { Location = new Point(14, y), Width = 276, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo.Items.AddRange(new object[] { "Producto", "Servicio" });
            cboTipo.SelectedIndex = 0; // por defecto queda seleccionado "Producto"
            panelForm.Controls.Add(cboTipo);
            y += 32;
            panelForm.Controls.Add(Etiqueta("Precio de venta:", ref y));
            txtPrecio = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Costo:", ref y));
            txtCosto = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Stock (existencia):", ref y));
            txtStock = Caja(ref y, panelForm);

            chkActivo = new CheckBox { Text = "Activo", Location = new Point(14, y), Checked = true, AutoSize = true };
            panelForm.Controls.Add(chkActivo);
            y += 36;

            // Boton Guardar (verde): crea o actualiza el producto segun _idSeleccionado.
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
            txtBuscar.TextChanged += (s, e) => Cargar(); // busqueda en vivo
            btnEliminar = new Button { Text = "Eliminar", Location = new Point(330, 8), Width = 120 };
            UI.EstiloBoton(btnEliminar, UI.ColorPeligro);
            btnEliminar.Click += (s, e) => Eliminar();
            panelTop.Controls.Add(lblB);
            panelTop.Controls.Add(txtBuscar);
            panelTop.Controls.Add(btnEliminar);
            Controls.Add(panelTop);

            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false; // columnas definidas a mano, enlazadas a la entidad Producto
            grid.Columns.Add(Col("ProductoID", "ID", 50));
            grid.Columns.Add(Col("Codigo", "Codigo", 70));
            grid.Columns.Add(Col("Nombre", "Nombre", 200));
            grid.Columns.Add(Col("Tipo", "Tipo", 80));
            grid.Columns.Add(ColMoneda("Precio", "Precio"));
            grid.Columns.Add(Col("Stock", "Stock", 60));
            grid.Columns.Add(Col("Activo", "Activo", 60));
            grid.SelectionChanged += (s, e) => MostrarSeleccion(); // al elegir fila, vuelca el producto al formulario
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
        private DataGridViewTextBoxColumn ColMoneda(string prop, string titulo)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = 90 };
            c.DefaultCellStyle.Format = "N2";
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }

        /// <summary>Carga la tabla con los productos, filtrando por el texto del buscador. Muestra error si falla.</summary>
        private void Cargar()
        {
            try { grid.DataSource = _negocio.Listar(false, txtBuscar != null ? txtBuscar.Text : null); }
            catch (Exception ex) { UI.Error("No se pudieron cargar los productos.\n\n" + ex.Message); }
        }

        /// <summary>Al seleccionar una fila, vuelca ese producto al formulario y guarda su ID (modo edicion).</summary>
        private void MostrarSeleccion()
        {
            if (grid.CurrentRow == null || !(grid.CurrentRow.DataBoundItem is Producto)) return; // fila no valida
            Producto p = (Producto)grid.CurrentRow.DataBoundItem; // producto de la fila seleccionada
            _idSeleccionado = p.ProductoID; // se recuerda su ID -> el proximo Guardar sera una actualizacion
            txtCodigo.Text = p.Codigo;
            txtNombre.Text = p.Nombre;
            txtDescripcion.Text = p.Descripcion;
            cboTipo.SelectedItem = p.Tipo; // posiciona el combo en "Producto" o "Servicio" segun corresponda
            txtPrecio.Text = p.Precio.ToString("N2");
            txtCosto.Text = p.Costo.ToString("N2");
            txtStock.Text = p.Stock.ToString();
            chkActivo.Checked = p.Activo;
        }

        /// <summary>Vacia el formulario y pone _idSeleccionado en 0 para dar de alta un producto nuevo.</summary>
        private void LimpiarFormulario()
        {
            _idSeleccionado = 0; // 0 = modo crear
            txtCodigo.Text = txtNombre.Text = txtDescripcion.Text = "";
            cboTipo.SelectedIndex = 0; // vuelve a "Producto"
            txtPrecio.Text = txtCosto.Text = "0.00";
            txtStock.Text = "0";
            chkActivo.Checked = true;
            txtCodigo.Focus();
        }

        /// <summary>
        /// Guarda el producto. Primero convierte y valida los campos numericos (precio,
        /// costo y stock); si alguno no es valido lanza una NegocioException que se
        /// mostrara como advertencia. Luego arma la entidad y llama a Crear o Actualizar
        /// segun _idSeleccionado.
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Variables locales donde se dejan los valores ya convertidos desde el texto de las cajas.
                decimal precio, costo; int stock;
                // TryParse devuelve false si el texto no es un numero valido; en ese caso avisamos al usuario.
                if (!decimal.TryParse(txtPrecio.Text, out precio))
                    throw new NegocioException("El precio debe ser un valor numerico.");
                if (!decimal.TryParse(txtCosto.Text, out costo))
                    throw new NegocioException("El costo debe ser un valor numerico.");
                if (!int.TryParse(txtStock.Text, out stock))
                    throw new NegocioException("El stock debe ser un numero entero.");

                // Se arma la entidad con los datos ya validados. Trim() limpia espacios sobrantes.
                Producto p = new Producto
                {
                    ProductoID = _idSeleccionado,
                    Codigo = txtCodigo.Text.Trim(),
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text.Trim(),
                    Tipo = cboTipo.SelectedItem.ToString(),
                    Precio = precio,
                    Costo = costo,
                    Stock = stock,
                    Activo = chkActivo.Checked
                };
                // ID 0 = alta; ID distinto de 0 = modificacion.
                if (_idSeleccionado == 0) { _negocio.Crear(p); UI.Info("Producto registrado correctamente."); }
                else { _negocio.Actualizar(p); UI.Info("Producto actualizado correctamente."); }
                LimpiarFormulario();
                Cargar();
            }
            catch (NegocioException nex) { UI.Advertencia(nex.Message); } // regla de negocio: advertencia amigable
            catch (Exception ex) { UI.Error("No se pudo guardar el producto.\n\n" + ex.Message); } // fallo tecnico: error
        }

        /// <summary>
        /// Elimina el producto seleccionado tras validar la seleccion y confirmar. Si el
        /// producto ya aparece en facturas o compras, el borrado fallara y se avisa.
        /// </summary>
        private void Eliminar()
        {
            if (_idSeleccionado == 0) { UI.Advertencia("Seleccione un producto de la tabla."); return; } // nada elegido
            if (!UI.Confirmar("Esta seguro de eliminar el producto seleccionado?")) return; // usuario cancelo
            try
            {
                _negocio.Eliminar(_idSeleccionado);
                UI.Info("Producto eliminado.");
                LimpiarFormulario();
                Cargar();
            }
            catch (Exception ex) { UI.Error("No se pudo eliminar. Es posible que tenga movimientos asociados.\n\n" + ex.Message); }
        }
    }
}
