using System;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Common.Models.UserRoles;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Nellebot.Data;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions options)
        : base(options) { }

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
            .HasSortOrder(SortOrder.Ascending, SortOrder.Ascending, SortOrder.Descending);

        builder.Entity<UserLog>()
            .Property(x => x.RawValue)
            .HasColumnName("Value");

        builder.Entity<UserLog>()
            .Property(x => x.ValueType)
            .HasConversion(
                convertToProviderExpression: x => x.FullName ?? typeof(object).FullName!,
                convertFromProviderExpression: x => Type.GetType(x) ?? typeof(object));
    }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<UserRoleAlias> UserRoleAliases { get; set; }

    public DbSet<UserRoleGroup> UserRoleGroups { get; set; }

    public DbSet<AwardMessage> AwardMessages { get; set; }

    public DbSet<BotSettting> GuildSettings { get; set; }

    public DbSet<MessageRef> MessageRefs { get; set; }

    public DbSet<UserLog> UserLogs { get; set; }
}
