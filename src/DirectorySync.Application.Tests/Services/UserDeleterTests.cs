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

public class UserDeleterTests
{

    private readonly Mock<IUserCloudPort> _userCloudPortMock;
    private readonly Mock<IMemberDatabase> _memberDatabaseMock;
    private readonly Mock<IOptions<UserProcessingOptions>> _optionsMock;
    private readonly CodeTimer _codeTimer;
    private readonly UserDeleter _userDeleter;
    private readonly UserProcessingOptions _userProcessingOptions;

    public UserDeleterTests()
    {
        _userCloudPortMock = new Mock<IUserCloudPort>();
        _memberDatabaseMock = new Mock<IMemberDatabase>();

        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var options = Options.Create(new MeasuringOptions
        {
            MeasureExecutionTime = true
        });
        _codeTimer = new CodeTimer(loggerFactoryMock.Object, options);

        _userProcessingOptions = new UserProcessingOptions
        {
            DeletingBatchSize = 2
        };

        _optionsMock = new Mock<IOptions<UserProcessingOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_userProcessingOptions);

        _userDeleter = new UserDeleter(_userCloudPortMock.Object,
            _memberDatabaseMock.Object,
            _optionsMock.Object,
            _codeTimer);
    }

    [Fact]
    public async Task DeleteManyAsync_ThrowsArgumentNullException_WhenInputIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _userDeleter.DeleteManyAsync(null!));
    }

    [Fact]
    public async Task DeleteManyAsync_ReturnsEmpty_WhenNoUsers()
    {
        // Act
        var result = await _userDeleter.DeleteManyAsync(Array.Empty<MemberModel>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _userCloudPortMock.Verify(p => p.DeleteManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberDatabaseMock.Verify(d => d.DeleteMany(It.IsAny<IEnumerable<DirectoryGuid>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteManyAsync_DeletesUsersInBatches()
    {
        // Arrange
        _userCloudPortMock.Setup(p => p.DeleteManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        var delUsers = new List<MemberModel>
        {
            MemberModel.Create(Guid.NewGuid(), new Identity("user1"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user2"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user3@domain.com"), new List<DirectoryGuid>()),
        };

        // Act
        var result = await _userDeleter.DeleteManyAsync(delUsers);

        // Assert
        Assert.Equal(3, result.Count);
        _memberDatabaseMock.Verify(d => d.DeleteMany(It.IsAny<IEnumerable<DirectoryGuid>>()), Times.Exactly(2));
        _userCloudPortMock.Verify(p => p.DeleteManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
