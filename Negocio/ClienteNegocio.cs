using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para Clientes (validaciones + CRUD).</summary>
    public class ClienteNegocio
    {
        // Acceso a datos de clientes.
        private readonly ClienteDAO _dao = new ClienteDAO();

        /// <summary>Lista clientes con filtro opcional por texto y por estado activo.</summary>
        public List<Cliente> Listar(bool soloActivos = false, string filtro = null)
        {
            return _dao.Listar(soloActivos, filtro);
        }

        /// <summary>Devuelve un cliente por su ID (o null si no existe).</summary>
        public Cliente ObtenerPorId(int id)
        {
            return _dao.ObtenerPorId(id);
        }

        /// <summary>Valida y crea el cliente. Devuelve el ID generado.</summary>
        public int Crear(Cliente c)
        {
            Validar(c);
            return _dao.Insertar(c);
        }

        /// <summary>Valida y guarda los cambios de un cliente existente.</summary>
        public void Actualizar(Cliente c)
        {
            Validar(c);
            _dao.Actualizar(c);
        }

        /// <summary>Elimina un cliente por su ID.</summary>
        public void Eliminar(int id)
        {
            _dao.Eliminar(id);
        }

        // Reglas del cliente: nombre obligatorio, limite de credito no negativo
        // (0 = cliente sin credito, solo contado) y email con formato valido si se indica.
        private void Validar(Cliente c)
        {
            Validaciones.RequerirTexto(c.Nombre, "Nombre");
            Validaciones.RequerirNoNegativo(c.LimiteCredito, "Limite de credito");
            Validaciones.ValidarEmail(c.Email);
        }
    }
}
