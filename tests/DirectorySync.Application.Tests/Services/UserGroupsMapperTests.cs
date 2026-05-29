using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Services;

namespace DirectorySync.Application.Tests.Services;

public class UserGroupsMapperTests
{
    private readonly UserGroupsMapper _mapper;

    public UserGroupsMapperTests()
    {
        _mapper = new UserGroupsMapper();
    }

    #region GetCloudGroupChanges

    [Fact]
    public void GetCloudGroupChanges_ThrowsArgumentNullException_WhenMemberIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _mapper.GetCloudGroupChanges(null!, new Dictionary<DirectoryGuid, string[]>()));
    }

    [Fact]
    public void GetCloudGroupChanges_ThrowsArgumentNullException_WhenMappingIsNull()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), []);
        Assert.Throws<ArgumentNullException>(() => _mapper.GetCloudGroupChanges(member, null!));
    }

    [Fact]
    public void GetCloudGroupChanges_ReturnsCorrectToAddAndToRemove()
    {
        // Arrange
        var groupId1 = new DirectoryGuid(Guid.NewGuid());
        var groupId2 = new DirectoryGuid(Guid.NewGuid());
        var groupId3 = new DirectoryGuid(Guid.NewGuid());

        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), [groupId1, groupId2]);
        member.AddGroups([groupId3]);
        member.RemoveGroups([groupId2]);

        var mapping = new Dictionary<DirectoryGuid, string[]>
        {
            { groupId1, new[] { "cloud1" } },
            { groupId2, new[] { "cloud2" } },
            { groupId3, new[] { "cloud3" } },
        };

        // Act
        var (toAdd, toRemove) = _mapper.GetCloudGroupChanges(member, mapping);

        // Assert
        Assert.Single(toAdd);
        Assert.Contains("cloud3", toAdd);
        Assert.Single(toRemove);
        Assert.Contains("cloud2", toRemove);
    }

    [Fact]
    public void GetCloudGroupChanges_RemovedGroupStillMapped_NotRemoved()
    {
        // Arrange
        var groupId1 = new DirectoryGuid(Guid.NewGuid());
        var groupId2 = new DirectoryGuid(Guid.NewGuid());

        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), [groupId1, groupId2]);
        member.RemoveGroups([groupId1]);

        var mapping = new Dictionary<DirectoryGuid, string[]>
        {
            { groupId1, new[] { "cloud1" } },
            { groupId2, new[] { "cloud1", "cloud2" } },
        };

        // Act
        var (toAdd, toRemove) = _mapper.GetCloudGroupChanges(member, mapping);

        // Assert
        Assert.Empty(toAdd);
        Assert.Empty(toRemove);
    }

    #endregion

    #region SetUserCloudGroupsDiff

    [Fact]
    public void SetUserCloudGroupsDiff_ThrowsArgumentNullException_WhenMemberIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.SetUserCloudGroupsDiff(null!,
                new Dictionary<DirectoryGuid, string[]>(),
                new Dictionary<DirectoryGuid, string[]>()));
    }

    [Fact]
    public void SetUserCloudGroupsDiff_ThrowsArgumentNullException_WhenOldMappingsIsNull()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), []);
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.SetUserCloudGroupsDiff(member,
                null!,
                new Dictionary<DirectoryGuid, string[]>()));
    }

    [Fact]
    public void SetUserCloudGroupsDiff_ThrowsArgumentNullException_WhenNewMappingsIsNull()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), []);
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.SetUserCloudGroupsDiff(member,
                new Dictionary<DirectoryGuid, string[]>(),
                null!));
    }

    [Fact]
    public void SetUserCloudGroupsDiff_ReturnsCorrectToAddAndToRemove()
    {
        // Arrange
        var groupId = new DirectoryGuid(Guid.NewGuid());
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("user"), [groupId]);

        var oldMappings = new Dictionary<DirectoryGuid, string[]>
        {
            { groupId, new[] { "old1", "common" } }
        };

        var newMappings = new Dictionary<DirectoryGuid, string[]>
        {
            { groupId, new[] { "new1", "common" } }
        };

        // Act
        var (toAdd, toRemove) = _mapper.SetUserCloudGroupsDiff(member, oldMappings, newMappings);

        // Assert
        Assert.Single(toAdd);
        Assert.Contains("new1", toAdd);
        Assert.Single(toRemove);
        Assert.Contains("old1", toRemove);
    }

    #endregion

    #region GetChangedDirectoryGroups

    [Fact]
    public void GetChangedDirectoryGroups_ThrowsArgumentNullException_WhenOldMappingsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.GetChangedDirectoryGroups(null!, Array.Empty<GroupMapping>()));
    }

    [Fact]
    public void GetChangedDirectoryGroups_ThrowsArgumentNullException_WhenNewMappingsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.GetChangedDirectoryGroups(Array.Empty<GroupMapping>(), null!));
    }

    [Fact]
    public void GetChangedDirectoryGroups_ReturnsChangedGroups()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dirGuid = new DirectoryGuid(guid);

        var oldMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = guid.ToString(),
                SignUpGroups = ["group1", "group2"]
            }
        };

        var newMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = guid.ToString(),
                SignUpGroups = ["group1", "group3"]
            }
        };

        // Act
        var changed = _mapper.GetChangedDirectoryGroups(oldMappings, newMappings);

        // Assert
        Assert.Single(changed);
        Assert.Contains(dirGuid, changed);
    }

    [Fact]
    public void GetChangedDirectoryGroups_NoChanges_ReturnsEmpty()
    {
        // Arrange
        var guid = Guid.NewGuid();

        var oldMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = guid.ToString(),
                SignUpGroups = ["group1", "group2"]
            }
        };

        var newMappings = new[]
        {
            new GroupMapping
            {
                DirectoryGroup = guid.ToString(),
                SignUpGroups = ["group1", "group2"]
            }
        };

        // Act
        var changed = _mapper.GetChangedDirectoryGroups(oldMappings, newMappings);

        // Assert
        Assert.Empty(changed);
    }

    #endregion
}
