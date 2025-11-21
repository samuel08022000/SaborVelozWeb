using SaborVeloz.Models;
namespace SaborVeloz.Models
{
    public class Ventas
    {
        public int IdVenta { get; set; }
        public int IdUsuario { get; set; } // Cajero
        public Usuarios Usuario { get; set; } = null!; 
        public int IdPago { get; set; }
        public Pagos Pago { get; set; } = null!;
        public DateTime FechaVenta { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public ICollection<DetalleVenta> Detalles { get; set; } = null!;
        public Comandas Comanda { get; set; } = null!;
    }
}

