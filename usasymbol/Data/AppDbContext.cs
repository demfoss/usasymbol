using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using usasymbol.Models;
using USASymbol.Models;

namespace USASymbol.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {

        }

        public DbSet<State> States { get; set; }
        public DbSet<Symbol> Symbols { get; set; }
        public DbSet<SymbolCategory> SymbolCategories { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizEvent> QuizEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<QuizEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuizSlug).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Mode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.QuestionKey).HasMaxLength(300);
                entity.Property(e => e.Choice).HasMaxLength(200);
                entity.HasIndex(e => new { e.QuizSlug, e.EventType });
            });

            modelBuilder.Entity<State>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Abbreviation).HasMaxLength(2);
            });


            modelBuilder.Entity<Symbol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StateId, e.Type });
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);


                entity.HasOne(e => e.State)
                    .WithMany(s => s.Symbols)
                    .HasForeignKey(e => e.StateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SymbolCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(300);

                entity.HasIndex(e => e.Type).IsUnique();
            });
        }
    }
}