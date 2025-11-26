using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaborVeloz.Models
{
    public class Ventas
    {
        [Key]
        public int IdVenta { get; set; }

        // El ticket generado por C# (SIN [DatabaseGenerated])
        public string NumeroTicket { get; set; } = null!;

        // 🚨 NUEVO: Campo obligatorio en BD ahora
        public string TipoPedido { get; set; } = null!;

        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuarios Usuario { get; set; } = null!;

        public int IdPago { get; set; }
        [ForeignKey("IdPago")]
        public Pagos Pago { get; set; } = null!;

        public int IdCaja { get; set; }
        [ForeignKey("IdCaja")]
        public Caja? Caja { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; } = null!;
        public Comandas? Comanda { get; set; }
    }
}