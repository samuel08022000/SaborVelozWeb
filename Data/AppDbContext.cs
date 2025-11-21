using Microsoft.EntityFrameworkCore;
using SaborVeloz.Models;

namespace SaborVeloz.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // MEJORA 1: Inicializar con = null! para quitar warnings de nulabilidad
        public DbSet<Usuarios> Usuarios { get; set; } = null!;
        public DbSet<Productos> Productos { get; set; } = null!;
        public DbSet<Pagos> Pagos { get; set; } = null!;
        public DbSet<Caja> Cajas { get; set; } = null!;
        public DbSet<Ventas> Ventas { get; set; } = null!;
        public DbSet<DetalleVenta> DetalleVentas { get; set; } = null!;
        public DbSet<Comandas> Comandas { get; set; } = null!;
        public DbSet<VentasDiarias> VentasDiarias { get; set; }
        public DbSet<VentasSemanales> VentasSemanales { get; set; }
        public DbSet<VentasMensuales> VentasMensuales { get; set; }
        public DbSet<VentasAnuales> VentasAnuales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- MEJORA 2: Especificar precisión para todo el dinero ---
            // Esto le dice a SQL Server que guarde los decimales como decimal(18, 2)
            modelBuilder.Entity<Productos>().Property(p => p.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<Caja>().Property(c => c.MontoInicial).HasPrecision(18, 2);
            modelBuilder.Entity<Caja>().Property(c => c.MontoFinal).HasPrecision(18, 2);
            modelBuilder.Entity<Ventas>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            // (EF es inteligente y no intentará mapear 'Subtotal' porque es calculado en C#)

            // --- MEJORA 3: Prevenir borrado en cascada (¡MUY IMPORTANTE!) ---
            // Si borras un Producto, NO quieres que se borren los detalles de ventas antiguas.
            // Esta línea lo prohíbe.
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany() // Producto no tiene lista de DetalleVentas (y está bien)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict); // <-- La mejora

            // --- MEJORA 4: Índice Único para que no se repitan usuarios ---
            modelBuilder.Entity<Usuarios>()
                .HasIndex(u => u.Usuario)
                .IsUnique();

            // --- MEJORA 5: Código que SÍ es bueno mantener ---
            // La relación 1-a-1 entre Venta y Comanda es buena idea dejarla explícita.
            modelBuilder.Entity<Ventas>()
                .HasOne(v => v.Comanda)
                .WithOne(c => c.Venta)
                .HasForeignKey<Comandas>(c => c.IdVenta);

            // NOTA: La relación entre DetalleVenta y Venta la borré
            // porque EF la descubre 100% sola por convención.
            // No hace falta escribirla.
            modelBuilder.Entity<Caja>().HasKey(c => c.IdCaja);
            modelBuilder.Entity<Comandas>().HasKey(c => c.IdComanda);
            modelBuilder.Entity<DetalleVenta>().HasKey(d => d.IdDetalle);
            modelBuilder.Entity<Pagos>().HasKey(p => p.IdPago);
            modelBuilder.Entity<Usuarios>().HasKey(u => u.IdUsuario);
            modelBuilder.Entity<Productos>().HasKey(p => p.IdProducto);
            modelBuilder.Entity<Ventas>().HasKey(v => v.IdVenta);
            modelBuilder.Entity<VentasDiarias>().HasKey(v => v.Fecha);
            modelBuilder.Entity<VentasSemanales>().HasKey(v => new { v.Semana, v.Año });
            modelBuilder.Entity<VentasMensuales>().HasKey(v => new { v.Mes, v.Año });
            modelBuilder.Entity<VentasAnuales>().HasKey(v => v.Año);


        }
    }
}