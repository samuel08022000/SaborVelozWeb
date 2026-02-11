using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsistenciaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AsistenciaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("ingreso")]
        public async Task<IActionResult> RegistrarIngreso([FromBody] Asistencia modelo)
        {
            var asistencia = new Asistencia
            {
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Fecha = DateTime.UtcNow.Date,
                HoraIngreso = DateTime.UtcNow
            };

            _db.Asistencia.Add(asistencia);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Ingreso registrado correctamente" });
        }

        [HttpPost("salida")]
        public async Task<IActionResult> RegistrarSalida([FromBody] Asistencia modelo)
        {
            // Busca el ingreso de hoy para esa persona que aún no tenga salida
            var hoy = DateTime.UtcNow.Date;
            var asistencia = await _db.Asistencia
                .Where(a => a.Nombre == modelo.Nombre &&
                            a.Apellido == modelo.Apellido &&
                            a.Fecha == hoy &&
                            a.HoraSalida == null)
                .OrderByDescending(a => a.HoraIngreso)
                .FirstOrDefaultAsync();

            if (asistencia == null)
                return BadRequest("No se encontró un registro de ingreso pendiente para hoy.");

            asistencia.HoraSalida = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Salida registrada correctamente" });
        }
    }
}