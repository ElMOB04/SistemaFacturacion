using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Listado de compras a proveedores (Cuentas por Pagar). Permite ver las
    /// compras, filtrarlas por estado, registrar una nueva y ver su detalle.
    /// A diferencia de las facturas, aqui no hay opcion de anular.
    /// </summary>
    public class FrmCompras : Form
    {
        // Tabla principal de compras.
        private DataGridView grid;
        // Filtro por estado (Todos / Pendiente / Pagada).
        private ComboBox cboEstado;
        // Botones de la barra de acciones.
        private Button btnNueva, btnDetalle, btnRefrescar;

        // Capa de negocio de compras.
        private readonly CompraNegocio _negocio = new CompraNegocio();

        /// <summary>Constructor: arma la interfaz y carga la lista inicial.</summary>
        public FrmCompras()
        {
            Text = "Compras";
            ConstruirUI();
            Cargar();
        }

        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Compras - Cuentas por Pagar",
                Font = UI.FuenteTitulo, ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            Panel barra = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(12, 9, 12, 9) };
            btnNueva = new Button { Text = "Nueva Compra", Location = new Point(12, 9), Width = 150 };
            UI.EstiloBoton(btnNueva, UI.ColorExito);
            btnNueva.Click += (s, e) => NuevaCompra();
            barra.Controls.Add(btnNueva);

            btnDetalle = new Button { Text = "Ver Detalle", Location = new Point(172, 9), Width = 120 };
            UI.EstiloBoton(btnDetalle, UI.ColorSecundario);
            btnDetalle.Click += (s, e) => VerDetalle();
            barra.Controls.Add(btnDetalle);

            btnRefrescar = new Button { Text = "Refrescar", Location = new Point(302, 9), Width = 110 };
            UI.EstiloBoton(btnRefrescar, Color.Gray);
            btnRefrescar.Click += (s, e) => Cargar();
            barra.Controls.Add(btnRefrescar);

            barra.Controls.Add(new Label { Text = "Estado:", Location = new Point(440, 16), AutoSize = true });
            cboEstado = new ComboBox { Location = new Point(495, 12), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboEstado.Items.AddRange(new object[] { "(Todos)", "Pendiente", "Pagada" });
            cboEstado.SelectedIndex = 0;
            // Al cambiar el filtro se recarga la lista.
            cboEstado.SelectedIndexChanged += (s, e) => Cargar();
            barra.Controls.Add(cboEstado);
            Controls.Add(barra);

            // Grid que ocupa el resto de la ventana, con columnas definidas a mano.
            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NumeroDocumento", HeaderText = "Documento", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NombreProveedor", HeaderText = "Proveedor", FillWeight = 180 });
            DataGridViewTextBoxColumn cf = new DataGridViewTextBoxColumn { DataPropertyName = "Fecha", HeaderText = "Fecha", FillWeight = 100 };
            cf.DefaultCellStyle.Format = "dd/MM/yyyy";
            grid.Columns.Add(cf);
            grid.Columns.Add(ColMoneda("Total", "Total"));
            grid.Columns.Add(ColMoneda("Saldo", "Saldo"));
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Estado", HeaderText = "Estado", FillWeight = 90 });
            Controls.Add(grid);
            grid.BringToFront();
        }

        /// <summary>
        /// Fabrica una columna de dinero (formato N2, alineada a la derecha)
        /// enlazada a la propiedad indicada. Evita repetir la misma configuracion.
        /// </summary>
        private DataGridViewTextBoxColumn ColMoneda(string prop, string titulo)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = 110 };
            c.DefaultCellStyle.Format = "N2";
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }

        /// <summary>Carga (o recarga) el listado de compras respetando el filtro de estado.</summary>
        private void Cargar()
        {
            try
            {
                // "(Todos)" (indice 0) -> null (sin filtro); cualquier otro -> ese estado.
                string estado = cboEstado.SelectedIndex <= 0 ? null : cboEstado.SelectedItem.ToString();
                grid.DataSource = _negocio.Listar(estado);
            }
            catch (Exception ex) { UI.Error("No se pudieron cargar las compras.\n\n" + ex.Message); }
        }

        /// <summary>
        /// Abre el formulario de nueva compra como dialogo; si se guardo, recarga
        /// la lista para mostrar la compra recien registrada.
        /// </summary>
        private void NuevaCompra()
        {
            using (FrmCompraNueva frm = new FrmCompraNueva())
            {
                if (frm.ShowDialog() == DialogResult.OK)
                    Cargar();
            }
        }

        /// <summary>
        /// Muestra el detalle completo de la compra seleccionada (encabezado,
        /// lineas y totales) en un cuadro de informacion.
        /// </summary>
        private void VerDetalle()
        {
            if (grid.CurrentRow == null) { UI.Advertencia("Seleccione una compra."); return; }
            Compra c = grid.CurrentRow.DataBoundItem as Compra;
            if (c == null) return;
            try
            {
                // Traer la compra con sus lineas de detalle cargadas.
                Compra completa = _negocio.ObtenerConDetalle(c.CompraID);
                // Armar el texto multilinea del detalle.
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Documento: " + completa.NumeroDocumento);
                sb.AppendLine("Proveedor: " + completa.NombreProveedor);
                sb.AppendLine("Fecha:     " + completa.Fecha.ToString("dd/MM/yyyy"));
                sb.AppendLine("Estado:    " + completa.Estado);
                sb.AppendLine("----------------------------------------------------");
                // Una linea por producto: cantidad x nombre @ costo = importe.
                foreach (DetalleCompra d in completa.Detalles)
                    sb.AppendLine(d.Cantidad + " x " + d.NombreProducto +
                                  "   @ " + d.CostoUnitario.ToString("N2") +
                                  "   = " + d.Importe.ToString("N2"));
                sb.AppendLine("----------------------------------------------------");
                sb.AppendLine("Subtotal:    " + completa.Subtotal.ToString("N2"));
                sb.AppendLine("ITBIS (18%): " + completa.Impuesto.ToString("N2"));
                sb.AppendLine("TOTAL:       " + completa.Total.ToString("N2"));
                sb.AppendLine("Saldo:       " + completa.Saldo.ToString("N2"));
                UI.Info(sb.ToString(), "Detalle de la compra");
            }
            catch (Exception ex) { UI.Error("No se pudo obtener el detalle.\n\n" + ex.Message); }
        }
    }
}
