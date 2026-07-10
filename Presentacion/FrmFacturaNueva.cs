using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Formulario para registrar una nueva factura de venta. Permite agregar
    /// lineas de detalle, calcula automaticamente el subtotal, el ITBIS y el
    /// total, y guarda todo de forma transaccional a traves de la capa de negocio.
    /// </summary>
    public class FrmFacturaNueva : Form
    {
        // ----- Controles del encabezado y de captura de lineas -----
        // cboCliente: a quien se le factura.  cboEmpleado: vendedor (opcional).
        // cboTipoPago: "Contado" o "Credito".  cboProducto: articulo a agregar.
        private ComboBox cboCliente, cboEmpleado, cboTipoPago, cboProducto;
        // Cantidad del producto a agregar (minimo 1).
        private NumericUpDown numCantidad;
        // Precio unitario editable (se autocompleta al elegir el producto).
        private TextBox txtPrecio;
        // Botones de accion de la pantalla.
        private Button btnAgregar, btnQuitar, btnGuardar, btnCancelar;
        // Tabla que muestra las lineas de detalle ya agregadas a la factura.
        private DataGridView gridDetalle;
        // Etiquetas que muestran los totales calculados (subtotal, ITBIS y total).
        private Label lblSubtotal, lblImpuesto, lblTotal;

        // ----- Referencias a la capa de Negocio (logica y acceso a datos) -----
        // Son "readonly" porque se crean una sola vez y no se reemplazan. Cada
        // una encapsula las reglas y consultas de su entidad correspondiente.
        private readonly ClienteNegocio _clienteNeg = new ClienteNegocio();
        private readonly EmpleadoNegocio _empleadoNeg = new EmpleadoNegocio();
        private readonly ProductoNegocio _productoNeg = new ProductoNegocio();
        private readonly FacturaNegocio _facturaNeg = new FacturaNegocio();

        // Lista en memoria con las lineas (detalle) que el usuario va armando
        // antes de guardar. Es el "carrito" de la factura: se le agregan y quitan
        // productos y de ella se calculan los totales. Al guardar, se envia entera
        // a la capa de negocio.
        private readonly List<DetalleFactura> _detalles = new List<DetalleFactura>();

        /// <summary>
        /// Constructor: da titulo a la ventana, construye la interfaz por codigo
        /// y llena los combos con los datos iniciales (clientes, empleados, etc.).
        /// </summary>
        public FrmFacturaNueva()
        {
            Text = "Nueva Factura";
            ConstruirUI();     // arma todos los controles
            CargarCombos();    // trae los datos desde la base
        }

        /// <summary>
        /// Construye toda la interfaz de forma manual (sin diseñador): tamaño y
        /// estilo de la ventana, encabezado, panel para agregar lineas, grid de
        /// detalle, etiquetas de totales y botones. Aqui solo se "arman" los
        /// controles y se conectan sus eventos; la logica esta en otros metodos.
        /// </summary>
        private void ConstruirUI()
        {
            // Ventana de tamaño fijo, centrada respecto a su padre, sin maximizar.
            Size = new Size(820, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Registrar Nueva Factura",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Location = new Point(16, 12), AutoSize = true
            };
            Controls.Add(titulo);

            // ----- Encabezado -----
            Controls.Add(new Label { Text = "Cliente:", Location = new Point(16, 60), AutoSize = true });
            cboCliente = new ComboBox { Location = new Point(16, 82), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cboCliente);

            Controls.Add(new Label { Text = "Empleado (vendedor):", Location = new Point(400, 60), AutoSize = true });
            cboEmpleado = new ComboBox { Location = new Point(400, 82), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cboEmpleado);

            Controls.Add(new Label { Text = "Tipo de pago:", Location = new Point(656, 60), AutoSize = true });
            cboTipoPago = new ComboBox { Location = new Point(656, 82), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            // Solo dos opciones fijas; no vienen de la base de datos.
            cboTipoPago.Items.AddRange(new object[] { "Contado", "Credito" });
            cboTipoPago.SelectedIndex = 0;   // por defecto "Contado"
            Controls.Add(cboTipoPago);

            // ----- Agregar linea -----
            // Panel blanco que agrupa los controles para armar UNA linea de detalle.
            Panel panelLinea = new Panel { Location = new Point(16, 124), Size = new Size(770, 70), BackColor = Color.White };
            panelLinea.Controls.Add(new Label { Text = "Producto / Servicio:", Location = new Point(10, 8), AutoSize = true });
            cboProducto = new ComboBox { Location = new Point(10, 30), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            // Al elegir un producto, se copia su precio de venta a la caja de precio.
            cboProducto.SelectedIndexChanged += (s, e) => AutocompletarPrecio();
            panelLinea.Controls.Add(cboProducto);

            panelLinea.Controls.Add(new Label { Text = "Cantidad:", Location = new Point(360, 8), AutoSize = true });
            numCantidad = new NumericUpDown { Location = new Point(360, 30), Width = 80, Minimum = 1, Maximum = 100000, Value = 1 };
            panelLinea.Controls.Add(numCantidad);

            panelLinea.Controls.Add(new Label { Text = "Precio unit.:", Location = new Point(450, 8), AutoSize = true });
            txtPrecio = new TextBox { Location = new Point(450, 30), Width = 100, Text = "0.00" };
            panelLinea.Controls.Add(txtPrecio);

            // "Agregar": mete la linea capturada dentro de la lista _detalles.
            btnAgregar = new Button { Text = "Agregar", Location = new Point(566, 28), Width = 100 };
            UI.EstiloBoton(btnAgregar, UI.ColorSecundario);
            btnAgregar.Click += (s, e) => AgregarLinea();
            panelLinea.Controls.Add(btnAgregar);

            // "Quitar": elimina de la lista la linea seleccionada en el grid.
            btnQuitar = new Button { Text = "Quitar", Location = new Point(672, 28), Width = 90 };
            UI.EstiloBoton(btnQuitar, UI.ColorPeligro);
            btnQuitar.Click += (s, e) => QuitarLinea();
            panelLinea.Controls.Add(btnQuitar);
            Controls.Add(panelLinea);

            // ----- Grid de detalle -----
            // Muestra las lineas de _detalles. AutoGenerateColumns = false porque
            // definimos manualmente las columnas y las enlazamos por DataPropertyName
            // a las propiedades de la entidad DetalleFactura.
            gridDetalle = new DataGridView { Location = new Point(16, 204), Size = new Size(770, 300) };
            UI.EstiloGrid(gridDetalle);
            gridDetalle.AutoGenerateColumns = false;
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreProducto", HeaderText = "Producto", FillWeight = 300 });
            gridDetalle.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cantidad", HeaderText = "Cantidad", FillWeight = 90 });
            // Columna de precio unitario: formato numerico con 2 decimales (N2) y
            // alineada a la derecha, como es habitual con importes de dinero.
            DataGridViewTextBoxColumn colP = new DataGridViewTextBoxColumn { DataPropertyName = "PrecioUnitario", HeaderText = "Precio", FillWeight = 100 };
            colP.DefaultCellStyle.Format = "N2"; colP.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridDetalle.Columns.Add(colP);
            // Columna "Importe" = Cantidad x PrecioUnitario (propiedad calculada en
            // la entidad DetalleFactura). Tambien con formato N2 a la derecha.
            DataGridViewTextBoxColumn colI = new DataGridViewTextBoxColumn { DataPropertyName = "Importe", HeaderText = "Importe", FillWeight = 110 };
            colI.DefaultCellStyle.Format = "N2"; colI.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridDetalle.Columns.Add(colI);
            Controls.Add(gridDetalle);

            // ----- Totales -----
            // Estas etiquetas se actualizan en RefrescarDetalle cada vez que cambia
            // la lista de lineas.
            lblSubtotal = new Label { Text = "Subtotal: 0.00", Location = new Point(500, 512), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            lblImpuesto = new Label { Text = "ITBIS (18%): 0.00", Location = new Point(500, 536), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            lblTotal = new Label { Text = "TOTAL: 0.00", Location = new Point(500, 562), AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), ForeColor = UI.ColorPrimario };
            Controls.Add(lblSubtotal);
            Controls.Add(lblImpuesto);
            Controls.Add(lblTotal);

            // "Guardar Factura": valida y envia todo a la capa de negocio.
            btnGuardar = new Button { Text = "Guardar Factura", Location = new Point(16, 556), Width = 180 };
            UI.EstiloBoton(btnGuardar, UI.ColorExito);
            btnGuardar.Click += (s, e) => Guardar();
            Controls.Add(btnGuardar);

            // "Cancelar": cierra sin guardar. DialogResult.Cancel le avisa al
            // formulario que lo abrio que no hubo cambios que recargar.
            btnCancelar = new Button { Text = "Cancelar", Location = new Point(206, 556), Width = 120 };
            UI.EstiloBoton(btnCancelar, Color.Gray);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancelar);
        }

        /// <summary>
        /// Llena los tres combos de datos que vienen de la base: clientes,
        /// empleados y productos. En cada uno se define que propiedad se muestra
        /// (DisplayMember) y cual es el valor real (ValueMember, normalmente el Id).
        /// </summary>
        private void CargarCombos()
        {
            try
            {
                // Clientes activos (el "true" filtra solo los habilitados).
                cboCliente.DataSource = _clienteNeg.Listar(true);
                cboCliente.DisplayMember = "Nombre";
                cboCliente.ValueMember = "ClienteID";
                cboCliente.SelectedIndex = -1;   // arrancar sin seleccion

                // Empleados activos. El vendedor es opcional, por eso insertamos
                // al inicio una opcion "(Ninguno)" con Id 0 que representa "sin vendedor".
                List<Empleado> empleados = _empleadoNeg.Listar(true);
                empleados.Insert(0, new Empleado { EmpleadoID = 0, Nombre = "(Ninguno)", Apellido = "" });
                cboEmpleado.DataSource = empleados;
                cboEmpleado.DisplayMember = "NombreCompleto";
                cboEmpleado.ValueMember = "EmpleadoID";

                // Productos/servicios activos que se pueden facturar.
                cboProducto.DataSource = _productoNeg.Listar(true);
                cboProducto.DisplayMember = "Nombre";
                cboProducto.ValueMember = "ProductoID";
                cboProducto.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                // Cualquier fallo de conexion o consulta se muestra como error.
                UI.Error("No se pudieron cargar los datos.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Cuando se elige un producto en el combo, copia su precio de venta a la
        /// caja de texto para ahorrarle el tecleo al usuario (igual lo puede editar).
        /// </summary>
        private void AutocompletarPrecio()
        {
            // SelectedItem es un object; lo convertimos a Producto con "as" (si no
            // lo es, queda null y no hacemos nada).
            Producto p = cboProducto.SelectedItem as Producto;
            if (p != null) txtPrecio.Text = p.Precio.ToString("N2");
        }

        /// <summary>
        /// Toma lo capturado en el panel (producto, cantidad y precio), lo valida y
        /// lo agrega a la lista _detalles. Si el producto ya estaba, suma la cantidad
        /// en vez de duplicar la linea. Al final refresca el grid y los totales.
        /// </summary>
        private void AgregarLinea()
        {
            // Debe haber un producto elegido.
            Producto p = cboProducto.SelectedItem as Producto;
            if (p == null) { UI.Advertencia("Seleccione un producto o servicio."); return; }

            // El precio debe ser un numero valido y no negativo.
            decimal precio;
            if (!decimal.TryParse(txtPrecio.Text, out precio) || precio < 0)
            {
                UI.Advertencia("El precio unitario no es valido."); return;
            }

            // La cantidad viene del NumericUpDown (siempre >= 1).
            int cantidad = (int)numCantidad.Value;

            // Si el producto ya esta en la lista, se acumula la cantidad en lugar
            // de crear una segunda linea del mismo articulo.
            DetalleFactura existente = _detalles.Find(d => d.ProductoID == p.ProductoID);
            if (existente != null)
                existente.Cantidad += cantidad;
            else
                // Producto nuevo: se crea una linea de detalle con sus datos.
                _detalles.Add(new DetalleFactura
                {
                    ProductoID = p.ProductoID,
                    NombreProducto = p.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = precio
                });

            RefrescarDetalle();              // recalcular grid y totales
            // Limpiar el panel para capturar la siguiente linea comodamente.
            cboProducto.SelectedIndex = -1;
            numCantidad.Value = 1;
            txtPrecio.Text = "0.00";
        }

        /// <summary>
        /// Elimina de la lista _detalles la linea seleccionada en el grid y
        /// vuelve a refrescar totales.
        /// </summary>
        private void QuitarLinea()
        {
            if (gridDetalle.CurrentRow == null) return;   // no hay fila seleccionada
            // DataBoundItem es el objeto DetalleFactura enlazado a esa fila.
            DetalleFactura d = gridDetalle.CurrentRow.DataBoundItem as DetalleFactura;
            if (d != null)
            {
                _detalles.Remove(d);
                RefrescarDetalle();
            }
        }

        /// <summary>
        /// Vuelve a pintar el grid con la lista actual y recalcula los totales.
        /// Aqui esta el corazon del calculo: Subtotal (suma de importes), ITBIS
        /// (18% sobre el subtotal) y Total (subtotal + impuesto).
        /// </summary>
        private void RefrescarDetalle()
        {
            // Truco tipico de WinForms: poner el DataSource en null y reasignarlo
            // fuerza al grid a refrescarse aunque la lista tenga los mismos objetos.
            // Se pasa una COPIA de _detalles para no exponer la lista original.
            gridDetalle.DataSource = null;
            gridDetalle.DataSource = new List<DetalleFactura>(_detalles);

            // Subtotal = suma de los importes (cantidad x precio) de cada linea.
            decimal subtotal = 0m;
            foreach (DetalleFactura d in _detalles) subtotal += d.Importe;
            // Impuesto = subtotal x tasa (0.18), redondeado a 2 decimales.
            decimal impuesto = Math.Round(subtotal * Configuracion.TasaImpuesto, 2);
            // Total = lo que finalmente paga el cliente.
            decimal total = subtotal + impuesto;

            // Prefijo con el simbolo de moneda (ej. "RD$ ") para mostrar bonito.
            string m = Configuracion.SimboloMoneda + " ";
            lblSubtotal.Text = "Subtotal: " + m + subtotal.ToString("N2");
            lblImpuesto.Text = "ITBIS (18%): " + m + impuesto.ToString("N2");
            lblTotal.Text = "TOTAL: " + m + total.ToString("N2");
        }

        /// <summary>
        /// Valida los datos minimos, arma la entidad Factura con su encabezado y
        /// sus detalles, y la manda a la capa de negocio para que la guarde de
        /// forma transaccional (cabecera + lineas + descuento de inventario).
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Reglas minimas: debe haber cliente y al menos una linea. Se lanzan
                // como NegocioException para tratarlas como advertencias amigables.
                if (cboCliente.SelectedValue == null || Convert.ToInt32(cboCliente.SelectedValue) == 0)
                    throw new NegocioException("Debe seleccionar un cliente.");
                if (_detalles.Count == 0)
                    throw new NegocioException("Agregue al menos un producto o servicio a la factura.");

                // Encabezado de la factura. El Id del cliente y el tipo de pago se
                // toman de los combos; la fecha es el momento actual.
                Factura factura = new Factura
                {
                    ClienteID = Convert.ToInt32(cboCliente.SelectedValue),
                    TipoPago = cboTipoPago.SelectedItem.ToString(),
                    Fecha = DateTime.Now
                };
                // Empleado (vendedor) es opcional: si quedo en "(Ninguno)" (Id 0),
                // guardamos null; si no, el Id real. EmpleadoID es int? (nullable).
                int empId = cboEmpleado.SelectedValue != null ? Convert.ToInt32(cboEmpleado.SelectedValue) : 0;
                factura.EmpleadoID = empId == 0 ? (int?)null : empId;
                // Se adjuntan todas las lineas capturadas al encabezado.
                factura.Detalles.AddRange(_detalles);

                // La capa de negocio inserta todo y devuelve el Id generado. Tambien
                // rellena NumeroFactura y Total al calcular la factura.
                int id = _facturaNeg.CrearFactura(factura);
                UI.Info("Factura " + factura.NumeroFactura + " registrada correctamente.\n\n" +
                        "Total: " + Configuracion.SimboloMoneda + " " + factura.Total.ToString("N2"),
                        "Factura registrada");
                // Cerrar con OK para que el listado que abrio esta ventana se recargue.
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (NegocioException nex)
            {
                // Errores de reglas de negocio: se muestran como advertencia suave.
                UI.Advertencia(nex.Message);
            }
            catch (Exception ex)
            {
                // Errores inesperados (conexion, SQL, etc.): mensaje de error.
                UI.Error("No se pudo registrar la factura.\n\n" + ex.Message);
            }
        }
    }
}
