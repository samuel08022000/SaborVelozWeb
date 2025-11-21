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

            DateTime fecha = venta.FechaVenta;
            decimal total = venta.Total;

            // -------------------------
            // VENTAS DIARIAS (por fecha)
            // -------------------------
            var diario = db.VentasDiarias.FirstOrDefault(v => v.Fecha == fecha.Date);
            if (diario == null)
            {
                diario = new VentasDiarias { Fecha = fecha.Date, TotalVentas = total };
                db.VentasDiarias.Add(diario);
            }
            else
            {
                diario.TotalVentas += total;
                db.VentasDiarias.Update(diario);
            }
            db.SaveChanges();

            // -------------------------
            // VENTAS SEMANALES (semana ISO, año)
            // -------------------------
            var cal = CultureInfo.InvariantCulture.Calendar;
            // Para ISO week use CalendarWeekRule.FirstFourDayWeek and Monday
            int semana = cal.GetWeekOfYear(fecha, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int año = fecha.Year;
            // Ajuste para week crossing year start (ISO weeks that belong to previous year)
            // Este ajuste es básico; si quieres ISO completamente exacto usa NodaTime o función más avanzada.
            if (fecha.Month == 1 && semana >= 52) año = fecha.Year - 1;

            var semanal = db.VentasSemanales.FirstOrDefault(v => v.Semana == semana && v.Año == año);
            if (semanal == null)
            {
                semanal = new VentasSemanales { Semana = semana, Año = año, TotalVentas = total };
                db.VentasSemanales.Add(semanal);
            }
            else
            {
                semanal.TotalVentas += total;
                db.VentasSemanales.Update(semanal);
            }
            db.SaveChanges();

            // -------------------------
            // VENTAS MENSUALES (mes,año)
            // -------------------------
            var mes = fecha.Month;
            var mesAño = fecha.Year;
            var mensual = db.VentasMensuales.FirstOrDefault(v => v.Mes == mes && v.Año == mesAño);
            if (mensual == null)
            {
                mensual = new VentasMensuales { Mes = mes, Año = mesAño, TotalVentas = total };
                db.VentasMensuales.Add(mensual);
            }
            else
            {
                mensual.TotalVentas += total;
                db.VentasMensuales.Update(mensual);
            }
            db.SaveChanges();

            // -------------------------
            // VENTAS ANUALES (año)
            // -------------------------
            var anual = db.VentasAnuales.FirstOrDefault(v => v.Año == fecha.Year);
            if (anual == null)
            {
                anual = new VentasAnuales { Año = fecha.Year, TotalVentas = total };
                db.VentasAnuales.Add(anual);
            }
            else
            {
                anual.TotalVentas += total;
                db.VentasAnuales.Update(anual);
            }
            db.SaveChanges();
        }
    }
}
