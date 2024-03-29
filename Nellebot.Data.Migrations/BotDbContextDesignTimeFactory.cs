using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nellebot.Data.Migrations;

public class BotDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();

        services.AddDataProtection()
            .SetApplicationName(nameof(Nellebot))
            .SetDefaultKeyLifetime(TimeSpan.FromDays(180));

        var provider = services.BuildServiceProvider();

        var dataProtector = provider.GetDataProtectionProvider();

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(
                                      Directory.GetCurrentDirectory(),
                                      $"../{typeof(BotDbContextDesignTimeFactory).Namespace}"))
            .AddJsonFile("appsettings.json", false)
            .AddUserSecrets(typeof(BotDbContextDesignTimeFactory).Assembly)
            .Build();

        var builder = new DbContextOptionsBuilder<BotDbContext>();

        var dbConnString = config["Nellebot:ConnectionString"];

        builder.UseNpgsql(
                          dbConnString,
                          options =>
                          {
                              options.MigrationsAssembly(typeof(BotDbContextDesignTimeFactory).Assembly.FullName);
                          });

        return new BotDbContext(builder.Options, dataProtector);
    }
}
