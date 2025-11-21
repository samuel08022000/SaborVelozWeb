using System.Collections.Concurrent;

namespace SaborVeloz.Services
{
    public static class SesionService
    {
        // Guardamos token → rol del usuario
        private static readonly ConcurrentDictionary<string, string> Tokens = new();

        // Registrar (al hacer login)
        public static void RegistrarSesion(string token, string rol)
        {
            Tokens[token] = rol;
        }

        // Verifica si el token existe
        public static bool ValidarToken(string token)
        {
            return Tokens.ContainsKey(token);
        }

        // Obtiene el rol del token
        public static string? ObtenerRol(string token)
        {
            return Tokens.TryGetValue(token, out var rol) ? rol : null;
        }
    }
}
