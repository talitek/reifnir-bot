﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nellebot.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Nellebot.Data.Migrations.Migrations
{
    [DbContext(typeof(BotDbContext))]
    [Migration("20210422215719_MakeAwardChannelIdNonNullable")]
    partial class MakeAwardChannelIdNonNullable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.4")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Nellebot.Common.Models.AwardMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AwardChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("AwardCount")
                        .HasColumnType("bigint");

                    b.Property<decimal>("AwardedMessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("OriginalMessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("AwardedMessageId")
                        .IsUnique();

                    b.HasIndex("OriginalMessageId")
                        .IsUnique();

                    b.ToTable("AwardMessages");
                });

            modelBuilder.Entity("Nellebot.Common.Models.UserRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long?>("GroupNumber")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId")
                        .IsUnique();

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("Nellebot.Common.Models.UserRoleAlias", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid>("UserRoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("Alias")
                        .IsUnique();

                    b.HasIndex("UserRoleId");

                    b.ToTable("UserRoleAliases");
                });

            modelBuilder.Entity("Nellebot.Common.Models.UserRoleAlias", b =>
                {
                    b.HasOne("Nellebot.Common.Models.UserRole", "UserRole")
                        .WithMany("UserRoleAliases")
                        .HasForeignKey("UserRoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserRole");
                });

            modelBuilder.Entity("Nellebot.Common.Models.UserRole", b =>
                {
                    b.Navigation("UserRoleAliases");
                });
#pragma warning restore 612, 618
        }
    }
}
