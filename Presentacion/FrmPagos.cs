using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Registro de Pagos a proveedores (abonos a las Cuentas por Pagar). Es la
    /// version "espejo" de FrmCobros: se elige un proveedor -> se cargan sus
    /// compras pendientes -> se selecciona una y se ve su saldo -> se registra un
    /// pago validando el monto contra el saldo.
    /// </summary>
    public class FrmPagos : Form
    {
        // cboProveedor: a quien se paga. cboCompra: compra pendiente a abonar.
        // cboFormaPago: efectivo, transferencia, etc.
        private ComboBox cboProveedor, cboCompra, cboFormaPago;
        // txtSaldo: saldo pendiente (solo lectura). txtMonto: cuanto se paga.
        // txtReferencia: numero de transferencia/cheque (opcional).
        private TextBox txtSaldo, txtMonto, txtReferencia;
        // Boton que confirma el pago.
        private Button btnRegistrar;
        // Tabla con el historial de pagos realizados.
        private DataGridView gridHistorial;

        // Capas de negocio: proveedores, compras y pagos.
        private readonly ProveedorNegocio _proveedorNeg = new ProveedorNegocio();
        private readonly CompraNegocio _compraNeg = new CompraNegocio();
        private readonly PagoNegocio _pagoNeg = new PagoNegocio();

        /// <summary>
        /// Constructor: arma la interfaz, carga los proveedores y muestra el
        /// historial de pagos ya registrados.
        /// </summary>
        public FrmPagos()
        {
            Text = "Pagos";
            ConstruirUI();
            CargarProveedores();
            CargarHistorial();
        }

        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Registro de Pagos a Proveedores",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            // Panel de registro (derecha). "int y" es el cursor vertical que los
            // ayudantes Lbl/Caja/Combo van avanzando para apilar los controles.
            Panel panel = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(14), BackColor = Color.White };
            int y = 8;
            panel.Controls.Add(Lbl("Proveedor:", ref y));
            cboProveedor = Combo(ref y, panel);
            // Al elegir proveedor, se recargan sus compras pendientes.
            cboProveedor.SelectedIndexChanged += (s, e) => CargarComprasPendientes();

            panel.Controls.Add(Lbl("Compra pendiente:", ref y));
            cboCompra = Combo(ref y, panel);
            // Al elegir compra, se muestra su saldo y se propone como monto.
            cboCompra.SelectedIndexChanged += (s, e) => MostrarSaldo();

            panel.Controls.Add(Lbl("Saldo de la compra:", ref y));
            txtSaldo = Caja(ref y, panel); txtSaldo.ReadOnly = true; txtSaldo.BackColor = Color.FromArgb(240, 240, 240);

            panel.Controls.Add(Lbl("Monto a pagar:", ref y));
            txtMonto = Caja(ref y, panel);

            panel.Controls.Add(Lbl("Forma de pago:", ref y));
            cboFormaPago = Combo(ref y, panel);
            cboFormaPago.Items.AddRange(new object[] { "Efectivo", "Transferencia", "Tarjeta", "Cheque" });
            cboFormaPago.SelectedIndex = 0;

            panel.Controls.Add(Lbl("Referencia (opcional):", ref y));
            txtReferencia = Caja(ref y, panel);

            btnRegistrar = new Button { Text = "Registrar Pago", Location = new Point(14, y + 6), Width = 296 };
            UI.EstiloBoton(btnRegistrar, UI.ColorExito);
            btnRegistrar.Click += (s, e) => Registrar();
            panel.Controls.Add(btnRegistrar);
            Controls.Add(panel);

            Label lblHist = new Label { Text = "Historial de pagos", Dock = DockStyle.Top, Height = 28, Padding = new Padding(12, 6, 0, 0), ForeColor = UI.ColorTexto, Font = new Font("Segoe UI Semibold", 10F) };
            Controls.Add(lblHist);

            gridHistorial = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(gridHistorial);
            gridHistorial.AutoGenerateColumns = false;
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroDocumento", HeaderText = "Documento", FillWeight = 110 });
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreProveedor", HeaderText = "Proveedor", FillWeight = 160 });
            DataGridViewTextBoxColumn cf = new DataGridViewTextBoxColumn { DataPropertyName = "Fecha", HeaderText = "Fecha", FillWeight = 100 };
            cf.DefaultCellStyle.Format = "dd/MM/yyyy";
            gridHistorial.Columns.Add(cf);
            DataGridViewTextBoxColumn cm = new DataGridViewTextBoxColumn { DataPropertyName = "Monto", HeaderText = "Monto", FillWeight = 100 };
            cm.DefaultCellStyle.Format = "N2"; cm.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridHistorial.Columns.Add(cm);
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FormaPago", HeaderText = "Forma", FillWeight = 100 });
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Referencia", HeaderText = "Referencia", FillWeight = 100 });
            Controls.Add(gridHistorial);
            gridHistorial.BringToFront();
            lblHist.BringToFront();
        }

        // ----- Ayudantes para apilar controles verticalmente -----
        // Crean el control en la posicion "y" actual y luego avanzan "y" (por eso
        // es "ref"). Evitan tener que calcular coordenadas manualmente.
        private Label Lbl(string t, ref int y) { Label l = new Label { Text = t, Location = new Point(14, y), AutoSize = true, ForeColor = UI.ColorTexto }; y += 20; return l; }
        private TextBox Caja(ref int y, Panel p) { TextBox t = new TextBox { Location = new Point(14, y), Width = 296 }; p.Controls.Add(t); y += 34; return t; }
        private ComboBox Combo(ref int y, Panel p) { ComboBox c = new ComboBox { Location = new Point(14, y), Width = 296, DropDownStyle = ComboBoxStyle.DropDownList }; p.Controls.Add(c); y += 34; return c; }

        /// <summary>
        /// Llena el combo de proveedores. El orden de asignacion (DisplayMember y
        /// ValueMember ANTES del DataSource) es intencional para evitar un
        /// InvalidCastException al enlazar; ver el comentario detallado abajo.
        /// </summary>
        private void CargarProveedores()
        {
            try
            {
                // Se definen DisplayMember y ValueMember ANTES del DataSource para evitar
                // que el evento SelectedIndexChanged se dispare cuando ValueMember aun no
                // existe (en ese caso SelectedValue devolveria el objeto Proveedor completo
                // en vez de su Id y la conversion a entero fallaria).
                cboProveedor.DisplayMember = "Nombre";        // texto visible
                cboProveedor.ValueMember = "ProveedorID";      // valor real (el Id)
                cboProveedor.DataSource = _proveedorNeg.Listar(true);
                cboProveedor.SelectedIndex = -1;               // iniciar sin proveedor seleccionado
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar los proveedores.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Segun el proveedor elegido, carga en el combo solo las compras que aun
        /// tienen saldo pendiente. Se dispara al cambiar de proveedor.
        /// </summary>
        private void CargarComprasPendientes()
        {
            // Limpiar combo y saldo antes de recargar.
            cboCompra.DataSource = null;
            txtSaldo.Text = "";
            // Guarda defensiva: solo continuar si ya hay un Id de proveedor valido.
            if (!(cboProveedor.SelectedValue is int)) return;
            try
            {
                int provID = (int)cboProveedor.SelectedValue;
                // Compras del proveedor que aun se deben (Cuentas por Pagar).
                List<Compra> pendientes = _compraNeg.ListarPendientesPorProveedor(provID);
                cboCompra.DataSource = pendientes;
                cboCompra.DisplayMember = "NumeroDocumento";
                cboCompra.ValueMember = "CompraID";
                // Preseleccionar la primera si hay; si no, sin seleccion.
                cboCompra.SelectedIndex = pendientes.Count > 0 ? 0 : -1;
                MostrarSaldo();
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar las compras.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Muestra el saldo de la compra seleccionada y lo propone como monto a
        /// pagar (editable, por si se quiere abonar solo una parte).
        /// </summary>
        private void MostrarSaldo()
        {
            Compra c = cboCompra.SelectedItem as Compra;
            txtSaldo.Text = c != null ? c.Saldo.ToString("N2") : "";
            txtMonto.Text = c != null ? c.Saldo.ToString("N2") : "";
        }

        /// <summary>
        /// Registra el pago: valida que haya compra y monto numerico, arma la
        /// entidad Pago y la envia al negocio (que valida el monto contra el saldo
        /// y actualiza la compra). Luego refresca pendientes e historial.
        /// </summary>
        private void Registrar()
        {
            try
            {
                // Debe haber una compra pendiente seleccionada.
                Compra c = cboCompra.SelectedItem as Compra;
                if (c == null) { UI.Advertencia("Seleccione una compra pendiente."); return; }

                // El monto debe ser numerico. Que no exceda el saldo lo valida el
                // negocio, que lanza NegocioException si algo no cuadra.
                decimal monto;
                if (!decimal.TryParse(txtMonto.Text, out monto))
                    throw new NegocioException("El monto debe ser un valor numerico.");

                // Armar el pago con los datos de la compra y del formulario.
                Pago pago = new Pago
                {
                    CompraID = c.CompraID,
                    ProveedorID = c.ProveedorID,
                    Fecha = DateTime.Now,
                    Monto = monto,
                    FormaPago = cboFormaPago.SelectedItem.ToString(),
                    Referencia = txtReferencia.Text.Trim()
                };

                _pagoNeg.Registrar(pago);
                UI.Info("Pago registrado correctamente.");
                txtReferencia.Text = "";
                // Recargar: la compra pudo quedar saldada y el pago debe verse en el historial.
                CargarComprasPendientes();
                CargarHistorial();
            }
            // Regla de negocio (ej. pago mayor al saldo) -> advertencia amigable.
            catch (NegocioException nex) { UI.Advertencia(nex.Message); }
            // Error inesperado -> mensaje de error tecnico.
            catch (Exception ex) { UI.Error("No se pudo registrar el pago.\n\n" + ex.Message); }
        }

        /// <summary>Carga en el grid el historial completo de pagos registrados.</summary>
        private void CargarHistorial()
        {
            try { gridHistorial.DataSource = _pagoNeg.Listar(); }
            catch (Exception ex) { UI.Error("No se pudo cargar el historial.\n\n" + ex.Message); }
        }
    }
}
