﻿using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using Nellebot.Common.Models.Modmail;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Common.Models.UserRoles;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Nellebot.Data;

public class BotDbContext : DbContext
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public BotDbContext(DbContextOptions options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<UserRole>()
            .HasIndex(x => x.RoleId)
            .IsUnique();

        builder.Entity<UserRole>()
            .Property(x => x.Name)
            .HasMaxLength(256);

        builder.Entity<UserRoleAlias>()
            .HasIndex(x => x.Alias)
            .IsUnique();

        builder.Entity<UserRoleAlias>()
            .Property(x => x.Alias)
            .HasMaxLength(256);

        builder.Entity<UserRoleGroup>()
            .Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Entity<UserRoleGroup>()
            .Property(x => x.Name)
            .HasMaxLength(256);

        builder.Entity<AwardMessage>()
            .HasIndex(x => x.OriginalMessageId)
            .IsUnique();

        builder.Entity<AwardMessage>()
            .HasIndex(x => x.AwardedMessageId)
            .IsUnique();

        builder.Entity<BotSettting>()
            .HasIndex(x => x.Key)
            .IsUnique();

        builder.Entity<MessageRef>()
            .HasKey(x => x.MessageId);

        builder.Entity<MessageRef>()
            .Property(x => x.MessageId)
            .ValueGeneratedNever();

        builder.Entity<UserLog>()
            .HasIndex(x => new { x.UserId, x.LogType, x.Timestamp })
            .IsDescending(false, false, true);

        builder.Entity<UserLog>()
            .Property(x => x.RawValue)
            .HasColumnName("Value");

        builder.Entity<UserLog>()
            .Property(x => x.ValueType)
            .HasConversion(
                convertToProviderExpression: x => x.FullName ?? typeof(object).FullName!,
                convertFromProviderExpression: x => Type.GetType(x) ?? typeof(object));

        builder.Entity<ModmailTicketEntity>()
            .OwnsOne(x => x.TicketPost, x =>
            {
                x.Property(x => x.ChannelThreadId).HasColumnName("ChannelThreadId");
                x.Property(x => x.MessageId).HasColumnName("MessageId");
            });

        builder.Entity<ModmailTicketEntity>()
            .Property(x => x.RequesterIdEncrypted)
            .HasConversion(new ProtectedConverter(_dataProtectionProvider, "RequesterId"));
    }

#pragma warning disable SA1201 // Elements should appear in the correct order
    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<UserRoleAlias> UserRoleAliases { get; set; }

    public DbSet<UserRoleGroup> UserRoleGroups { get; set; }

    public DbSet<AwardMessage> AwardMessages { get; set; }

    public DbSet<BotSettting> GuildSettings { get; set; }

    public DbSet<MessageRef> MessageRefs { get; set; }

    public DbSet<UserLog> UserLogs { get; set; }

    public DbSet<ModmailTicketEntity> ModmailTickets { get; set; }
}
