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
        _userCloudPortMock.Verify(x => x.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturn_WhenNoReferenceGroupsFound()
    {
        // Arrange
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<CloudUserModel>(new List<CloudUserModel>()));

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns((new List<GroupModel>().AsReadOnly(), ReadOnlyCollection<LdapDomain>.Empty));

        // Act
        await _useCase.ExecuteAsync(new[] { new DirectoryGuid(Guid.NewGuid()) });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDelete_WhenAllCloudUsersFoundByIdentity()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var cloudUsers = new List<CloudUserModel>
        {
            new(new Identity("user1@example.com")),
            new(new Identity("user2@example.com"))
        }.AsReadOnly();

        var domain = new LdapDomain("domain.example");

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns((new List<GroupModel> { groupModel }.AsReadOnly(), new[] { domain }.AsReadOnly()));

        var adMembers = cloudUsers
            .Select(u => MemberModel.Create(Guid.NewGuid(), u.Identity, []))
            .ToList();

        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<LdapDomain[]>()))
            .Returns(adMembers.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelete_WhenCloudUserNotFoundInAd_ByIdentity()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var cloudUsers = new List<CloudUserModel>
        {
            new(new Identity("user1@example.com")),
            new(new Identity("user2@example.com")),
            new(new Identity("deleted@example.com"))
        }.AsReadOnly();
        
        var domain = new LdapDomain("domain.example");

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuid(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns((new List<GroupModel> { groupModel }.AsReadOnly(), new[] { domain }.AsReadOnly()));

        var existingAdMembers = cloudUsers
            .Where(u => u.Identity.Value != "deleted@example.com")
            .Select(u => MemberModel.Create(Guid.NewGuid(), u.Identity, []))
            .ToList();

        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<LdapDomain[]>()))
            .Returns(existingAdMembers.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(
            x => x.DeleteManyAsync(It.Is<List<MemberModel>>(l => l.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDelete_WhenCloudUserFoundByGuid_DespiteIdentityChange()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var adMemberGuid = Guid.NewGuid();
        var oldIdentity = new Identity("old.name@example.com");
        var newIdentity = new Identity("new.name@example.com");

        var cloudUsers = new List<CloudUserModel>
        {
            new(oldIdentity, new DirectoryGuid(adMemberGuid))
        }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, [new DirectoryGuid(adMemberGuid)]);
        _ldapGroupPortMock.Setup(x => x.GetByGuidAsync(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        var adMember = MemberModel.Create(adMemberGuid, newIdentity, []);
        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(new List<MemberModel> { adMember }.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelete_WhenCloudUserGuidRemovedFromAd()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var removedGuid = Guid.NewGuid();
        var removedIdentity = new Identity("removed@example.com");

        var cloudUsers = new List<CloudUserModel>
        {
            new(removedIdentity, new DirectoryGuid(removedGuid))
        }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuidAsync(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        // AD has different member
        var otherMember = MemberModel.Create(Guid.NewGuid(), new Identity("other@example.com"), []);
        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(new List<MemberModel> { otherMember }.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(
            x => x.DeleteManyAsync(It.Is<List<MemberModel>>(l => l.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDelete_WhenIdentityFormatDiffersBetweenCloudAndAd()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var cloudUsers = new List<CloudUserModel>
        {
            new(new Identity("user@company.com"))
        }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuidAsync(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        var adMember = MemberModel.Create(Guid.NewGuid(), new Identity("COMPANY\\user"), []);
        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(new List<MemberModel> { adMember }.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchByIdentity_WhenCloudUserHasNoGuid()
    {
        // Arrange
        var trackingGroupGuid = new DirectoryGuid(Guid.NewGuid());
        _systemDatabaseMock.Setup(x => x.IsDatabaseInitialized()).Returns(false);

        var identity = new Identity("user@example.com");
        var cloudUsers = new List<CloudUserModel>
        {
            new(identity)
        }.AsReadOnly();

        _userCloudPortMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cloudUsers);

        _syncSettingsOptionsMock.Setup(x => x.GetRequiredAttributeNames())
            .Returns(Array.Empty<string>());

        var groupModel = GroupModel.Create(trackingGroupGuid, []);
        _ldapGroupPortMock.Setup(x => x.GetByGuidAsync(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel> { groupModel }.AsReadOnly());

        var adMember = MemberModel.Create(Guid.NewGuid(), identity, []);
        _ldapMemberPortMock.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(new List<MemberModel> { adMember }.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync(new[] { trackingGroupGuid });

        // Assert
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
