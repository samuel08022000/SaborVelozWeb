using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
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

        [HttpGet("estado")]
        public async Task<IActionResult> GetEstadoCaja()
        {
            var cajaAbierta = await _context.Caja
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync(c => c.FechaCierre == null);

            if (cajaAbierta != null)
                return Ok(new { abierta = true, idCaja = cajaAbierta.IdCaja, montoInicial = cajaAbierta.MontoInicial });

            return Ok(new { abierta = false });
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCaja([FromBody] CajaDTO dto)
        {
            if (dto.IdUsuario <= 0) return BadRequest("Usuario no válido.");

            var existeAbierta = await _context.Caja.AnyAsync(c => c.IdUsuario == dto.IdUsuario && c.FechaCierre == null);
            if (existeAbierta) return BadRequest("Ya tienes una caja abierta.");

            var nuevaCaja = new Caja
            {
                IdUsuario = dto.IdUsuario,
                MontoInicial = dto.MontoInicial,
                FechaApertura = DateTime.UtcNow,
                Estado = "Abierta"
            };

            _context.Caja.Add(nuevaCaja);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Caja abierta correctamente", idCaja = nuevaCaja.IdCaja });
        }

        // 🔥 AQUI ESTA LA MAGIA DEL ARQUEO CIEGO 🔥
        [HttpPost("cerrar-ciego")]
        public async Task<IActionResult> CerrarCajaCiego([FromBody] CerrarCajaCiegoDTO dto)
        {
            var caja = await _context.Caja
                .Where(c => c.IdUsuario == dto.IdUsuario && c.FechaCierre == null)
                .FirstOrDefaultAsync();

            if (caja == null) return BadRequest("No hay caja abierta.");

            // 1. Calculamos internamente cuánto DEBERÍA haber (Solo sumamos ventas en Efectivo)
            // Asumimos que los pagos digitales (QR/Tarjeta) van directo al banco, no a la gaveta.
            var ventasEfectivo = await _context.Ventas
                .Include(v => v.Pago)
                .Where(v => v.IdCaja == caja.IdCaja && v.Pago.TipoPago.ToLower() == "efectivo")
                .SumAsync(v => v.Total);

            decimal montoEsperado = caja.MontoInicial + ventasEfectivo;

            // 2. Calculamos si sobró o faltó
            decimal diferencia = dto.MontoEfectivoFisico - montoEsperado;

            // 3. Guardamos todo para auditoría
            caja.FechaCierre = DateTime.UtcNow;
            caja.Estado = "Cerrada";
            caja.MontoFinalDeclarado = dto.MontoEfectivoFisico; // Lo que contó el cajero
            caja.MontoCalculadoSistema = montoEsperado;         // Lo que la máquina dice
            caja.Diferencia = diferencia;                       // Faltante o Sobrante

            await _context.SaveChangesAsync();

            string mensajeFinal = diferencia == 0 ? "Caja cuadrada perfectamente." :
                                  diferencia > 0 ? $"Sobrante detectado: Bs {diferencia}" :
                                  $"Faltante detectado: Bs {Math.Abs(diferencia)}";

            return Ok(new
            {
                mensaje = "Turno cerrado.",
                detalle = mensajeFinal,
                diferencia = diferencia
            });
        }
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