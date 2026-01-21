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
    [Authorize(Roles = "Administrador,Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen")]
        public IActionResult GetDashboardResumen()
        {
            try
            {
                // --- CONFIGURACIÓN DE FECHAS UTC (Correcto) ---
                var ahoraUtc = DateTime.UtcNow;
                var hoyInicioUtc = ahoraUtc.Date;
                var mañanaInicioUtc = hoyInicioUtc.AddDays(1);
                var inicioMesUtc = new DateTime(ahoraUtc.Year, ahoraUtc.Month, 1);

                // 1. MÉTRICAS DE HOY
                var ventasHoy = _context.Ventas
                    .Where(v => v.FechaVenta >= hoyInicioUtc && v.FechaVenta < mañanaInicioUtc)
                    .Select(v => new { v.Total, v.TipoPedido })
                    .ToList();

                var totalDineroHoy = ventasHoy.Sum(v => v.Total);
                var cantidadPedidosHoy = ventasHoy.Count;
                var ticketPromedio = cantidadPedidosHoy > 0 ? totalDineroHoy / cantidadPedidosHoy : 0;

                // 2. MÉTRICAS DEL MES
                var totalMes = _context.Ventas
                    .Where(v => v.FechaVenta >= inicioMesUtc)
                    .Sum(v => v.Total);

                // 3. TOP 5 PRODUCTOS (CORREGIDO PARA POSTGRES)
                // Postgres necesita que agrupes también por nombre si vas a seleccionarlo
                var topProductos = _context.DetallesVenta
                    .Include(d => d.Producto)
                    .GroupBy(d => new { d.IdProducto, d.Producto.NombreProducto }) // <--- CORRECCIÓN AQUÍ
                    .Select(g => new
                    {
                        Producto = g.Key.NombreProducto,
                        CantidadTotal = g.Sum(d => d.Cantidad),
                        DineroGenerado = g.Sum(d => d.Subtotal) // Asegúrate que 'Subtotal' existe en DetalleVenta
                    })
                    .OrderByDescending(x => x.CantidadTotal)
                    .Take(5)
                    .ToList();

                // 4. GRÁFICO SEMANAL (CORREGIDO)
                var hace7dias = hoyInicioUtc.AddDays(-6);

                // PASO 1: Traer datos CRUDOS (sin ToString)
                var datosSemanaRaw = _context.VentasDiarias
                    .Where(v => v.Fecha >= hace7dias)
                    .OrderBy(v => v.Fecha)
                    .ToList();

                // PASO 2: Formatear en MEMORIA (Aquí sí funciona el ToString)
                var ventasSemana = datosSemanaRaw
                    .Select(v => new
                    {
                        Fecha = v.Fecha.ToString("dd/MM"),
                        Total = v.TotalVentas
                    })
                    .ToList();

                var pedidosLocal = ventasHoy.Count(v => v.TipoPedido == "Local");
                var pedidosLlevar = ventasHoy.Count(v => v.TipoPedido == "Llevar");

                // 5. LISTA DE VENTAS RECIENTES (CORREGIDO)
                var ultimasVentas = _context.Ventas
                    .Include(v => v.Usuario)
                    .Include(v => v.Pago)
                    .Include(v => v.Comanda)
                    .OrderByDescending(v => v.FechaVenta)
                    .Take(10)
                    .Select(v => new
                    {
                        // 🔴 CORRECCIÓN CLAVE: Enviamos la fecha tal cual (DateTime)
                        // Si usas .ToString("HH:mm") aquí, EXPLOTA.
                        Fecha = v.FechaVenta,

                        Cajero = v.Usuario.Nombre,
                        Total = v.Total,
                        MetodoPago = v.Pago.TipoPago,
                        Estado = v.Comanda != null ? v.Comanda.Estado : "Completado"
                    })
                    .ToList();

                return Ok(new
                {
                    exito = true,
                    fecha = DateTime.UtcNow,
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
                    listaVentas = ultimasVentas
                });
            }
            catch (Exception ex)
            {
                // Muestra el error real en la consola de Railway
                Console.WriteLine($"ERROR DASHBOARD: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");

                return StatusCode(500, $"Error Interno: {ex.Message}");
            }
        }
    }
}