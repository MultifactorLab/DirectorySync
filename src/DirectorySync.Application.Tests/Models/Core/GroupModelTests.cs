using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Tests.Models.Core;

public class GroupModelTests
{
    [Fact]
    public void Create_WithValidMembers_ShouldInitializeCorrectly()
    {
        var id = Guid.NewGuid();
        var members = new List<DirectoryGuid> { new DirectoryGuid(Guid.NewGuid()) };

        var group = GroupModel.Create(id, members);

        Assert.Equal(id, group.Id.Value);
        Assert.Equal(members.Count, group.MemberIds.Count);
        Assert.NotNull(group.MembersHash);
        Assert.Equal(ChangeOperation.None, group.Operation);
    }

    [Fact]
    public void Create_WithNullMembers_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GroupModel.Create(Guid.NewGuid(), null));
    }

    [Fact]
    public void AddMembers_WithNewMembers_ShouldAddAndRecalculateHash()
    {
        var id = Guid.NewGuid();
        var group = GroupModel.Create(id, []);

        var newMember = new DirectoryGuid(Guid.NewGuid());
        group.AddMembers([newMember]);

        Assert.Contains(newMember, group.MemberIds);
        Assert.NotNull(group.MembersHash);
    }

    [Fact]
    public void AddMembers_WithExistingMember_ShouldThrowInvalidOperationException()
    {
        var member = new DirectoryGuid(Guid.NewGuid());
        var group = GroupModel.Create(Guid.NewGuid(), [member]);

        var ex = Assert.Throws<InvalidOperationException>(() => group.AddMembers([member]));
        Assert.Contains("Specified users already exist", ex.Message);
    }

    [Fact]
    public void RemoveMembers_ShouldRemoveAndRecalculateHash()
    {
        var member = new DirectoryGuid(Guid.NewGuid());
        var group = GroupModel.Create(Guid.NewGuid(), [member]);

        group.RemoveMembers([member]);

        Assert.DoesNotContain(member, group.MemberIds);
        Assert.NotNull(group.MembersHash);
    }

    [Fact]
    public void MarkForUpdate_ShouldSetOperationUpdate()
    {
        var group = GroupModel.Create(Guid.NewGuid(), []);

        group.MarkForUpdate();

        Assert.Equal(ChangeOperation.Update, group.Operation);
    }

    [Fact]
    public void ResetOperation_ShouldSetOperationNone()
    {
        var group = GroupModel.Create(Guid.NewGuid(), []);
        group.MarkForUpdate();

        group.ResetOperation();

        Assert.Equal(ChangeOperation.None, group.Operation);
    }
} 
