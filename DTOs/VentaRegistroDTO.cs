namespace SaborVeloz.DTOs
{
    public class VentaRegistroDTO
    {
        public string Usuario { get; set; } = null!;
        public string MetodoPago { get; set; } = null!;
        public List<ProductoVentaDTO> Productos { get; set; } = new();
    }

    public class ProductoVentaDTO
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }
}
