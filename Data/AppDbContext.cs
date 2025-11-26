using Microsoft.EntityFrameworkCore;
using SaborVeloz.Models;

namespace SaborVeloz.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DB Sets
        public DbSet<Usuarios> Usuarios { get; set; } = null!;
        public DbSet<Productos> Productos { get; set; } = null!;
        public DbSet<Pagos> Pagos { get; set; } = null!;
        public DbSet<Caja> Caja { get; set; } = null!;
        public DbSet<Ventas> Ventas { get; set; } = null!;
        public DbSet<Comandas> Comandas { get; set; } = null!;

        // Tablas de Reportes
        public DbSet<VentasDiarias> VentasDiarias { get; set; }
        public DbSet<VentasSemanales> VentasSemanales { get; set; }
        public DbSet<VentasMensuales> VentasMensuales { get; set; }
        public DbSet<VentasAnuales> VentasAnuales { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configuración de Precisión (Dinero)
            modelBuilder.Entity<Productos>().Property(p => p.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<Caja>().Property(c => c.MontoInicial).HasPrecision(18, 2);
            modelBuilder.Entity<Caja>().Property(c => c.MontoFinal).HasPrecision(18, 2);
            modelBuilder.Entity<Ventas>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);

            // ==================================================================
            // 2. MAPEO DE RELACIONES (CORRECCIÓN TOTAL) 🛠️
            // ==================================================================

            // A) VENTAS -> PAGO
            modelBuilder.Entity<Ventas>()
                .HasOne(v => v.Pago)
                .WithMany()
                .HasForeignKey(v => v.IdPago)
                .OnDelete(DeleteBehavior.Restrict);

            // B) VENTAS -> CAJERO (Usuario)
            modelBuilder.Entity<Ventas>()
                .HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // C) CAJA -> CAJERO (Usuario)
            modelBuilder.Entity<Caja>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // D) DETALLES DE VENTA (¡Aquí estaba el error!) 👈
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 ESTA ES LA LÍNEA QUE FALTABA: Conectar Detalle con Venta 🔥
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.Cascade); // Si borras venta, se borran detalles

            // E) COMANDA -> VENTA
            modelBuilder.Entity<Comandas>()
                .HasOne(c => c.Venta)
                .WithOne(v => v.Comanda)
                .HasForeignKey<Comandas>(c => c.IdVenta)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================================================================

            // Otras configuraciones
            modelBuilder.Entity<Usuarios>().HasIndex(u => u.Usuario).IsUnique();

            // Definición de Llaves Primarias
            modelBuilder.Entity<Caja>().HasKey(c => c.IdCaja);
            modelBuilder.Entity<Comandas>().HasKey(c => c.IdComanda);
            modelBuilder.Entity<DetalleVenta>().HasKey(d => d.IdDetalle);
            modelBuilder.Entity<Pagos>().HasKey(p => p.IdPago);
            modelBuilder.Entity<Usuarios>().HasKey(u => u.IdUsuario);
            modelBuilder.Entity<Productos>().HasKey(p => p.IdProducto);
            modelBuilder.Entity<Ventas>().HasKey(v => v.IdVenta);

            // Llaves de Reportes
            modelBuilder.Entity<VentasDiarias>().HasKey(v => v.Fecha);
            modelBuilder.Entity<VentasSemanales>().HasKey(v => new { v.Semana, v.Año });
            modelBuilder.Entity<VentasMensuales>().HasKey(v => new { v.Mes, v.Año });
            modelBuilder.Entity<VentasAnuales>().HasKey(v => v.Año);
        }
    }
}