using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para Proveedores (validaciones + CRUD).</summary>
    public class ProveedorNegocio
    {
        // Acceso a datos de proveedores.
        private readonly ProveedorDAO _dao = new ProveedorDAO();

        /// <summary>Lista proveedores con filtro opcional por texto y por estado activo.</summary>
        public List<Proveedor> Listar(bool soloActivos = false, string filtro = null)
        {
            return _dao.Listar(soloActivos, filtro);
        }

        /// <summary>Devuelve un proveedor por su ID (o null si no existe).</summary>
        public Proveedor ObtenerPorId(int id)
        {
            return _dao.ObtenerPorId(id);
        }

        /// <summary>Valida y crea el proveedor. Devuelve el ID generado.</summary>
        public int Crear(Proveedor p)
        {
            Validar(p);
            return _dao.Insertar(p);
        }

        /// <summary>Valida y guarda los cambios de un proveedor existente.</summary>
        public void Actualizar(Proveedor p)
        {
            Validar(p);
            _dao.Actualizar(p);
        }

        /// <summary>Elimina un proveedor por su ID.</summary>
        public void Eliminar(int id)
        {
            _dao.Eliminar(id);
        }

        // Reglas del proveedor: nombre obligatorio y email valido si se indica.
        private void Validar(Proveedor p)
        {
            Validaciones.RequerirTexto(p.Nombre, "Nombre");
            Validaciones.ValidarEmail(p.Email);
        }
    }
}
