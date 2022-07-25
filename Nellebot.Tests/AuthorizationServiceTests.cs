using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Attributes;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nellebot.Tests
{
    [TestClass]
    public class AuthorizationServiceTests
    {
        private AuthorizationService _sut = null!;
        private Mock<IOptions<BotOptions>> _optionsMock = null!;

        [TestInitialize]
        public void Initialize()
        {
            _sut = null!;
            _optionsMock = new Mock<IOptions<BotOptions>>();
        }

        [TestMethod]
        public void IsOwnerOrAdmin_WhenAdmin_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions()
            {
                AdminRoleId = 1
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            var member = new AppDiscordMember();

            member.Roles = new List<AppDiscordRole>()
            {
                new AppDiscordRole()
                {
                    Id = 1
                }
            };

            var discordApplication = new AppDiscordApplication()
            {
                Owners = new List<AppDiscordUser>()
            };

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsOwnerOrAdmin_WhenOwner_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions()
            {
                AdminRoleId = 1
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            var member = new AppDiscordMember()
            {
                Id = 1
            };

            var discordApplication = new AppDiscordApplication()
            {
                Owners = new List<AppDiscordUser>() {
                    new AppDiscordUser()
                    {
                        Id = 1
                    }
                }
            };

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsOwnerOrAdmin_WhenCoOwner_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions()
            {
                CoOwnerUserId = 1
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            var member = new AppDiscordMember()
            {
                Id = 1
            };

            var discordApplication = new AppDiscordApplication()
            {
                Owners = new List<AppDiscordUser>() {}
            };

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsOwnerOrAdmin_WhenNeitherAdminOrOwner_ReturnFalse()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions()
            {
                AdminRoleId = 1
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            var member = new AppDiscordMember()
            {
                Id = 1,
                Roles = new List<AppDiscordRole>() { new AppDiscordRole() { Id = 2 } }
            };

            var discordApplication = new AppDiscordApplication()
            {
                Owners = new List<AppDiscordUser>() {
                    new AppDiscordUser()
                    {
                        Id = 2
                    }
                }
            };

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
