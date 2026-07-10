using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Listado de facturas de venta (Cuentas por Cobrar). Permite ver todas las
    /// facturas, filtrarlas por estado, crear una nueva, ver su detalle y anularlas.
    /// </summary>
    public class FrmFacturas : Form
    {
        // Tabla principal donde se muestran las facturas.
        private DataGridView grid;
        // Filtro por estado de la factura (Todos / Pendiente / Pagada / Anulada).
        private ComboBox cboEstado;
        // Botones de la barra de acciones.
        private Button btnNueva, btnDetalle, btnAnular, btnRefrescar;

        // Capa de negocio de facturas: aqui viven las consultas y las reglas.
        private readonly FacturaNegocio _negocio = new FacturaNegocio();

        /// <summary>Constructor: arma la interfaz y carga la lista inicial.</summary>
        public FrmFacturas()
        {
            Text = "Facturacion";
            ConstruirUI();
            Cargar();
        }

        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Facturacion - Cuentas por Cobrar",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel barra = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(12, 9, 12, 9) };
            btnNueva = new Button { Text = "Nueva Factura", Location = new Point(12, 9), Width = 150 };
            UI.EstiloBoton(btnNueva, UI.ColorExito);
            btnNueva.Click += (s, e) => NuevaFactura();
            barra.Controls.Add(btnNueva);

            btnDetalle = new Button { Text = "Ver Detalle", Location = new Point(172, 9), Width = 120 };
            UI.EstiloBoton(btnDetalle, UI.ColorSecundario);
            btnDetalle.Click += (s, e) => VerDetalle();
            barra.Controls.Add(btnDetalle);

            btnAnular = new Button { Text = "Anular", Location = new Point(302, 9), Width = 110 };
            UI.EstiloBoton(btnAnular, UI.ColorPeligro);
            btnAnular.Click += (s, e) => Anular();
            barra.Controls.Add(btnAnular);

            btnRefrescar = new Button { Text = "Refrescar", Location = new Point(422, 9), Width = 110 };
            UI.EstiloBoton(btnRefrescar, Color.Gray);
            btnRefrescar.Click += (s, e) => Cargar();
            barra.Controls.Add(btnRefrescar);

            barra.Controls.Add(new Label { Text = "Estado:", Location = new Point(560, 16), AutoSize = true });
            cboEstado = new ComboBox { Location = new Point(615, 12), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboEstado.Items.AddRange(new object[] { "(Todos)", "Pendiente", "Pagada", "Anulada" });
            cboEstado.SelectedIndex = 0;
            // Al cambiar el filtro se recarga la lista automaticamente.
            cboEstado.SelectedIndexChanged += (s, e) => Cargar();
            barra.Controls.Add(cboEstado);
            Controls.Add(barra);

            // Grid que ocupa el resto de la ventana. Columnas definidas a mano.
            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroFactura", HeaderText = "Numero", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreCliente", HeaderText = "Cliente", FillWeight = 180 });
            DataGridViewTextBoxColumn cf = new DataGridViewTextBoxColumn { DataPropertyName = "Fecha", HeaderText = "Fecha", FillWeight = 110 };
            cf.DefaultCellStyle.Format = "dd/MM/yyyy";
            grid.Columns.Add(cf);
            grid.Columns.Add(ColMoneda("Total", "Total"));
            grid.Columns.Add(ColMoneda("Saldo", "Saldo"));
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TipoPago", HeaderText = "Pago", FillWeight = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 90 });
            Controls.Add(grid);
            grid.BringToFront();
        }

        /// <summary>
        /// Fabrica una columna de dinero: enlazada a la propiedad indicada, con
        /// formato de 2 decimales y alineada a la derecha. Evita repetir codigo.
        /// </summary>
        private DataGridViewTextBoxColumn ColMoneda(string prop, string titulo)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = 110 };
            c.DefaultCellStyle.Format = "N2";
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }

        /// <summary>
        /// Carga (o recarga) la lista de facturas en el grid, respetando el filtro
        /// de estado seleccionado.
        /// </summary>
        private void Cargar()
        {
            try
            {
                // Si el filtro es "(Todos)" (indice 0) pasamos null para no filtrar;
                // en otro caso, el texto del estado elegido.
                string estado = cboEstado.SelectedIndex <= 0 ? null : cboEstado.SelectedItem.ToString();
                grid.DataSource = _negocio.Listar(estado);
            }
            catch (Exception ex)
            {
                UI.Error("No se pudieron cargar las facturas.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Devuelve la factura correspondiente a la fila seleccionada en el grid,
        /// o null si no hay ninguna seleccionada.
        /// </summary>
        private Factura Seleccionada()
        {
            if (grid.CurrentRow == null) return null;
            return grid.CurrentRow.DataBoundItem as Factura;
        }

        /// <summary>
        /// Abre el formulario de nueva factura como dialogo. Si el usuario guardo
        /// (DialogResult.OK), recarga la lista para que aparezca la nueva factura.
        /// </summary>
        private void NuevaFactura()
        {
            using (FrmFacturaNueva frm = new FrmFacturaNueva())
            {
                if (frm.ShowDialog() == DialogResult.OK)
                    Cargar();
            }
        }

        /// <summary>
        /// Muestra en un cuadro de texto el detalle completo de la factura
        /// seleccionada: encabezado, cada linea y los totales. Trae los datos
        /// completos (con detalles) desde la capa de negocio.
        /// </summary>
        private void VerDetalle()
        {
            Factura f = Seleccionada();
            if (f == null) { UI.Advertencia("Seleccione una factura."); return; }
            try
            {
                // El grid solo tiene el encabezado; aqui pedimos la factura con sus
                // lineas de detalle cargadas.
                Factura completa = _negocio.ObtenerConDetalle(f.FacturaID);
                // StringBuilder para ir armando el texto multilinea de forma eficiente.
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Factura:  " + completa.NumeroFactura);
                sb.AppendLine("Cliente:  " + completa.NombreCliente);
                sb.AppendLine("Fecha:    " + completa.Fecha.ToString("dd/MM/yyyy hh:mm tt"));
                sb.AppendLine("Vendedor: " + completa.NombreEmpleado);
                sb.AppendLine("Tipo:     " + completa.TipoPago + "     Estado: " + completa.Estado);
                sb.AppendLine("----------------------------------------------------");
                // Una linea por cada producto: cantidad x nombre @ precio = importe.
                foreach (DetalleFactura d in completa.Detalles)
                    sb.AppendLine(d.Cantidad + " x " + d.NombreProducto +
                                  "   @ " + d.PrecioUnitario.ToString("N2") +
                                  "   = " + d.Importe.ToString("N2"));
                sb.AppendLine("----------------------------------------------------");
                sb.AppendLine("Subtotal:    " + completa.Subtotal.ToString("N2"));
                sb.AppendLine("ITBIS (18%): " + completa.Impuesto.ToString("N2"));
                sb.AppendLine("TOTAL:       " + completa.Total.ToString("N2"));
                sb.AppendLine("Saldo:       " + completa.Saldo.ToString("N2"));
                UI.Info(sb.ToString(), "Detalle de la factura " + completa.NumeroFactura);
            }
            catch (Exception ex)
            {
                UI.Error("No se pudo obtener el detalle.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Anula la factura seleccionada. Antes valida que exista, que no este ya
        /// anulada y pide confirmacion (porque la anulacion devuelve inventario y
        /// ajusta el saldo del cliente). El trabajo real lo hace la capa de negocio.
        /// </summary>
        private void Anular()
        {
            Factura f = Seleccionada();
            if (f == null) { UI.Advertencia("Seleccione una factura."); return; }
            // No tiene sentido anular algo que ya esta anulado.
            if (f.Estado == "Anulada") { UI.Advertencia("La factura ya esta anulada."); return; }
            // Confirmacion del usuario; si dice que no, se corta aqui.
            if (!UI.Confirmar("Anular la factura " + f.NumeroFactura + "?\n\n" +
                              "Se devolvera el inventario y se ajustara el saldo del cliente.")) return;
            try
            {
                _negocio.Anular(f.FacturaID);
                UI.Info("Factura anulada correctamente.");
                Cargar();   // refrescar para ver el nuevo estado
            }
            catch (Exception ex)
            {
                UI.Error("No se pudo anular la factura.\n\n" + ex.Message);
            }
        }
    }
}
