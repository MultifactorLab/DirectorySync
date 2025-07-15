using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Ports.Directory;
using DirectorySync.Application.Ports.Options;
using DirectorySync.Application.Services;
using DirectorySync.Application.Tests.Services;
using DirectorySync.Application.UseCases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DirectorySync.Application.Tests.UseCases;

public class SynchronizeGroupsUseCaseTests
{
    private readonly Mock<IGroupDatabase> _groupDatabase = new();
    private readonly Mock<IMemberDatabase> _memberDatabase = new();
    private readonly Mock<ILdapGroupPort> _groupPort = new();
    private readonly Mock<ILdapMemberPort> _memberPort = new();
    private readonly Mock<IUserGroupsMapper> _userGroupsMapper = new();
    private readonly Mock<IUserCreator> _userCreator = new();
    private readonly Mock<IUserUpdater> _userUpdater = new();
    private readonly Mock<IUserDeleter> _userDeleter = new();
    private readonly Mock<IGroupUpdater> _groupUpdater = new();
    private readonly Mock<ISyncSettingsOptions> _syncSettingsOptions = new();
    private readonly CodeTimer _codeTimer;
    private readonly Mock<ILogger<SynchronizeGroupsUseCase>> _logger = new();

    private readonly SynchronizeGroupsUseCase _useCase;

    public SynchronizeGroupsUseCaseTests()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var options = Options.Create(new MeasuringOptions
        {
            MeasureExecutionTime = true
        });
        _codeTimer = new CodeTimer(loggerFactoryMock.Object, options);

        _useCase = new SynchronizeGroupsUseCase(
            _groupDatabase.Object,
            _memberDatabase.Object,
            _groupPort.Object,
            _memberPort.Object,
            _userGroupsMapper.Object,
            _userCreator.Object,
            _userUpdater.Object,
            _userDeleter.Object,
            _groupUpdater.Object,
            _syncSettingsOptions.Object,
            _codeTimer,
            _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogWarning_WhenGroupNotFound()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        _groupPort.Setup(x => x.GetByGuid(groupId))
            .Returns((GroupModel)null);

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _logger.VerifyLog(LogLevel.Warning, Times.Once(), "Reference group not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInsertGroup_WhenNotCached()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var groupModel = GroupModel.Create(groupId, []);

        _groupPort.Setup(x => x.GetByGuid(groupId))
            .Returns(groupModel);
        _groupDatabase.Setup(x => x.FindById(groupId)).Returns((GroupModel)null);

        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns([]);

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _groupDatabase.Verify(x => x.Insert(It.IsAny<GroupModel>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotProcess_WhenHashIsEqual()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var userId = new DirectoryGuid(Guid.NewGuid());
        var reference = GroupModel.Create(groupId, [userId]);

        var cached = GroupModel.Create(groupId, [userId]);

        _groupPort.Setup(x => x.GetByGuid(groupId)).Returns(reference);
        _groupDatabase.Setup(x => x.FindById(groupId)).Returns(cached);

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _userCreator.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateNewMembers_WhenAdded()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var addedMemberId = new DirectoryGuid(Guid.NewGuid());

        var reference = GroupModel.Create(groupId, [addedMemberId]);

        var cached = GroupModel.Create(groupId, []);

        _groupPort.Setup(x => x.GetByGuid(groupId)).Returns(reference);
        _groupDatabase.Setup(x => x.FindById(groupId)).Returns(cached);
        _memberDatabase.Setup(x => x.FindManyById(It.IsAny<IEnumerable<DirectoryGuid>>())).Returns(ReadOnlyCollection<MemberModel>.Empty);

        var member = MemberModel.Create(addedMemberId, new Identity("newUser"), []);
        _memberPort.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(new[] { member }.AsReadOnly());

        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns([]);
        _syncSettingsOptions.Setup(x => x.Current).Returns(new SyncSettings
        {
            DirectoryGroupMappings = []
        });

        _userGroupsMapper.Setup(x => x.GetCloudGroupChanges(It.IsAny<MemberModel>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>()))
            .Returns(([], []));

        _userCreator.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _userCreator.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _groupUpdater.Verify(x => x.UpdateGroupsWithMembers(It.IsAny<IEnumerable<MemberModel>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteMember_WhenRemovedAndNoGroups()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var removedId = new DirectoryGuid(Guid.NewGuid());

        var cached = GroupModel.Create(groupId, [removedId]);

        var reference = GroupModel.Create(groupId, []);

        _groupPort.Setup(x => x.GetByGuid(groupId)).Returns(reference);
        _groupDatabase.Setup(x => x.FindById(groupId)).Returns(cached);

        var member = MemberModel.Create(removedId, new Identity("oldUser"), [groupId]);
        _memberDatabase.Setup(x => x.FindManyById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new[] { member }.AsReadOnly());

        _syncSettingsOptions.Setup(x => x.Current).Returns(new SyncSettings
        {
            DirectoryGroupMappings = []
        });

        _userGroupsMapper.Setup(x => x.GetCloudGroupChanges(It.IsAny<MemberModel>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>()))
            .Returns(([], []));

        _userDeleter.Setup(x => x.DeleteManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _userDeleter.Verify(x => x.DeleteManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _groupUpdater.Verify(x => x.UpdateGroupsWithMembers(It.IsAny<IEnumerable<MemberModel>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateMember_WhenGroupsChanged()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var memberId = new DirectoryGuid(Guid.NewGuid());

        var cached = GroupModel.Create(groupId, []);

        var reference = GroupModel.Create(groupId, [memberId]);

        _groupPort.Setup(x => x.GetByGuid(groupId)).Returns(reference);
        _groupDatabase.Setup(x => x.FindById(groupId)).Returns(cached);

        var member = MemberModel.Create(memberId, new Identity("user"), []);
        _memberDatabase.Setup(x => x.FindManyById(It.IsAny<IEnumerable<DirectoryGuid>>()))
            .Returns(new[] { member }.AsReadOnly());
        _memberPort.Setup(x => x.GetByGuids(It.IsAny<IEnumerable<DirectoryGuid>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(ReadOnlyCollection<MemberModel>.Empty);

        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns([]);
        _syncSettingsOptions.Setup(x => x.Current).Returns(new SyncSettings
        {
            DirectoryGroupMappings = []
        });

        _userGroupsMapper.Setup(x => x.GetCloudGroupChanges(It.IsAny<MemberModel>(), It.IsAny<Dictionary<DirectoryGuid, string[]>>()))
            .Returns(([], []));

        _userUpdater.Setup(x => x.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        // Act
        await _useCase.ExecuteAsync(new[] { groupId });

        // Assert
        _userUpdater.Verify(x => x.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _groupUpdater.Verify(x => x.UpdateGroupsWithMembers(It.IsAny<IEnumerable<MemberModel>>()), Times.Once);
    }
}
