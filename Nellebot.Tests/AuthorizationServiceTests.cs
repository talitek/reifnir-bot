using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Services;
using NSubstitute;

namespace Nellebot.Tests;

[TestClass]
public class AuthorizationServiceTests
{
    private IOptions<BotOptions> _optionsMock = null!;
    private AuthorizationService _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sut = null!;
        _optionsMock = Substitute.For<IOptions<BotOptions>>();
    }

    [TestMethod]
    public void IsOwnerOrAdmin_WhenAdmin_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                AdminRoleId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member with admin role
        AppDiscordMember member = BuildMember(1, 1);

        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsOwnerOrAdmin(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsOwnerOrAdmin_WhenOwner_ReturnTrue()
    {
        // Arrange
        _sut = new AuthorizationService(_optionsMock);

        AppDiscordMember member = BuildMember(1, 0);

        // App with member as owner
        AppDiscordApplication discordApplication = BuildApplication(1);

        // Act
        bool result = _sut.IsOwnerOrAdmin(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsOwnerOrAdmin_WhenCoOwner_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                CoOwnerUserId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member with co-owner id
        AppDiscordMember member = BuildMember(1, 0);

        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsOwnerOrAdmin(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsOwnerOrAdmin_WhenNeitherAdminOrOwner_ReturnFalse()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                AdminRoleId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member without admin role
        AppDiscordMember member = BuildMember(1, 2);

        // App without member as owner
        AppDiscordApplication discordApplication = BuildApplication(2);

        // Act
        bool result = _sut.IsOwnerOrAdmin(member, discordApplication);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenHasTrustedMemberRole_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { TrustedRoleIds = new ulong[] { 1 } });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 1);

        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsTrustedMember(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenIsOwner_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions());

        _sut = new AuthorizationService(_optionsMock);

        // Member without trusted role
        AppDiscordMember member = BuildMember(1, 2);

        // App with member as owner
        AppDiscordApplication discordApplication = BuildApplication(1);

        // Act
        bool result = _sut.IsTrustedMember(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenIsCoOwner_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                CoOwnerUserId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member without trusted role
        AppDiscordMember member = BuildMember(1, 0);

        // App without member as owner
        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsTrustedMember(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenIsAdmin_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { AdminRoleId = 1 });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 1);

        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsTrustedMember(member, discordApplication);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenDoesNotHaveTrustedRole_ReturnFalse()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { TrustedRoleIds = new ulong[] { 1 } });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 2);

        AppDiscordApplication discordApplication = BuildApplication(0);

        // Act
        bool result = _sut.IsTrustedMember(member, discordApplication);

        // Assert
        Assert.IsFalse(result);
    }

    private static AppDiscordMember BuildMember(ulong id, ulong roleId)
    {
        return new AppDiscordMember
        {
            Id = id,
            Roles = new List<AppDiscordRole>
            {
                new()
                {
                    Id = roleId,
                },
            },
        };
    }

    private static AppDiscordApplication BuildApplication(ulong ownerId)
    {
        return new AppDiscordApplication
        {
            Owners = new List<AppDiscordUser>
            {
                new()
                {
                    Id = ownerId,
                },
            },
        };
    }
}
