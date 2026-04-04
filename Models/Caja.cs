
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

        // --- LO QUE EL CAJERO DICE QUE TIENE ---
        public decimal? MontoFinalDeclarado { get; set; }

        // --- NUEVO: LO QUE EL SISTEMA CALCULA ---
        public decimal? MontoCalculadoSistema { get; set; }
        public decimal? Diferencia { get; set; } // Negativo = faltante, Positivo = sobrante

        public string Estado { get; set; } = "Abierta";
    }
}

