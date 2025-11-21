namespace SaborVeloz.DTOs
{
    public class ProductosDTO
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = null!;
        public decimal Precio { get; set; }
        public string Categoria { get; set; } = null!;
        public bool Disponible { get; set; }
    }
}
