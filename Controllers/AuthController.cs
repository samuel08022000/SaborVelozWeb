using Microsoft.AspNetCore.Mvc;
using SaborVeloz.Data;
using SaborVeloz.Services;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuthController(AppDbContext db) => _db = db;

        // POST: api/Auth/registrar
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] UsuarioRegisterDto dto)
        {
            if (_db.Usuarios.Any(u => u.Usuario == dto.Usuario))
                return BadRequest("El usuario ya existe.");

            AuthService.CrearUsuario(_db, dto.Nombre, dto.Usuario, dto.Password, dto.Rol);
            return Ok("Usuario creado correctamente.");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UsuarioLoginDto dto)
        {
            var hash = AuthService.HashPassword(dto.Password);
            var user = _db.Usuarios.FirstOrDefault(u => u.Usuario == dto.Usuario && u.ContrasenaHash == hash);

            if (user == null)
                return Unauthorized("Credenciales incorrectas.");

            return Ok(new
            {
                message = "Inicio de sesión exitoso.",
                rol = user.Rol,
                nombre = user.Usuario
            });
        }


        // DTOs internos para Auth
        public class UsuarioRegisterDto
        {
            public string Nombre { get; set; } = null!;
            public string Usuario { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string Rol { get; set; } = null!;
        }

        public class UsuarioLoginDto
        {
            public string Usuario { get; set; } = null!;
            public string Password { get; set; } = null!;
        }
    }
}
