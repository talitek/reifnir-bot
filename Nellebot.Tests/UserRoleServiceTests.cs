﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Tests
{
    [TestClass]
    public class UserRoleServiceTests
    {
        private UserRoleService _sut = null!;
        private Mock<IUserRoleRepository> _userRoleRepoMock = null!;
        private Mock<IDiscordErrorLogger> _discordErrorLoggerMock = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sut = null!;
            _userRoleRepoMock = new Mock<IUserRoleRepository>();
            _discordErrorLoggerMock = new Mock<IDiscordErrorLogger>();
        }

        [TestMethod]
        public void CreateRole_UserRoleAlreadyExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var aliases = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync(new UserRole());

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.CreateRole(discordRole, aliases);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role already exists");
        }

        [TestMethod]
        public void DeleteRole_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.DeleteRole(discordRole);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void GetRole_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.GetRole(discordRole);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void AddRoleAlias_WithEmptyName_ThrowsException(string inputAlias)
        {
            var discordRole = It.IsAny<AppDiscordRole>();
            var alias = inputAlias;

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Alias cannot be empty");
        }

        [TestMethod]
        public void AddRoleAlias_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void AddRoleAlias_AliasAlreadyExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync(new UserRole());

            _userRoleRepoMock
                .Setup(x => x.GetRoleAlias(It.IsAny<string>()))
                .ReturnsAsync(new UserRoleAlias());

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Alias already exists");
        }

        [TestMethod]
        public void RemoveRoleAlias_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.RemoveRoleAlias(discordRole, alias);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void RemoveRoleAlias_RoleHasNoAliases_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            var userRole = new UserRole()
            {
                UserRoleAliases = new List<UserRoleAlias>()
            };

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync(userRole);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.RemoveRoleAlias(discordRole, alias);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role has no aliases");
        }

        [TestMethod]
        public void SetRoleGroup_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            uint groupNumber = 1;

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.SetRoleGroup(discordRole, groupNumber);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void UnsetRoleGroup_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRoleByDiscordRoleId(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = BuildSutWithMocks();

            Func<Task> act = async () => await _sut.UnsetRoleGroup(discordRole);

            act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        private UserRoleService BuildSutWithMocks()
        {
            return new UserRoleService(_userRoleRepoMock.Object, _discordErrorLoggerMock.Object);
        }
    }
}
