using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization; // Importante para seguridad
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO; // INDISPENSABLE para MemoryStream
using System.Linq;
using SaborVeloz.Models; // Asegúrate de usar tu namespace correcto

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Administrador,admin,Admin")] // Descomenta si quieres seguridad estricta
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportesController(AppDbContext db)
        {
            _db = db;
        }

        // DTO auxiliar para respuestas JSON rápidas
        private ReporteDTO BuildReporte(DateTime fecha, decimal total, int cantidad) =>
            new ReporteDTO { Fecha = fecha, TotalVentas = total, CantidadVentas = cantidad };

        // --- ENDPOINTS JSON (KPIs) ---

        [HttpGet("diario")]
        public IActionResult GetDiario()
        {
            var dia = DateTime.Today;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Date == dia).ToList();
            return Ok(BuildReporte(dia, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("semanal")]
        public IActionResult GetSemanal()
        {
            var hoy = DateTime.Today;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var semanaActual = cal.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var anioActual = hoy.Year;

            // Traemos a memoria para usar funciones de calendario
            var ventas = _db.Ventas.AsEnumerable()
                .Where(v => cal.GetWeekOfYear(v.FechaVenta, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == semanaActual && v.FechaVenta.Year == anioActual)
                .ToList();

            return Ok(BuildReporte(hoy, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("mensual")]
        public IActionResult GetMensual()
        {
            var hoy = DateTime.Today;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Month == hoy.Month && v.FechaVenta.Year == hoy.Year).ToList();
            return Ok(BuildReporte(new DateTime(hoy.Year, hoy.Month, 1), ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("anual")]
        public IActionResult GetAnual()
        {
            var hoy = DateTime.Today;
            var ventas = _db.Ventas.Where(v => v.FechaVenta.Year == hoy.Year).ToList();
            return Ok(BuildReporte(new DateTime(hoy.Year, 1, 1), ventas.Sum(v => v.Total), ventas.Count));
        }

        // --- ENDPOINTS EXCEL (Descargas) ---

        [HttpGet("exportar/diario")]
        public IActionResult ExportarDiario()
        {
            var dia = DateTime.Today;
            var ventas = ObtenerVentasConDetalles(v => v.FechaVenta.Date == dia);
            return GenerarArchivoExcel(ventas, "VentasDiarias", $"Reporte_{dia:yyyyMMdd}.xlsx");
        }

        // --- MÉTODOS PRIVADOS DE AYUDA ---

        private List<Ventas> ObtenerVentasConDetalles(Func<Ventas, bool> filtro)
        {
            return _db.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .AsEnumerable() // Importante para filtrar en memoria si la consulta es compleja
                .Where(filtro)
                .ToList();
        }

        private IActionResult GenerarArchivoExcel(List<Ventas> ventas, string nombreHoja, string nombreArchivo)
        {
            var datosExcel = new List<dynamic>();

            foreach (var v in ventas)
            {
                foreach (var d in v.Detalles)
                {
                    datosExcel.Add(new
                    {
                        ID = v.IdVenta,
                        Fecha = v.FechaVenta.ToString("g"),
                        Cajero = v.Usuario?.Nombre ?? "Sin Cajero",
                        Pago = v.Pago?.TipoPago ?? "N/A",
                        Producto = d.Producto?.NombreProducto ?? "Borrado",
                        Cant = d.Cantidad,
                        Total = d.Cantidad * d.PrecioUnitario
                    });
                }
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(nombreHoja);
                worksheet.Cell(1, 1).InsertTable(datosExcel);
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    // ⭐ CORRECCIÓN CRÍTICA: Usar base.File para evitar ambigüedad ⭐
                    return base.File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo
                    );
                }
            }
        }
    }
}