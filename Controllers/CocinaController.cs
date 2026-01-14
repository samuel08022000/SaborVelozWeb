using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using System;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Cocinero,admin,cocinero,Admin, Cocina")]
    public class CocinaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CocinaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Comandas Activas
        [HttpGet("pendientes")]
        public IActionResult GetComandasPendientes()
        {
            // 🔥 Obtenemos la fecha de hoy a las 00:00:00
            var hoy = DateTime.Today;

            var comandas = _context.Comandas
                .Include(c => c.Venta).ThenInclude(v => v.Detalles).ThenInclude(d => d.Producto)
                // 🔥 AGREGAMOS ESTE FILTRO: FechaEnvio debe ser mayor o igual a hoy
                .Where(c => c.FechaEnvio >= hoy)
                .Where(c => c.Estado == "Pendiente" || c.Estado == "En Preparación" || c.Estado == "Listo")
                .OrderBy(c => c.FechaEnvio)
                .Select(c => new ComandasDTO
                {
                    // ... (el resto del select sigue igual) ...
                    IdComanda = c.IdComanda,
                    IdVenta = c.IdVenta,
                    NumeroTicket = c.Venta.NumeroTicket,
                    TipoPedido = c.Venta.TipoPedido,
                    Estado = c.Estado,
                    FechaEnvio = c.FechaEnvio,
                    FechaEntrega = c.FechaEntrega,
                    Productos = c.Venta.Detalles.Select(d => new DetalleComandaDTO
                    {
                        Producto = d.Producto != null ? d.Producto.NombreProducto : "Eliminado",
                        Cantidad = d.Cantidad
                    }).ToList()
                })
                .ToList();

            return Ok(comandas);
        }

        // PUT: Avanzar Estado
        [HttpPut("actualizar/{id}")]
        public IActionResult ActualizarEstado(int id, [FromBody] string nuevoEstado)
        {
            var comanda = _context.Comandas.Find(id);
            if (comanda == null) return NotFound("Comanda no encontrada.");

            // 1. Validar que el estado sea real
            var estadosValidos = new[] { "Pendiente", "En Preparación", "Listo", "Entregado" };
            if (!estadosValidos.Contains(nuevoEstado))
            {
                return BadRequest($"Estado inválido. Opciones: {string.Join(", ", estadosValidos)}");
            }

            // 2. Actualizar
            comanda.Estado = nuevoEstado;

            // 3. Si se completa, marcar hora de entrega (Métricas)
            if (nuevoEstado == "Listo" || nuevoEstado == "Entregado")
            {
                comanda.FechaEntrega = DateTime.Now;
            }

            _context.SaveChanges();
            return Ok(new { message = $"Comanda #{id} ({comanda.Estado}) actualizada correctamente." });
        }
    }
}