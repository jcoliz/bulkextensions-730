using Microsoft.EntityFrameworkCore;

namespace bulkextensions_730.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Parent> Parents { get; set; }
    public DbSet<Child> Children { get; set; }

    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
}
