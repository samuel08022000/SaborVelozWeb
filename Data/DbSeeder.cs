
using SaborVeloz.Data;
using SaborVeloz.Models;
using SaborVeloz.Services;

namespace SaborVeloz.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Usuarios.Any())
            {
                context.Usuarios.AddRange(
                    new Usuarios
                    {
                        Nombre = "Admin Sabor",
                        Usuario = "admin",
                        ContrasenaHash = AuthService.HashPassword("1a2e3i4o5U"), // <-- ¡APLICA EL HASH A LA CONTRASEÑA!
                        Rol = "Administrador"
                    },
                    new Usuarios
                    {
                        Nombre = "Cajero Veloz",
                        Usuario = "cajero",
                        ContrasenaHash = AuthService.HashPassword("1a2e3i4o5U"), // <-- ¡APLICA EL HASH A LA CONTRASEÑA!
                        Rol = "Cajero"
                    },
                    new Usuarios
                    {
                        Nombre = "Cocina Sabor",
                        Usuario = "cocina",
                        ContrasenaHash = AuthService.HashPassword("1a2e3i4o5U"), // <-- ¡APLICA EL HASH A LA CONTRASEÑA!
                        Rol = "Cocina"
                    }
                );
            }

            if (!context.Pagos.Any())
            {
                context.Pagos.AddRange(
                    new Pagos { TipoPago = "Efectivo" },
                    new Pagos { TipoPago = "Tarjeta" },
                    new Pagos { TipoPago = "QR" }
                );
            }

            if (!context.Productos.Any())
            {
                context.Productos.AddRange(
                    new Productos { NombreProducto = "Salchipapa", Precio = 13 },
                    new Productos { NombreProducto = "Hamburguesa", Precio = 13 },
                    new Productos { NombreProducto = "Pollo Frito", Precio = 18 },
                    new Productos { NombreProducto = "Salchiburguer", Precio = 18 },
                    new Productos { NombreProducto = "Pipocas de Pollo", Precio = 18 },
                    new Productos { NombreProducto = "Pipocas de Carne", Precio = 18 }
                );
            }

            context.SaveChanges();
        }
    }
}

