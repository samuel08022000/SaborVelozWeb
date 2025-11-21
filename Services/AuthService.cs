using System.Security.Cryptography;
using System.Text;
using SaborVeloz.Data;
using SaborVeloz.Models;
using BCrypt.Net; // <<-- ¡Asegúrate de que este using exista!

namespace SaborVeloz.Services
{
    public static class AuthService
    {
        // 🔑 Método para hashear la contraseña de forma segura con BCrypt.
        public static string HashPassword(string password)
        {
            // BCrypt se encarga automáticamente de generar el 'salt' (sal) y del hashing lento.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // 🔒 Método para validar usuario y contraseña.
        public static Usuarios? Login(AppDbContext db, string username, string password)
        {
            var user = db.Usuarios.FirstOrDefault(u => u.Usuario == username);

            if (user == null)
                return null; // Usuario no existe

            // Compara la contraseña de texto plano (password) con el hash almacenado.
            // BCrypt.Verify realiza la verificación sin riesgo.
            return BCrypt.Net.BCrypt.Verify(password, user.ContrasenaHash) ? user : null;
        }

        // ➕ Método para Crear Usuario (Utiliza el nuevo HashPassword automáticamente)
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