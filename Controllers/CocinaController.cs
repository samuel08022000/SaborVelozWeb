using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;
using SaborVelozWeb;




namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
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
                .Where(c => c.Estado == "Pendiente")
                .OrderByDescending(c => c.FechaEnvio)
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
            _db.SaveChanges();

            return Ok(new { message = $"Estado de comanda #{id} actualizado a '{nuevoEstado}'" });
        }

        // 🔹 Ver comandas completadas
        [HttpGet("completadas")]
        public IActionResult GetCompletadas()
        {
            var comandas = _db.Comandas
                .Include(c => c.Venta)
                .ThenInclude(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(c => c.Estado == "Listo")
                .OrderByDescending(c => c.FechaActualizacion)
                .ToList();

            return Ok(comandas);
        }
    }
}
