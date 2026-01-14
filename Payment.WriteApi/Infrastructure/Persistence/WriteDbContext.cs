using Microsoft.EntityFrameworkCore;
using Payment.WriteApi.Domain;

namespace Payment.WriteApi.Infrastructure.Persistence;

public class WriteDbContext : DbContext
{
    public WriteDbContext(DbContextOptions<WriteDbContext> options)
        : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
}
