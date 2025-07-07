using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Tests.Models.ValueObjects;

public class DirectoryGuidTests
{
    [Fact]
    public void CreateInstance_FromGuid_ShouldReturnInstance()
    {
        var dGuid = new DirectoryGuid(Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32"));
        Assert.Equal(Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32"), dGuid.Value);
        Assert.Equal(@"\2F\B2\6B\E0\F0\94\B5\45\81\D5\0E\9A\7E\4E\9C\32", dGuid.OctetString);
    }
    
    [Fact]
    public void CreateInstance_FromBytes_ShouldReturnInstance()
    {
        var bytes = Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32").ToByteArray();
        var dGuid = new DirectoryGuid(bytes);
        Assert.Equal(Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32"), dGuid.Value);
        Assert.Equal(@"\2F\B2\6B\E0\F0\94\B5\45\81\D5\0E\9A\7E\4E\9C\32", dGuid.OctetString);
    }
    
    [Fact]
    public void CreateInstance_FromEmptyGuid_ShouldExplode()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new DirectoryGuid(Guid.Empty));
        Assert.Equal("Empty directory guid", ex.Message);
    }
    
    [Fact]
    public void Equals_GuidCtor_ShouldReturnTrue()
    {
        var guid = Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32");
        Assert.True(new DirectoryGuid(guid) == new DirectoryGuid(guid));
    }
    
    [Fact]
    public void Equals_BytesCtor_ShouldReturnTrue()
    {
        var bytes = Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32").ToByteArray();
        Assert.True(new DirectoryGuid(bytes) == new DirectoryGuid(bytes));
    }
    
    [Fact]
    public void Equals_BytesAndGuidCtor_ShouldReturnTrue()
    {
        var bytes = Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32").ToByteArray();
        var guid = Guid.Parse("e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32");
        Assert.True(new DirectoryGuid(bytes) == new DirectoryGuid(guid));
    }
}
