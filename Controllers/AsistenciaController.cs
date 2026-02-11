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
            // 🟢 FLEXIBILIDAD: Eliminamos el "yaIngreso". 
            // Ahora cada vez que den "Ingreso", creamos una nueva fila.

            var asistencia = new Asistencia
            {
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Fecha = DateTime.UtcNow.Date,
                HoraIngreso = DateTime.UtcNow.AddHours(-4) // Hora Bolivia
            };

            _db.Asistencia.Add(asistencia);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Ingreso registrado. ¡A darle con todo!" });
        }

        [HttpPost("salida")]
        public async Task<IActionResult> RegistrarSalida([FromBody] Asistencia modelo)
        {
            var hoy = DateTime.UtcNow.Date;

            // 🟢 LÓGICA DE PUENTES: Buscamos el ÚLTIMO ingreso de hoy que no tenga salida
            var ultimoIngresoAbierto = await _db.Asistencia
                .Where(a => a.Nombre.ToLower() == modelo.Nombre.ToLower() &&
                            a.Apellido.ToLower() == modelo.Apellido.ToLower() &&
                            a.Fecha == hoy &&
                            a.HoraSalida == null)
                .OrderByDescending(a => a.HoraIngreso) // El más reciente primero
                .FirstOrDefaultAsync();

            if (ultimoIngresoAbierto == null)
                return BadRequest("No tienes un ingreso pendiente. Marca entrada primero.");

            ultimoIngresoAbierto.HoraSalida = DateTime.UtcNow.AddHours(-4);

            _db.Asistencia.Update(ultimoIngresoAbierto);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Salida registrada. ¡Buen descanso!" });
        }

    }
}