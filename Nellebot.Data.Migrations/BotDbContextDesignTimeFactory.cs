using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Nellebot.Data.Migrations
{
    public class BotDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                            $"../{typeof(BotDbContextDesignTimeFactory).Namespace}"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets(typeof(BotDbContextDesignTimeFactory).Assembly)
                .Build();

            var builder = new DbContextOptionsBuilder<BotDbContext>();

            var dbConnString = config["Nellebot:ConnectionString"];

            builder.UseNpgsql(dbConnString, options => {
                options.MigrationsAssembly(typeof(BotDbContextDesignTimeFactory).Assembly.FullName);
            });

            return new BotDbContext(builder.Options);
        }
    }
}
