using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DirectorySync.Application.Tests.Services
{
    public class GroupUpdaterTests
    {
        private readonly Mock<IGroupDatabase> _groupDatabaseMock;
        private readonly Mock<ILogger<GroupUpdater>> _loggerMock;
        private readonly GroupUpdater _groupUpdater;

        public GroupUpdaterTests()
        {
            _groupDatabaseMock = new Mock<IGroupDatabase>();
            _loggerMock = new Mock<ILogger<GroupUpdater>>();
            _groupUpdater = new GroupUpdater(_groupDatabaseMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void UpdateGroupsWithMembers_ThrowsArgumentNullException_WhenMembersIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _groupUpdater.UpdateGroupsWithMembers(null));
        }

        [Fact]
        public void UpdateGroupsWithMembers_AddsAndRemovesMembersCorrectly()
        {
            // Arrange
            var memberId = new DirectoryGuid(Guid.NewGuid());
            var groupId1 = new DirectoryGuid(Guid.NewGuid());
            var groupId2 = new DirectoryGuid(Guid.NewGuid());

            var group1 = GroupModel.Create(groupId1.Value, new List<DirectoryGuid>());
            var group2 = GroupModel.Create(groupId2.Value, new List<DirectoryGuid> { memberId });

            var member = MemberModel.Create(memberId.Value,
                new Identity("user"),
                new List<DirectoryGuid>());

            // Добавляем в group1, удаляем из group2
            member.AddGroups(new[] { groupId1 });
            member.RemoveGroups(new[] { groupId2 });

            _groupDatabaseMock.Setup(db => db.FindById(groupId1)).Returns(group1);
            _groupDatabaseMock.Setup(db => db.FindById(groupId2)).Returns(group2);

            // Act
            _groupUpdater.UpdateGroupsWithMembers(new[] { member });

            // Assert
            Assert.Contains(memberId, group1.MemberIds);
            Assert.DoesNotContain(memberId, group2.MemberIds);

            Assert.Equal(ChangeOperation.Update, group1.Operation);
            Assert.Equal(ChangeOperation.Update, group2.Operation);

            _groupDatabaseMock.Verify(db => db.UpdateMany(It.Is<IEnumerable<GroupModel>>(groups =>
                groups.Contains(group1) && groups.Contains(group2))), Times.Once);
        }

        [Fact]
        public void UpdateGroupsWithMembers_LogsWarning_WhenGroupNotFound()
        {
            // Arrange
            var memberId = new DirectoryGuid(Guid.NewGuid());
            var missingGroupId = new DirectoryGuid(Guid.NewGuid());

            var member = MemberModel.Create(memberId.Value,
                new Identity("user"),
                new List<DirectoryGuid>());

            member.AddGroups(new[] { missingGroupId });

            _groupDatabaseMock.Setup(db => db.FindById(missingGroupId)).Returns((GroupModel)null);

            // Act
            _groupUpdater.UpdateGroupsWithMembers(new[] { member });

            // Assert
            _loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), $"Group with id {missingGroupId} not found");
            _groupDatabaseMock.Verify(db => db.UpdateMany(It.IsAny<IEnumerable<GroupModel>>()), Times.Once);
        }

        [Fact]
        public void UpdateGroupsWithMembers_UpdatesMultipleGroups()
        {
            // Arrange
            var memberId1 = new DirectoryGuid(Guid.NewGuid());
            var memberId2 = new DirectoryGuid(Guid.NewGuid());

            var groupId1 = new DirectoryGuid(Guid.NewGuid());
            var groupId2 = new DirectoryGuid(Guid.NewGuid());

            var group1 = GroupModel.Create(groupId1.Value, new List<DirectoryGuid>());
            var group2 = GroupModel.Create(groupId2.Value, new List<DirectoryGuid>());

            var member1 = MemberModel.Create(memberId1.Value,
                new Identity("user1"),
                new List<DirectoryGuid>());
            member1.AddGroups(new[] { groupId1 });

            var member2 = MemberModel.Create(memberId2.Value,
                new Identity("user2"),
                new List<DirectoryGuid>());
            member2.AddGroups(new[] { groupId2 });

            _groupDatabaseMock.Setup(db => db.FindById(groupId1)).Returns(group1);
            _groupDatabaseMock.Setup(db => db.FindById(groupId2)).Returns(group2);

            // Act
            _groupUpdater.UpdateGroupsWithMembers(new[] { member1, member2 });

            // Assert
            Assert.Contains(memberId1, group1.MemberIds);
            Assert.Contains(memberId2, group2.MemberIds);

            Assert.Equal(ChangeOperation.Update, group1.Operation);
            Assert.Equal(ChangeOperation.Update, group2.Operation);

            _groupDatabaseMock.Verify(db => db.UpdateMany(It.Is<IEnumerable<GroupModel>>(groups =>
                groups.Contains(group1) && groups.Contains(group2))), Times.Once);
        }
    }

    // Extension to verify ILogger logs
    public static class LoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel logLevel, Times times, string message)
        {
            logger.Verify(x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString().Contains(message)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), times);
        }
    }
}
