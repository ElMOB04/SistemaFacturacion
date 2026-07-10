using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaFacturacion.Negocio
{
    /// <summary>Utilidades de seguridad: hashing de contrasenas con SHA-256.</summary>
    public static class Seguridad
    {
        /// <summary>
        /// Devuelve el hash SHA-256 (en hexadecimal minusculas) de un texto.
        /// Asi las contrasenas nunca se almacenan en texto plano.
        /// </summary>
        /// <param name="texto">Texto plano a proteger (normalmente la contrasena).</param>
        /// <returns>Cadena hexadecimal de 64 caracteres que representa el hash.</returns>
        public static string CalcularHashSHA256(string texto)
        {
            // Si llega null lo tratamos como cadena vacia para no romper el calculo.
            if (texto == null) texto = string.Empty;

            // 'using' asegura que el algoritmo libere sus recursos al terminar.
            using (SHA256 sha = SHA256.Create())
            {
                // 1) Convertimos el texto a bytes usando UTF-8 y calculamos el
                //    hash: 'bytes' es un arreglo de 32 bytes (256 bits) irreversible.
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));

                // 2) 'sb' va acumulando la representacion legible del hash.
                StringBuilder sb = new StringBuilder();

                // 3) Cada byte se escribe como 2 digitos hexadecimales ("x2" =
                //    minusculas, con cero a la izquierda). 32 bytes -> 64 caracteres.
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));

                // La misma entrada produce siempre el mismo hash, por eso al
                // autenticar comparamos hashes en lugar de contrasenas en claro.
                return sb.ToString();
            }
        }
    }
}
