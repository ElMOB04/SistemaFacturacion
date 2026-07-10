using System.Collections.Generic;
using SistemaFacturacion.DatosAcceso;
using SistemaFacturacion.Entidades;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Reglas de negocio para Empleados (validaciones + CRUD).</summary>
    public class EmpleadoNegocio
    {
        // Acceso a datos de empleados; la capa de negocio solo valida y delega.
        private readonly EmpleadoDAO _dao = new EmpleadoDAO();

        /// <summary>Lista empleados. Si soloActivos es true, omite los dados de baja.</summary>
        public List<Empleado> Listar(bool soloActivos = false)
        {
            return _dao.Listar(soloActivos);
        }

        /// <summary>Devuelve un empleado por su ID (o null si no existe).</summary>
        public Empleado ObtenerPorId(int id)
        {
            return _dao.ObtenerPorId(id);
        }

        /// <summary>Valida y crea el empleado. Devuelve el ID generado.</summary>
        public int Crear(Empleado e)
        {
            Validar(e);               // primero validar, luego persistir
            return _dao.Insertar(e);
        }

        /// <summary>Valida y guarda los cambios de un empleado existente.</summary>
        public void Actualizar(Empleado e)
        {
            Validar(e);
            _dao.Actualizar(e);
        }

        /// <summary>Elimina un empleado por su ID.</summary>
        public void Eliminar(int id)
        {
            _dao.Eliminar(id);
        }

        // Reglas comunes a crear y actualizar, para no repetir codigo:
        // nombre y apellido obligatorios y, si hay email, que tenga formato valido.
        private void Validar(Empleado e)
        {
            Validaciones.RequerirTexto(e.Nombre, "Nombre");
            Validaciones.RequerirTexto(e.Apellido, "Apellido");
            Validaciones.ValidarEmail(e.Email);
        }
    }
}
