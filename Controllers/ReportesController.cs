using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.DTOs;
using SaborVeloz.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
            // 🔴 CORRECCIÓN UTC
            var hoyUtc = DateTime.UtcNow.Date; // Fecha actual UTC (00:00:00)
            var mananaUtc = hoyUtc.AddDays(1);

            // Sumar todas las ventas de hoy (Rango >= HOY y < MAÑANA)
            var totalVentasHoy = await _db.Ventas
                .Where(v => v.FechaVenta >= hoyUtc && v.FechaVenta < mananaUtc)
                .SumAsync(v => v.Total);

            // Contar cuántas ventas se hicieron
            var cantidadVentas = await _db.Ventas
                .CountAsync(v => v.FechaVenta >= hoyUtc && v.FechaVenta < mananaUtc);

            // Obtener el método de pago más usado
            var metodoPagoMasUsado = await _db.Ventas
              .Include(v => v.Pago)
              .Where(v => v.FechaVenta >= hoyUtc && v.FechaVenta < mananaUtc)
              .GroupBy(v => v.Pago.TipoPago)
              .OrderByDescending(g => g.Count())
              .Select(g => g.Key)
              .FirstOrDefaultAsync() ?? "N/A";

            return Ok(new
            {
                // Mostramos fecha local para el usuario (UTC - 4h aprox)
                Fecha = DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy"),
                TotalVendido = totalVentasHoy,
                CantidadTransacciones = cantidadVentas,
                MetodoPagoTop = metodoPagoMasUsado
            });
        }

        [HttpGet("diario")]
        public IActionResult GetDiario()
        {
            // 🔴 CORRECCIÓN UTC
            var hoyUtc = DateTime.UtcNow.Date;
            var mananaUtc = hoyUtc.AddDays(1);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= hoyUtc && v.FechaVenta < mananaUtc)
                .ToList();

            return Ok(BuildReporte(hoyUtc, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("semanal")]
        public IActionResult GetSemanal()
        {
            // 🔴 CORRECCIÓN UTC
            var hoyUtc = DateTime.UtcNow.Date;
            int diff = (7 + (hoyUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunesUtc = hoyUtc.AddDays(-1 * diff).Date;
            var domingoFinalUtc = lunesUtc.AddDays(7);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= lunesUtc && v.FechaVenta < domingoFinalUtc)
                .ToList();

            return Ok(BuildReporte(lunesUtc, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("mensual")]
        public IActionResult GetMensual()
        {
            // 🔴 CORRECCIÓN UTC + Explicit DateTimeKind
            var hoyUtc = DateTime.UtcNow;
            var inicioMesUtc = new DateTime(hoyUtc.Year, hoyUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMesUtc = inicioMesUtc.AddMonths(1);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= inicioMesUtc && v.FechaVenta < finMesUtc)
                .ToList();

            return Ok(BuildReporte(inicioMesUtc, ventas.Sum(v => v.Total), ventas.Count));
        }

        [HttpGet("anual")]
        public IActionResult GetAnual()
        {
            // 🔴 CORRECCIÓN UTC + Explicit DateTimeKind
            var hoyUtc = DateTime.UtcNow;
            var inicioAnioUtc = new DateTime(hoyUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var finAnioUtc = inicioAnioUtc.AddYears(1);

            var ventas = _db.Ventas
                .Where(v => v.FechaVenta >= inicioAnioUtc && v.FechaVenta < finAnioUtc)
                .ToList();

            return Ok(BuildReporte(inicioAnioUtc, ventas.Sum(v => v.Total), ventas.Count));
        }

        // --- EXPORTAR EXCEL (CORREGIDO UTC) ---

        [HttpGet("exportar/diario")]
        public IActionResult ExportarDiario()
        {
            // 🔴 CORRECCIÓN UTC
            var hoyUtc = DateTime.UtcNow.Date;
            var mananaUtc = hoyUtc.AddDays(1);

            var ventas = _db.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.Pago)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Where(v => v.FechaVenta >= hoyUtc && v.FechaVenta < mananaUtc)
                .OrderByDescending(v => v.FechaVenta) // Ordenado por hora
                .ToList();

            // Pasamos la fecha local para el nombre del archivo
            var fechaLocal = DateTime.UtcNow.AddHours(-4);
            return GenerarExcel(ventas, "Diario", $"Ventas_{fechaLocal:yyyyMMdd}.xlsx");
        }

        private IActionResult GenerarExcel(List<Ventas> ventas, string hoja, string nombreArchivo)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(hoja);

                // ==========================================
                // 1. CABECERAS
                // ==========================================
                worksheet.Cell(1, 1).Value = "Ticket";
                worksheet.Cell(1, 2).Value = "Fecha y Hora (Bolivia)";
                worksheet.Cell(1, 3).Value = "Cajero";
                worksheet.Cell(1, 4).Value = "Método Pago";
                worksheet.Cell(1, 5).Value = "Total";
                worksheet.Cell(1, 6).Value = "Productos";

                var rangoHeader = worksheet.Range("A1:F1");
                rangoHeader.Style.Font.Bold = true;
                rangoHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

                // ==========================================
                // 2. LLENADO DE DATOS
                // ==========================================
                int row = 2;
                foreach (var v in ventas)
                {
                    worksheet.Cell(row, 1).Value = v.NumeroTicket;

                    // 🔴 CONVERSIÓN DE HORA PARA EXCEL (UTC -> Bolivia)
                    var fechaBolivia = v.FechaVenta.AddHours(-4);
                    worksheet.Cell(row, 2).Value = fechaBolivia.ToString("dd/MM/yyyy HH:mm");

                    worksheet.Cell(row, 3).Value = v.Usuario?.Nombre ?? "N/A";
                    worksheet.Cell(row, 4).Value = v.Pago?.TipoPago ?? "Sin Pago";

                    worksheet.Cell(row, 5).Value = v.Total;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "$ #,##0.00";

                    var resumenProductos = string.Join(", ", v.Detalles.Select(d =>
                        $"{d.Cantidad}x {d.Producto?.NombreProducto ?? "Borrado"}"));

                    worksheet.Cell(row, 6).Value = resumenProductos;

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                // ==========================================
                // 3. GENERAR ARCHIVO
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

        // --- NUEVO: EXPORTAR SEMANAL ---
        [HttpGet("exportar/semanal")]
        public IActionResult ExportarSemanal()
        {
            // 🔴 CORRECCIÓN UTC
            var hoyUtc = DateTime.UtcNow.Date;
            int diff = (7 + (hoyUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
            var lunesUtc = hoyUtc.AddDays(-1 * diff).Date;
            var domingoFinalUtc = lunesUtc.AddDays(7);

            // 1. Traer datos crudos de BD primero (para evitar error de traducción LINQ)
            var ventasSemanaRaw = _db.Ventas
                .Where(v => v.FechaVenta >= lunesUtc && v.FechaVenta < domingoFinalUtc)
                .Select(v => new { v.FechaVenta, v.Total }) // Solo lo necesario
                .ToList();

            // 2. Procesar en memoria (Aquí sí podemos usar DateTime local y CultureInfo)
            var resumenSemanal = ventasSemanaRaw
                .GroupBy(v => v.FechaVenta.AddHours(-4).Date) // Agrupar por día local
                .Select(g => new {
                    Etiqueta = g.Key.ToString("dd/MM/yyyy") + " (" + g.Key.ToString("dddd", new CultureInfo("es-ES")) + ")",
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            var fechaLocal = DateTime.UtcNow.AddHours(-4);
            return GenerarExcelResumido(resumenSemanal.Cast<object>().ToList(), "Reporte_Semanal", "Dia", $"Reporte_Semanal_{fechaLocal:yyyyMMdd}.xlsx");
        }

        // --- NUEVO: EXPORTAR MENSUAL ---
        [HttpGet("exportar/mensual")]
        public IActionResult ExportarMensual()
        {
            // 🔴 CORRECCIÓN UTC + Explicit Kind
            var hoyUtc = DateTime.UtcNow;
            var inicioMesUtc = new DateTime(hoyUtc.Year, hoyUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMesUtc = inicioMesUtc.AddMonths(1);

            // 1. Traer datos crudos
            var ventasMesRaw = _db.Ventas
                .Where(v => v.FechaVenta >= inicioMesUtc && v.FechaVenta < finMesUtc)
                .Select(v => new { v.FechaVenta, v.Total })
                .ToList();

            // 2. Procesar en memoria
            var resumenMensual = ventasMesRaw
                .GroupBy(v => {
                    // Calculamos semana basado en fecha local
                    var fechaLocal = v.FechaVenta.AddHours(-4);
                    int semanaNum = (fechaLocal.Day - 1) / 7 + 1;
                    return $"Semana {semanaNum}";
                })
                .Select(g => new {
                    Etiqueta = g.Key,
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(x => x.Etiqueta)
                .ToList();

            var fechaLocalHoy = DateTime.UtcNow.AddHours(-4);
            return GenerarExcelResumido(resumenMensual.Cast<object>().ToList(), "Reporte_Mensual", "Semana", $"Reporte_Mensual_{fechaLocalHoy:yyyyMM}.xlsx");
        }

        // --- MÉTODO AUXILIAR ---
        private IActionResult GenerarExcelResumido(List<object> datos, string nombreHoja, string columnaNombre, string nombreArchivo)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(nombreHoja);

                // Cabeceras
                worksheet.Cell(1, 1).Value = columnaNombre;
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
        [HttpGet("exportar/asistencia")]
        public IActionResult ExportarAsistencia()
        {
            var registros = _db.Asistencia
                .OrderByDescending(a => a.Fecha)
                .ThenBy(a => a.Nombre)
                .ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Asistencia");

                // Cabeceras
                worksheet.Cell(1, 1).Value = "Fecha";
                worksheet.Cell(1, 2).Value = "Nombre Completo";
                worksheet.Cell(1, 3).Value = "Hora Ingreso";
                worksheet.Cell(1, 4).Value = "Hora Salida";

                var header = worksheet.Range("A1:D1");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                int row = 2;
                foreach (var a in registros)
                {
                    worksheet.Cell(row, 1).Value = a.Fecha.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 2).Value = $"{a.Nombre} {a.Apellido}";
                    worksheet.Cell(row, 3).Value = a.HoraIngreso?.AddHours(-4).ToString("HH:mm:ss") ?? "--:--";
                    worksheet.Cell(row, 4).Value = a.HoraSalida?.AddHours(-4).ToString("HH:mm:ss") ?? "--:--";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Asistencia.xlsx");
                }
            }
        }
    }
}