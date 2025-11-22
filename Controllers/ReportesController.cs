using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

// ⚠️ NOTA: NO agregues 'using System.IO;' aquí arriba. 
// Eso es lo que causaba el conflicto con "File".

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportesController(AppDbContext db)
        {
            _db = db;
        }

        private ReporteDTO BuildReporte(DateTime fecha, decimal total, int cantidad) =>
            new ReporteDTO { Fecha = fecha, TotalVentas = total, CantidadVentas = cantidad };

        // --- ENDPOINTS KPI (Sin errores de fecha) ---

        [HttpGet("diario")]
        public IActionResult GetDiario()
        {
            var hoy = DateTime.Today;
            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
                .ToList();

            return Ok(BuildReporte(hoy, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("semanal")]
        public IActionResult GetSemanal()
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunes = hoy.AddDays(-1 * diff).Date;
            var domingoFinal = lunes.AddDays(7);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= lunes && v.FechaVenta < domingoFinal)
                .ToList();

            return Ok(BuildReporte(lunes, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("mensual")]
        public IActionResult GetMensual()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
                .ToList();

            return Ok(BuildReporte(inicioMes, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("anual")]
        public IActionResult GetAnual()
        {
            var hoy = DateTime.Today;
            var inicioAnio = new DateTime(hoy.Year, 1, 1);
            var finAnio = inicioAnio.AddYears(1);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= inicioAnio && v.FechaVenta < finAnio)
                .ToList();

            return Ok(BuildReporte(inicioAnio, ventas.Sum(v => v.Total), ventas.Count));
        }

        // --- EXPORTAR EXCEL (Corregido al 100%) ---

        [HttpGet("exportar/diario")]
        public IActionResult ExportarDiario()
        {
            var hoy = DateTime.Today;
            var ventas = _db.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Where(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
                .ToList();

            return GenerarExcel(ventas, "Diario", $"Ventas_{hoy:yyyyMMdd}.xlsx");
        }

        private IActionResult GenerarExcel(List<Ventas> ventas, string hoja, string nombreArchivo)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(hoja);

                // Cabeceras
                worksheet.Cell(1, 1).Value = "ID Venta";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Cajero";
                worksheet.Cell(1, 4).Value = "Total";
                worksheet.Cell(1, 5).Value = "Productos";

                int row = 2;
                foreach (var v in ventas)
                {
                    worksheet.Cell(row, 1).Value = v.IdVenta;
                    worksheet.Cell(row, 2).Value = v.FechaVenta.ToString("g");
                    worksheet.Cell(row, 3).Value = v.Usuario?.Nombre ?? "N/A";
                    worksheet.Cell(row, 4).Value = v.Total;

                    // Unimos los productos en una sola celda
                    var resumenProductos = string.Join(", ", v.Detalles.Select(d =>
                        $"{d.Cantidad}x {d.Producto?.NombreProducto ?? "Borrado"}"));

                    worksheet.Cell(row, 5).Value = resumenProductos;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                // TRUCO: Usamos System.IO.MemoryStream completo para evitar conflictos
                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    // Ahora 'File' NO puede confundirse porque quitamos 'using System.IO'
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo
                    );
                }
            }
        }
    }
}