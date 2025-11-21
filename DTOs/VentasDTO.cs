namespace SaborVeloz.DTOs
{
    public class VentaDTO
    {
        public int IdVenta { get; set; }
        public string Cajero { get; set; } = null!;
        public string MetodoPago { get; set; } = null!;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public List<DetalleVentaDTO> Detalles { get; set; } = new();
    }
}
