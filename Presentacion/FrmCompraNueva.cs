using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Formulario para registrar una nueva compra a proveedor (Cuentas por Pagar).
    /// Aumenta el inventario y calcula subtotal, ITBIS y total.
    /// </summary>
    public class FrmCompraNueva : Form
    {
        // cboProveedor: a quien se le compra. cboProducto: articulo que se compra.
        private ComboBox cboProveedor, cboProducto;
        // txtDocumento: numero de comprobante fiscal (NCF). txtCosto: costo unitario.
        private TextBox txtDocumento, txtCosto;
        // Cantidad que se compra del producto (minimo 1).
        private NumericUpDown numCantidad;
        // Botones de accion de la pantalla.
        private Button btnAgregar, btnQuitar, btnGuardar, btnCancelar;
        // Tabla con las lineas de detalle de la compra.
        private DataGridView gridDetalle;
        // Etiquetas con los totales (subtotal, ITBIS y total).
        private Label lblSubtotal, lblImpuesto, lblTotal;

        // Capas de negocio: proveedores, productos y compras.
        private readonly ProveedorNegocio _proveedorNeg = new ProveedorNegocio();
        private readonly ProductoNegocio _productoNeg = new ProductoNegocio();
        private readonly CompraNegocio _compraNeg = new CompraNegocio();

        // "Carrito" de la compra: lista en memoria con las lineas antes de guardar.
        // Igual que en la factura, es la fuente de la que se calculan los totales.
        private readonly List<DetalleCompra> _detalles = new List<DetalleCompra>();

        /// <summary>Constructor: arma la UI y carga los combos de proveedores y productos.</summary>
        public FrmCompraNueva()
        {
            Text = "Nueva Compra";
            ConstruirUI();
            CargarCombos();
        }

        /// <summary>
        /// Construye la interfaz por codigo: encabezado (proveedor y documento),
        /// panel para agregar lineas, grid de detalle, totales y botones. Es casi
        /// gemela de la de facturas, pero maneja costos en lugar de precios.
        /// </summary>
        private void ConstruirUI()
        {
            Size = new Size(820, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Registrar Nueva Compra",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Location = new Point(16, 12), AutoSize = true
            };
            Controls.Add(titulo);

            Controls.Add(new Label { Text = "Proveedor:", Location = new Point(16, 60), AutoSize = true });
            cboProveedor = new ComboBox { Location = new Point(16, 82), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cboProveedor);

            Controls.Add(new Label { Text = "Numero de documento (NCF):", Location = new Point(440, 60), AutoSize = true });
            txtDocumento = new TextBox { Location = new Point(440, 82), Width = 200 };
            Controls.Add(txtDocumento);

            Panel panelLinea = new Panel { Location = new Point(16, 124), Size = new Size(770, 70), BackColor = Color.White };
            panelLinea.Controls.Add(new Label { Text = "Producto:", Location = new Point(10, 8), AutoSize = true });
            cboProducto = new ComboBox { Location = new Point(10, 30), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            // Al elegir producto, se sugiere su costo conocido en la caja de costo.
            cboProducto.SelectedIndexChanged += (s, e) => AutocompletarCosto();
            panelLinea.Controls.Add(cboProducto);

            panelLinea.Controls.Add(new Label { Text = "Cantidad:", Location = new Point(360, 8), AutoSize = true });
            numCantidad = new NumericUpDown { Location = new Point(360, 30), Width = 80, Minimum = 1, Maximum = 100000, Value = 1 };
            panelLinea.Controls.Add(numCantidad);

            panelLinea.Controls.Add(new Label { Text = "Costo unit.:", Location = new Point(450, 8), AutoSize = true });
            txtCosto = new TextBox { Location = new Point(450, 30), Width = 100, Text = "0.00" };
            panelLinea.Controls.Add(txtCosto);

            btnAgregar = new Button { Text = "Agregar", Location = new Point(566, 28), Width = 100 };
            UI.EstiloBoton(btnAgregar, UI.ColorSecundario);
            btnAgregar.Click += (s, e) => AgregarLinea();
            panelLinea.Controls.Add(btnAgregar);

            btnQuitar = new Button { Text = "Quitar", Location = new Point(672, 28), Width = 90 };
            UI.EstiloBoton(btnQuitar, UI.ColorPeligro);
            btnQuitar.Click += (s, e) => QuitarLinea();
            panelLinea.Controls.Add(btnQuitar);
            Controls.Add(panelLinea);

            // Grid de detalle: columnas definidas a mano y enlazadas a DetalleCompra.
            gridDetalle = new DataGridView { Location = new Point(16, 204), Size = new Size(770, 300) };
            UI.EstiloGrid(gridDetalle);
            gridDetalle.AutoGenerateColumns = false;
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreProducto", HeaderText = "Producto", FillWeight = 300 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cantidad", HeaderText = "Cantidad", FillWeight = 90 });
            DataGridViewTextBoxColumn colC = new DataGridViewTextBoxColumn { DataPropertyName = "CostoUnitario", HeaderText = "Costo", FillWeight = 100 };
            colC.DefaultCellStyle.Format = "N2"; colC.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridDetalle.Columns.Add(colC);
            DataGridViewTextBoxColumn colI = new DataGridViewTextBoxColumn { DataPropertyName = "Importe", HeaderText = "Importe", FillWeight = 110 };
            colI.DefaultCellStyle.Format = "N2"; colI.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridDetalle.Columns.Add(colI);
            Controls.Add(gridDetalle);

            lblSubtotal = new Label { Text = "Subtotal: 0.00", Location = new Point(500, 512), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            lblImpuesto = new Label { Text = "ITBIS (18%): 0.00", Location = new Point(500, 536), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            lblTotal = new Label { Text = "TOTAL: 0.00", Location = new Point(500, 562), AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), ForeColor = UI.ColorPrimario };
            Controls.Add(lblSubtotal);
            Controls.Add(lblImpuesto);
            Controls.Add(lblTotal);

            btnGuardar = new Button { Text = "Guardar Compra", Location = new Point(16, 556), Width = 180 };
            UI.EstiloBoton(btnGuardar, UI.ColorExito);
            btnGuardar.Click += (s, e) => Guardar();
            Controls.Add(btnGuardar);

            btnCancelar = new Button { Text = "Cancelar", Location = new Point(206, 556), Width = 120 };
            UI.EstiloBoton(btnCancelar, Color.Gray);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancelar);
        }

        /// <summary>
        /// Llena los combos de proveedores y productos (solo activos) con los datos
        /// de la base. Para cada uno define el texto visible y el Id como valor.
        /// </summary>
        private void CargarCombos()
        {
            try
            {
                cboProveedor.DataSource = _proveedorNeg.Listar(true);
                cboProveedor.DisplayMember = "Nombre";
                cboProveedor.ValueMember = "ProveedorID";
                cboProveedor.SelectedIndex = -1;   // sin proveedor al inicio

                cboProducto.DataSource = _productoNeg.Listar(true);
                cboProducto.DisplayMember = "Nombre";
                cboProducto.ValueMember = "ProductoID";
                cboProducto.SelectedIndex = -1;
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar los datos.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Al elegir un producto, sugiere su costo conocido en la caja de costo
        /// (el usuario puede sobreescribirlo si el proveedor cobra otro precio).
        /// </summary>
        private void AutocompletarCosto()
        {
            Producto p = cboProducto.SelectedItem as Producto;
            if (p != null) txtCosto.Text = p.Costo.ToString("N2");
        }

        /// <summary>
        /// Valida el producto y el costo capturados y agrega la linea a _detalles.
        /// Si el producto ya estaba, acumula la cantidad en vez de duplicar la linea.
        /// </summary>
        private void AgregarLinea()
        {
            Producto p = cboProducto.SelectedItem as Producto;
            if (p == null) { UI.Advertencia("Seleccione un producto."); return; }
            // El costo debe ser numerico y no negativo.
            decimal costo;
            if (!decimal.TryParse(txtCosto.Text, out costo) || costo < 0)
            {
                UI.Advertencia("El costo unitario no es valido."); return;
            }
            int cantidad = (int)numCantidad.Value;

            // Buscar si el producto ya esta en la lista para acumular cantidad.
            DetalleCompra existente = _detalles.Find(d => d.ProductoID == p.ProductoID);
            if (existente != null)
                existente.Cantidad += cantidad;
            else
                _detalles.Add(new DetalleCompra
                {
                    ProductoID = p.ProductoID,
                    NombreProducto = p.Nombre,
                    Cantidad = cantidad,
                    CostoUnitario = costo
                });

            RefrescarDetalle();
            // Limpiar el panel para la siguiente linea.
            cboProducto.SelectedIndex = -1;
            numCantidad.Value = 1;
            txtCosto.Text = "0.00";
        }

        /// <summary>Quita del "carrito" la linea seleccionada en el grid.</summary>
        private void QuitarLinea()
        {
            if (gridDetalle.CurrentRow == null) return;
            DetalleCompra d = gridDetalle.CurrentRow.DataBoundItem as DetalleCompra;
            if (d != null) { _detalles.Remove(d); RefrescarDetalle(); }
        }

        /// <summary>
        /// Repinta el grid con la lista actual y recalcula Subtotal (suma de
        /// importes), ITBIS (18% del subtotal) y Total (subtotal + impuesto).
        /// </summary>
        private void RefrescarDetalle()
        {
            // Reasignar el DataSource (null y luego copia) fuerza el refresco del grid.
            gridDetalle.DataSource = null;
            gridDetalle.DataSource = new List<DetalleCompra>(_detalles);

            // Subtotal = suma de importes (cantidad x costo) de cada linea.
            decimal subtotal = 0m;
            foreach (DetalleCompra d in _detalles) subtotal += d.Importe;
            // Impuesto (ITBIS) = subtotal x tasa (0.18), redondeado a 2 decimales.
            decimal impuesto = Math.Round(subtotal * Configuracion.TasaImpuesto, 2);
            // Total a pagar al proveedor.
            decimal total = subtotal + impuesto;

            string m = Configuracion.SimboloMoneda + " ";
            lblSubtotal.Text = "Subtotal: " + m + subtotal.ToString("N2");
            lblImpuesto.Text = "ITBIS (18%): " + m + impuesto.ToString("N2");
            lblTotal.Text = "TOTAL: " + m + total.ToString("N2");
        }

        /// <summary>
        /// Valida, arma la entidad Compra con su encabezado y detalles, y la envia
        /// al negocio para guardarla de forma transaccional. Registrar la compra
        /// tambien AUMENTA el inventario de los productos comprados.
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Reglas minimas: proveedor obligatorio y al menos una linea.
                if (cboProveedor.SelectedValue == null)
                    throw new NegocioException("Debe seleccionar un proveedor.");
                if (_detalles.Count == 0)
                    throw new NegocioException("Agregue al menos un producto a la compra.");

                // Encabezado de la compra (proveedor, NCF y fecha actual).
                Compra compra = new Compra
                {
                    ProveedorID = Convert.ToInt32(cboProveedor.SelectedValue),
                    NumeroDocumento = txtDocumento.Text.Trim(),
                    Fecha = DateTime.Now
                };
                // Adjuntar todas las lineas capturadas.
                compra.Detalles.AddRange(_detalles);

                // El negocio inserta la compra, calcula el total y sube el inventario.
                _compraNeg.CrearCompra(compra);
                UI.Info("Compra registrada correctamente.\n\nTotal: " +
                        Configuracion.SimboloMoneda + " " + compra.Total.ToString("N2"),
                        "Compra registrada");
                DialogResult = DialogResult.OK;   // avisar al listado que recargue
                Close();
            }
            // Reglas de negocio -> advertencia amigable.
            catch (NegocioException nex) { UI.Advertencia(nex.Message); }
            // Errores inesperados -> mensaje de error.
            catch (Exception ex) { UI.Error("No se pudo registrar la compra.\n\n" + ex.Message); }
        }
    }
}
