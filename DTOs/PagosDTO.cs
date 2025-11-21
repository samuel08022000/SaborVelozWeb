namespace SaborVeloz.DTOs
{
    public class PagosDTO
    {
        public int IdPago { get; set; }
        public string TipoPago { get; set; } = null!; // Efectivo, Tarjeta, QR
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
