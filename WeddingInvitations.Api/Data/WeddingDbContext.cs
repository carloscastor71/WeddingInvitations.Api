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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar relación uno-a-muchos entre Family y Guest
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Family)
                .WithMany(f => f.Guests)
                .HasForeignKey(g => g.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar índices únicos
            modelBuilder.Entity<Family>()
                .HasIndex(f => f.InvitationCode)
                .IsUnique();

        }
    }
}