using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Infrastructure.Integrations.Multifactor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using System.Net;

namespace DirectorySync.Infrastructure.Tests;

public class MultifactorApiTests
{
    public class CreateMany
    {
        [Fact]
        public async Task EmptyBucket_ShouldReturnEmpty()
        {
            var mocker = new AutoMocker();
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new NewUsersBucket();

            var response = await api.CreateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.CreatedUsers);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task IsSuccessStatusCode_ShouldReturnEmpty(int statusCode)
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
            bucket.AddNewUser(Guid.NewGuid(), "identity1");

            var response = await api.CreateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.CreatedUsers);
        }

        [Fact]
        public async Task Unauthorized_ShouldLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Create(statusCode: HttpStatusCode.Unauthorized));
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new NewUsersBucket();
            bucket.AddNewUser(Guid.NewGuid(), "identity1");

            _ = await api.CreateManyAsync(bucket);

            const string log = "Recieved 401 status from Multifactor API. Check API integration";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task NullModel_ShouldReturnEmptyAndLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Create());
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new NewUsersBucket();
            bucket.AddNewUser(Guid.NewGuid(), "identity1");

            var response = await api.CreateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.CreatedUsers);

            const string log = "Response model is null";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task ShouldReturnSuccessfulyProcessedUserIdentities()
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
            bucket.AddNewUser(Guid.NewGuid(), "identity1");
            bucket.AddNewUser(Guid.NewGuid(), "identity2");

            var response = await api.CreateManyAsync(bucket);

            Assert.Contains("identity1", response.CreatedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.CreatedUsers.Select(x => x.Identity));
        }

        [Fact]
        public async Task ShouldNotContainFailedUserIdentity()
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
            bucket.AddNewUser(Guid.NewGuid(), "identity1");
            bucket.AddNewUser(Guid.NewGuid(), "identity2");

            var response = await api.CreateManyAsync(bucket);

            Assert.DoesNotContain("identity1", response.CreatedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.CreatedUsers.Select(x => x.Identity));
        }
    }

    public class UpdateMany
    {
        [Fact]
        public async Task EmptyBucket_ShouldReturnEmpty()
        {
            var mocker = new AutoMocker();
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();

            var response = await api.UpdateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.UpdatedUsers);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task IsSuccessStatusCode_ShouldReturnEmpty(int statusCode)
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IOptions<MultifactorApiOptions>>()
                .Setup(x => x.Value).Returns(new MultifactorApiOptions
                {
                    Key = "key"
                });
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Update(statusCode: (HttpStatusCode)statusCode));
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            var response = await api.UpdateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.UpdatedUsers.Select(x => x.Identity));
        }

        [Fact]
        public async Task Unauthorized_ShouldLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Update(statusCode: HttpStatusCode.Unauthorized));
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            _ = await api.UpdateManyAsync(bucket);

            const string log = "Recieved 401 status from Multifactor API. Check API integration";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task NullModel_ShouldReturnEmptyAndLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Update());
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            var response = await api.UpdateManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.UpdatedUsers.Select(x => x.Identity));

            const string log = "Response model is null";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task ShouldReturnSuccessfulyProcessedUserIdentities()
        {
            var mocker = new AutoMocker();
            var body = new
            {
                Failures = Array.Empty<object>()
            };
            var cli = FakeMultifactorCloud.ClientMock.Users_Update(body);
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(cli);
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");
            bucket.Add(Guid.NewGuid(), "identity2");

            var response = await api.UpdateManyAsync(bucket);

            Assert.Contains("identity1", response.UpdatedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.UpdatedUsers.Select(x => x.Identity));
        }

        [Fact]
        public async Task ShouldNotContainFailedUserIdentity()
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
            var cli = FakeMultifactorCloud.ClientMock.Users_Update(body);
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(cli);
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new ModifiedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");
            bucket.Add(Guid.NewGuid(), "identity2");

            var response = await api.UpdateManyAsync(bucket);

            Assert.DoesNotContain("identity1", response.UpdatedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.UpdatedUsers.Select(x => x.Identity));
        }
    }

    public class DeleteMany
    {
        [Fact]
        public async Task EmptyBucket_ShouldReturnEmpty()
        {
            var mocker = new AutoMocker();
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();

            var response = await api.DeleteManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.DeletedUsers);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task IsSuccessStatusCode_ShouldReturnEmpty(int statusCode)
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IOptions<MultifactorApiOptions>>()
                .Setup(x => x.Value).Returns(new MultifactorApiOptions
                {
                    Key = "key"
                });
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Delete(statusCode: (HttpStatusCode)statusCode));
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            var response = await api.DeleteManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.DeletedUsers);
        }

        [Fact]
        public async Task Unauthorized_ShouldLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Delete(statusCode: HttpStatusCode.Unauthorized));
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            _ = await api.DeleteManyAsync(bucket);

            const string log = "Recieved 401 status from Multifactor API. Check API integration";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task NullModel_ShouldReturnEmptyAndLog()
        {
            var mocker = new AutoMocker();
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(FakeMultifactorCloud.ClientMock.Users_Delete());
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");

            var response = await api.DeleteManyAsync(bucket);

            Assert.NotNull(response);
            Assert.Empty(response.DeletedUsers);

            const string log = "Response model is null";
            mocker.GetMock<ILogger<MultifactorApi>>().VerifyLog(LogLevel.Warning, Times.Once(), log);
        }

        [Fact]
        public async Task ShouldReturnSuccessfulyProcessedUserIdentities()
        {
            var mocker = new AutoMocker();
            var body = new
            {
                Failures = Array.Empty<object>()
            };
            var cli = FakeMultifactorCloud.ClientMock.Users_Delete(body);
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(cli);
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");
            bucket.Add(Guid.NewGuid(), "identity2");

            var response = await api.DeleteManyAsync(bucket);

            Assert.Contains("identity1", response.DeletedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.DeletedUsers.Select(x => x.Identity));
        }

        [Fact]
        public async Task ShouldNotContainFailedUserIdentity()
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
            var cli = FakeMultifactorCloud.ClientMock.Users_Delete(body);
            mocker.GetMock<IHttpClientFactory>()
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(cli);
            var api = mocker.CreateInstance<MultifactorApi>();
            var bucket = new DeletedUsersBucket();
            bucket.Add(Guid.NewGuid(), "identity1");
            bucket.Add(Guid.NewGuid(), "identity2");

            var response = await api.DeleteManyAsync(bucket);

            Assert.DoesNotContain("identity1", response.DeletedUsers.Select(x => x.Identity));
            Assert.Contains("identity2", response.DeletedUsers.Select(x => x.Identity));
        }
    }
}
