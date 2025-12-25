using Microsoft.EntityFrameworkCore;

namespace ExternalService;

public class DefaultDbContext(
    DbContextOptions<DefaultDbContext> options)
    : DbContext(options)
{
    public DbSet<Quotation> Quotations { get; set; }
    
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        var q = modelBuilder.Entity<Quotation>();
        q.ToTable("quotations");
        q.HasKey(x => x.TickerId);
        q.Property(x => x.TickerId)
            .HasMaxLength(32)
            .IsRequired();

        q.Property(x => x.Value)
            .HasColumnType("decimal(18,4)")
            .IsRequired();
    }

}