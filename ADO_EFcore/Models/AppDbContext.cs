using Microsoft.EntityFrameworkCore;

namespace ADO_EFcore.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Items> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var items = modelBuilder.Entity<Items>();

            items.HasKey(i => i.Id);

            items.Property(i => i.Name)
                 .IsRequired()
                 .HasMaxLength(100);

            items.Property(i => i.Description)
                 .IsRequired()
                 .HasMaxLength(100);

            // Most reliable way in EF Core 8 to avoid HasColumnType error
            items.Property(i => i.Price)
                 .HasPrecision(18, 2)           // ← Use HasPrecision instead of HasColumnType
                 .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}