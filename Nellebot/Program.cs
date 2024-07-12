using System;
using System.Threading.Channels;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nellebot.CommandHandlers;
using Nellebot.CommandModules;
using Nellebot.Data;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services;
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (hostContext, services) =>
                {
                    services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.OptionsKey));

                    services.AddDataProtection()
                        .SetApplicationName(nameof(Nellebot))
                        .SetDefaultKeyLifetime(TimeSpan.FromDays(180));

                    services.AddHttpClient<OrdbokHttpClient>();

                    services.AddMediatR(
                        cfg =>
                        {
                            cfg.RegisterServicesFromAssemblyContaining<Program>();
                            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
                        });
                    services.AddTransient<NotificationPublisher>();

                    services.AddSingleton<SharedCache>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();
                    services.AddSingleton<PuppeteerFactory>();

                    services.AddSingleton<GoodbyeMessageBuffer>();

                    AddWorkers(services);

                    AddChannels(services);

                    AddBotEventHandlers(services);

                    AddInternalServices(services);

                    AddRepositories(services);

                    AddDbContext(hostContext, services);

                    services.AddDiscordClient(hostContext);
                })
            .UseSystemd();
    }

    private static void AddDbContext(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddDbContext<BotDbContext>(
            builder =>
            {
                var dbConnString =
                    hostContext.Configuration
                        .GetValue<string>("Nellebot:ConnectionString");
                var logLevel =
                    hostContext.Configuration
                        .GetValue<string>("Logging:LogLevel:Default");

                builder.EnableSensitiveDataLogging(logLevel == "Debug");

                builder.UseNpgsql(dbConnString);
            },
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton);
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddTransient<AwardMessageRepository>();
        services.AddTransient<BotSettingsRepository>();
        services.AddTransient<MessageRefRepository>();
        services.AddTransient<UserLogRepository>();
        services.AddTransient<ModmailTicketRepository>();
        services.AddTransient<OrdbokRepository>();
        services.AddTransient<MessageTemplateRepository>();
    }

    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddTransient<AuthorizationService>();
        services.AddTransient<IDiscordErrorLogger, DiscordErrorLogger>();
        services.AddTransient<DiscordLogger>();
        services.AddTransient<AwardMessageService>();
        services.AddTransient<DiscordResolver>();
        services.AddTransient<ScribanTemplateLoader>();
        services.AddTransient<OrdbokModelMapper>();
        services.AddTransient<IOrdbokContentParser, OrdbokContentParser>();
        services.AddTransient<HtmlToImageService>();
        services.AddTransient<BotSettingsService>();
        services.AddTransient<MessageRefsService>();
        services.AddTransient<UserLogService>();
    }

    private static void AddBotEventHandlers(IServiceCollection services)
    {
        services.AddSingleton<CommandEventHandler>();
    }

    private static void AddChannels(IServiceCollection services)
    {
        const int channelSize = 1024;

        services.AddSingleton(new RequestQueueChannel(Channel.CreateBounded<IRequest>(channelSize)));
        services.AddSingleton(new CommandQueueChannel(Channel.CreateBounded<ICommand>(channelSize)));
        services.AddSingleton(new CommandParallelQueueChannel(Channel.CreateBounded<ICommand>(channelSize)));
        services.AddSingleton(new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
        services.AddSingleton(new DiscordLogChannel(Channel.CreateBounded<BaseDiscordLogItem>(channelSize)));
    }

    private static void AddWorkers(IServiceCollection services)
    {
        services.AddHostedService<BotWorker>();
        services.AddHostedService<RequestQueueWorker>();
        services.AddHostedService<CommandQueueWorker>();
        services.AddHostedService<CommandParallelQueueWorker>();
        services.AddHostedService<EventQueueWorker>();
        services.AddHostedService<DiscordLoggerWorker>();
        services.AddHostedService<ModmailCleanupWorker>();
    }
}
