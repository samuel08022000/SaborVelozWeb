namespace SaborVeloz.DTOs
{
    public class UsuarioCreateDTO
    {
        public string Nombre { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Rol { get; set; } = null!;
    }
}
