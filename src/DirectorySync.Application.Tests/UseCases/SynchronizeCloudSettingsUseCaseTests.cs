using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.ConfigurationProviders;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using DirectorySync.Application.UseCases;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.ObjectModel;

namespace DirectorySync.Application.Tests.UseCases;

public class SynchronizeCloudSettingsUseCaseTests
{
    private readonly Mock<ISyncSettingsOptions> _syncSettingsOptionsMock = new();
    private readonly Mock<ISyncSettingsDatabase> _syncSettingsDatabaseMock = new();
    private readonly Mock<IMemberDatabase> _memberDatabaseMock = new();
    private readonly Mock<IGroupDatabase> _groupDatabaseMock = new();
    private readonly Mock<IUserGroupsMapper> _userGroupsMapperMock = new();
    private readonly Mock<IUserUpdater> _userUpdaterMock = new();
    private readonly Mock<IUserDeleter> _userDeleterMock = new();
    private readonly Mock<IGroupUpdater> _groupUpdaterMock = new();
    private readonly Mock<ILogger<SynchronizeCloudSettingsUseCase>> _loggerMock = new();
    private readonly Mock<ICloudConfigurationProvider> _providerMock = new();

    private readonly SynchronizeCloudSettingsUseCase _useCase;

    public SynchronizeCloudSettingsUseCaseTests()
    {
        _useCase = new SynchronizeCloudSettingsUseCase(
            _syncSettingsOptionsMock.Object,
            _syncSettingsDatabaseMock.Object,
            _memberDatabaseMock.Object,
            _groupDatabaseMock.Object,
            _userGroupsMapperMock.Object,
            _userUpdaterMock.Object,
            _userDeleterMock.Object,
            _groupUpdaterMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveSettings_WhenCurrentSettingsIsNull()
    {
        // Arrange
        _syncSettingsDatabaseMock.Setup(x => x.GetSyncSettings()).Returns((SyncSettings)null);
        _syncSettingsOptionsMock.Setup(x => x.Current).Returns(new SyncSettings { DirectoryGroupMappings = Array.Empty<GroupMapping>() });

        // Act
        await _useCase.ExecuteAsync(true, _providerMock.Object);

        // Assert
        _syncSettingsDatabaseMock.Verify(x => x.SaveSettings(It.IsAny<SyncSettings>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkip_WhenMappingsDidNotChange()
    {
        // Arrange
        var mappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = Guid.NewGuid().ToString(),
                SignUpGroups = new[] { "g1" }
            }
        };
        var settings = new SyncSettings
        {
            DirectoryGroupMappings = mappings,
        };

        _syncSettingsDatabaseMock.Setup(x => x.GetSyncSettings()).Returns(settings);
        _syncSettingsOptionsMock.Setup(x => x.Current).Returns(settings);

        // Act
        await _useCase.ExecuteAsync(true, _providerMock.Object);

        // Assert
        _userUpdaterMock.Verify(x => x.UpdateManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteUsersAndGroup_WhenMappingsChanged()
    {
        // Arrange
        var directoryGroupGuid1 = Guid.NewGuid();
        var directoryGroupGuid2 = Guid.NewGuid();
        var oldMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = directoryGroupGuid1.ToString(),
                SignUpGroups = new[] { "g1" }
            }
        };
        var newMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = directoryGroupGuid2.ToString(),
                SignUpGroups = new[] { "g2" }
            }
        };

        var oldSettings = new SyncSettings
        {
            DirectoryGroupMappings = oldMappings,
        };
        var newSettings = new SyncSettings
        {
            DirectoryGroupMappings = newMappings
        };

        _syncSettingsDatabaseMock.Setup(x => x.GetSyncSettings()).Returns(oldSettings);
        _syncSettingsOptionsMock.Setup(x => x.Current).Returns(newSettings);

        _groupDatabaseMock.Setup(x => x.FindById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel>
            {
                GroupModel.Create(new DirectoryGuid(Guid.NewGuid()), [new DirectoryGuid(Guid.NewGuid())]),
            }.AsReadOnly());

        _memberDatabaseMock.Setup(x => x.FindManyById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new ReadOnlyCollection<MemberModel>(new List<MemberModel>
            {
                MemberModel.Create(Guid.NewGuid(), new Identity("user1"), [directoryGroupGuid1])
            }));

        _userGroupsMapperMock.Setup(x => x.SetUserCloudGroupsDiff(It.IsAny<MemberModel>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>()))
            .Returns((new string[] { "new" }, new string[] { "old" }));

        _userUpdaterMock.Setup(x => x.UpdateManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<MemberModel>(new List<MemberModel>()));

        _userDeleterMock.Setup(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<MemberModel>(new List<MemberModel>()));

        // Act
        await _useCase.ExecuteAsync(true, _providerMock.Object);

        // Assert
        _userUpdaterMock.Verify(x => x.UpdateManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _syncSettingsDatabaseMock.Verify(x => x.SaveSettings(It.IsAny<SyncSettings>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateUsersAndGroup_WhenMappingsChanged()
    {
        // Arrange
        var directoryGroupGuid = Guid.NewGuid();

        var oldMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = directoryGroupGuid.ToString(),
                SignUpGroups = new[] { "g1", "g2" }
            }
        };
        var newMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = directoryGroupGuid.ToString(),
                SignUpGroups = new[] { "g2" }
            }
        };

        var oldSettings = new SyncSettings
        {
            DirectoryGroupMappings = oldMappings,
        };
        var newSettings = new SyncSettings
        {
            DirectoryGroupMappings = newMappings
        };

        _syncSettingsDatabaseMock.Setup(x => x.GetSyncSettings()).Returns(oldSettings);
        _syncSettingsOptionsMock.Setup(x => x.Current).Returns(newSettings);

        _groupDatabaseMock.Setup(x => x.FindById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new List<GroupModel>
            {
                GroupModel.Create(new DirectoryGuid(Guid.NewGuid()), [new DirectoryGuid(Guid.NewGuid())]),
            }.AsReadOnly());

        _memberDatabaseMock.Setup(x => x.FindManyById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new ReadOnlyCollection<MemberModel>(new List<MemberModel>
            {
                MemberModel.Create(Guid.NewGuid(), new Identity("user1"), [directoryGroupGuid])
            }));

        _userGroupsMapperMock.Setup(x => x.SetUserCloudGroupsDiff(It.IsAny<MemberModel>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>()))
            .Returns((new string[] { "new" }, new string[] { "old" }));

        _userUpdaterMock.Setup(x => x.UpdateManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<MemberModel>(new List<MemberModel>()));

        _userDeleterMock.Setup(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadOnlyCollection<MemberModel>(new List<MemberModel>()));

        // Act
        await _useCase.ExecuteAsync(true, _providerMock.Object);

        // Assert
        _userUpdaterMock.Verify(x => x.UpdateManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _userDeleterMock.Verify(x => x.DeleteManyAsync(It.IsAny<List<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _syncSettingsDatabaseMock.Verify(x => x.SaveSettings(It.IsAny<SyncSettings>()), Times.Once);
    }
}
