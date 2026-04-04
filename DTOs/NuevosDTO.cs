namespace SaborVeloz.DTOs
{
    // DTO para el Arqueo Ciego
    public class CerrarCajaCiegoDTO
    {
        public int IdUsuario { get; set; }
        public decimal MontoEfectivoFisico { get; set; } // Lo que el cajero cuenta con sus manos
    }

    // DTO para el Escaneo del QR
    public class EscaneoQrDTO
    {
        public int IdUsuario { get; set; }
        public string CodigoQrEscaneado { get; set; } = null!; // El código que lee la cámara
        public string TipoAccion { get; set; } = null!; // "Entrada" o "Salida"
    }
}