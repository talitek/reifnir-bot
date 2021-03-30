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
    public class RequireOwnerOrAdminTests
    {
        //private RequireOwnerOrAdmin _sut = null!;

        //[TestInitialize]
        //public void Initialize()
        //{
        //    _sut = new RequireOwnerOrAdmin();
        //}

        //[TestMethod]
        //public async Task ExecuteCheck_WithNoBotOptionsService_ThrowsException()
        //{
        //    // Arrange
        //    var contextMock = new Mock<CommandContext>();
        //    var serviceProvider = new ServiceCollection();
        //    var serviceCollection = serviceProvider.BuildServiceProvider();

        //    contextMock.Setup(p => p.Services).Returns(serviceCollection);

        //    var context = contextMock.Object;

        //    await Assert.ThrowsExceptionAsync<Exception>(async () => await _sut.ExecuteCheckAsync(context, false));
        //}

        private AuthorizationService _sut = null!;
        private Mock<IOptions<BotOptions>> _optionsMock = new Mock<IOptions<BotOptions>>();

        [TestInitialize]
        public void Initialize()
        {

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
    }
}
