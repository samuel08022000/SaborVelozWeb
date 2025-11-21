using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using SaborVelozWeb.DTOs;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductosController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener lista
        [HttpGet("lista")]
        public IActionResult GetProductos()
        {
            var productos = _db.Productos
                .Select(p => new ProductosDTO
                {
                    IdProducto = p.IdProducto,
                    NombreProducto = p.NombreProducto,
                    Precio = p.Precio,
                    Categoria = p.Categoria,
                    Disponible = p.Estado
                }).ToList();

            return Ok(productos);
        }

        // Crear producto
        [HttpPost("crear")]
        public IActionResult CrearProducto([FromBody] ProductoCrearDTO dto)
        {
            if (dto.Precio <= 0)
                return BadRequest("El precio del producto debe ser un valor positivo.");
            var producto = new Productos
            {
                NombreProducto = dto.NombreProducto,
                Precio = dto.Precio,
                Categoria = "Comida Rápida",   // Valor automático
                Estado = true                  // Siempre disponible al crear
            };

            _db.Productos.Add(producto);
            _db.SaveChanges();

            return Ok("Producto creado correctamente.");
        }

        // Editar producto
        [HttpPut("editar/{id}")]
        public IActionResult EditarProducto(int id, [FromBody] ProductoEditarDTO dto)
        {
            
            var prod = _db.Productos.Find(id);
            if (prod == null)
                return NotFound("Producto no encontrado.");
            if (dto.Precio <= 0)
                return BadRequest("El precio del producto debe ser un valor positivo.");
            prod.NombreProducto = dto.NombreProducto;
            prod.Precio = dto.Precio;
            prod.Estado = dto.Disponible;

            // ⚠ CATEGORIA NO SE EDITA

            _db.SaveChanges();

            return Ok("Producto actualizado.");
        }

        // Eliminar producto
        [HttpDelete("eliminar/{id}")]
        public IActionResult EliminarProducto(int id)
        {
            var prod = _db.Productos.Find(id);
            if (prod == null)
                return NotFound("Producto no encontrado.");

            _db.Productos.Remove(prod);
            _db.SaveChanges();

            return Ok("Producto eliminado.");
        }
    }
}
