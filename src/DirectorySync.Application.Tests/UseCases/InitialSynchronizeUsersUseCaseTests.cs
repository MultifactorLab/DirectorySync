using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using DirectorySync.Application.UseCases;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Tests.UseCases;

public class InitialSynchronizeUsersUseCaseTests
{
    private readonly Mock<ISystemDatabase> _systemDatabaseMock = new();
    private readonly Mock<ILdapGroupPort> _ldapGroupPortMock = new();
    private readonly Mock<ILdapMemberPort> _ldapMemberPortMock = new();
    private readonly Mock<IUserCloudPort> _userCloudPortMock = new();
    private readonly Mock<IUserDeleter> _userDeleterMock = new();
    private readonly Mock<ISyncSettingsOptions> _syncSettingsOptionsMock = new();
    private readonly Mock<ILogger<InitialSynchronizeUsersUseCase>> _loggerMock = new();

    private readonly InitialSynchronizeUsersUseCase _useCase;

    public InitialSynchronizeUsersUseCaseTests()
    {
        _useCase = new InitialSynchronizeUsersUseCase(
            _systemDatabaseMock.Object,
            _ldapGroupPortMock.Object,
            _ldapMemberPortMock.Object,
            _userCloudPortMock.Object,
            _userDeleterMock.Object,
            _syncSettingsOptionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTrackingGroupsIsEmpty()
    {
        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _useCase.ExecuteAsync(Enumerable.Empty<DirectoryGuid>()));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturn_WhenDatabaseIsInitialized()
    {
        // Arrange
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(true);

        // Act
        await _useCase.ExecuteAsync(new[] { new DirectoryGuid(Guid.NewGuid()) });

        // Assert
        _userCloudPortMock.Verify(x => x.GetUsersIdentitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturn_WhenNoReferenceGroupsFound()
    {
        // Arrange
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        _userCloudPortMock.Setup(x => x.GetUsersIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<Identity>(new List<Identity>()));

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel>().AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { new DirectoryGuid(Guid.NewGuid()) });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDelete_WhenNoDeletedUsers()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var cloudIdentities = new List<Identity>
            {
                new("user1@example.com"),
                new("user2@example.com")
            }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudIdentities);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        var memberModels = cloudIdentities.Select(identity =>
            MemberModel.Create(Guid.NewGuid(), identity, [])).ToList();

        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>()))
            .Returns(memberModels.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelete_WhenDeletedUsersExist()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var cloudIdentities = new List<Identity>
            {
                new("user1@example.com"),
                new("user2@example.com"),
                new("deleted@example.com")
            }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersIdentitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudIdentities);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        var existingIdentities = cloudIdentities
            .Where(i => i.Value != "deleted@example.com")
            .Select(identity => MemberModel.Create(Guid.NewGuid(), identity, []))
            .ToList();

        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>()))
            .Returns(existingIdentities.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.Is<List<MemberModel>>(l => l.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }
}
