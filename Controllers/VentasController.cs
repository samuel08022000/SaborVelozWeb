using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using SaborVeloz.Services;
using SaborVelozWeb;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Cajero")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Obtener todas las ventas con detalles
        [HttpGet("todas")]
        public IActionResult GetVentas()
        {
            var ventas = _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Select(v => new VentaDTO
                {
                    IdVenta = v.IdVenta,
                    Cajero = v.Usuario.Nombre,
                    MetodoPago = v.Pago.TipoPago,
                    FechaVenta = v.FechaVenta,
                    Total = v.Total,
                    Detalles = v.Detalles.Select(d => new DetalleVentaDTO
                    {
                        IdProducto = d.IdProducto,
                        Producto = d.Producto.NombreProducto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList()
                })
                .OrderByDescending(v => v.FechaVenta)
                .ToList();

            return Ok(ventas);
        }

        // 🔹 Registrar una nueva venta (simplificado)
        [HttpPost("registrar")]
        public IActionResult RegistrarVenta([FromBody] VentaRegistroDTO ventaDto)
        {
            if (ventaDto == null || ventaDto.Productos == null || !ventaDto.Productos.Any())
                return BadRequest("Debe incluir al menos un producto en la venta.");

            try
            {
                // 1️⃣ Buscar usuario (cajero)
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Nombre == ventaDto.Cajero);
                if (usuario == null)
                    return NotFound("Cajero no encontrado.");

                // 2️⃣ Buscar método de pago
                var pago = _context.Pagos.FirstOrDefault(p => p.TipoPago == ventaDto.MetodoPago);
                if (pago == null)
                    return NotFound("Método de pago no encontrado.");

                // 3️⃣ Agrupar productos repetidos y calcular precios automáticamente
                var detallesVenta = new List<DetalleVenta>();
                decimal totalVenta = 0;

                var productosAgrupados = ventaDto.Productos
                    .GroupBy(p => p.IdProducto)
                    .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(x => x.Cantidad) });

                foreach (var item in productosAgrupados)
                {
                    var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == item.IdProducto);
                    if (producto == null)
                        return NotFound($"Producto con ID {item.IdProducto} no encontrado.");
                    if (item.Cantidad <= 0)
                        return BadRequest($"La cantidad total del producto ID {item.IdProducto} debe ser positiva.");
                    var subtotal = producto.Precio * item.Cantidad;
                    totalVenta += subtotal;

                    detallesVenta.Add(new DetalleVenta
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.Precio
                    });

                }

                // 4️⃣ Crear la venta con total calculado
                var venta = new Ventas
                {
                    IdUsuario = usuario.IdUsuario,
                    IdPago = pago.IdPago,
                    FechaVenta = DateTime.Now,
                    Total = totalVenta,
                    Detalles = detallesVenta
                };

                _context.Ventas.Add(venta);
                _context.SaveChanges();

                // 5️⃣ Generar automáticamente la comanda
                var comanda = new Comandas
                {
                    IdVenta = venta.IdVenta,
                    Estado = "Pendiente",
                    FechaEnvio = DateTime.Now
                };

                _context.Comandas.Add(comanda);
                _context.SaveChanges();

                // 6️⃣ Actualizar reportes
                ReportesService.ActualizarReportes(_context, venta);

                return Ok(new
                {
                    message = "Venta y comanda registradas correctamente.",
                    idVenta = venta.IdVenta,
                    total = totalVenta,
                    estadoComanda = comanda.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al registrar la venta: {ex.Message}");
            }
        }
    }
}
