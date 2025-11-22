using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;
using System;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cocina,Administrador")]
    public class CocinaController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CocinaController(AppDbContext db) => _db = db;

        // 🔹 Ver todas las comandas pendientes
        [HttpGet("pendientes")]
        public IActionResult GetPendientes()
        {
            var comandas = _db.Comandas
                .Include(c => c.Venta)
                .ThenInclude(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(c => c.Estado != "Listo") // Traemos todo lo que NO esté listo
                .OrderBy(c => c.FechaEnvio)      // Las más antiguas primero (FIFO)
                .ToList();

            return Ok(comandas);
        }

        // 🔹 Cambiar estado de una comanda
        [HttpPut("actualizar-estado/{id}")]
        public IActionResult ActualizarEstado(int id, [FromBody] string nuevoEstado)
        {
            var comanda = _db.Comandas.FirstOrDefault(c => c.IdComanda == id);
            if (comanda == null)
                return NotFound("Comanda no encontrada.");

            comanda.Estado = nuevoEstado;

            // MEJORA CLAVE: Si el estado es "Listo", guardamos la fecha de entrega
            if (nuevoEstado == "Listo")
            {
                comanda.FechaEntrega = DateTime.Now;
            }

            _db.SaveChanges();

            return Ok(new { message = $"Estado actualizado a '{nuevoEstado}'" });
        }

        // 🔹 Ver comandas completadas (Historial)
        [HttpGet("completadas")]
        public IActionResult GetCompletadas()
        {
            var comandas = _db.Comandas
                .Include(c => c.Venta)
                .ThenInclude(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(c => c.Estado == "Listo")
                // CORRECCIÓN: Ordenar por la columna correcta 'FechaEntrega'
                .OrderByDescending(c => c.FechaEntrega)
                .Take(50) // Opcional: Traer solo las últimas 50 para no saturar
                .ToList();

            return Ok(comandas);
        }
    }
}