using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Domain;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace DirectorySync.Tests
{
    internal static class FakeMultifactorCloud
    {
        public const string Uri = "https://api.multifactor.dev";
        public const string Key = "key";
        public const string Secret = "ds_secret";

        public static class HttpClientMock
        {
            /// <summary>
            /// GET /ds/settings
            /// </summary>
            /// <returns></returns>
            public static HttpClient Ds_Settings(object? responseBody = null)
            {
                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                var cli = handler.CreateClient();
                cli.BaseAddress = new Uri(Uri);

                handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound);

                handler.SetupRequest(HttpMethod.Get, $"{Uri}/ds/settings", x =>
                {
                    var auth = new BasicAuthHeaderValue(Key, Secret);
                    var actualAuth = x.Headers.Authorization;
                    return actualAuth?.Scheme == "Basic" && actualAuth.Parameter == auth.GetBase64();
                }).ReturnsJsonResponse(System.Net.HttpStatusCode.OK, responseBody);

                return cli;
            }
        }

        public static string GetBasicAuthHeaderValue()
        {
            return $"Basic {new BasicAuthHeaderValue(Key, Secret).GetBase64()}";
        }
    }
}
