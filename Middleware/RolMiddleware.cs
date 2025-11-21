using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using SaborVeloz.Services;
using System.Linq;

namespace SaborVeloz.Middleware
{
    public class RolMiddleware
    {
        private readonly RequestDelegate _next;

        public RolMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Evitar proteger /api/auth/*
            var ruta = context.Request.Path.ToString().ToLower();
            if (ruta.StartsWith("/api/auth"))
            {
                await _next(context);
                return;
            }

            // Leer token del header
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Falta token de autorización.");
                return;
            }

            var partes = authHeader.ToString().Split(" ");
            if (partes.Length != 2 || partes[0] != "Bearer")
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Formato de token inválido.");
                return;
            }

            var token = partes[1];

            if (!SesionService.ValidarToken(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token inválido o expirado.");
                return;
            }

            // Guardar rol en HttpContext
            var rol = SesionService.ObtenerRol(token);
            context.Items["RolUsuario"] = rol;

            await _next(context);
        }
    }
}
