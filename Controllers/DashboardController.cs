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
                var hoy = DateTime.Now.Date;
                var mañana = hoy.AddDays(1);
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

                // 1. Métricas de HOY (Cards Superiores)
                // Traemos solo lo necesario para no sobrecargar la memoria
                var ventasHoy = _context.Ventas
                    .Where(v => v.FechaVenta >= hoy && v.FechaVenta < mañana)
                    .Select(v => new { v.Total, v.TipoPedido }) // Proyección ligera
                    .ToList();

                var totalDineroHoy = ventasHoy.Sum(v => v.Total);
                var cantidadPedidosHoy = ventasHoy.Count;
                var ticketPromedio = cantidadPedidosHoy > 0 ? totalDineroHoy / cantidadPedidosHoy : 0;

                // 2. Métricas del MES (Comparativa)
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
                var hace7dias = hoy.AddDays(-6);
                var ventasSemana = _context.VentasDiarias
                    .Where(v => v.Fecha >= hace7dias)
                    .OrderBy(v => v.Fecha)
                    .Select(v => new
                    {
                        Fecha = v.Fecha.ToString("dd/MM"),
                        Total = v.TotalVentas
                    })
                    .ToList();

                // 5. DESGLOSE TIPO DE PEDIDO (Donut Chart)
                var pedidosLocal = ventasHoy.Count(v => v.TipoPedido == "Local");
                var pedidosLlevar = ventasHoy.Count(v => v.TipoPedido == "Llevar");

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
                    }
                });
            }
            catch (Exception ex)
            {
                // Loguear error real en servidor
                return StatusCode(500, $"Error generando dashboard: {ex.Message}");
            }
        }
    }
}