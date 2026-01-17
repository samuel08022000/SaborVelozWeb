using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using SaborVeloz.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Cajero,admin,cajero,Admin")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // POST: Registrar una nueva venta
        [HttpPost("registrar")]
        public IActionResult RegistrarVenta([FromBody] VentaRegistroDTO ventaDto)
        {
            // 1. Validaciones básicas de entrada
            if (ventaDto == null || ventaDto.Productos == null || !ventaDto.Productos.Any())
                return BadRequest("Debe incluir al menos un producto en la venta.");

            // 2. Validar y Normalizar Tipo de Pedido
            var tipoNormalizado = ventaDto.TipoPedido?.Trim();
            if (string.IsNullOrEmpty(tipoNormalizado) || (tipoNormalizado != "Local" && tipoNormalizado != "Llevar"))
            {
                return BadRequest("El tipo de pedido debe ser 'Local' o 'Llevar'.");
            }

            // Usamos transacción para garantizar integridad
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 3. Validar Usuario (Cajero)
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Usuario == ventaDto.Usuario);
                if (usuario == null) return NotFound($"Cajero con usuario '{ventaDto.Usuario}' no encontrado.");

                // 4. Validar Caja Abierta
                var cajaAbierta = _context.Caja.FirstOrDefault(c => c.Estado == "Abierta");
                if (cajaAbierta == null)
                    return BadRequest("❌ NO SE PUEDE VENDER: No hay ninguna caja abierta. Por favor, abra turno primero.");

                // 5. Validar Método de Pago
                var pago = _context.Pagos.FirstOrDefault(p => p.TipoPago == ventaDto.MetodoPago);
                if (pago == null) return NotFound($"Método de pago '{ventaDto.MetodoPago}' no encontrado.");

                // 6. 🌟 CORRECCIÓN DE FECHA (UTC) 🌟
                // Usamos UTC para la base de datos
                var fechaAhoraUtc = DateTime.UtcNow;

                // Para el ticket visual, convertimos a hora Bolivia (-4)
                var fechaBolivia = fechaAhoraUtc.AddHours(-4);

                // Calculamos ventas del día usando rangos UTC aproximados o fecha Bolivia
                // Para simplificar y evitar errores, usamos el conteo simple:
                var cantidadVentasHoy = _context.Ventas.Count() + 1;

                string nuevoTicket = $"{fechaBolivia:dd/MM/yy} - {cantidadVentasHoy:D3}";

                // 7. Procesar Productos y Totales
                var detallesVenta = new List<DetalleVenta>();
                decimal totalVenta = 0;

                foreach (var item in ventaDto.Productos)
                {
                    var prod = _context.Productos.Find(item.IdProducto);
                    if (prod == null) return NotFound($"Producto ID {item.IdProducto} no existe.");

                    // Validar stock aquí si tuvieras control de inventario

                    totalVenta += prod.Precio * item.Cantidad;
                    detallesVenta.Add(new DetalleVenta
                    {
                        IdProducto = prod.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = prod.Precio
                        // Subtotal se calcula en BD con [DatabaseGenerated]
                    });
                }

                // 8. Crear Objeto Venta
                var venta = new Ventas
                {
                    IdUsuario = usuario.IdUsuario,
                    IdPago = pago.IdPago,
                    IdCaja = cajaAbierta.IdCaja,
                    NumeroTicket = nuevoTicket,
                    TipoPedido = tipoNormalizado, // "Local" o "Llevar"
                    // 🔥 ASIGNAMOS EL NOMBRE AQUÍ (Si viene vacío, ponemos "Cliente General")
                    NombreCliente = !string.IsNullOrEmpty(ventaDto.NombreCliente) ? ventaDto.NombreCliente : "Cliente General",
                    // 🔴 CORRECCIÓN IMPORTANTE: Guardar como UTC
                    FechaVenta = fechaAhoraUtc,
                    Total = totalVenta,
                    Detalles = detallesVenta
                };

                _context.Ventas.Add(venta);
                _context.SaveChanges(); // Genera IdVenta

                // 9. Crear Comanda Automática
                var comanda = new Comandas
                {
                    IdVenta = venta.IdVenta,
                    Estado = "Pendiente",
                    FechaEnvio = fechaAhoraUtc,
                    Venta = venta
                };

                _context.Comandas.Add(comanda);
                _context.SaveChanges();

                // 10. Actualizar Reportes (Sin romper la transacción si falla)
                try { ReportesService.ActualizarReportes(_context, venta); }
                catch (Exception ex) { Console.WriteLine("Error en reportes: " + ex.Message); }

                transaction.Commit();

                return Ok(new
                {
                    message = "Venta registrada exitosamente ✅",
                    ticket = nuevoTicket,
                    tipo = tipoNormalizado,
                    idVenta = venta.IdVenta,
                    total = totalVenta
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"❌ Error CRÍTICO: {error}");
            }
        }

        // GET: Historial completo
        [HttpGet("todas")]
        public IActionResult GetVentas()
        {
            var ventas = _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => new VentaDTO
                {
                    IdVenta = v.IdVenta,
                    NumeroTicket = v.NumeroTicket,
                    TipoPedido = v.TipoPedido,
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
                .ToList();

            return Ok(ventas);
        }
    }
}