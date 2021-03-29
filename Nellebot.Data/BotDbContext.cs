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
            
        }

        public DbSet<GuildSetting> GuildSettings { get; set; } = null!;
    }
}
