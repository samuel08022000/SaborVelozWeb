using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using SaborVelozWeb.DTOs;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductosController(AppDbContext db) => _db = db;

        [HttpGet("lista")]
        [Authorize(Roles = "admin,Admin,Administrador,cajero,Cajero")]
        public IActionResult GetProductos()
        {
            var productos = _db.Productos
                .Select(p => new
                {
                    // ⭐ FORZAMOS MINÚSCULAS PARA QUE JS LO LEA BIEN ⭐
                    idProducto = p.IdProducto,
                    nombreProducto = p.NombreProducto,
                    precio = p.Precio,
                    categoria = p.Categoria,
                    disponible = p.Estado
                }).ToList();

            return Ok(productos);
        }

        [HttpPost("crear")]
        [Authorize(Roles = "Administrador")]
        public IActionResult CrearProducto([FromBody] ProductoCrearDTO dto)
        {
            if (dto.Precio <= 0) return BadRequest("El precio debe ser mayor a 0.");
            var producto = new Productos
            {
                NombreProducto = dto.NombreProducto,
                Precio = dto.Precio,
                Categoria = string.IsNullOrEmpty(dto.Categoria) ? "General" : dto.Categoria,
                Estado = true
            };
            _db.Productos.Add(producto);
            _db.SaveChanges();
            return Ok(new { message = "Producto creado" });
        }

        [HttpPut("editar/{id}")]
        [Authorize(Roles = "Administrador")]
        public IActionResult EditarProducto(int id, [FromBody] ProductoEditarDTO dto)
        {
            var prod = _db.Productos.Find(id);
            if (prod == null) return NotFound("No encontrado");
            prod.NombreProducto = dto.NombreProducto;
            prod.Precio = dto.Precio;
            if (!string.IsNullOrEmpty(dto.Categoria)) prod.Categoria = dto.Categoria;
            prod.Estado = dto.Disponible;
            _db.SaveChanges();
            return Ok(new { message = "Actualizado" });
        }

        [HttpDelete("eliminar/{id}")]
        [Authorize(Roles = "Administrador")]
        public IActionResult EliminarProducto(int id)
        {
            var prod = _db.Productos.Find(id);
            if (prod != null) { _db.Productos.Remove(prod); _db.SaveChanges(); }
            return Ok(new { message = "Eliminado" });
        }
    }
}