using System;
using System.Text.RegularExpressions;

namespace SistemaFacturacion.Negocio
{
    /// <summary>
    /// Excepcion propia usada para los errores de reglas de negocio / validacion.
    /// Permite distinguir en la interfaz un error esperado (dato invalido) de un
    /// error inesperado del sistema.
    /// </summary>
    public class NegocioException : Exception
    {
        public NegocioException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Metodos reutilizables para validar datos de entrada.</summary>
    public static class Validaciones
    {
        /// <summary>Lanza NegocioException si el texto esta vacio o es solo espacios.</summary>
        /// <param name="valor">Texto a comprobar.</param>
        /// <param name="nombreCampo">Nombre del campo para armar el mensaje de error.</param>
        public static void RequerirTexto(string valor, string nombreCampo)
        {
            // IsNullOrWhiteSpace cubre null, "" y cadenas con solo espacios/tabs.
            // Regla: los campos obligatorios (nombre de usuario, etc.) no pueden ir vacios.
            if (string.IsNullOrWhiteSpace(valor))
                throw new NegocioException("El campo '" + nombreCampo + "' es obligatorio.");
        }

        /// <summary>Lanza NegocioException si el valor no es positivo (mayor que cero).</summary>
        /// <param name="valor">Numero a comprobar.</param>
        /// <param name="nombreCampo">Nombre del campo para el mensaje de error.</param>
        public static void RequerirPositivo(decimal valor, string nombreCampo)
        {
            // Se usa para cantidades y montos donde el cero no tiene sentido
            // (por ejemplo el monto de un cobro o la cantidad de una linea).
            if (valor <= 0)
                throw new NegocioException("El campo '" + nombreCampo + "' debe ser mayor que cero.");
        }

        /// <summary>Lanza NegocioException si el valor es negativo.</summary>
        /// <param name="valor">Numero a comprobar.</param>
        /// <param name="nombreCampo">Nombre del campo para el mensaje de error.</param>
        public static void RequerirNoNegativo(decimal valor, string nombreCampo)
        {
            // Aqui el cero SI es valido (ej. precio 0, stock 0), solo se rechazan
            // los valores por debajo de cero, que serian datos incoherentes.
            if (valor < 0)
                throw new NegocioException("El campo '" + nombreCampo + "' no puede ser negativo.");
        }

        /// <summary>Valida un formato de correo electronico basico (si no esta vacio).</summary>
        /// <param name="email">Correo a validar; si viene vacio se considera valido (campo opcional).</param>
        public static void ValidarEmail(string email)
        {
            // El email es opcional: si no se proporciono, no hay nada que validar.
            if (string.IsNullOrWhiteSpace(email)) return;

            // Expresion regular simple: "algo@algo.algo" sin espacios ni arrobas
            // extra. No pretende ser exhaustiva, solo atrapar errores evidentes.
            //   [^@\s]+  -> uno o mas caracteres que no sean @ ni espacio
            //   @        -> la arroba
            //   \.       -> un punto literal antes del dominio
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new NegocioException("El correo electronico '" + email + "' no tiene un formato valido.");
        }
    }
}
