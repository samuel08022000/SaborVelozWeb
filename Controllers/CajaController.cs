using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs; // Asegúrate de tener este namespace o quítalo si usas Models
using SaborVeloz.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CajaController(AppDbContext context)
        {
            _context = context;
        }

        // 1. VERIFICAR ESTADO (¿Ya hay caja abierta hoy?)
        [HttpGet("estado")]
        public async Task<IActionResult> GetEstadoCaja()
        {
            // Buscamos si hay alguna caja abierta (sin fecha de cierre)
            // Opcional: Podrías filtrar por el usuario logueado si quisieras
            var cajaAbierta = await _context.Caja
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync(c => c.FechaCierre == null);

            if (cajaAbierta != null)
            {
                return Ok(new { abierta = true, idCaja = cajaAbierta.IdCaja, montoInicial = cajaAbierta.MontoInicial });
            }
            return Ok(new { abierta = false });
        }

        // 2. ABRIR CAJA
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCaja([FromBody] CajaDTO dto)
        {
            try
            {
                // Validación básica
                if (dto.IdUsuario <= 0)
                    return BadRequest("Error: ID de usuario no válido. Cierra sesión y vuelve a entrar.");

                // Verificar que no tenga ya una abierta
                var existeAbierta = await _context.Caja
                    .AnyAsync(c => c.IdUsuario == dto.IdUsuario && c.FechaCierre == null);

                if (existeAbierta)
                    return BadRequest("Ya tienes una caja abierta.");

                var nuevaCaja = new Caja
                {
                    IdUsuario = dto.IdUsuario,
                    MontoInicial = dto.MontoInicial,
                    FechaApertura = DateTime.Now,
                    FechaCierre = null, // Importante: Null indica que está abierta
                    MontoFinal = 0
                };

                _context.Caja.Add(nuevaCaja);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Caja abierta correctamente", idCaja = nuevaCaja.IdCaja });
            }
            catch (Exception ex)
            {
                // Esto te ayudará a ver el error real en la consola del backend
                Console.WriteLine($"ERROR AL ABRIR CAJA: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");

                return StatusCode(500, "Error interno al abrir caja: " + ex.Message);
            }
        }

        // 3. CERRAR CAJA
        [HttpPost("cerrar")]
        public async Task<IActionResult> CerrarCaja([FromBody] CerrarCajaDTO dto)
        {
            var cajaAbierta = await _context.Caja
                .Where(c => c.IdUsuario == dto.IdUsuario && c.FechaCierre == null)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (cajaAbierta == null)
                return BadRequest("No tienes una caja abierta para cerrar.");

            cajaAbierta.FechaCierre = DateTime.Now;
            cajaAbierta.MontoFinal = dto.MontoCierreCalculado;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Turno cerrado correctamente." });
        }
    }

    // DTOs SIMPLES (Pégalos aquí mismo o en tu carpeta DTOs)
    public class CajaDTO
    {
        public int IdUsuario { get; set; }
        public decimal MontoInicial { get; set; }
    }

    public class CerrarCajaDTO
    {
        public int IdUsuario { get; set; }
        public decimal MontoCierreCalculado { get; set; }
    }
}