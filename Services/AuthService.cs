using System.Security.Cryptography;
using System.Text;
using SaborVeloz.Data;
using SaborVeloz.Models;

namespace SaborVeloz.Services
{
    public static class AuthService
    {
        // Hashear contraseña
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Validar usuario y contraseña
        public static Usuarios? Login(AppDbContext db, string username, string password)
        {
            var hash = HashPassword(password);
            return db.Usuarios.FirstOrDefault(u => u.Usuario == username && u.ContrasenaHash == hash);
        }

        // Crear usuario nuevo
        public static void CrearUsuario(AppDbContext db, string nombre, string username, string password, string rol)
        {
            var hash = HashPassword(password);
            db.Usuarios.Add(new Usuarios
            {
                Nombre = nombre,
                Usuario = username,
                ContrasenaHash = hash,
                Rol = rol
            });
            db.SaveChanges();
        }
    }
}
