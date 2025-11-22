using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using System.Globalization;
using System.IO; // Necesario para MemoryStream
using SaborVelozWeb;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportesController(AppDbContext db) => _db = db;

        // Helper DTO
        private ReporteDTO BuildReporte(DateTime fecha, decimal total, int cantidad) =>
            new ReporteDTO { Fecha = fecha, TotalVentas = total, CantidadVentas = cantidad };

        // --- ENDPOINTS JSON ---
        [HttpGet("diario")]
        public IActionResult GetDiario([FromQuery] DateTime? fecha = null)
        {
            var dia = fecha?.Date ?? DateTime.Today;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Date == dia).ToList();
            return Ok(BuildReporte(dia, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("semanal")]
        public IActionResult GetSemanal([FromQuery] int? semana = null, [FromQuery] int? año = null)
        {
            var hoy = DateTime.Today;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var week = semana ?? cal.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var yr = año ?? hoy.Year;

            var ventas = _db.Ventas.AsEnumerable()
                .Where(v => cal.GetWeekOfYear(v.FechaVenta, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week && v.FechaVenta.Year == yr)
                .ToList();
            return Ok(BuildReporte(new DateTime(yr, 1, 1), ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("mensual")]
        public IActionResult GetMensual([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var m = mes ?? DateTime.Now.Month;
            var a = año ?? DateTime.Now.Year;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Month == m && v.FechaVenta.Year == a).ToList();
            return Ok(BuildReporte(new DateTime(a, m, 1), ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("anual")]
        public IActionResult GetAnual([FromQuery] int? año = null)
        {
            var a = año ?? DateTime.Now.Year;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Year == a).ToList();
            return Ok(BuildReporte(new DateTime(a, 1, 1), ventas.Sum(v => v.Total), ventas.Count));
        }

        // --- ENDPOINTS EXCEL ---
        [HttpGet("exportar/diario")]
        public IActionResult ExportarDiario([FromQuery] DateTime? fecha = null)
        {
            var dia = fecha?.Date ?? DateTime.Today;
            var ventas = ObtenerVentas(v => v.FechaVenta.Date == dia);
            return GenerarExcel(ventas, "VentasDiarias", $"Ventas_{dia:yyyyMMdd}.xlsx");
        }

        [HttpGet("exportar/semanal")]
        public IActionResult ExportarSemanal([FromQuery] int? semana = null, [FromQuery] int? año = null)
        {
            var hoy = DateTime.Today;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var week = semana ?? cal.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var yr = año ?? hoy.Year;
            var ventas = _db.Ventas.AsEnumerable()
                .Where(v => cal.GetWeekOfYear(v.FechaVenta, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week && v.FechaVenta.Year == yr).ToList();
            // Nota: Aquí no usamos ObtenerVentas porque filtramos en memoria, pero para el Excel sirve igual pasarlo directo
            // (Para simplicidad, si da error, usa el helper privado con AsEnumerable dentro)
            return GenerarExcel(ventas, "VentasSemanales", $"Ventas_Semana.xlsx");
        }

        [HttpGet("exportar/mensual")]
        public IActionResult ExportarMensual([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var m = mes ?? DateTime.Now.Month;
            var a = año ?? DateTime.Now.Year;
            var ventas = ObtenerVentas(v => v.FechaVenta.Month == m && v.FechaVenta.Year == a);
            return GenerarExcel(ventas, "VentasMensuales", $"Ventas_{a}_{m:00}.xlsx");
        }

        [HttpGet("exportar/anual")]
        public IActionResult ExportarAnual([FromQuery] int? año = null)
        {
            var a = año ?? DateTime.Now.Year;
            var ventas = ObtenerVentas(v => v.FechaVenta.Year == a);
            return GenerarExcel(ventas, "VentasAnuales", $"Ventas_{a}.xlsx");
        }

        // --- HELPERS ---
        private List<SaborVeloz.Models.Ventas> ObtenerVentas(Func<SaborVeloz.Models.Ventas, bool> filtro)
        {
            return _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .AsEnumerable()
                .Where(filtro)
                .ToList();
        }

        private IActionResult GenerarExcel(List<SaborVeloz.Models.Ventas> ventas, string hoja, string archivo)
        {
            var data = new List<dynamic>();
            foreach (var v in ventas)
            {
                foreach (var d in v.Detalles)
                {
                    data.Add(new
                    {
                        Id = v.IdVenta,
                        Fecha = v.FechaVenta.ToString("g"),
                        Cajero = v.Usuario?.Nombre ?? "N/A",
                        Metodo = v.Pago?.TipoPago ?? "N/A",
                        Producto = d.Producto?.NombreProducto ?? "N/A",
                        Cant = d.Cantidad,
                        Total = d.Cantidad * d.PrecioUnitario
                    });
                }
            }

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(hoja);
            ws.Cell(1, 1).InsertTable(data);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            // ⭐ AQUÍ ESTÁ LA CORRECCIÓN: 'base.File' ⭐
            return base.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo);
        }
    }
}