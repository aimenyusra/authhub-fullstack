using AuthHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthHub.Api.Data
{
    public class AppDbContext : DbContext

    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
    }

    }
