using DirectorySync.Domain.ValueObjects;

namespace DirectorySync.Domain.Tests;

public class EntriesHashTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateInstance_EmptyArg_ShouldExplode(string value)
    {
        Assert.Throws<ArgumentException>(() => new EntriesHash(value));
    }
    
    [Fact]
    public void CreateInstance_ShouldCreate()
    {
        _ = new EntriesHash("hash");
    }
    
    [Fact]
    public void Create_ShouldReturnInstance()
    {
        var instance = EntriesHash.Create(Array.Empty<Guid>());
        Assert.NotNull(instance);
    }
    
    [Theory]
    [InlineData(new [] { "e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32" }, new [] { "e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32" })]
    public void Equals_ShouldReturnTrue(string[] leftArr, string[] rightArr)
    {
        var leftGuids = leftArr.Select(x => new DirectoryGuid(Guid.Parse(x)));
        var rightGuids = rightArr.Select(x => new DirectoryGuid(Guid.Parse(x)));
        
        var left = EntriesHash.Create(leftGuids);
        var right = EntriesHash.Create(rightGuids);
        
        Assert.True(left == right);
    }
    
    [Fact]
    public void Equals_DifferentOrder_ShouldReturnTrue()
    {
        var leftArr = new[]
        {
            "e9f7c934-5000-4f25-b972-68a2959008a9",
            "e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32"
        };
        var rightArr = new[]
        {
            "e06bb22f-94f0-45b5-81d5-0e9a7e4e9c32",
            "e9f7c934-5000-4f25-b972-68a2959008a9"
        };
        
        var leftGuids = leftArr.Select(x => new DirectoryGuid(Guid.Parse(x)));
        var rightGuids = rightArr.Select(x => new DirectoryGuid(Guid.Parse(x)));
        
        var left = EntriesHash.Create(leftGuids);
        var right = EntriesHash.Create(rightGuids);
        
        Assert.True(left == right);
    }
}
