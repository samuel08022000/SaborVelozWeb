using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs; // Asegúrate de tener los DTOs necesarios si usas alguno específico
using System;
using System.Linq;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Admin")] // Solo para el jefe
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // 🌟 ENDPOINT MAESTRO: Datos para el Dashboard Administrativo 🌟
        // GET: api/Dashboard/resumen
        [HttpGet("resumen")]
        public IActionResult GetDashboardResumen()
        {
            try
            {
                // Fechas clave
                var ahora = DateTime.Now;
                var hoyInicio = ahora.Date; // Hoy a las 00:00:00
                var mañanaInicio = hoyInicio.AddDays(1); // Mañana a las 00:00:00
                                                         // 🔥 El truco para el mes: Día 1 del mes actual, año actual
                var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

                // 1. Métricas de HOY (Ventas entre hoy 00:00 y mañana 00:00)
                var ventasHoy = _context.Ventas
                    .Where(v => v.FechaVenta >= hoyInicio && v.FechaVenta < mañanaInicio)
                    .Select(v => new { v.Total, v.TipoPedido })
                    .ToList();

                var totalDineroHoy = ventasHoy.Sum(v => v.Total);
                var cantidadPedidosHoy = ventasHoy.Count;
                var ticketPromedio = cantidadPedidosHoy > 0 ? totalDineroHoy / cantidadPedidosHoy : 0;

                // 🔥 2. Métricas del MES (Ventas desde el día 1 del mes hasta AHORA MISMO)
                var totalMes = _context.Ventas
                    .Where(v => v.FechaVenta >= inicioMes)
                    .Sum(v => v.Total);


                // 3. TOP 5 PRODUCTOS MÁS VENDIDOS (Histórico)
                // Requiere que DbSet<DetalleVenta> esté en AppDbContext
                var topProductos = _context.DetallesVenta
                    .Include(d => d.Producto)
                    .GroupBy(d => d.IdProducto)
                    .Select(g => new
                    {
                        Producto = g.First().Producto.NombreProducto,
                        CantidadTotal = g.Sum(d => d.Cantidad),
                        DineroGenerado = g.Sum(d => d.Subtotal)
                    })
                    .OrderByDescending(x => x.CantidadTotal)
                    .Take(5)
                    .ToList();

                // 4. GRÁFICO DE VENTAS (Últimos 7 días)
                var hace7dias = hoyInicio.AddDays(-6);
                var ventasSemana = _context.VentasDiarias
                    .Where(v => v.Fecha >= hace7dias)
                    .OrderBy(v => v.Fecha)
                    .Select(v => new
                    {
                        Fecha = v.Fecha.ToString("dd/MM"),
                        Total = v.TotalVentas
                    })
                    .ToList();

                var pedidosLocal = ventasHoy.Count(v => v.TipoPedido == "Local");
                var pedidosLlevar = ventasHoy.Count(v => v.TipoPedido == "Llevar");

                // 🔥 5. LISTA DE VENTAS RECIENTES (Esto es lo que faltaba) 🔥
                var ultimasVentas = _context.Ventas
                    .Include(v => v.Usuario)
                    .Include(v => v.Pago)
                    .Include(v => v.Comanda) // Importante para ver el estado
                    .OrderByDescending(v => v.FechaVenta)
                    .Take(10)
                    .Select(v => new
                    {
                        Fecha = v.FechaVenta.ToString("HH:mm"),
                        Cajero = v.Usuario.Nombre,
                        Total = v.Total,
                        MetodoPago = v.Pago.TipoPago,
                        // Si hay comanda mostramos su estado, si no, "Completado"
                        Estado = v.Comanda != null ? v.Comanda.Estado : "Completado"
                    })
                    .ToList();

                return Ok(new
                {
                    exito = true,
                    fecha = DateTime.Now,
                    metricas = new
                    {
                        ingresosHoy = totalDineroHoy,
                        pedidosHoy = cantidadPedidosHoy,
                        ticketPromedio = Math.Round(ticketPromedio, 2),
                        ingresosMes = totalMes
                    },
                    graficos = new
                    {
                        topProductos = topProductos,
                        tendenciaSemanal = ventasSemana,
                        distribucionPedidos = new { Local = pedidosLocal, Llevar = pedidosLlevar }
                    },
                    listaVentas = ultimasVentas // <--- AQUÍ SE ENVÍA LA LISTA
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}