using System.ComponentModel.DataAnnotations.Schema;

namespace SaborVeloz.Models
{
    [Table("DetalleVenta")] // 👈 nombre exacto de la tabla en SQL
    public class DetalleVenta
    {
        public int IdDetalle { get; set; }
        public int IdVenta { get; set; }
        public Ventas Venta { get; set; } = null!;
        public int IdProducto { get; set; }
        public Productos Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Esto está perfecto, EF no lo mapea a la DB
        // y se calcula en C#
        // ✅ Correcto (Lectura y Escritura)
        public decimal Subtotal { get; set; }
    }
}