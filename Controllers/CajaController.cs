using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Cajero")]
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
            var cajaAbierta = _db.Cajas
                .Include(c => c.Usuario)
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefault();

            if (cajaAbierta == null)
                return Ok(new { abierta = false, mensaje = "Caja cerrada" });

            var ventasHoy = _db.Ventas
                .Where(v => v.FechaVenta >= cajaAbierta.FechaApertura)
                .Sum(v => v.Total);

            return Ok(new
            {
                abierta = true,
                idCaja = cajaAbierta.IdCaja,
                montoInicial = cajaAbierta.MontoInicial,
                totalVendido = ventasHoy,
                totalCaja = cajaAbierta.MontoInicial + ventasHoy,
                cajero = cajaAbierta.Usuario != null ? cajaAbierta.Usuario.Nombre : "Desconocido"
            });
        }

        // POST: Abrir Caja
        [HttpPost("abrir")]
        public IActionResult AbrirCaja([FromBody] CajaInputDTO input)
        {
            var existe = _db.Cajas.Any(c => c.Estado == "Abierta");
            if (existe) return BadRequest("La caja ya está abierta.");

            // --- CORRECCIÓN AQUÍ ---
            // Antes buscaba por u.Nombre, ahora busca por u.Usuario (que es el dato que manda el JS)
            var usuarioDb = _db.Usuarios.FirstOrDefault(u => u.Usuario == input.Usuario);

            if (usuarioDb == null)
                return BadRequest($"El usuario '{input.Usuario}' no existe en la base de datos.");

            var nuevaCaja = new Caja
            {
                IdUsuario = usuarioDb.IdUsuario,
                FechaApertura = DateTime.Now,
                MontoInicial = input.MontoInicial,
                MontoFinal = 0,
                Estado = "Abierta"
            };

            _db.Cajas.Add(nuevaCaja);
            _db.SaveChanges();

            return Ok(new { message = "Caja abierta correctamente" });
        }

        // POST: Cerrar Caja
        [HttpPost("cerrar")]
        public IActionResult CerrarCaja()
        {
            var caja = _db.Cajas.FirstOrDefault(c => c.Estado == "Abierta");
            if (caja == null) return BadRequest("No hay ninguna caja abierta para cerrar.");

            var ventasHoy = _db.Ventas
                .Where(v => v.FechaVenta >= caja.FechaApertura)
                .Sum(v => v.Total);

            caja.MontoFinal = caja.MontoInicial + ventasHoy;
            caja.FechaCierre = DateTime.Now;
            caja.Estado = "Cerrada";

            _db.SaveChanges();

            return Ok(new { message = "Caja cerrada. Total final: " + caja.MontoFinal });
        }

        public class CajaInputDTO
        {
            public decimal MontoInicial { get; set; }
            public string Usuario { get; set; } = "";
        }
    }
}