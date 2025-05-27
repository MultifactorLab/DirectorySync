using DirectorySync.Infrastructure.Shared.Http;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace DirectorySync.Tests;

internal static class FakeMultifactorCloud
{
    public const string Uri = "https://api.multifactor.dev";
    public const string Key = "key";
    public const string Secret = "ds_secret";

    public static class HttpClientMock
    {
        /// <summary>
        /// GET https://api.multifactor.dev/ds/settings
        /// </summary>
        /// <returns></returns>
        public static HttpClient Ds_Settings(object? responseBody = null)
        {
            return GetHttpClientMock(handler =>
            {
                handler.SetupRequest(HttpMethod.Get, $"{Uri}/v2/ds/settings", x =>
                {
                    var auth = new BasicAuthHeaderValue(Key, Secret);
                    var actualAuth = x.Headers.Authorization;
                    return actualAuth?.Scheme == "Basic" && actualAuth.Parameter == auth.GetBase64();
                }).ReturnsJsonResponse(System.Net.HttpStatusCode.OK, responseBody);
            });
        }
    }

    public static string GetBasicAuthHeaderValue()
    {
        return $"Basic {new BasicAuthHeaderValue(Key, Secret).GetBase64()}";
    }

    private static HttpClient GetHttpClientMock(Action<Mock<HttpMessageHandler>> setup)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound);

        setup(handler);

        var cli = handler.CreateClient();
        cli.BaseAddress = new Uri(Uri);
        return cli;
    }
}
