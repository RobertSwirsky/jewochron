using Microsoft.EntityFrameworkCore;
using Jewochron.Models;

namespace Jewochron.Data
{
    /// <summary>
    /// Database context for managing Yahrzeit data
    /// </summary>
    public class YahrzeitDbContext : DbContext
    {
        public DbSet<Yahrzeit> Yahrzeits { get; set; }

        public YahrzeitDbContext(DbContextOptions<YahrzeitDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Yahrzeit>(entity =>
            {
                entity.ToTable("yahrzeits");
                
                entity.HasIndex(e => new { e.HebrewMonth, e.HebrewDay })
                    .HasDatabaseName("idx_hebrew_date");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            });
        }
    }
}
