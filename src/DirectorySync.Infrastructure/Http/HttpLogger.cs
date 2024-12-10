using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Http;

internal class HttpLogger : DelegatingHandler
{
    private readonly ILogger<HttpLogger> _logger;

    public HttpLogger(ILogger<HttpLogger> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            return await ActWithDebug(request, cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> ActWithDebug(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending {Method} request to {Url}:{ContentLength} content length",
            request.Method, request.RequestUri, request.Content?.Headers.ContentLength ?? 0);

        var responseDbg = await base.SendAsync(request, cancellationToken);

        var respBody = string.Empty;
        if (responseDbg?.Content != null)
        {
            respBody = await responseDbg.Content.ReadAsStringAsync(cancellationToken);
        }
        _logger.LogDebug("Got {HttpCode} response from {Url} with {Body}",
            responseDbg?.StatusCode, request.RequestUri, respBody);

        return responseDbg;
    }
}
