using DirectorySync.Application.Integrations.Multifactor;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace DirectorySync.Infrastructure.Tests;

internal static class FakeMultifactorCloud
{
    public const string Uri = "https://api.multifactor.dev";
    public const string Key = "key";
    public const string Secret = "ds_secret";

    public static class ClientMock
    {
        /// <summary>
        /// POST https://api.multifactor.dev/ds/users
        /// </summary>
        /// <returns></returns>
        public static HttpClient Users_Create(object? responseBody = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return GetHttpClientMock(handler =>
            {
                handler.SetupRequest(HttpMethod.Post, $"{Uri}/ds/users").ReturnsJsonResponse(statusCode, responseBody);
            });
        }

        /// <summary>
        /// PUT https://api.multifactor.dev/ds/users
        /// </summary>
        /// <returns></returns>
        public static HttpClient Users_Update(object? responseBody = null,
                HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return GetHttpClientMock(handler =>
            {
                handler.SetupRequest(HttpMethod.Put, $"{Uri}/ds/users").ReturnsJsonResponse(statusCode, responseBody);
            });
        }

        /// <summary>
        /// DELETE https://api.multifactor.dev/ds/users
        /// </summary>
        /// <returns></returns>
        public static HttpClient Users_Delete(object? responseBody = null,
                HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return GetHttpClientMock(handler =>
            {
                handler.SetupRequest(HttpMethod.Delete, $"{Uri}/ds/users").ReturnsJsonResponse(statusCode, responseBody);
            });
        }
    }

    private static HttpClient GetHttpClientMock(Action<Mock<HttpMessageHandler>> setup)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound);

        setup(handler);

        var cli = handler.CreateClient();
        cli.BaseAddress = new Uri(Uri);

        var auth = new BasicAuthHeaderValue(Key, Secret);
        cli.DefaultRequestHeaders.Add("Authorization", $"Basic {auth.GetBase64()}");

        return cli;
    }
}
