using System;
using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>
    /// Reglas de negocio para la Facturacion (Cuentas por Cobrar).
    /// Concentra los calculos de subtotal, impuesto y total, la validacion de
    /// inventario y la definicion del saldo segun el tipo de pago.
    /// </summary>
    public class FacturaNegocio
    {
        // Tres DAOs porque facturar cruza tres tablas: la propia factura, los
        // productos (para verificar stock) y el cliente (para el limite de credito).
        private readonly FacturaDAO _dao = new FacturaDAO();
        private readonly ProductoDAO _productoDao = new ProductoDAO();
        private readonly ClienteDAO _clienteDao = new ClienteDAO();

        /// <summary>
        /// Calcula subtotal, impuesto (ITBIS) y total a partir de las lineas de
        /// la factura. Ademas define el Saldo y el Estado segun el tipo de pago:
        /// Contado -> saldo 0 y estado Pagada; Credito -> saldo = total, Pendiente.
        /// </summary>
        /// <param name="factura">Factura con sus lineas ya cargadas; se le rellenan los totales.</param>
        public void CalcularTotales(Factura factura)
        {
            // 'subtotal' acumula el importe de cada linea (cantidad * precio) SIN impuesto.
            decimal subtotal = 0m;
            foreach (DetalleFactura d in factura.Detalles)
                subtotal += d.Importe;   // Importe = lo que vale esa linea

            // Subtotal = suma de lineas.
            factura.Subtotal = subtotal;
            // Impuesto = subtotal * 18%, redondeado a 2 decimales (centavos).
            // Se redondea aqui para que Total cuadre exactamente con lo mostrado.
            factura.Impuesto = Math.Round(subtotal * Configuracion.TasaImpuesto, 2);
            // Total = lo que realmente paga el cliente (base + ITBIS).
            factura.Total = factura.Subtotal + factura.Impuesto;

            // El tipo de pago decide si queda deuda o no:
            if (string.Equals(factura.TipoPago, "Contado", StringComparison.OrdinalIgnoreCase))
            {
                // Contado = paga todo de una vez -> no queda saldo y ya esta pagada.
                factura.Saldo = 0m;
                factura.Estado = "Pagada";
            }
            else
            {
                // Credito = queda a deber el total completo -> saldo = total y pendiente.
                // Ese saldo es la cuenta por cobrar que luego se ira reduciendo con los cobros.
                factura.Saldo = factura.Total;
                factura.Estado = "Pendiente";
            }
        }

        /// <summary>
        /// Crea una factura: valida datos, verifica inventario y limite de credito,
        /// calcula los totales, genera el numero y la persiste. Devuelve el ID.
        /// </summary>
        /// <param name="factura">Factura a crear (cliente, tipo de pago y detalles).</param>
        /// <returns>El ID de la factura recien insertada.</returns>
        public int CrearFactura(Factura factura)
        {
            // --- Validaciones de cabecera ---
            // Sin cliente no se puede facturar.
            if (factura.ClienteID <= 0)
                throw new NegocioException("Debe seleccionar un cliente.");
            // Una factura sin lineas no tiene nada que cobrar.
            if (factura.Detalles == null || factura.Detalles.Count == 0)
                throw new NegocioException("La factura debe tener al menos un producto o servicio.");

            // Verificar existencia de inventario para cada producto
            // Recorremos cada linea comprobando cantidad valida y stock disponible.
            foreach (DetalleFactura d in factura.Detalles)
            {
                if (d.Cantidad <= 0)
                    throw new NegocioException("La cantidad debe ser mayor que cero en todas las lineas.");

                // 'p' es el producto real en la BD; sirve para saber su stock actual.
                Producto p = _productoDao.ObtenerPorId(d.ProductoID);
                if (p == null)
                    throw new NegocioException("Uno de los productos seleccionados no existe.");
                // Solo los bienes fisicos controlan inventario; los servicios no.
                // Si se pide mas de lo disponible, se bloquea la venta.
                if (!p.EsServicio && p.Stock < d.Cantidad)
                    throw new NegocioException("Stock insuficiente de '" + p.Nombre +
                                               "'. Disponible: " + p.Stock + ", solicitado: " + d.Cantidad + ".");
            }

            // Calcular totales y definir saldo/estado
            // (esto ya deja fijados Subtotal, Impuesto, Total, Saldo y Estado).
            CalcularTotales(factura);

            // Validar limite de credito cuando la venta es a credito
            // Al contado no aplica porque el cliente paga en el acto.
            if (string.Equals(factura.TipoPago, "Credito", StringComparison.OrdinalIgnoreCase))
            {
                Cliente cliente = _clienteDao.ObtenerPorId(factura.ClienteID);
                // LimiteCredito > 0 significa que el cliente tiene un tope definido;
                // si es 0 se asume "sin limite configurado" y no se restringe aqui.
                if (cliente != null && cliente.LimiteCredito > 0)
                {
                    // 'nuevoSaldo' = lo que ya debia + lo que sumaria esta factura.
                    decimal nuevoSaldo = cliente.Saldo + factura.Total;
                    // Si al sumar esta venta se pasa del limite, no se permite.
                    if (nuevoSaldo > cliente.LimiteCredito)
                        throw new NegocioException(
                            "La venta excede el limite de credito del cliente. " +
                            "Limite: " + cliente.LimiteCredito.ToString("N2") +
                            ", saldo actual: " + cliente.Saldo.ToString("N2") +
                            ", total factura: " + factura.Total.ToString("N2") + ".");
                }
            }

            // Numero de factura correlativo generado por la capa de datos, y se inserta.
            factura.NumeroFactura = _dao.GenerarNumeroFactura();
            return _dao.Insertar(factura);
        }

        /// <summary>Lista facturas, opcionalmente filtradas por estado (Pagada, Pendiente, etc.).</summary>
        public List<Factura> Listar(string estado = null)
        {
            return _dao.Listar(estado);
        }

        /// <summary>Devuelve las facturas a credito aun con saldo de un cliente (para cobrarlas).</summary>
        public List<Factura> ListarPendientesPorCliente(int clienteID)
        {
            return _dao.ListarPendientesPorCliente(clienteID);
        }

        /// <summary>Trae una factura junto con todas sus lineas de detalle.</summary>
        public Factura ObtenerConDetalle(int facturaID)
        {
            return _dao.ObtenerConDetalle(facturaID);
        }

        /// <summary>Anula una factura (la deja sin efecto) delegando en la capa de datos.</summary>
        public void Anular(int facturaID)
        {
            _dao.Anular(facturaID);
        }
    }
}
