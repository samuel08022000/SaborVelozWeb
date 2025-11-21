using System.ComponentModel.DataAnnotations.Schema;

namespace SaborVeloz.Models
{
    [Table("Comandas")] // 👈 Usa el mismo nombre de la tabla real
    public class Comandas
    {
        public int IdComanda { get; set; } // 👈 Primary key obligatoria

        public int IdVenta { get; set; }
        public Ventas Venta { get; set; } = null!;

        public string Estado { get; set; } = "Pendiente"; // Pendiente, En preparación, Listo
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }
}
