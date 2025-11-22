namespace SaborVelozWeb.DTOs
{
    public class ProductoCrearDTO
    {
        public string NombreProducto { get; set; } = null!;
        public decimal Precio { get; set; }
        public string Categoria { get; set; } = "General"; // <--- AGREGAR ESTO
        public bool Disponible { get; set; } = true;
    }

}
