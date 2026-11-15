using Microsoft.EntityFrameworkCore;
using Prophix.Api.Domain;

namespace Prophix.Api.Data;

public class FpaDbContext(DbContextOptions<FpaDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<FactEntry> Facts => Set<FactEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>(e =>
        {
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.Code).HasMaxLength(20);
            e.Property(a => a.Name).HasMaxLength(200);
            e.Property(a => a.Type).HasConversion<string>();      // readable enums in the DB
        });

        b.Entity<Department>(e => e.Property(d => d.Name).HasMaxLength(100));

        b.Entity<FactEntry>(e =>
        {
            e.Property(f => f.Scenario).HasConversion<string>().HasMaxLength(20);
            e.Property(f => f.Amount).HasPrecision(18, 2);         // numeric(18,2) in Postgres
            // Composite index matching the report query's WHERE clause.
            e.HasIndex(f => new { f.Year, f.Scenario, f.Month });
            e.HasOne(f => f.Account).WithMany(a => a.Facts).HasForeignKey(f => f.AccountId);
            e.HasOne(f => f.Department).WithMany().HasForeignKey(f => f.DepartmentId);
        });

        // SQLite stores decimal as TEXT and cannot SUM it server-side.
        // Postgres (prod) keeps real numeric; SQLite (dev/tests) maps to REAL.
        if (Database.IsSqlite())
            b.Entity<FactEntry>().Property(f => f.Amount).HasConversion<double>();
    }
}
