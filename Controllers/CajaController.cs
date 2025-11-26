using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models; // Asegúrate de que este namespace sea correcto
using System;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Cajero,admin,cajero,Admin")] // Roles blindados
    public class CajaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CajaController(AppDbContext db)
        {
            _db = db;
        }

        // GET: Estado de la caja
        [HttpGet("estado")]
        public IActionResult GetEstadoCaja()
        {
            // Buscamos la última caja ABIERTA
            var cajaAbierta = _db.Caja
                .Include(c => c.Usuario)
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefault();

            if (cajaAbierta == null)
            {
                // IMPORTANTE: Devolvemos 'abierta: false' para que el JS sepa que debe pedir apertura
                return Ok(new { abierta = false, mensaje = "Caja cerrada" });
            }

            // Calculamos ventas del turno actual
            var ventasTurno = _db.Ventas
                .Where(v => v.FechaVenta >= cajaAbierta.FechaApertura)
                .Sum(v => v.Total);

            return Ok(new
            {
                abierta = true,
                idCaja = cajaAbierta.IdCaja,
                montoInicial = cajaAbierta.MontoInicial,
                totalVendido = ventasTurno,
                totalCaja = cajaAbierta.MontoInicial + ventasTurno,
                cajero = cajaAbierta.Usuario?.Nombre ?? "Desconocido",
                fechaApertura = cajaAbierta.FechaApertura
            });
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCaja([FromBody] CajaDTO cajaDto)
        {
            // 1. Validar que llegan datos
            if (cajaDto == null) return BadRequest("Datos inválidos.");

            // 2. Validar que no haya una caja ya abierta por este usuario (o en general, según tu regla)
            // Usamos 'IdUsuario' y 'Estado' como string, que es lo que tienes en tu modelo.
            var cajaAbierta = await _db.Caja // Ojo: _db o _context según como lo hayas inyectado arriba
                .AnyAsync(c => c.Estado == "Abierta" && c.IdUsuario == cajaDto.IdUsuario);

            if (cajaAbierta)
            {
                return BadRequest("¡Ya tienes una caja abierta! Debes cerrarla antes de abrir otra.");
            }

            // 3. Crear la nueva caja con los nombres CORRECTOS de tu modelo
            var nuevaCaja = new Caja
            {
                IdUsuario = cajaDto.IdUsuario,       // Corrección: IdUsuario
                MontoInicial = cajaDto.MontoInicial, // Corrección: MontoInicial
                FechaApertura = DateTime.Now,
                Estado = "Abierta",
                MontoFinal = 0 // Inicializamos en 0
            };

            _db.Caja.Add(nuevaCaja);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Caja abierta exitosamente",
                idCaja = nuevaCaja.IdCaja // Corrección: IdCaja (no Id)
            });
        }

        // POST: Cerrar Caja
        [HttpPost("cerrar")]
        public IActionResult CerrarCaja()
        {
            // Buscar la caja abierta
            var caja = _db.Caja.FirstOrDefault(c => c.Estado == "Abierta");

            if (caja == null)
            {
                return BadRequest("No hay ninguna caja abierta para cerrar.");
            }

            // Calcular total de ventas del turno
            var ventasTurno = _db.Ventas
                .Where(v => v.FechaVenta >= caja.FechaApertura)
                .Sum(v => v.Total);

            // Actualizar datos de cierre
            caja.MontoFinal = caja.MontoInicial + ventasTurno;
            caja.FechaCierre = DateTime.Now;
            caja.Estado = "Cerrada";

            _db.SaveChanges();

            return Ok(new { message = "Caja cerrada exitosamente. Total final: " + caja.MontoFinal });
        }

        // DTO auxiliar simple
        public class CajaInputDTO
        {
            public decimal MontoInicial { get; set; }
            public string Usuario { get; set; } = "";
        }
    }
}