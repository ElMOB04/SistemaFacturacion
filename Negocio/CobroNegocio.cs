using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para los Cobros (abonos de clientes).</summary>
    public class CobroNegocio
    {
        // Acceso a datos de cobros.
        private readonly CobroDAO _dao = new CobroDAO();

        /// <summary>Registra un cobro validando el monto. Devuelve el ID del cobro.</summary>
        /// <param name="cobro">Abono del cliente: factura, monto y forma de pago.</param>
        public int Registrar(Cobro cobro)
        {
            // Un cobro debe apuntar a una factura concreta.
            if (cobro.FacturaID <= 0)
                throw new NegocioException("Debe seleccionar la factura a cobrar.");
            // El monto debe ser positivo (no tiene sentido cobrar 0 o negativo).
            Validaciones.RequerirPositivo(cobro.Monto, "Monto");
            // Forma de pago obligatoria (efectivo, transferencia, etc.).
            Validaciones.RequerirTexto(cobro.FormaPago, "Forma de pago");

            // La validacion final del monto contra el saldo se realiza en el DAO,
            // dentro de la transaccion, para garantizar consistencia.
            // (Alli tambien se descuenta el saldo de la factura y del cliente de forma atomica,
            //  evitando que dos cobros simultaneos dejen datos inconsistentes.)
            return _dao.Registrar(cobro);
        }

        /// <summary>Lista todos los cobros registrados.</summary>
        public List<Cobro> Listar()
        {
            return _dao.Listar();
        }
    }
}
