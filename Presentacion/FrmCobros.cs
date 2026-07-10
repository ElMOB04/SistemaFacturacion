using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Registro de Cobros (abonos de clientes a las Cuentas por Cobrar).
    /// Flujo: se elige un cliente -> se cargan sus facturas pendientes -> se
    /// selecciona una y se muestra su saldo -> se registra un abono validando
    /// el monto contra ese saldo.
    /// </summary>
    public class FrmCobros : Form
    {
        // cboCliente: cliente que paga. cboFactura: factura pendiente a abonar.
        // cboFormaPago: efectivo, transferencia, etc.
        private ComboBox cboCliente, cboFactura, cboFormaPago;
        // txtSaldo: saldo pendiente (solo lectura). txtMonto: cuanto se cobra.
        // txtReferencia: numero de transferencia/cheque (opcional).
        private TextBox txtSaldo, txtMonto, txtReferencia;
        // Boton que confirma el cobro.
        private Button btnRegistrar;
        // Tabla con el historial de cobros ya registrados.
        private DataGridView gridHistorial;

        // Capas de negocio necesarias: clientes, facturas y cobros.
        private readonly ClienteNegocio _clienteNeg = new ClienteNegocio();
        private readonly FacturaNegocio _facturaNeg = new FacturaNegocio();
        private readonly CobroNegocio _cobroNeg = new CobroNegocio();

        /// <summary>
        /// Constructor: arma la interfaz, carga la lista de clientes y muestra el
        /// historial de cobros existentes.
        /// </summary>
        public FrmCobros()
        {
            Text = "Cobros";
            ConstruirUI();
            CargarClientes();
            CargarHistorial();
        }

        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Registro de Cobros",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            // Panel de registro (derecha). El "int y" lleva el cursor vertical:
            // los metodos ayudantes Lbl/Caja/Combo lo van incrementando para ir
            // apilando los controles uno debajo del otro sin calcular posiciones.
            Panel panel = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(14), BackColor = Color.White };
            int y = 8;
            panel.Controls.Add(Lbl("Cliente:", ref y));
            cboCliente = Combo(ref y, panel);
            // Al elegir cliente, se recargan sus facturas pendientes.
            cboCliente.SelectedIndexChanged += (s, e) => CargarFacturasPendientes();

            panel.Controls.Add(Lbl("Factura pendiente:", ref y));
            cboFactura = Combo(ref y, panel);
            // Al elegir factura, se muestra su saldo y se propone como monto.
            cboFactura.SelectedIndexChanged += (s, e) => MostrarSaldo();

            panel.Controls.Add(Lbl("Saldo de la factura:", ref y));
            txtSaldo = Caja(ref y, panel); txtSaldo.ReadOnly = true; txtSaldo.BackColor = Color.FromArgb(240, 240, 240);

            panel.Controls.Add(Lbl("Monto a cobrar:", ref y));
            txtMonto = Caja(ref y, panel);

            panel.Controls.Add(Lbl("Forma de pago:", ref y));
            cboFormaPago = Combo(ref y, panel);
            cboFormaPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFormaPago.Items.AddRange(new object[] { "Efectivo", "Transferencia", "Tarjeta", "Cheque" });
            cboFormaPago.SelectedIndex = 0;

            panel.Controls.Add(Lbl("Referencia (opcional):", ref y));
            txtReferencia = Caja(ref y, panel);

            btnRegistrar = new Button { Text = "Registrar Cobro", Location = new Point(14, y + 6), Width = 296 };
            UI.EstiloBoton(btnRegistrar, UI.ColorExito);
            btnRegistrar.Click += (s, e) => Registrar();
            panel.Controls.Add(btnRegistrar);
            Controls.Add(panel);

            // Historial (centro)
            Label lblHist = new Label { Text = "Historial de cobros", Dock = DockStyle.Top, Height = 28, Padding = new Padding(12, 6, 0, 0), ForeColor = UI.ColorTexto, Font = new Font("Segoe UI Semibold", 10F) };
            Controls.Add(lblHist);

            gridHistorial = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(gridHistorial);
            gridHistorial.AutoGenerateColumns = false;
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroFactura", HeaderText = "Factura", FillWeight = 90 });
            gridHistorial.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreCliente", HeaderText = "Cliente", FillWeight = 160 });
            DataGridViewTextBoxColumn cf = new DataGridViewTextBoxColumn { DataPropertyName = "Fecha", HeaderText = "Fecha", FillWeight = 110 };
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

        // ----- Ayudantes para apilar controles -----
        // Cada uno crea su control en la posicion vertical actual (y) y luego
        // avanza "y" para el siguiente. El parametro "ref" permite modificar la
        // variable del llamador. Asi se evita escribir coordenadas a mano.
        private Label Lbl(string t, ref int y) { Label l = new Label { Text = t, Location = new Point(14, y), AutoSize = true, ForeColor = UI.ColorTexto }; y += 20; return l; }
        private TextBox Caja(ref int y, Panel p) { TextBox t = new TextBox { Location = new Point(14, y), Width = 296 }; p.Controls.Add(t); y += 34; return t; }
        private ComboBox Combo(ref int y, Panel p) { ComboBox c = new ComboBox { Location = new Point(14, y), Width = 296, DropDownStyle = ComboBoxStyle.DropDownList }; p.Controls.Add(c); y += 34; return c; }

        /// <summary>
        /// Llena el combo de clientes. OJO con el orden de asignacion de
        /// DisplayMember/ValueMember respecto al DataSource (ver comentario abajo):
        /// es intencional para evitar un InvalidCastException al enlazar.
        /// </summary>
        private void CargarClientes()
        {
            try
            {
                // IMPORTANTE: se definen DisplayMember y ValueMember ANTES de asignar
                // el DataSource. Si se hiciera al reves, al enlazar la lista el combo
                // seleccionaria automaticamente el primer elemento y dispararia el
                // evento SelectedIndexChanged cuando ValueMember todavia no existe; en
                // ese momento SelectedValue devolveria el objeto Cliente completo (no su
                // Id) y la conversion a entero fallaria con InvalidCastException.
                cboCliente.DisplayMember = "Nombre";      // texto visible en el combo
                cboCliente.ValueMember = "ClienteID";      // valor real que se usa (el Id)
                cboCliente.DataSource = _clienteNeg.Listar(true);
                cboCliente.SelectedIndex = -1;             // iniciar sin cliente seleccionado
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar los clientes.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Segun el cliente elegido, carga en el combo de facturas solo las que
        /// tienen saldo pendiente. Se dispara cada vez que cambia el cliente.
        /// </summary>
        private void CargarFacturasPendientes()
        {
            // Limpiar el combo y el saldo antes de recargar.
            cboFactura.DataSource = null;
            txtSaldo.Text = "";
            // Guarda defensiva: solo continuar si ya hay un Id de cliente valido
            // (durante el enlace inicial SelectedValue puede no ser un entero todavia).
            if (!(cboCliente.SelectedValue is int)) return;
            try
            {
                int clienteID = (int)cboCliente.SelectedValue;
                // Facturas del cliente que aun deben algo (Cuentas por Cobrar).
                List<Factura> pendientes = _facturaNeg.ListarPendientesPorCliente(clienteID);
                cboFactura.DataSource = pendientes;
                cboFactura.DisplayMember = "NumeroFactura";
                cboFactura.ValueMember = "FacturaID";
                // Preseleccionar la primera si hay; si no, dejar sin seleccion.
                cboFactura.SelectedIndex = pendientes.Count > 0 ? 0 : -1;
                MostrarSaldo();
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar las facturas.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Muestra el saldo de la factura seleccionada y lo copia como monto
        /// propuesto (lo comun es cobrar el saldo completo, pero se puede editar).
        /// </summary>
        private void MostrarSaldo()
        {
            Factura f = cboFactura.SelectedItem as Factura;
            txtSaldo.Text = f != null ? f.Saldo.ToString("N2") : "";
            txtMonto.Text = f != null ? f.Saldo.ToString("N2") : "";
        }

        /// <summary>
        /// Registra el cobro: valida que haya factura y un monto numerico, arma la
        /// entidad Cobro y la manda al negocio (que valida el monto contra el saldo
        /// y actualiza la factura). Luego refresca facturas pendientes e historial.
        /// </summary>
        private void Registrar()
        {
            try
            {
                // Debe haber una factura pendiente seleccionada.
                Factura f = cboFactura.SelectedItem as Factura;
                if (f == null) { UI.Advertencia("Seleccione una factura pendiente."); return; }

                // El monto tecleado debe ser numerico. Las reglas de "no cobrar mas
                // que el saldo" las valida la capa de negocio, que lanza NegocioException.
                decimal monto;
                if (!decimal.TryParse(txtMonto.Text, out monto))
                    throw new NegocioException("El monto debe ser un valor numerico.");

                // Armar el abono con los datos de la factura y del formulario.
                Cobro cobro = new Cobro
                {
                    FacturaID = f.FacturaID,
                    ClienteID = f.ClienteID,
                    Fecha = DateTime.Now,
                    Monto = monto,
                    FormaPago = cboFormaPago.SelectedItem.ToString(),
                    Referencia = txtReferencia.Text.Trim()
                };

                _cobroNeg.Registrar(cobro);
                UI.Info("Cobro registrado correctamente.");
                txtReferencia.Text = "";
                // Recargar: la factura pudo quedar saldada (desaparece de pendientes)
                // y el nuevo cobro debe verse en el historial.
                CargarFacturasPendientes();
                CargarHistorial();
            }
            // NegocioException = regla de negocio (ej. monto mayor al saldo): aviso suave.
            catch (NegocioException nex) { UI.Advertencia(nex.Message); }
            // Cualquier otro error: mensaje de error tecnico.
            catch (Exception ex) { UI.Error("No se pudo registrar el cobro.\n\n" + ex.Message); }
        }

        /// <summary>Carga en el grid el historial completo de cobros registrados.</summary>
        private void CargarHistorial()
        {
            try { gridHistorial.DataSource = _cobroNeg.Listar(); }
            catch (Exception ex) { UI.Error("No se pudo cargar el historial.\n\n" + ex.Message); }
        }
    }
}
