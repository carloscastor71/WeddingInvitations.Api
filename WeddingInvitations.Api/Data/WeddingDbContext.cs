using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Data
{
    public class WeddingDbContext : DbContext
    {
        public WeddingDbContext(DbContextOptions<WeddingDbContext> options) : base(options)
        {
        }

        // DbSets (tablas)
        public DbSet<Family> Families { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Table> Tables { get; set; } // <-- AGREGAR ESTA LÍNEA


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar relación uno-a-muchos entre Family y Guest
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Family)
                .WithMany(f => f.Guests)
                .HasForeignKey(g => g.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar relación uno-a-muchos entre Table y Guest  <-- AGREGAR DESDE AQUÍ
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Table)
                .WithMany(t => t.Guests)
                .HasForeignKey(g => g.TableId)
                .OnDelete(DeleteBehavior.SetNull); // Si se elimina mesa, invitados quedan sin asignar


            // Configurar índices únicos
            modelBuilder.Entity<Family>()
                .HasIndex(f => f.InvitationCode)
                .IsUnique();
            // Índice para TableNumber para búsquedas rápidas
            modelBuilder.Entity<Table>()
                .HasIndex(t => t.TableNumber);  // <-- HASTA AQUÍ

        }
    }
}