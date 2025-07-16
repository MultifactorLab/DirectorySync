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

public class SynchronizeUsersUseCaseTests
{
    private readonly Mock<IMemberDatabase> _memberDatabase = new();
    private readonly Mock<ILdapMemberPort> _memberPort = new();
    private readonly Mock<IUserUpdater> _userUpdater = new();
    private readonly Mock<ISyncSettingsOptions> _syncSettingsOptions = new();
    private readonly CodeTimer _codeTimer;
    private readonly Mock<ILogger<SynchronizeGroupsUseCase>> _logger = new();

    private readonly SynchronizeUsersUseCase _useCase;

    public SynchronizeUsersUseCaseTests()
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

        _useCase = new SynchronizeUsersUseCase(
            _memberDatabase.Object,
            _memberPort.Object,
            _userUpdater.Object,
            _syncSettingsOptions.Object,
            _codeTimer,
            _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogWarning_WhenRequiredAttributesEmpty()
    {
        // Arrange
        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns([]);

        // Act
        await _useCase.ExecuteAsync();

        // Assert
        _logger.VerifyLog(LogLevel.Warning, Times.Once(), "Required LDAP attributes not defined. Please check attribute mapping");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogDebugAndReturn_WhenCacheIsEmpty()
    {
        // Arrange
        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns(["cn"]);
        _memberDatabase.Setup(x => x.FindAll()).Returns(ReadOnlyCollection<MemberModel>.Empty);

        // Act
        await _useCase.ExecuteAsync();

        // Assert
        _logger.VerifyLog(LogLevel.Debug, Times.Once(), "Users in cache not found");
        _logger.VerifyLog(LogLevel.Information, Times.Once(), "Complete users synchronization");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotUpdate_WhenNoChanges()
    {
        // Arrange
        var memberId = new DirectoryGuid(Guid.NewGuid());
        var cached = MemberModel.Create(memberId, new Identity("user1"), []);
        cached.SetProperties([new MemberProperty("cn", "User")], new AttributesHash("hash1"));

        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns(["cn"]);
        _memberDatabase.Setup(x => x.FindAll()).Returns(new[] { cached }.AsReadOnly());

        var reference = MemberModel.Create(memberId, new Identity("user1"), []);
        reference.SetProperties([new MemberProperty("cn", "User")], new AttributesHash("hash1"));

        _memberPort.Setup(x => x.GetByGuids(new[] { memberId }, new[] { "cn" }))
            .Returns(new[] { reference }.AsReadOnly());

        // Act
        await _useCase.ExecuteAsync();

        // Assert
        _userUpdater.Verify(x => x.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.VerifyLog(LogLevel.Debug, Times.Once(), "Modified users was not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateUsers_WhenAttributesChanged()
    {
        // Arrange
        var memberId = new DirectoryGuid(Guid.NewGuid());
        var cached = MemberModel.Create(memberId, new Identity("user1"), []);
        cached.SetProperties([new MemberProperty("cn", "OldUser")], new AttributesHash("oldHash"));

        _syncSettingsOptions.Setup(x => x.GetRequiredAttributeNames()).Returns(["cn"]);
        _memberDatabase.Setup(x => x.FindAll()).Returns(new [] { cached }.AsReadOnly());

        var reference = MemberModel.Create(memberId, new Identity("user1"), []);
        reference.SetProperties([new MemberProperty("cn", "NewUser")], new AttributesHash("newHash"));

        _memberPort.Setup(x => x.GetByGuids(new[] { memberId }, new[] { "cn" }))
            .Returns(new[] { reference }.AsReadOnly());

        _userUpdater.Setup(x => x.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        // Act
        await _useCase.ExecuteAsync();

        // Assert
        _userUpdater.Verify(x => x.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Once);
        _logger.VerifyLog(LogLevel.Information, Times.Once(), "Complete users synchronization");
    }
}
