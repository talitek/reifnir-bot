using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Services;
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

        [TestInitialize]
        public void Initialize()
        {
            _sut = null!;
            _userRoleRepoMock = new Mock<IUserRoleRepository>();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void CreateRole_WithEmptyName_ThrowsException(string inputName)
        {
            var discordRole = It.IsAny<AppDiscordRole>();
            var name = inputName;
            var aliases = It.IsAny<string>();

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.CreateRole(discordRole, name, aliases);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Role name cannot be empty");
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(",")]
        public void CreateRole_WithLessThan1ValidAlias_ThrowsException(string inputAliasList)
        {
            var discordRole = It.IsAny<AppDiscordRole>();
            var name = "name";
            var aliases = inputAliasList;

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.CreateRole(discordRole, name, aliases);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Alias list cannot be empty");
        }

        [TestMethod]
        public void CreateRole_UserRoleAlreadyExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var name = "name";
            var aliases = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync(new UserRole());

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.CreateRole(discordRole, name, aliases);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role already exists");
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void Update_WithEmptyName_ThrowsException(string inputName)
        {
            var discordRole = It.IsAny<AppDiscordRole>();
            var name = inputName;

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.UpdateRole(discordRole, name);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Role name cannot be empty");
        }

        [TestMethod]
        public void UpdateRole_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var name = "name";

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.UpdateRole(discordRole, name);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void DeleteRole_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.DeleteRole(discordRole);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void GetRole_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.GetRole(discordRole);

            act.Should()
                .Throw<ArgumentException>()
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

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Alias cannot be empty");
        }

        [TestMethod]
        public void AddRoleAlias_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void AddRoleAlias_AliasAlreadyExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync(new UserRole());

            _userRoleRepoMock
                .Setup(x => x.GetRoleAlias(It.IsAny<string>()))
                .ReturnsAsync(new UserRoleAlias());

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.AddRoleAlias(discordRole, alias);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Alias already exists");
        }

        [TestMethod]
        public void RemoveRoleAlias_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.RemoveRoleAlias(discordRole, alias);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void RemoveRoleAlias_RoleHas1Alias_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            var alias = "alias";

            var userRole = new UserRole()
            {
                UserRoleAliases = new List<UserRoleAlias>()
                {
                    new UserRoleAlias()
                }
            };

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync(userRole);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.RemoveRoleAlias(discordRole, alias);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role must have at least 1 alias");
        }

        [TestMethod]
        public void SetRoleGroup_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();
            uint groupNumber = 1;

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.SetRoleGroup(discordRole, groupNumber);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }

        [TestMethod]
        public void UnsetRoleGroup_UserRoleNotExists_ThrowException()
        {
            var discordRole = new AppDiscordRole();

            _userRoleRepoMock
                .Setup(x => x.GetRole(It.IsAny<ulong>()))
                .ReturnsAsync((UserRole)null!);

            _sut = new UserRoleService(_userRoleRepoMock.Object);

            Func<Task> act = async () => await _sut.UnsetRoleGroup(discordRole);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("User role doesn't exist");
        }
    }
}
