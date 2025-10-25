using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using USASymbol.Models;

namespace USASymbol.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<State> States { get; set; }
        public DbSet<Symbol> Symbols { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // State configuration
            modelBuilder.Entity<State>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Abbreviation).HasMaxLength(2);
            });

            // Symbol configuration
            modelBuilder.Entity<Symbol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StateId, e.Type });
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);

                // Relationship
                entity.HasOne(e => e.State)
                    .WithMany(s => s.Symbols)
                    .HasForeignKey(e => e.StateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}