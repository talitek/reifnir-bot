using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.Infrastructure;

public static class JobSchedulerProvider
{
    public static IServiceCollection AddJobScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(
            config =>
            {
                config.SchedulerName = "DefaultScheduler";

                config.AddJob<RoleMaintenanceJob>(j => j.WithIdentity(RoleMaintenanceJob.Key).StoreDurably());

                // Run at startup
                config.AddTrigger(t => t.ForJob(RoleMaintenanceJob.Key).StartNow());

                // Run every 6 hours starting at midnight
                config.AddTrigger(t => t.ForJob(RoleMaintenanceJob.Key).WithCronSchedule("0 0 0/6 ? * * *"));
            });

        services.AddQuartzHostedService(
            opts =>
            {
                opts.AwaitApplicationStarted = true;
                opts.WaitForJobsToComplete = true;

                // Give the bot time to start up before running the jobs
                opts.StartDelay = TimeSpan.FromSeconds(30);
#if DEBUG
                opts.StartDelay = TimeSpan.FromSeconds(5);
#endif
            });

        return services;
    }
}
