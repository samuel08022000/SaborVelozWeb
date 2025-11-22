using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using SaborVelozWeb.DTOs;
// using SaborVelozWeb.DTOs; // Eliminado si no se usa, usamos SaborVeloz.DTOs

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductosController(AppDbContext db)
        {
            _db = db;
        }

        // 1. Obtener lista (Visible para Admin y Cajero)
        [HttpGet("lista")]
        [Authorize(Roles = "Administrador,Cajero")]
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

        // 2. Crear producto (Solo Admin)
        [HttpPost("crear")]
        [Authorize(Roles = "Administrador")]
        public IActionResult CrearProducto([FromBody] ProductoCrearDTO dto)
        {
            if (dto.Precio <= 0)
                return BadRequest("El precio debe ser mayor a 0.");

            var producto = new Productos
            {
                NombreProducto = dto.NombreProducto,
                Precio = dto.Precio,
                // Usamos la categoría del DTO, o "General" si viene nula
                Categoria = string.IsNullOrEmpty(dto.Categoria) ? "General" : dto.Categoria,
                Estado = true
            };

            _db.Productos.Add(producto);
            _db.SaveChanges();

            return Ok(new { message = "Producto creado correctamente." });
        }

        // 3. Editar producto (Solo Admin)
        [HttpPut("editar/{id}")]
        [Authorize(Roles = "Administrador")]
        public IActionResult EditarProducto(int id, [FromBody] ProductoEditarDTO dto)
        {
            var prod = _db.Productos.Find(id);
            if (prod == null)
                return NotFound("Producto no encontrado.");

            if (dto.Precio <= 0)
                return BadRequest("El precio debe ser mayor a 0.");

            // Actualizamos los campos
            prod.NombreProducto = dto.NombreProducto;
            prod.Precio = dto.Precio;
            prod.Estado = dto.Disponible;

            // Ahora sí permitimos editar la categoría si se envía
            if (!string.IsNullOrEmpty(dto.Categoria))
            {
                prod.Categoria = dto.Categoria;
            }

            _db.SaveChanges();

            return Ok(new { message = "Producto actualizado correctamente." });
        }

        // 4. Eliminar producto (Solo Admin)
        [HttpDelete("eliminar/{id}")]
        [Authorize(Roles = "Administrador")]
        public IActionResult EliminarProducto(int id)
        {
            var prod = _db.Productos.Find(id);
            if (prod == null)
                return NotFound("Producto no encontrado.");

            _db.Productos.Remove(prod);
            _db.SaveChanges();

            return Ok(new { message = "Producto eliminado correctamente." });
        }
    }
}