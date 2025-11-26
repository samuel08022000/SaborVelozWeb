using System.ComponentModel.DataAnnotations.Schema;

namespace SaborVeloz.Models
{
    [Table("DetalleVenta")] // Asegura que coincida con el nombre exacto de la tabla en SQL
    public class DetalleVenta
    {
        public int IdDetalle { get; set; }

        public int IdVenta { get; set; }
        // Relación inversa hacia Ventas (puede ser nula para evitar ciclos en JSON si no se configura IgnoreCycles)
        public Ventas Venta { get; set; } = null!;

        public int IdProducto { get; set; }
        [ForeignKey("IdProducto")]
        public Productos Producto { get; set; } = null!;

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")] // Opcional: especifica la precisión si es necesario
        public decimal PrecioUnitario { get; set; }

        // 🚨 ESTO ES LO CRÍTICO PARA QUE FUNCIONE:
        // Le dice a EF que la columna es calculada en la BD.
        // Si quitas esto, EF intentará guardar el valor y fallará con el error "computed column".
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal Subtotal { get; set; }
    }
}