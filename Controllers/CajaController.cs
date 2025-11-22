using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
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
            // 1. Buscar la última caja que esté ABIERTA
            var cajaAbierta = _db.Caja
                .Include(c => c.Usuario) // Cargar datos del usuario
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefault();

            if (cajaAbierta == null)
            {
                // Si no hay caja abierta, devolvemos estado cerrado
                return Ok(new { abierta = false, mensaje = "Caja cerrada" });
            }

            // 2. Calcular ventas realizadas DESPUÉS de que se abrió esa caja
            var fechaApertura = cajaAbierta.FechaApertura;

            var ventasHoy = _db.Ventas
                .Where(v => v.FechaVenta >= fechaApertura)
                .Sum(v => v.Total);

            // 3. Devolver el estado completo
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
            // Validar si ya existe una caja abierta
            if (_db.Caja.Any(c => c.Estado == "Abierta"))
            {
                return BadRequest("La caja ya se encuentra abierta. Debe cerrarla primero.");
            }

            // ⭐ TRUCO ANTIFALLOS: Usar variable local para la búsqueda ⭐
            string usuarioBuscado = input.Usuario;

            var usuarioDb = _db.Usuarios.FirstOrDefault(u => u.Usuario == usuarioBuscado);

            if (usuarioDb == null)
            {
                return BadRequest($"No se encontró el usuario '{usuarioBuscado}'.");
            }

            var nuevaCaja = new Caja
            {
                IdUsuario = usuarioDb.IdUsuario,
                FechaApertura = DateTime.Now,
                MontoInicial = input.MontoInicial,
                MontoFinal = 0, // Se calcula al cerrar
                Estado = "Abierta"
            };

            _db.Caja.Add(nuevaCaja);
            _db.SaveChanges();

            return Ok(new { message = "Caja abierta correctamente" });
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