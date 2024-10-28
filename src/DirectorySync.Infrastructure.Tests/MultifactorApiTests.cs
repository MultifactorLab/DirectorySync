using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Infrastructure.Integrations.Multifactor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using System.Net;

namespace DirectorySync.Infrastructure.Tests;

public class MultifactorApiTests
{
    [Fact]
    public async Task CreateMany_EmptyBucket_ShouldReturnEmpty()
    {
        var mocker = new AutoMocker();
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();

        var response = await api.CreateManyAsync(bucket);

        Assert.NotNull(response);
        Assert.Empty(response.CreatedUserIdentities);
    }    
    
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task CreateMany_IsSuccessStatusCode_ShouldReturnEmpty(int statusCode)
    {
        var mocker = new AutoMocker();
        mocker.GetMock<IOptions<MultifactorApiOptions>>()
            .Setup(x => x.Value).Returns(new MultifactorApiOptions
            {
                Key = "key"
            });
        mocker.GetMock<IHttpClientFactory>()
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(FakeMultifactorCloud.ClientMock.Users_Create(statusCode: (HttpStatusCode)statusCode));
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();
        bucket.AddNewUser("identity1");

        var response = await api.CreateManyAsync(bucket);

        Assert.NotNull(response);
        Assert.Empty(response.CreatedUserIdentities);
    }    
    
    [Fact]
    public async Task CreateMany_Unauthorized_ShouldLog()
    {
        var mocker = new AutoMocker();
        mocker.GetMock<IHttpClientFactory>()
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(FakeMultifactorCloud.ClientMock.Users_Create(HttpStatusCode.Unauthorized));
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();
        bucket.AddNewUser("identity1");

        _ = await api.CreateManyAsync(bucket);

        const string log = "Recieved 401 status from Multifactor API. Check API integration";
        mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
    }    
    
    [Fact]
    public async Task CreateMany_NullModel_ShouldReturnEmptyAndLog()
    {
        var mocker = new AutoMocker();
        mocker.GetMock<IHttpClientFactory>()
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(FakeMultifactorCloud.ClientMock.Users_Create());
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();
        bucket.AddNewUser("identity1");

        var response = await api.CreateManyAsync(bucket);

        Assert.NotNull(response);
        Assert.Empty(response.CreatedUserIdentities);

        const string log = "Response model is null";
        mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
    }    
    
    [Fact]
    public async Task CreateMany_ShouldReturnSuccessfulyProcessedUserIdentities()
    {
        var mocker = new AutoMocker();
        var body = new 
        { 
            Failures = Array.Empty<object>()
        };
        var cli = FakeMultifactorCloud.ClientMock.Users_Create(body);
        mocker.GetMock<IHttpClientFactory>()
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(cli);
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();
        bucket.AddNewUser("identity1");
        bucket.AddNewUser("identity2");

        var response = await api.CreateManyAsync(bucket);

        Assert.Contains("identity1", response.CreatedUserIdentities);
        Assert.Contains("identity2", response.CreatedUserIdentities);
    }    
    
    [Fact]
    public async Task CreateMany_ShouldNotContainFailedUserIdentity()
    {
        var mocker = new AutoMocker();
        var body = new
        {
            Failures = new[]
            {
                new
                {
                    Identity = "identity1"
                }
            }
        };
        var cli = FakeMultifactorCloud.ClientMock.Users_Create(body);
        mocker.GetMock<IHttpClientFactory>()
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(cli);
        var api = mocker.CreateInstance<MultifactorApi>();
        var bucket = new NewUsersBucket();
        bucket.AddNewUser("identity1");
        bucket.AddNewUser("identity2");

        var response = await api.CreateManyAsync(bucket);

        Assert.DoesNotContain("identity1", response.CreatedUserIdentities);
        Assert.Contains("identity2", response.CreatedUserIdentities);
    }
}
