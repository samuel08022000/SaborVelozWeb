namespace SaborVelozWeb.DTOs
{
    public class ProductoEditarDTO
    {
        public string NombreProducto { get; set; } = null!;
        public decimal Precio { get; set; }
        public string Categoria { get; set; } = null!; // <--- AGREGAR ESTO (Opcional) 
        public bool Disponible { get; set; }
    }

}
