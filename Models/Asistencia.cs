namespace SaborVeloz.Models
{
    public class Asistencia
    {
        public int IdAsistencia { get; set; }
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
        public DateTime? HoraIngreso { get; set; }
        public DateTime? HoraSalida { get; set; }
    }
}