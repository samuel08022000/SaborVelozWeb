using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;
using SaborVeloz.DTOs;
using SaborVeloz.Services;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsuariosController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los usuarios
        [HttpGet("lista")]
        public IActionResult GetUsuarios()
        {
            var usuarios = _db.Usuarios
                .Select(u => new UsuariosDTO
                {
                    IdUsuario = u.IdUsuario,
                    Nombre = u.Nombre,
                    Usuario = u.Usuario,
                    Rol = u.Rol
                }).ToList();

            return Ok(usuarios);
        }

        // Registrar usuario
        [HttpPost("crear")]
        public IActionResult Crear([FromBody] UsuarioCreateDTO dto)
        {
            if (_db.Usuarios.Any(u => u.Usuario == dto.Usuario))
                return BadRequest("El usuario ya existe.");

            var hash = AuthService.HashPassword(dto.Password);

            var user = new Usuarios
            {
                Nombre = dto.Nombre,
                Usuario = dto.Usuario,
                ContrasenaHash = hash,
                Rol = dto.Rol
            };

            _db.Usuarios.Add(user);
            _db.SaveChanges();

            return Ok("Usuario creado correctamente.");
        }

        // Editar usuario (excepto contraseña)
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] UsuarioEditDTO dto)
        {
            var user = _db.Usuarios.Find(id);
            if (user == null)
                return NotFound("Usuario no encontrado.");

            user.Nombre = dto.Nombre;
            user.Rol = dto.Rol;

            _db.SaveChanges();

            return Ok("Usuario actualizado.");
        }

        // Eliminar usuario
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            var user = _db.Usuarios.Find(id);
            if (user == null)
                return NotFound("Usuario no encontrado.");

            _db.Usuarios.Remove(user);
            _db.SaveChanges();

            return Ok("Usuario eliminado.");
        }
    }
}
