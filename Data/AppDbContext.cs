using Microsoft.EntityFrameworkCore;
using FGP.Server.Models;

namespace FGP.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Repository> Repositories { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<PullRequest> PullRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // This ensures the "HeadHash" is treated as a concurrency token.
            // When we try to update a branch, EF Core will check if this value 
            // has changed since we loaded it.
            modelBuilder.Entity<Branch>()
                .Property(b => b.HeadHash)
                .IsConcurrencyToken();
        }
}