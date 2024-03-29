using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models.UserRoles;
using Nellebot.Data.Repositories;
using Nellebot.Services;
using NSubstitute;

namespace Nellebot.Tests;

[TestClass]
public class UserRoleServiceTests
{
    private UserRoleService _sut = null!;
    private IUserRoleRepository _userRoleRepoMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sut = null!;
        _userRoleRepoMock = Substitute.For<IUserRoleRepository>();
    }

    [TestMethod]
    public void CreateRole_UserRoleAlreadyExists_ThrowException()
    {
        var discordRole = new AppDiscordRole();
        var aliases = "alias";

        _userRoleRepoMock
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns(new UserRole());

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

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
        var discordRole = new AppDiscordRole();
        string alias = inputAlias;

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns(new UserRole());

        _userRoleRepoMock
            .GetRoleAlias(Arg.Any<string>())
            .Returns(new UserRoleAlias());

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

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

        var userRole = new UserRole
        {
            UserRoleAliases = new List<UserRoleAlias>(),
        };

        _userRoleRepoMock
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns(userRole);

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

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
            .GetRoleByDiscordRoleId(Arg.Any<ulong>())
            .Returns((UserRole)null!);

        _sut = BuildSutWithMocks();

        Func<Task> act = async () => await _sut.UnsetRoleGroup(discordRole);

        act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("User role doesn't exist");
    }

    private UserRoleService BuildSutWithMocks()
    {
        return new UserRoleService(_userRoleRepoMock);
    }
}
