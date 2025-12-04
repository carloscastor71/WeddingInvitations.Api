using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Data
{
    public class WeddingDbContext : DbContext
    {
        public WeddingDbContext(DbContextOptions<WeddingDbContext> options) : base(options)
        {
        }

        // ===== TABLAS (DbSets) =====
        public DbSet<Family> Families { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<TempPdfPass> TempPdfPasses { get; set; }  // ⭐ NUEVA TABLA


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================
            // RELACIONES EXISTENTES (No cambiar)
            // ========================================

            // Relación: Family → Guests (uno a muchos)
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Family)
                .WithMany(f => f.Guests)
                .HasForeignKey(g => g.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación: Table → Guests (uno a muchos)
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Table)
                .WithMany(t => t.Guests)
                .HasForeignKey(g => g.TableId)
                .OnDelete(DeleteBehavior.SetNull); // Si se elimina mesa, invitados quedan sin asignar


            // ========================================
            // ⭐ NUEVAS RELACIONES PARA TempPdfPass
            // ========================================

            // Relación: TempPdfPass → Family (muchos a uno, opcional)
            modelBuilder.Entity<TempPdfPass>()
                .HasOne(p => p.Family)
                .WithMany() // Family no necesita lista de PDFs
                .HasForeignKey(p => p.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina familia, eliminar sus PDFs

            // Relación: TempPdfPass → Table (muchos a uno, opcional)
            modelBuilder.Entity<TempPdfPass>()
                .HasOne(p => p.Table)
                .WithMany() // Table no necesita lista de PDFs
                .HasForeignKey(p => p.TableId)
                .OnDelete(DeleteBehavior.SetNull); // Si se elimina mesa, el PDF queda sin mesa


            // ========================================
            // ÍNDICES EXISTENTES (No cambiar)
            // ========================================

            // Índice único en código de invitación
            modelBuilder.Entity<Family>()
                .HasIndex(f => f.InvitationCode)
                .IsUnique();

            // Índice en número de mesa
            modelBuilder.Entity<Table>()
                .HasIndex(t => t.TableNumber);


            // ========================================
            // ⭐ NUEVOS ÍNDICES PARA TempPdfPass
            // ========================================

            // ÍNDICE 1: Por FamilyId (para regenerar pases)
            // Uso: DELETE FROM TempPdfPasses WHERE FamilyId = X
            modelBuilder.Entity<TempPdfPass>()
                .HasIndex(p => p.FamilyId)
                .HasDatabaseName("IX_TempPdfPasses_FamilyId");

            // ÍNDICE 2: Por FileName ÚNICO (para servir PDFs)
            // Uso: SELECT * FROM TempPdfPasses WHERE FileName = 'Garcia_Mesa5_xxx.pdf'
            modelBuilder.Entity<TempPdfPass>()
                .HasIndex(p => p.FileName)
                .IsUnique() // Cada PDF debe tener nombre único
                .HasDatabaseName("IX_TempPdfPasses_FileName");

            // ÍNDICE 3: Por ExpiresAt (para limpieza automática)
            // Uso: DELETE FROM TempPdfPasses WHERE ExpiresAt < NOW()
            modelBuilder.Entity<TempPdfPass>()
                .HasIndex(p => p.ExpiresAt)
                .HasDatabaseName("IX_TempPdfPasses_ExpiresAt");

            // ÍNDICE 4: Compuesto FamilyId + TableId (para buscar combinación específica)
            // Uso: SELECT * FROM TempPdfPasses WHERE FamilyId = X AND TableId = Y
            modelBuilder.Entity<TempPdfPass>()
                .HasIndex(p => new { p.FamilyId, p.TableId })
                .HasDatabaseName("IX_TempPdfPasses_FamilyId_TableId");
        }
    }
}