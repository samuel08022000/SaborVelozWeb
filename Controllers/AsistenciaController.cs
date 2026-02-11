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
            // Guardamos la fecha y hora en UTC puro (Postgres feliz)
            var asistencia = new Asistencia
            {
                Nombre = modelo.Nombre.Trim(),
                Apellido = modelo.Apellido.Trim(),
                Fecha = DateTime.UtcNow.Date, // Fecha UTC
                HoraIngreso = DateTime.UtcNow  // Hora exacta UTC
            };

            _db.Asistencia.Add(asistencia);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Ingreso registrado correctamente" });
        }

        [HttpPost("salida")]
        public async Task<IActionResult> RegistrarSalida([FromBody] Asistencia modelo)
        {
            var hoy = DateTime.UtcNow.Date;

            // Usamos Trim() y ToLower() para asegurar que encuentre al usuario
            var registro = await _db.Asistencia
                .Where(a => a.Nombre.ToLower().Trim() == modelo.Nombre.ToLower().Trim() &&
                            a.Apellido.ToLower().Trim() == modelo.Apellido.ToLower().Trim() &&
                            a.Fecha == hoy &&
                            a.HoraSalida == null)
                .OrderByDescending(a => a.HoraIngreso)
                .FirstOrDefaultAsync();

            if (registro == null)
                return BadRequest("No se encontró un ingreso pendiente para hoy. ¡Verifica tu nombre!");

            // Guardamos la salida en UTC
            registro.HoraSalida = DateTime.UtcNow;

            _db.Asistencia.Update(registro);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Salida registrada correctamente" });
        }

    }
}