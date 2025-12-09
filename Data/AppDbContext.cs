using Microsoft.EntityFrameworkCore;
using FGP.Server.Models;

namespace FGP.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This tells the DB: "Please create a table for PullRequests"
    public DbSet<PullRequest> PullRequests { get; set; }
}