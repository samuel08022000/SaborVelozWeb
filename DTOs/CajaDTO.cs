namespace SaborVeloz.DTOs
{
    public class CajaDTO
    {
        public int IdCaja { get; set; }
        public string Cajero { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal? MontoFinal { get; set; }
        public string Estado { get; set; } = null!;
    }
}
