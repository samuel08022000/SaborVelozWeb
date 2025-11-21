using System.ComponentModel.DataAnnotations.Schema;

namespace SaborVeloz.Models
{
    [Table("Pagos")] // 👈 usa el mismo nombre que la tabla en SQL Server
    public class Pagos
    {
        public int IdPago { get; set; }
        public string TipoPago { get; set; } = null!; // Efectivo, Tarjeta, QR
    }
}