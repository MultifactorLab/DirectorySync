using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Enums;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Tests.Models.Core;

public class MemberModelTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        var id = Guid.NewGuid();
        var identity = new Identity("test");
        var groups = new List<DirectoryGuid> { new DirectoryGuid(Guid.NewGuid()) };

        var member = MemberModel.Create(id, identity, groups);

        Assert.Equal(id, member.Id.Value);
        Assert.Equal(identity, member.Identity);
        Assert.Equal(groups.Count, member.GroupIds.Count);
        Assert.Equal(ChangeOperation.None, member.Operation);
    }

    [Fact]
    public void Create_WithNullIdentity_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MemberModel.Create(Guid.NewGuid(), null, []));
    }

    [Fact]
    public void Create_WithNullGroups_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MemberModel.Create(Guid.NewGuid(), new Identity("test"), null));
    }

    [Fact]
    public void AddGroups_WithNewGroups_ShouldAddAndTrackAddedGroups()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);

        var group = new DirectoryGuid(Guid.NewGuid());
        member.AddGroups([group]);

        Assert.Contains(group, member.GroupIds);
        Assert.Contains(group, member.AddedGroupIds);
    }

    [Fact]
    public void AddGroups_WithExistingGroup_ShouldThrowInvalidOperationException()
    {
        var group = new DirectoryGuid(Guid.NewGuid());
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), [group]);

        var ex = Assert.Throws<InvalidOperationException>(() => member.AddGroups([group]));
        Assert.Contains("Groups already assigned", ex.Message);
    }

    [Fact]
    public void RemoveGroups_ShouldRemoveAndTrackRemovedGroups()
    {
        var group = new DirectoryGuid(Guid.NewGuid());
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), [group]);

        member.RemoveGroups([group]);

        Assert.DoesNotContain(group, member.GroupIds);
        Assert.Contains(group, member.RemovedGroupIds);
    }

    [Fact]
    public void AddCloudGroups_ShouldTrackAddedCloudGroups()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.AddCloudGroups(["cloudGroup"]);

        Assert.Contains("cloudGroup", member.AddedCloudGroups);
    }

    [Fact]
    public void RemoveCloudGroups_ShouldTrackRemovedCloudGroups()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.RemoveCloudGroups(["cloudGroup"]);

        Assert.Contains("cloudGroup", member.RemovedCloudGroups);
    }

    [Fact]
    public void SetProperties_WithDifferentHash_ShouldReplacePropertiesAndUpdateHash()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);

        var ldapAttributes = new LdapAttributeCollection(new LdapAttribute[] { new LdapAttribute(new LdapAttributeName("propOld1"), string.Empty), new LdapAttribute(new LdapAttributeName("propOld2"), string.Empty) });

        var newHash = new AttributesHash(ldapAttributes);
        var newProperties = new MemberProperty[] { new MemberProperty("prop1", "value1") };

        member.SetProperties(newProperties, newHash);

        Assert.Equal(newHash, member.AttributesHash);
        Assert.Contains(newProperties[0], member.Properties);
    }

    [Fact]
    public void SetProperties_WithSameHash_ShouldNotChangeProperties()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        var ldapAttributes = new LdapAttributeCollection(new LdapAttribute[] { new LdapAttribute(new LdapAttributeName("attr"), string.Empty) });
        var hash = new AttributesHash(ldapAttributes);
        var properties = new MemberProperty[] { new MemberProperty("prop1", "value1") };

        member.SetProperties(properties, hash);
        var countBefore = member.Properties.Count;

        member.SetProperties([new MemberProperty("prop2", "value2")], hash);

        Assert.Equal(countBefore, member.Properties.Count);
    }

    [Fact]
    public void MarkForCreate_ShouldSetOperationCreate()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.MarkForCreate();

        Assert.Equal(ChangeOperation.Create, member.Operation);
    }

    [Fact]
    public void MarkForUpdate_ShouldSetOperationUpdate()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.MarkForUpdate();

        Assert.Equal(ChangeOperation.Update, member.Operation);
    }

    [Fact]
    public void MarkForDelete_ShouldSetOperationDelete()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.MarkForDelete();

        Assert.Equal(ChangeOperation.Delete, member.Operation);
    }

    [Fact]
    public void ResetOperation_ShouldSetOperationNone()
    {
        var member = MemberModel.Create(Guid.NewGuid(), new Identity("test"), []);
        member.MarkForUpdate();

        member.ResetOperation();

        Assert.Equal(ChangeOperation.None, member.Operation);
    }
} 
