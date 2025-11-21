namespace SaborVelozWeb.DTOs
{
    public class ProductoEditarDTO
    {
        public string NombreProducto { get; set; } = null!;
        public decimal Precio { get; set; }
        public bool Disponible { get; set; }
    }

}
