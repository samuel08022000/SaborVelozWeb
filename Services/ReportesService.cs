using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using SaborVeloz.Models;

namespace SaborVeloz.Services
{
    public static class ReportesService
    {
        // Llamar después de guardar una venta
        public static void ActualizarReportes(AppDbContext db, Ventas venta)
        {
            if (venta == null) return;

            try
            {
                DateTime fecha = venta.FechaVenta;
                decimal total = venta.Total;

                // 1. DIARIO
                var diario = db.VentasDiarias.FirstOrDefault(v => v.Fecha == fecha.Date);
                if (diario == null) db.VentasDiarias.Add(new VentasDiarias { Fecha = fecha.Date, TotalVentas = total });
                else { diario.TotalVentas += total; db.VentasDiarias.Update(diario); }

                // 2. SEMANAL
                var cal = CultureInfo.InvariantCulture.Calendar;
                int semana = cal.GetWeekOfYear(fecha, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int año = fecha.Year;
                // Ajuste simplificado
                if (fecha.Month == 1 && semana >= 52) año--;

                var semanal = db.VentasSemanales.FirstOrDefault(v => v.Semana == semana && v.Año == año);
                if (semanal == null) db.VentasSemanales.Add(new VentasSemanales { Semana = semana, Año = año, TotalVentas = total });
                else { semanal.TotalVentas += total; db.VentasSemanales.Update(semanal); }

                // 3. MENSUAL
                var mensual = db.VentasMensuales.FirstOrDefault(v => v.Mes == fecha.Month && v.Año == fecha.Year);
                if (mensual == null) db.VentasMensuales.Add(new VentasMensuales { Mes = fecha.Month, Año = fecha.Year, TotalVentas = total });
                else { mensual.TotalVentas += total; db.VentasMensuales.Update(mensual); }

                // 4. ANUAL
                var anual = db.VentasAnuales.FirstOrDefault(v => v.Año == fecha.Year);
                if (anual == null) db.VentasAnuales.Add(new VentasAnuales { Año = fecha.Year, TotalVentas = total });
                else { anual.TotalVentas += total; db.VentasAnuales.Update(anual); }

                // ⭐ SOLO UN SAVECHANGES AL FINAL ⭐
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log del error, pero NO lanzamos throw.
                // Esto asegura que la venta NO se cancele si el reporte falla.
                Console.WriteLine($"Error actualizando reportes: {ex.Message}");
            }
        }
    }
}
