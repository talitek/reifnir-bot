using DSharpPlus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;
using Nellebot.Data;
using Nellebot.Data.Repositories;
using Nellebot.EventHandlers;
using Nellebot.Services;
using Nellebot.Services.HtmlToImage;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Nellebot.Workers;
using System;

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

                    services.AddHttpClient<OrdbokHttpClient>();

                    services.AddMediatR(typeof(Program));
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));

                    services.AddSingleton<SharedCache>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();

                    services.AddHostedService<BotWorker>();
                    services.AddHostedService<CommandQueueWorker>();
                    services.AddHostedService<MessageAwardQueueWorker>();

                    services.AddSingleton<CommandQueue>();
                    services.AddSingleton<MessageAwardQueue>();

                    services.AddSingleton<AwardEventHandler>();
                    services.AddSingleton<CommandEventHandler>();
                    services.AddSingleton<BlacklistEventHandler>();

                    services.AddTransient<AuthorizationService>();
                    services.AddTransient<DiscordErrorLogger>();
                    services.AddTransient<UserRoleService>();
                    services.AddTransient<RoleService>();
                    services.AddTransient<AwardMessageService>();
                    services.AddTransient<DiscordResolver>();
                    services.AddTransient<ScribanTemplateLoader>();
                    services.AddTransient<OrdbokModelMapper>();
                    services.AddTransient<IOrdbokContentParser, OrdbokContentParser>();
                    services.AddTransient<HtmlToImageService>();
                    services.AddTransient<WkHtmlToImageClient>();

                    services.AddTransient<IUserRoleRepository, UserRoleRepository>();
                    services.AddTransient<AwardMessageRepository>();

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
                            Token = botToken,
                            Intents = DiscordIntents.All
                        };

                        var client = new DiscordClient(socketConfig);

                        return client;
                    });
                })
                .UseSystemd();
    }
}
