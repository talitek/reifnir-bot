using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.Data;
using Nellebot.Data.Repositories;
using Nellebot.EventHandlers;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nellebot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.OptionsKey));

                    services.AddHostedService<BotWorker>();

                    services.AddSingleton<SharedCache>();

                    services.AddTransient<AuthorizationService>();
                    services.AddTransient<DiscordErrorLogger>();
                    services.AddTransient<UserRoleService>();
                    services.AddTransient<RoleService>();

                    services.AddTransient<IUserRoleRepository, UserRoleRepository>();

                    services.AddDbContext<BotDbContext>(builder =>
                    {
                        var dbConnString = hostContext.Configuration.GetValue<string>("Nellebot:ConnectionString");
                        var logLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");

                        builder.EnableSensitiveDataLogging(logLevel == "Debug");

                        builder.UseNpgsql(dbConnString);
                    },
                    ServiceLifetime.Transient,
                    ServiceLifetime.Singleton);

                    services.AddSingleton((_) =>
                    {
                        var defaultLogLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");
                        var botToken = hostContext.Configuration.GetValue<string>("Nellebot:BotToken");

                        var logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

                        var socketConfig = new DiscordConfiguration
                        {
                            MinimumLogLevel = logLevel,
                            TokenType = TokenType.Bot,
                            Token = botToken
                        };

                        var client = new DiscordClient(socketConfig);

                        return client;
                    });

                    services.AddSingleton<CommandEventHandler>();
                })
                .UseSystemd();
    }
}
