using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Http;

internal class HttpTracer : DelegatingHandler
{
    private readonly ILogger<HttpTracer> _logger;

    public HttpTracer(ILogger<HttpTracer> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var reqBody = string.Empty;
            if (request.Content != null)
            {
                reqBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            _logger.LogDebug("Sending {Method} request to {Url} with {Body}",
                request.Method, request.RequestUri, reqBody);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var respBody = string.Empty;
            if (response?.Content != null)
            {
                respBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            _logger.LogDebug("Got {HttpCode} response from {Url} with {Body}",
                response?.StatusCode, request.RequestUri, respBody);
        }

        return response;
    }
}
