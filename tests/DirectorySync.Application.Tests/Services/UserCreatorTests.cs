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

public class UserCreatorTests
{
    private readonly Mock<IUserCloudPort> _userCloudPortMock;
    private readonly Mock<IMemberDatabase> _memberDatabaseMock;
    private readonly Mock<IOptions<UserProcessingOptions>> _optionsMock;
    private readonly CodeTimer _codeTimer;
    private readonly UserCreator _userCreator;
    private readonly UserProcessingOptions _userProcessingOptions;

    public UserCreatorTests()
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
            CreatingBatchSize = 2
        };

        _optionsMock = new Mock<IOptions<UserProcessingOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_userProcessingOptions);

        _userCreator = new UserCreator(_userCloudPortMock.Object,
            _memberDatabaseMock.Object,
            _optionsMock.Object,
            _codeTimer);
    }

    [Fact]
    public async Task CreateManyAsync_ThrowsArgumentNullException_WhenNewUsersIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _userCreator.CreateManyAsync(null));
    }

    [Fact]
    public async Task CreateManyAsync_ReturnsEmpty_WhenNoUsers()
    {
        // Act
        var result = await _userCreator.CreateManyAsync(Enumerable.Empty<MemberModel>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _userCloudPortMock.Verify(p => p.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberDatabaseMock.Verify(db => db.InsertMany(It.IsAny<IEnumerable<MemberModel>>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_CreatesUsersInBatches()
    {
        // Arrange
        _userCloudPortMock.Setup(p => p.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<MemberModel> input, CancellationToken _) => Task.FromResult(input.ToList().AsReadOnly()));

        var newUsers = new List<MemberModel>
        {
            MemberModel.Create(Guid.NewGuid(), new Identity("user1"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user2"), new List<DirectoryGuid>()),
            MemberModel.Create(Guid.NewGuid(), new Identity("user3@domain.com"), new List<DirectoryGuid>()),
        };

        // Act
        var result = await _userCreator.CreateManyAsync(newUsers);

        // Assert
        Assert.Equal(3, result.Count);
        _memberDatabaseMock.Verify(d => d.InsertMany(It.IsAny<IEnumerable<MemberModel>>()), Times.Exactly(2));
        _userCloudPortMock.Verify(p => p.CreateManyAsync(It.IsAny<IEnumerable<MemberModel>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
