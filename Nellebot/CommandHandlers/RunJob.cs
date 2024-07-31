using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.CommandHandlers;

public record RunJobCommand : BotCommandCommand
{
    public RunJobCommand(CommandContext ctx, string jobKeyName)
        : base(ctx)
    {
        JobKeyName = jobKeyName;
    }

    public string JobKeyName { get; }
}

public class RunJobCommandHandler : IRequestHandler<RunJobCommand>
{
    private static readonly JobKey[] RunnableJobs =
    [
        RoleMaintenanceJob.Key,
    ];

    private readonly ISchedulerFactory _schedulerFactory;

    public RunJobCommandHandler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(RunJobCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string jobName = request.JobKeyName;

        if (!RunnableJobs.Select(k => k.Name).Contains(jobName))
        {
            await ctx.RespondAsync("Unknown job name: {jobName}");
            return;
        }

        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        await scheduler.TriggerJob(RoleMaintenanceJob.Key, cancellationToken);

        await ctx.RespondAsync($"Job triggered: {jobName}");
    }
}
