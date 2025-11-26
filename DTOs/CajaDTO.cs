namespace SaborVeloz.DTOs
{
    public class CajaDTO
    {
        public int IdCaja { get; set; }
        public int IdUsuario { get; set; } // Antes tenías UsuarioId
        public string Cajero { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal? MontoFinal { get; set; }
        public string Estado { get; set; } = null!;
    }
}
