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

        // GET: api/Dashboard/resumen
        [HttpGet("resumen")]
        public IActionResult GetDashboardResumen()
        {
            try
            {
                // Configuración de Fechas UTC
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

                // 3. TOP 5 PRODUCTOS (Corregido para evitar errores de agrupación)
                // Agrupamos por ID y por Nombre para que Postgres sea feliz
                var topProductos = _context.DetallesVenta
                    .Include(d => d.Producto)
                    .GroupBy(d => new { d.IdProducto, d.Producto.NombreProducto })
                    .Select(g => new
                    {
                        Producto = g.Key.NombreProducto,
                        CantidadTotal = g.Sum(d => d.Cantidad),
                        // Asegúrate de que 'Subtotal' existe en tu modelo DetalleVenta. 
                        // Si no, usa: g.Sum(d => d.Cantidad * d.PrecioUnitario)
                        DineroGenerado = g.Sum(d => d.Subtotal)
                    })
                    .OrderByDescending(x => x.CantidadTotal)
                    .Take(5)
                    .ToList();

                // 4. GRÁFICO SEMANAL (Corregido el error de ToString)
                var hace7dias = hoyInicioUtc.AddDays(-6);

                // Paso 1: Traemos los datos crudos de la BD
                var datosSemanaRaw = _context.VentasDiarias
                    .Where(v => v.Fecha >= hace7dias)
                    .OrderBy(v => v.Fecha)
                    .ToList();

                // Paso 2: Formateamos en memoria (aquí sí podemos usar ToString)
                var ventasSemana = datosSemanaRaw
                    .Select(v => new
                    {
                        Fecha = v.Fecha.ToString("dd/MM"),
                        Total = v.TotalVentas
                    })
                    .ToList();

                var pedidosLocal = ventasHoy.Count(v => v.TipoPedido == "Local");
                var pedidosLlevar = ventasHoy.Count(v => v.TipoPedido == "Llevar");

                // 5. LISTA DE VENTAS RECIENTES (Corregido el error de ToString)
                var ultimasVentas = _context.Ventas
                    .Include(v => v.Usuario)
                    .Include(v => v.Pago)
                    .Include(v => v.Comanda)
                    .OrderByDescending(v => v.FechaVenta)
                    .Take(10)
                    .Select(v => new
                    {
                        // 🔴 CAMBIO CLAVE: Devolvemos la fecha ORIGINAL (DateTime)
                        // El frontend se encargará de convertirla a "HH:mm" hora boliviana.
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
                // Esto te ayudará a ver el error real en los logs de Railway si algo más falla
                Console.WriteLine($"Error Dashboard: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");

                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}