using Microsoft.EntityFrameworkCore;
using Jewochron.Models;

namespace Jewochron.Data
{
    /// <summary>
    /// Database context for managing Simcha data
    /// </summary>
    public class SimchaDbContext : DbContext
    {
        public DbSet<Simcha> Simchas { get; set; }

        public SimchaDbContext(DbContextOptions<SimchaDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Simcha>(entity =>
            {
                entity.ToTable("simchas");

                entity.HasIndex(e => new { e.HebrewMonth, e.HebrewDay })
                    .HasDatabaseName("idx_hebrew_date");

                entity.HasIndex(e => e.EnglishDate)
                    .HasDatabaseName("idx_english_date");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.IsRecurring)
                    .HasDefaultValue(true);
            });
        }
    }
}
