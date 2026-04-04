using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace SaborVeloz.Models
{
    public class Asistencia
    {
        public int IdAsistencia { get; set; }

        // ADIOS A ESCRIBIR NOMBRES MANUALMENTE, AHORA USAMOS EL ID DEL USUARIO
        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuarios Usuario { get; set; } = null!;

        public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
        public DateTime? HoraIngreso { get; set; }
        public DateTime? HoraSalida { get; set; }

        // NUEVO: Para saber si llegó tarde o puntual
        public string EstadoPuntualidad { get; set; } = "Puntual";
    }
}