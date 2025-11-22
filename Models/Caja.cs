
namespace SaborVeloz.Models
{
    public class Caja
    {
        public int IdCaja { get; set; }
        public int IdUsuario { get; set; }
        public Usuarios Usuario { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal? MontoFinal { get; set; }
        public string Estado { get; set; } = "Abierta"; // Abierta, Cerrada

    }
}

