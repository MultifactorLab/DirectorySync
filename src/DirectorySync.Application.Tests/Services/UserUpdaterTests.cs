using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DirectorySync.Application.Tests.Services;

public class UserUpdaterTests
{
    private readonly Mock<IUserCloudPort> _userCloudPortMock;
    private readonly Mock<IMemberDatabase> _memberDatabaseMock;
    private readonly Mock<IOptions<UserProcessingOptions>> _optionsMock;
    private readonly Mock<ILogger<UserUpdater>> _loggerMock;
    private readonly CodeTimer _codeTimer;
    private readonly UserUpdater _userUpdater;
    private readonly UserProcessingOptions _userProcessingOptions;

    public UserUpdaterTests()
    {
        _userCloudPortMock = new Mock<IUserCloudPort>();
        _memberDatabaseMock = new Mock<IMemberDatabase>();
        _loggerMock = new Mock<ILogger<UserUpdater>>();

        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerGeneralMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerGeneralMock.Object);

        var measuringOptions = Options.Create(new MeasuringOptions
        {
            MeasureExecutionTime = true
        });
        _codeTimer = new CodeTimer(loggerFactoryMock.Object, measuringOptions);

        _userProcessingOptions = new UserProcessingOptions
        {
            UpdatingBatchSize = 2
        };

        _optionsMock = new Mock<IOptions<UserProcessingOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_userProcessingOptions);

        _userUpdater = new UserUpdater(
            _userCloudPortMock.Object,
            _memberDatabaseMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _codeTimer);
    }

    [Fact]
    public async Task UpdateManyAsync_ThrowsArgumentNullException_WhenInputIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _userUpdater.UpdateManyAsync(null!));
    }

    [Fact]
    public async Task UpdateManyAsync_ReturnsEmpty_WhenNoUsers()
    {
        // Act
        var result = await _userUpdater.UpdateManyAsync(Array.Empty<MemberModel>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _userCloudPortMock.Verify(p => p.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberDatabaseMock.Verify(d => d.UpdateMany(It.IsAny<IEnumerable<MemberModel>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateManyAsync_UpdatesUsersInBatches()
    {
        // Arrange
        _userCloudPortMock.Setup(p => p.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        var updUsers = new List<MemberModel>
        {
            MemberModel.Create(Guid.NewGuid(), new Identity("user1"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user2"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user3"), new List<DirectoryGuid>()),
        };

        // Act
        var result = await _userUpdater.UpdateManyAsync(updUsers);

        // Assert
        Assert.Equal(3, result.Count);
        _memberDatabaseMock.Verify(d => d.UpdateMany(It.IsAny<IEnumerable<MemberModel>>()), Times.Exactly(2));
        _userCloudPortMock.Verify(p => p.UpdateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
