using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaFacturacion.Entidades;
using SistemaFacturacion.Negocio;

namespace SistemaFacturacion.Presentacion
{
    /// <summary>
    /// Mantenimiento (CRUD) de Clientes. Permite listar, buscar, crear, editar y
    /// eliminar clientes. La pantalla se divide en: tabla al centro con el listado,
    /// panel derecho con el formulario de datos y barra superior de busqueda.
    /// </summary>
    public class FrmClientes : Form
    {
        // ---------- Controles (campos miembro) ----------
        private DataGridView grid; // tabla que muestra el listado de clientes
        // Cajas de texto del formulario: buscador y los datos de cada cliente.
        private TextBox txtBuscar, txtNombre, txtIdent, txtTelefono, txtEmail, txtDireccion, txtLimite, txtSaldo;
        private CheckBox chkActivo;                    // indica si el cliente esta activo o dado de baja
        private Button btnNuevo, btnGuardar, btnEliminar; // botones de accion del CRUD

        // Puente hacia la capa de Negocio: valida y realiza las operaciones sobre la BD.
        private readonly ClienteNegocio _negocio = new ClienteNegocio();

        // Guarda el ID del cliente seleccionado en la tabla. Es la variable clave del CRUD:
        //   _idSeleccionado == 0  -> no hay seleccion: al guardar se CREA un cliente nuevo.
        //   _idSeleccionado != 0  -> hay un cliente elegido: al guardar se ACTUALIZA ese registro.
        private int _idSeleccionado = 0;

        /// <summary>Constructor: pone el titulo, construye la interfaz y carga el listado inicial.</summary>
        public FrmClientes()
        {
            Text = "Clientes";
            ConstruirUI();
            Cargar();
        }

        /// <summary>
        /// Construye por codigo toda la interfaz: el titulo, el panel derecho con las
        /// cajas de datos, la barra superior de busqueda con el boton Eliminar y la
        /// tabla central con sus columnas.
        /// </summary>
        private void ConstruirUI()
        {
            BackColor = UI.ColorFondo;
            Font = UI.FuenteNormal;

            Label titulo = new Label
            {
                Text = "Gestion de Clientes",
                Font = UI.FuenteTitulo,
                ForeColor = UI.ColorPrimario,
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 8, 0, 0)
            };
            Controls.Add(titulo);

            // ---------- Panel derecho: formulario de datos ----------
            Panel panelForm = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(14), BackColor = Color.White };
            // 'y' es un contador vertical: los helpers Etiqueta() y Caja() lo van
            // incrementando para ir apilando cada control debajo del anterior.
            int y = 10;
            panelForm.Controls.Add(Etiqueta("Nombre / Razon social:", ref y));
            txtNombre = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Identificacion (Cedula/RNC):", ref y));
            txtIdent = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Telefono:", ref y));
            txtTelefono = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Correo electronico:", ref y));
            txtEmail = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Direccion:", ref y));
            txtDireccion = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Limite de credito:", ref y));
            txtLimite = Caja(ref y, panelForm);
            panelForm.Controls.Add(Etiqueta("Saldo pendiente (solo lectura):", ref y));
            txtSaldo = Caja(ref y, panelForm);
            // El saldo pendiente lo calcula el sistema (facturas menos cobros), por eso
            // es de solo lectura y se pinta en gris: el usuario no debe editarlo a mano.
            txtSaldo.ReadOnly = true;
            txtSaldo.BackColor = Color.FromArgb(240, 240, 240);

            chkActivo = new CheckBox { Text = "Activo", Location = new Point(14, y), Checked = true, AutoSize = true };
            panelForm.Controls.Add(chkActivo);
            y += 36;

            // Boton Guardar (verde): crea o actualiza segun _idSeleccionado.
            btnGuardar = new Button { Text = "Guardar", Location = new Point(14, y), Width = 140 };
            UI.EstiloBoton(btnGuardar, UI.ColorExito);
            btnGuardar.Click += (s, e) => Guardar();
            panelForm.Controls.Add(btnGuardar);

            // Boton Nuevo (azul): limpia el formulario para empezar un registro desde cero.
            btnNuevo = new Button { Text = "Nuevo", Location = new Point(162, y), Width = 128 };
            UI.EstiloBoton(btnNuevo, UI.ColorSecundario);
            btnNuevo.Click += (s, e) => LimpiarFormulario();
            panelForm.Controls.Add(btnNuevo);

            Controls.Add(panelForm);

