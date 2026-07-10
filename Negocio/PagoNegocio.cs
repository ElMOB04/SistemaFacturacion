using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para los Pagos a proveedores.</summary>
    public class PagoNegocio
    {
        // Acceso a datos de pagos a proveedores.
        private readonly PagoDAO _dao = new PagoDAO();

        /// <summary>Valida y registra un pago a proveedor. Devuelve el ID generado.</summary>
        /// <param name="pago">Pago a aplicar: compra, monto y forma de pago.</param>
        public int Registrar(Pago pago)
        {
            // Espejo del cobro pero del lado de cuentas por pagar:
            // el pago debe apuntar a una compra concreta.
            if (pago.CompraID <= 0)
                throw new NegocioException("Debe seleccionar la compra a pagar.");
            // Monto positivo y forma de pago obligatoria.
            Validaciones.RequerirPositivo(pago.Monto, "Monto");
            Validaciones.RequerirTexto(pago.FormaPago, "Forma de pago");

            // El DAO valida el monto contra el saldo y descuenta la deuda dentro
            // de una transaccion, igual que en los cobros.
            return _dao.Registrar(pago);
        }

        /// <summary>Lista todos los pagos registrados.</summary>
        public List<Pago> Listar()
        {
            return _dao.Listar();
        }
    }
}
