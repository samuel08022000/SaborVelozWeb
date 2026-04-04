using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;
using SaborVeloz.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsistenciaController : ControllerBase
    {
        private readonly AppDbContext _db;

        // Simulación: Este código debería generarse a diario y mostrarse en la tablet del local.
        // Por ahora lo ponemos fijo para probar, luego el admin lo puede cambiar.
        private readonly string CODIGO_QR_DEL_DIA = "SABOR-VELOZ-SEC-2026";

        public AsistenciaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("registro-qr")]
        public async Task<IActionResult> RegistroMedianteQR([FromBody] EscaneoQrDTO dto)
        {
            // 1. Validar que esté en el local (el QR coincide con el de la pantalla)
            if (dto.CodigoQrEscaneado != CODIGO_QR_DEL_DIA)
                return BadRequest("El código QR no es válido o ha expirado. Por favor, escanea el QR actual del local.");

            var hoyUtc = DateTime.UtcNow.Date;
            var ahoraUtc = DateTime.UtcNow;

            // Buscar si ya tiene un registro abierto hoy
            var registroHoy = await _db.Asistencia
                .FirstOrDefaultAsync(a => a.IdUsuario == dto.IdUsuario && a.Fecha == hoyUtc);

            if (dto.TipoAccion.ToLower() == "entrada")
            {
                if (registroHoy != null)
                    return BadRequest("Ya registraste tu entrada el día de hoy.");

                // Evaluamos si llegó tarde (Ejemplo: Turno empieza a las 09:00 AM hora local)
                // Hacemos el cálculo básico (13:00 UTC = 09:00 Bolivia)
                string estado = ahoraUtc.Hour >= 13 && ahoraUtc.Minute > 15 ? "Atraso" : "Puntual";

                var nuevaAsistencia = new Asistencia
                {
                    IdUsuario = dto.IdUsuario,
                    Fecha = hoyUtc,
                    HoraIngreso = ahoraUtc,
                    EstadoPuntualidad = estado
                };

                _db.Asistencia.Add(nuevaAsistencia);
                await _db.SaveChangesAsync();
                return Ok(new { mensaje = $"Entrada registrada. Estado: {estado}" });
            }
            else if (dto.TipoAccion.ToLower() == "salida")
            {
                if (registroHoy == null)
                    return BadRequest("No puedes marcar salida porque no marcaste entrada hoy.");

                if (registroHoy.HoraSalida != null)
                    return BadRequest("Ya marcaste tu salida previamente.");

                registroHoy.HoraSalida = ahoraUtc;
                _db.Asistencia.Update(registroHoy);
                await _db.SaveChangesAsync();

                return Ok(new { mensaje = "Salida registrada correctamente. ¡Buen descanso!" });
            }

            return BadRequest("Acción no reconocida. Usa 'entrada' o 'salida'.");
        }
    }
}