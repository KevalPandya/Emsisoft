using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Common
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Hashes> Hashes { get; set; }
    }
}
