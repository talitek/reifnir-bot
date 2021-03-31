using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;

namespace Nellebot.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserRole>()
                    .HasIndex(x => new { x.RoleId, x.Name })
                    .IsUnique();

            builder.Entity<UserRoleAlias>()
                    .HasIndex(x => new { x.Alias })
                    .IsUnique();
        }

        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserRoleAlias> UserRoleAliases { get; set; } = null!;

    }
}
