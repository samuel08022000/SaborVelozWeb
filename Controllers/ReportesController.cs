using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
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
        // 1. Endpoint para el Resumen Diario (Solo Admin debería ver esto)
        [HttpGet("resumen-diario")]
        public async Task<IActionResult> GetResumenDiario()
        {
            var hoy = DateTime.Today;

            // Sumar todas las ventas de hoy
            var totalVentasHoy = await _db.Ventas
                .Where(v => v.FechaVenta.Date == hoy)
                .SumAsync(v => v.Total);

            // Contar cuántas ventas se hicieron
            var cantidadVentas = await _db.Ventas
                .CountAsync(v => v.FechaVenta.Date == hoy);

            // Obtener el método de pago más usado (Opcional, pero se ve pro en el dashboard)
            var metodoPagoMasUsado = await _db.Ventas
      .Include(v => v.Pago) // 1. Unimos la tabla Ventas con Pagos
      .Where(v => v.FechaVenta.Date == hoy)
      .GroupBy(v => v.Pago.TipoPago) // 2. Aquí usamos la propiedad correcta: TipoPago
      .OrderByDescending(g => g.Count())
      .Select(g => g.Key)
      .FirstOrDefaultAsync() ?? "N/A";

            return Ok(new
            {
                Fecha = hoy.ToString("dd/MM/yyyy"),
                TotalVendido = totalVentasHoy,
                CantidadTransacciones = cantidadVentas,
                MetodoPagoTop = metodoPagoMasUsado
            });
        }
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

                // ==========================================
                // 1. CABECERAS (MODIFICADAS)
                // ==========================================
                worksheet.Cell(1, 1).Value = "Ticket";        // 👈 Antes ID Venta, ahora Ticket (ej. 14/01/26 - 01)
                worksheet.Cell(1, 2).Value = "Fecha y Hora";
                worksheet.Cell(1, 3).Value = "Cajero";
                worksheet.Cell(1, 4).Value = "Método Pago";   // 👈 NUEVO: Antes del Total
                worksheet.Cell(1, 5).Value = "Total";
                worksheet.Cell(1, 6).Value = "Productos";

                // (Opcional) Le ponemos negrita al encabezado para que se vea pro
                var rangoHeader = worksheet.Range("A1:F1");
                rangoHeader.Style.Font.Bold = true;
                rangoHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

                // ==========================================
                // 2. LLENADO DE DATOS
                // ==========================================
                int row = 2;
                foreach (var v in ventas)
                {
                    // Col 1: El Ticket formateado (ej: 14/01/26 - 01)
                    worksheet.Cell(row, 1).Value = v.NumeroTicket;

                    // Col 2: La fecha exacta del sistema
                    worksheet.Cell(row, 2).Value = v.FechaVenta.ToString("dd/MM/yyyy HH:mm");

                    // Col 3: Nombre del Cajero
                    worksheet.Cell(row, 3).Value = v.Usuario?.Nombre ?? "N/A";

                    // Col 4: Método de Pago (Recuperado de la relación)
                    // Nota: Asegúrate que en la consulta (ExportarDiario) tengas .Include(v => v.Pago)
                    worksheet.Cell(row, 4).Value = v.Pago?.TipoPago ?? "Sin Pago";

                    // Col 5: Total (Con formato moneda)
                    worksheet.Cell(row, 5).Value = v.Total;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "$ #,##0.00";

                    // Col 6: Resumen de productos
                    var resumenProductos = string.Join(", ", v.Detalles.Select(d =>
                        $"{d.Cantidad}x {d.Producto?.NombreProducto ?? "Borrado"}"));

                    worksheet.Cell(row, 6).Value = resumenProductos;

                    row++;
                }

                // Ajustar ancho de columnas automáticamente
                worksheet.Columns().AdjustToContents();

                // ==========================================
                // 3. GENERAR ARCHIVO (MemoryStream)
                // ==========================================
                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo
                    );
                }


            }
        }
        // --- NUEVO: EXPORTAR SEMANAL (Resumen por días) ---
        [HttpGet("exportar/semanal")]
        public IActionResult ExportarSemanal()
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunes = hoy.AddDays(-1 * diff).Date;
            var domingoFinal = lunes.AddDays(7);

            // Agrupamos las ventas de la semana por día
            var resumenSemanal = _db.Ventas
                .Where(v => v.FechaVenta >= lunes && v.FechaVenta < domingoFinal)
                .AsEnumerable() // Pasamos a memoria para formatear la fecha fácilmente
                .GroupBy(v => v.FechaVenta.Date)
                .Select(g => new {
                    Etiqueta = g.Key.ToString("dd/MM/yyyy") + " (" + g.Key.ToString("dddd", new System.Globalization.CultureInfo("es-ES")) + ")",
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            return GenerarExcelResumido(resumenSemanal.Cast<object>().ToList(), "Reporte_Semanal", "Dia", $"Reporte_Semanal_{hoy:yyyyMMdd}.xlsx");
        }

        // --- NUEVO: EXPORTAR MENSUAL (Resumen por semanas) ---
        [HttpGet("exportar/mensual")]
        public IActionResult ExportarMensual()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var ventasMes = _db.Ventas
                .Where(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
                .ToList();

            // Lógica para agrupar por semanas del mes
            var resumenMensual = ventasMes
                .GroupBy(v => {
                    int semanaNum = (v.FechaVenta.Day - 1) / 7 + 1;
                    return $"Semana {semanaNum}";
                })
                .Select(g => new {
                    Etiqueta = g.Key,
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            return GenerarExcelResumido(resumenMensual.Cast<object>().ToList(), "Reporte_Mensual", "Semana", $"Reporte_Mensual_{hoy:yyyyMM}.xlsx");
        }

        // --- MÉTODO AUXILIAR PARA REPORTES RESUMIDOS ---
        private IActionResult GenerarExcelResumido(List<object> datos, string nombreHoja, string columnaNombre, string nombreArchivo)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(nombreHoja);

                // Cabeceras
                worksheet.Cell(1, 1).Value = columnaNombre; // "Dia" o "Semana"
                worksheet.Cell(1, 2).Value = "Total Ingresado";

                var header = worksheet.Range("A1:B1");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (dynamic d in datos)
                {
                    worksheet.Cell(row, 1).Value = d.Etiqueta;
                    worksheet.Cell(row, 2).Value = d.Total;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "$ #,##0.00";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
                }
            }
        }
    }
}