            // ---------- Panel superior: busqueda ----------
            Panel panelTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 8) };
            Label lblB = new Label { Text = "Buscar:", Location = new Point(12, 14), AutoSize = true };
            txtBuscar = new TextBox { Location = new Point(70, 11), Width = 240 };
            // Busqueda en vivo: cada vez que cambia el texto se recarga la tabla filtrando.
            txtBuscar.TextChanged += (s, e) => Cargar();
            // Boton Eliminar (rojo): borra el cliente seleccionado en la tabla.
            btnEliminar = new Button { Text = "Eliminar", Location = new Point(330, 8), Width = 120 };
            UI.EstiloBoton(btnEliminar, UI.ColorPeligro);
            btnEliminar.Click += (s, e) => Eliminar();
            panelTop.Controls.Add(lblB);
            panelTop.Controls.Add(txtBuscar);
            panelTop.Controls.Add(btnEliminar);
            Controls.Add(panelTop);

            // ---------- Tabla ----------
            grid = new DataGridView { Dock = DockStyle.Fill };
            UI.EstiloGrid(grid);
            grid.AutoGenerateColumns = false; // definimos las columnas a mano para controlar cuales se ven y su formato
            // Cada columna se enlaza a una propiedad de la entidad Cliente (DataPropertyName).
            grid.Columns.Add(Col("ClienteID", "ID", 50));
            grid.Columns.Add(Col("Nombre", "Nombre", 180));
            grid.Columns.Add(Col("Identificacion", "Identificacion", 110));
            grid.Columns.Add(Col("Telefono", "Telefono", 100));
            grid.Columns.Add(ColMoneda("LimiteCredito", "Limite")); // columna con formato de moneda
            grid.Columns.Add(ColMoneda("Saldo", "Saldo"));
            grid.Columns.Add(Col("Activo", "Activo", 60));
            // Al cambiar la fila seleccionada, se vuelca ese cliente al formulario.
            grid.SelectionChanged += (s, e) => MostrarSeleccion();
            Controls.Add(grid);

            // Orden visual correcto (Fill primero, luego los Top/Right)
            grid.BringToFront();
        }

        // ----- Helpers de construccion -----
        /// <summary>Crea una etiqueta en la posicion vertical actual y avanza 'y' 20 px para el siguiente control.</summary>
        private Label Etiqueta(string texto, ref int y)
        {
            Label l = new Label { Text = texto, Location = new Point(14, y), AutoSize = true, ForeColor = UI.ColorTexto };
            y += 20;
            return l;
        }
        /// <summary>Crea una caja de texto, la agrega al panel y avanza 'y' 32 px. Devuelve la caja para guardarla en su campo.</summary>
        private TextBox Caja(ref int y, Panel p)
        {
            TextBox t = new TextBox { Location = new Point(14, y), Width = 276 };
            p.Controls.Add(t);
            y += 32;
            return t;
        }
        /// <summary>Crea una columna de texto de la tabla enlazada a la propiedad 'prop' de la entidad.</summary>
        private DataGridViewTextBoxColumn Col(string prop, string titulo, int ancho)
        {
            return new DataGridViewTextBoxColumn { DataPropertyName = prop, HeaderText = titulo, FillWeight = ancho };
        }
        /// <summary>Igual que Col pero con formato de moneda: dos decimales ("N2") y alineado a la derecha.</summary>
        private DataGridViewTextBoxColumn ColMoneda(string prop, string titulo)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop, HeaderText = titulo, FillWeight = 90
            };
            c.DefaultCellStyle.Format = "N2";
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }

        // ----- Logica -----
        /// <summary>
        /// Carga (o recarga) la tabla con los clientes. Pide a la capa de negocio la
        /// lista, aplicando el filtro escrito en el buscador. Si falla la consulta,
        /// muestra un error tecnico.
        /// </summary>
        private void Cargar()
        {
            try
            {
                // El primer parametro (false) indica que NO se filtre solo por activos: se traen todos.
                // El segundo es el texto de busqueda (o null si aun no se ha creado la caja de busqueda).
                grid.DataSource = _negocio.Listar(false, txtBuscar != null ? txtBuscar.Text : null);
            }
            catch (Exception ex)
            {
                UI.Error("No se pudieron cargar los clientes.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Se dispara al elegir una fila de la tabla. Toma el cliente enlazado a esa
        /// fila y vuelca todos sus datos al formulario de la derecha, guardando su ID
        /// en _idSeleccionado para que el proximo Guardar sepa que es una edicion.
        /// </summary>
        private void MostrarSeleccion()
        {
            // Si no hay fila valida o el objeto enlazado no es un Cliente, no se hace nada (evita errores).
            if (grid.CurrentRow == null || !(grid.CurrentRow.DataBoundItem is Cliente)) return;
            Cliente c = (Cliente)grid.CurrentRow.DataBoundItem; // el cliente de la fila seleccionada
            _idSeleccionado = c.ClienteID; // se recuerda su ID -> modo edicion
            txtNombre.Text = c.Nombre;
            txtIdent.Text = c.Identificacion;
            txtTelefono.Text = c.Telefono;
            txtEmail.Text = c.Email;
            txtDireccion.Text = c.Direccion;
            txtLimite.Text = c.LimiteCredito.ToString("N2"); // se formatea el numero con 2 decimales
            txtSaldo.Text = c.Saldo.ToString("N2");
            chkActivo.Checked = c.Activo;
        }

        /// <summary>
        /// Deja el formulario en blanco para dar de alta un cliente nuevo. Vuelve a
        /// poner _idSeleccionado en 0 (modo "crear") y coloca el cursor en el nombre.
        /// </summary>
        private void LimpiarFormulario()
        {
            _idSeleccionado = 0; // 0 = no hay cliente elegido -> el proximo Guardar CREARA uno nuevo
            txtNombre.Text = txtIdent.Text = txtTelefono.Text = txtEmail.Text = txtDireccion.Text = "";
            txtLimite.Text = "0.00";
            txtSaldo.Text = "0.00";
            chkActivo.Checked = true;
            txtNombre.Focus(); // comodidad: el foco queda listo para escribir el nombre
        }

        /// <summary>
        /// Guarda el cliente del formulario. Valida que el limite de credito sea un
        /// numero, arma la entidad Cliente con los datos y, segun _idSeleccionado,
        /// llama a Crear (si es 0) o a Actualizar (si trae un ID). Al terminar limpia
        /// y recarga la tabla. Distingue errores de negocio (advertencia) de los tecnicos (error).
        /// </summary>
        private void Guardar()
        {
            try
            {
                // Validacion en pantalla: el texto del limite debe poder convertirse a decimal.
                decimal limite;
                if (!decimal.TryParse(txtLimite.Text, out limite))
                    throw new NegocioException("El limite de credito debe ser un valor numerico.");

                // Se arma la entidad con lo escrito. Trim() quita espacios sobrantes al inicio/fin.
                Cliente c = new Cliente
                {
                    ClienteID = _idSeleccionado,
                    Nombre = txtNombre.Text.Trim(),
                    Identificacion = txtIdent.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    LimiteCredito = limite,
                    Activo = chkActivo.Checked
                };

                // Aqui se decide si es alta o edicion segun el ID recordado.
                if (_idSeleccionado == 0)
                {
                    _negocio.Crear(c); // no habia seleccion: se inserta un cliente nuevo
                    UI.Info("Cliente registrado correctamente.");
                }
                else
                {
                    _negocio.Actualizar(c); // habia un cliente elegido: se modifica ese registro
                    UI.Info("Cliente actualizado correctamente.");
                }
                LimpiarFormulario(); // se deja listo el formulario para el siguiente registro
                Cargar();            // se refresca la tabla para ver el cambio
            }
            catch (NegocioException nex)
            {
                // Regla de negocio incumplida (ej. falta el nombre, identificacion duplicada):
                // se muestra como advertencia amigable, no como error del programa.
                UI.Advertencia(nex.Message);
            }
            catch (Exception ex)
            {
                // Fallo tecnico inesperado (ej. sin conexion a la base de datos).
                UI.Error("No se pudo guardar el cliente.\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Elimina el cliente seleccionado. Antes verifica que haya uno elegido y pide
        /// confirmacion. Si el borrado falla (por ejemplo, porque tiene facturas
        /// asociadas), avisa con un mensaje claro.
        /// </summary>
        private void Eliminar()
        {
            // Sin seleccion no hay nada que borrar: se avisa y se sale.
            if (_idSeleccionado == 0)
            {
                UI.Advertencia("Seleccione un cliente de la tabla para eliminarlo.");
                return;
            }
            // Confirmacion de seguridad: si el usuario dice "No", se cancela.
            if (!UI.Confirmar("Esta seguro de eliminar el cliente seleccionado?")) return;
            try
            {
                _negocio.Eliminar(_idSeleccionado);
                UI.Info("Cliente eliminado.");
                LimpiarFormulario();
                Cargar();
            }
            catch (Exception ex)
            {
                // Suele fallar por integridad referencial: el cliente tiene facturas ligadas.
                UI.Error("No se pudo eliminar el cliente. Es posible que tenga facturas asociadas.\n\n" + ex.Message);
            }
        }
    }
}
