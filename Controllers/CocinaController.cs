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
    [Authorize(Roles = "Administrador,Cocinero,admin,cocinero,Admin")]
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
            var comandas = _context.Comandas
                .Include(c => c.Venta).ThenInclude(v => v.Detalles).ThenInclude(d => d.Producto)
                .Where(c => c.Estado == "Pendiente" || c.Estado == "En Preparación")
                .OrderBy(c => c.FechaEnvio) // Primero las más antiguas (FIFO)
                .Select(c => new ComandasDTO
                {
                    IdComanda = c.IdComanda,
                    IdVenta = c.IdVenta,

                    // Datos visuales clave
                    NumeroTicket = c.Venta.NumeroTicket,
                    TipoPedido = c.Venta.TipoPedido,

                    Estado = c.Estado,
                    FechaEnvio = c.FechaEnvio,
                    FechaEntrega = c.FechaEntrega,

                    Productos = c.Venta.Detalles.Select(d => new DetalleComandaDTO
                    {
                        Producto = d.Producto.NombreProducto,
                        Cantidad = d.Cantidad
                        // Notas = "..." (Futuro)
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