using System;
using System.Threading.Channels;
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
using Nellebot.Services.Glosbe;
using Nellebot.Services.HtmlToImage;
using Nellebot.Services.Loggers;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.OptionsKey));

                services.AddHttpClient<OrdbokHttpClient>();

                services.AddMediatR(typeof(Program));
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
                services.AddTransient<NotificationPublisher>();

                services.AddSingleton<SharedCache>();
                services.AddSingleton<ILocalizationService, LocalizationService>();
                services.AddSingleton<PuppeteerFactory>();

                AddWorkers(services);

                AddChannels(services);

                AddBotEventHandlers(services);

                AddInternalServices(services);

                AddRepositories(services);

                AddDbContext(hostContext, services);

                AddDiscordClient(hostContext, services);
            })
            .UseSystemd();
    }

    private static void AddDiscordClient(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddSingleton((_) =>
        {
            string defaultLogLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");
            string botToken = hostContext.Configuration.GetValue<string>("Nellebot:BotToken");

            LogLevel logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

            var socketConfig = new DiscordConfiguration
            {
                MinimumLogLevel = logLevel,
                TokenType = TokenType.Bot,
                Token = botToken,
                Intents = DiscordIntents.All,
            };

            var client = new DiscordClient(socketConfig);

            return client;
        });
    }

    private static void AddDbContext(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddDbContext<BotDbContext>(
            builder =>
            {
                string dbConnString = hostContext.Configuration.GetValue<string>("Nellebot:ConnectionString");
                string logLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");

                builder.EnableSensitiveDataLogging(logLevel == "Debug");

                builder.UseNpgsql(dbConnString);
            },
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton);
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddTransient<IUserRoleRepository, UserRoleRepository>();
        services.AddTransient<AwardMessageRepository>();
        services.AddTransient<BotSettingsRepository>();
        services.AddTransient<MessageRefRepository>();
        services.AddTransient<UserLogRepository>();
    }

    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddTransient<AuthorizationService>();
        services.AddTransient<IDiscordErrorLogger, DiscordErrorLogger>();
        services.AddTransient<DiscordLogger>();
        services.AddTransient<UserRoleService>();
        services.AddTransient<RoleService>();
        services.AddTransient<AwardMessageService>();
        services.AddTransient<DiscordResolver>();
        services.AddTransient<ScribanTemplateLoader>();
        services.AddTransient<OrdbokModelMapper>();
        services.AddTransient<IOrdbokContentParser, OrdbokContentParser>();
        services.AddTransient<HtmlToImageService>();
        services.AddTransient<WkHtmlToImageClient>();
        services.AddTransient<GlosbeClient>();
        services.AddTransient<GlosbeModelMapper>();
        services.AddTransient<BotSettingsService>();
        services.AddTransient<MessageRefsService>();
        services.AddTransient<UserLogService>();
    }

    private static void AddBotEventHandlers(IServiceCollection services)
    {
        services.AddSingleton<AwardEventHandler>();
        services.AddSingleton<CommandEventHandler>();
    }

    private static void AddChannels(IServiceCollection services)
    {
        const int channelSize = 1024;

        services.AddSingleton((_) => new CommandQueueChannel(Channel.CreateBounded<CommandRequest>(channelSize)));
        services.AddSingleton((_) => new CommandParallelQueueChannel(Channel.CreateBounded<CommandRequest>(channelSize)));
        services.AddSingleton((_) => new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
        services.AddSingleton((_) => new DiscordLogChannel(Channel.CreateBounded<BaseDiscordLogItem>(channelSize)));
        services.AddSingleton((_) => new MessageAwardQueueChannel(Channel.CreateBounded<MessageAwardItem>(channelSize)));
    }

    private static void AddWorkers(IServiceCollection services)
    {
        services.AddHostedService<CommandQueueWorker>();
        services.AddHostedService<CommandParallelQueueWorker>();
        services.AddHostedService<EventQueueWorker>();
        services.AddHostedService<DiscordLoggerWorker>();
        services.AddHostedService<MessageAwardQueueWorker>();
        services.AddHostedService<BotWorker>();
    }
}
