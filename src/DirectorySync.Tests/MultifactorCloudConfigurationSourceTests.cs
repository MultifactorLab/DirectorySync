using DirectorySync.Domain;
using DirectorySync.Infrastructure.ConfigurationSources.MultifactorCloud;
using DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig.Dto;
using Moq;
using Moq.Contrib.HttpClient;

namespace DirectorySync.Tests;

public class MultifactorCloudConfigurationSourceTests
{
    [Fact]
    public void Load_ShouldSetSettings()
    {
        var model = new
        {
            Enabled = true,
            DirectoryGroups = new[] { (string)new DirectoryGuid(Guid.Parse("87fa4f55-16f2-4043-aa85-15936db5f1db")) },
            MultifactorGroupPolicyPreset = new
            {
                SignUpGroups = new[] { "mygroup" }
            },
            IncludeNestedGroups = true,
            PropertyMapping = new
            {
                IdentityAttribute = "sAmaccountname",
                NameAttribute = "Name",
                EmailAttributes = new[] { "mail", "email" },
                PhoneAttributes = new[] { "phone", "mobilephone" },
                AdditionalAttributes = null as object
            },
            ScanTimer = TimeSpan.FromSeconds(30),
            SyncTimer = TimeSpan.FromSeconds(30)
        };

        var cli = FakeMultifactorCloud.HttpClientMock.Ds_Settings(model);
        cli.DefaultRequestHeaders.Add("Authorization", FakeMultifactorCloud.GetBasicAuthHeaderValue());

        var src = new TestableMultifactorCloudConfigurationSource(cli);

        src.Load();

        Assert.Equal("True", src.ConfigurationData["Sync:Enabled"]);
        Assert.Equal("00:00:30", src.ConfigurationData["Sync:SyncTimer"]);
        Assert.Equal("00:00:30", src.ConfigurationData["Sync:ScanTimer"]);
        Assert.Equal("87fa4f55-16f2-4043-aa85-15936db5f1db", src.ConfigurationData["Sync:Groups:0"]);
        Assert.Equal("True", src.ConfigurationData["Sync:IncludeNestedGroups"]);

        Assert.Equal("sAmaccountname", src.ConfigurationData["Sync:IdentityAttribute"]);
        Assert.Equal("Name", src.ConfigurationData["Sync:NameAttribute"]);

        Assert.Equal("mail", src.ConfigurationData["Sync:EmailAttributes:0"]);
        Assert.Equal("email", src.ConfigurationData["Sync:EmailAttributes:1"]);

        Assert.Equal("phone", src.ConfigurationData["Sync:PhoneAttributes:0"]);
        Assert.Equal("mobilephone", src.ConfigurationData["Sync:PhoneAttributes:1"]);
    }

    [Fact]
    public void SetData_ShouldSetGroupsAsIndexedElements()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [ Guid.NewGuid().ToString(), Guid.NewGuid().ToString() ]
        };

        src.SetConfigurationData(dto, true);

        var pairs = src.ConfigurationData.Where(x => x.Key.StartsWith("Sync:Groups")).ToArray();
        Assert.Equal(2, pairs.Length);

        Assert.Contains(pairs, x => x.Key == "Sync:Groups:0");
        Assert.Contains(pairs, x => x.Key == "Sync:Groups:1");
    }    
    
    [Fact]
    public void SetData_ShouldReplace()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [ Guid.NewGuid().ToString(), Guid.NewGuid().ToString() ]
        };
        src.SetConfigurationData(dto, true);

        // Act

        var dto1 = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto1, true);

        // Assertion

        var pair = Assert.Single(src.ConfigurationData.Where(x => x.Key.StartsWith("Sync:Groups")));
        Assert.Equal("Sync:Groups:0", pair.Key);
    }

    [Fact]
    public void SetData_ShouldRemove()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto, true);

        // Act

        var dto1 = new CloudConfigDto
        {
            DirectoryGroups = []
        };
        src.SetConfigurationData(dto1, true);

        // Assertion

        Assert.Empty(src.ConfigurationData.Where(x => x.Key.StartsWith("Sync:Groups")));
    }

    [Fact]
    public void SetData_InitialAndNewGuidIsDifferent_ShouldNotThrow()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto, true);

        // Act

        var dto1 = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto1, true);
    }    
    
    [Fact]
    public void SetData_NotInitialAndNewGuidsLengthIsDifferent_ShouldNotThrow()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto, true);

        // Act

        var dto1 = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString()]
        };

        // Assertion

        var ex = Assert.Throws<InconsistentConfigurationException>(() => src.SetConfigurationData(dto1, false));
        Assert.Equal(MultifactorCloudConfigurationSource.InconsistentConfigMessage, ex.Message);
    }    
    
    [Fact]
    public void SetData_NotInitialAndNewGuidsValuesAreDifferent_ShouldNotThrow()
    {
        var src = new TestableMultifactorCloudConfigurationSource(new Mock<HttpMessageHandler>().CreateClient());
        var dto = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString()]
        };
        src.SetConfigurationData(dto, true);

        // Act

        var dto1 = new CloudConfigDto
        {
            DirectoryGroups = [Guid.NewGuid().ToString()]
        };

        // Assertion

        var ex = Assert.Throws<InconsistentConfigurationException>(() => src.SetConfigurationData(dto1, false));
        Assert.Equal(MultifactorCloudConfigurationSource.InconsistentConfigMessage, ex.Message);
    }
}
