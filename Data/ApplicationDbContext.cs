using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    // Constructor primario: reemplaza el constructor clásico + base(options)
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        // Tabla de mascotas (antes era "Products" en el esqueleto original)
        public DbSet<Mascota> Mascotas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Guarda el enum Tipo y Sexo como texto legible en la base de
            // datos (por ejemplo "Perro" en vez del número 0). Así, si
            // alguien mira la tabla directo en Supabase, se entiende.
            modelBuilder.Entity<Mascota>()
                .Property(m => m.Tipo)
                .HasConversion<string>();

            modelBuilder.Entity<Mascota>()
                .Property(m => m.Sexo)
                .HasConversion<string>();
        }
    }
}