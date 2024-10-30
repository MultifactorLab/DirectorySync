using DirectorySync.Domain;

namespace DirectorySync.Tests
{
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

            Assert.Equal("sAmaccountname", src.ConfigurationData["Sync:IdentityAttribute"]);
            Assert.Equal("Name", src.ConfigurationData["Sync:NameAttribute"]);

            Assert.Equal("mail", src.ConfigurationData["Sync:EmailAttributes:0"]);
            Assert.Equal("email", src.ConfigurationData["Sync:EmailAttributes:1"]);

            Assert.Equal("phone", src.ConfigurationData["Sync:PhoneAttributes:0"]);
            Assert.Equal("mobilephone", src.ConfigurationData["Sync:PhoneAttributes:1"]);
        }
    }
}
