namespace SaborVeloz.DTOs
{
    public class ComandaDTO
    {
        public int IdComanda { get; set; }
        public int IdVenta { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; } = null!; // Pendiente, En preparación, Entregada
        public List<DetalleVentaDTO> Detalles { get; set; } = new();
    }
}
