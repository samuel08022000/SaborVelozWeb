namespace SaborVeloz.Models
{
    public class Usuarios
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string ContrasenaHash { get; set; } = null!;
        public string Rol { get; set; } = null!; // Administrador, Cajero, Cocina
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}