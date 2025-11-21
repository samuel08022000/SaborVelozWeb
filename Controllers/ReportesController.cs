using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using System.Globalization;
using SaborVelozWeb;


namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportesController(AppDbContext db) => _db = db;

        private ReporteDTO BuildReporte(DateTime fecha, decimal total, int cantidad) =>
            new ReporteDTO { Fecha = fecha, TotalVentas = total, CantidadVentas = cantidad };

        // JSON: diario
        [HttpGet("diario")]
        public IActionResult GetDiario([FromQuery] DateTime? fecha = null)
        {
            var dia = fecha?.Date ?? DateTime.Today;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Date == dia).ToList();
            var rep = BuildReporte(dia, ventas.Sum(v => v.Total), ventas.Count);
            return Ok(rep);
        }

        // JSON: semanal (por semana y año)
        [HttpGet("semanal")]
        public IActionResult GetSemanal([FromQuery] int? semana = null, [FromQuery] int? año = null)
        {
            var hoy = DateTime.Today;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var week = semana ?? cal.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var yr = año ?? hoy.Year;

            var ventas = _db.Ventas
                .Where(v =>
                    cal.GetWeekOfYear(v.FechaVenta, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week &&
                    v.FechaVenta.Year == yr)
                .ToList();

            var rep = BuildReporte(new DateTime(yr, 1, 1).AddDays((week - 1) * 7), ventas.Sum(v => v.Total), ventas.Count);
            return Ok(rep);
        }

        // JSON: mensual
        [HttpGet("mensual")]
        public IActionResult GetMensual([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var m = mes ?? DateTime.Now.Month;
            var a = año ?? DateTime.Now.Year;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Month == m && v.FechaVenta.Year == a).ToList();
            var rep = BuildReporte(new DateTime(a, m, 1), ventas.Sum(v => v.Total), ventas.Count);
            return Ok(rep);
        }

        // JSON: anual
        [HttpGet("anual")]
        public IActionResult GetAnual([FromQuery] int? año = null)
        {
            var a = año ?? DateTime.Now.Year;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Year == a).ToList();
            var rep = BuildReporte(new DateTime(a, 1, 1), ventas.Sum(v => v.Total), ventas.Count);
            return Ok(rep);
        }

        // ----- EXPORTES -----

        [HttpGet("exportar/diario")]
        public IActionResult ExportarDiario([FromQuery] DateTime? fecha = null)
        {
            var dia = fecha?.Date ?? DateTime.Today;
            var ventas = _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Where(v => v.FechaVenta.Date == dia)
                .ToList();

            var rows = ventas.SelectMany(v => v.Detalles.Select(d => new {
                Fecha = v.FechaVenta,
                IdVenta = v.IdVenta,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "",
                MetodoPago = v.Pago != null ? v.Pago.TipoPago : "",
                Producto = d.Producto != null ? d.Producto.NombreProducto : "",
                d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })).ToList();

            var fileName = $"Ventas_{dia:yyyyMMdd}.xlsx";
            return FileFromObjects(rows, "VentasDiarias", fileName);
        }

        [HttpGet("exportar/semanal")]
        public IActionResult ExportarSemanal([FromQuery] int? semana = null, [FromQuery] int? año = null)
        {
            var hoy = DateTime.Today;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var week = semana ?? cal.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var yr = año ?? hoy.Year;

            var ventas = _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Where(v => cal.GetWeekOfYear(v.FechaVenta, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week &&
                            v.FechaVenta.Year == yr)
                .ToList();

            var rows = ventas.SelectMany(v => v.Detalles.Select(d => new {
                Fecha = v.FechaVenta,
                IdVenta = v.IdVenta,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "",
                MetodoPago = v.Pago != null ? v.Pago.TipoPago : "",
                Producto = d.Producto != null ? d.Producto.NombreProducto : "",
                d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })).ToList();

            var fileName = $"Ventas_Semana_{yr}_W{week:00}.xlsx";
            return FileFromObjects(rows, "VentasSemanales", fileName);
        }

        [HttpGet("exportar/mensual")]
        public IActionResult ExportarMensual([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var m = mes ?? DateTime.Now.Month;
            var a = año ?? DateTime.Now.Year;

            var ventas = _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Where(v => v.FechaVenta.Month == m && v.FechaVenta.Year == a)
                .ToList();

            var rows = ventas.SelectMany(v => v.Detalles.Select(d => new {
                Fecha = v.FechaVenta,
                IdVenta = v.IdVenta,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "",
                MetodoPago = v.Pago != null ? v.Pago.TipoPago : "",
                Producto = d.Producto != null ? d.Producto.NombreProducto : "",
                d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })).ToList();

            var fileName = $"Ventas_{a}_{m:00}.xlsx";
            return FileFromObjects(rows, "VentasMensuales", fileName);
        }

        [HttpGet("exportar/anual")]
        public IActionResult ExportarAnual([FromQuery] int? año = null)
        {
            var a = año ?? DateTime.Now.Year;

            var ventas = _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Where(v => v.FechaVenta.Year == a)
                .ToList();

            var rows = ventas.SelectMany(v => v.Detalles.Select(d => new {
                Fecha = v.FechaVenta,
                IdVenta = v.IdVenta,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "",
                MetodoPago = v.Pago != null ? v.Pago.TipoPago : "",
                Producto = d.Producto != null ? d.Producto.NombreProducto : "",
                d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })).ToList();

            var fileName = $"Ventas_{a}.xlsx";
            return FileFromObjects(rows, "VentasAnuales", fileName);
        }

        // Exportar rango (desde,hasta)
        [HttpGet("exportar/rango")]
        public IActionResult ExportarRango([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            if (desde > hasta) return BadRequest("El parámetro 'desde' no puede ser mayor a 'hasta'.");

            var ventas = _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Where(v => v.FechaVenta.Date >= desde.Date && v.FechaVenta.Date <= hasta.Date)
                .ToList();

            var rows = ventas.SelectMany(v => v.Detalles.Select(d => new {
                Fecha = v.FechaVenta,
                IdVenta = v.IdVenta,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "",
                MetodoPago = v.Pago != null ? v.Pago.TipoPago : "",
                Producto = d.Producto != null ? d.Producto.NombreProducto : "",
                d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })).ToList();

            var fileName = $"Ventas_{desde:yyyyMMdd}_to_{hasta:yyyyMMdd}.xlsx";
            return FileFromObjects(rows, "VentasRango", fileName);
        }

        // Util: crea y devuelve el Excel desde una lista de objetos
        private IActionResult FileFromObjects<T>(List<T> data, string sheetName, string fileName)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);
            ws.Cell(1, 1).InsertTable(data);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream.ToArray(), contentType, fileName);
        }
    }
}
