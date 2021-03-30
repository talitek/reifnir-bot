using Microsoft.EntityFrameworkCore;

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

    }
}
