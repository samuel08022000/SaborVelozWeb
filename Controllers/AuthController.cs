using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SaborVeloz.Data;
using SaborVeloz.Services;
using System.Security.Claims;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuthController(AppDbContext db) => _db = db;
        [HttpPost("registrar")]
        //public IActionResult Registrar([FromBody] UsuarioRegisterDto dto)
        //
        //if (_db.Usuarios.Any(u => u.Usuario == dto.Usuario))
          //return BadRequest("El usuario ya existe.");

            // Asumiendo que AuthService.CrearUsuario usa el Hashing BCrypt seguro.
            //AuthService.CrearUsuario(_db, dto.Nombre, dto.Usuario, dto.Password, dto.Rol);
            //re//turn Ok("Usuario creado correctamente.");
        //}
        // POST: api/Auth/registrar
        // [HttpPost("registrar")]
        // public IActionResult Registrar([FromBody] UsuarioRegisterDto dto)
        //{
        // if (_db.Usuarios.Any(u => u.Usuario == dto.Usuario))
        // return BadRequest("El usuario ya existe.");

        //  AuthService.CrearUsuario(_db, dto.Nombre, dto.Usuario, dto.Password, dto.Rol);
        // return Ok("Usuario creado correctamente.");
        // }

        // File: Controllers/AuthController.cs (Método Login)

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto dto)
        {
            var user = AuthService.Login(_db, dto.Usuario, dto.Password);

            if (user == null)
                return Unauthorized("Usuario o contraseña incorrectos.");

            // ⭐ PASO CLAVE: CREAR CLAIMS (Permisos) ⭐
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Usuario!),
        new Claim(ClaimTypes.Role, user.Rol!) // Esto le dice al sistema el Rol
    };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // Mantener la sesión incluso si cierra el navegador
            };

            // ⭐ CREAR LA COOKIE Y AUTENTICAR AL USUARIO ⭐
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok(new
            {
                Message = "Login exitoso",
                Rol = user.Rol,
                Nombre = user.Nombre,  // <--- AGREGADO: Para que el frontend sepa quién es
                Usuario = user.Usuario // <--- AGREGADO: Por si acaso
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
