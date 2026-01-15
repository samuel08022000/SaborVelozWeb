using System;
using System.Collections.Generic;

namespace SaborVeloz.DTOs
{
    public class ComandasDTO
    {
        public int IdComanda { get; set; }
        public string NumeroTicket { get; set; } = null!;

        // 🚨 NUEVO: Esto le dirá al cocinero si empacar o servir
        public string NombreCliente { get; set; } = "General";
        public string TipoPedido { get; set; } = null!;

        public int IdVenta { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public List<DetalleComandaDTO> Productos { get; set; } = new();
    }

    public class DetalleComandaDTO
    {
        public string Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        // public string? Notas { get; set; } // Futuro: "Sin cebolla"
    }
}