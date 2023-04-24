using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Services;

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
                AdminRoleId = 1,
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member with admin role
            var member = BuildMember(id: 1, roleId: 1);

            var discordApplication = BuildApplication(ownerId: 0);

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsOwnerOrAdmin_WhenOwner_ReturnTrue()
        {
            // Arrange
            _sut = new AuthorizationService(_optionsMock.Object);

            var member = BuildMember(id: 1, roleId: 0);

            // App with member as owner
            var discordApplication = BuildApplication(ownerId: 1);

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
                CoOwnerUserId = 1,
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member with co-owner id
            var member = BuildMember(id: 1, roleId: 0);

            var discordApplication = BuildApplication(ownerId: 0);

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
                AdminRoleId = 1,
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member without admin role
            var member = BuildMember(id: 1, roleId: 2);

            // App without member as owner
            var discordApplication = BuildApplication(ownerId: 2);

            // Act
            var result = _sut.IsOwnerOrAdmin(member, discordApplication);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsTrustedMember_WhenHasTrustedMemberRole_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions() { TrustedRoleIds = new ulong[] { 1 } });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member with trusted role
            var member = BuildMember(id: 1, roleId: 1);

            var discordApplication = BuildApplication(ownerId: 0);

            // Act
            var result = _sut.IsTrustedMember(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrustedMember_WhenIsOwner_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions());

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member without trusted role
            var member = BuildMember(id: 1, roleId: 2);

            // App with member as owner
            var discordApplication = BuildApplication(ownerId: 1);

            // Act
            var result = _sut.IsTrustedMember(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrustedMember_WhenIsCoOwner_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions()
            {
                CoOwnerUserId = 1,
            });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member without trusted role
            var member = BuildMember(id: 1, roleId: 0);

            // App without member as owner
            var discordApplication = BuildApplication(ownerId: 0);

            // Act
            var result = _sut.IsTrustedMember(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrustedMember_WhenIsAdmin_ReturnTrue()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions() { AdminRoleId = 1 });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member with trusted role
            var member = BuildMember(id: 1, roleId: 1);

            var discordApplication = BuildApplication(ownerId: 0);

            // Act
            var result = _sut.IsTrustedMember(member, discordApplication);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrustedMember_WhenDoesNotHaveTrustedRole_ReturnFalse()
        {
            // Arrange
            _optionsMock.Setup(s => s.Value).Returns(new BotOptions() { TrustedRoleIds = new ulong[] { 1 } });

            _sut = new AuthorizationService(_optionsMock.Object);

            // Member with trusted role
            var member = BuildMember(id: 1, roleId: 2);

            var discordApplication = BuildApplication(ownerId: 0);

            // Act
            var result = _sut.IsTrustedMember(member, discordApplication);

            // Assert
            Assert.IsFalse(result);
        }

        private static AppDiscordMember BuildMember(ulong id, ulong roleId)
        {
            return new AppDiscordMember()
            {
                Id = id,
                Roles = new List<AppDiscordRole>()
                {
                    new AppDiscordRole()
                    {
                        Id = roleId,
                    },
                },
            };
        }

        private static AppDiscordApplication BuildApplication(ulong ownerId)
        {
            return new AppDiscordApplication()
            {
                Owners = new List<AppDiscordUser>()
                {
                    new AppDiscordUser()
                    {
                        Id = ownerId,
                    },
                },
            };
        }
    }
}
