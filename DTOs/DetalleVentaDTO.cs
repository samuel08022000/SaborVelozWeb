namespace SaborVeloz.DTOs
{
    public class DetalleVentaDTO
    {
        public int IdProducto { get; set; } //OBSERVACION PORQUE FALTA ID VENTA
        public string Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
