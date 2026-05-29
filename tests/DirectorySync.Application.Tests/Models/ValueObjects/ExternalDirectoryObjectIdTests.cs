using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Tests.Models.ValueObjects;

public class ExternalDirectoryObjectIdTests
{
    [Fact]
    public void ToCanonicalString_Returns_Guid_Format_D()
    {
        var guid = new Guid("a1b2c3d4-e5f6-4789-a012-3456789abcde");
        var directoryGuid = new DirectoryGuid(guid);

        var s = ExternalDirectoryObjectId.ToCanonicalString(directoryGuid);

        Assert.Equal("a1b2c3d4-e5f6-4789-a012-3456789abcde", s);
    }

    [Fact]
    public void ToCanonicalString_Throws_When_DirectoryGuid_Null()
    {
        Assert.Throws<ArgumentNullException>(() => ExternalDirectoryObjectId.ToCanonicalString(null!));
    }
}
