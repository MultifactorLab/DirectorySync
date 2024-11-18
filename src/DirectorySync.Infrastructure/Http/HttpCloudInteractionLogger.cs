using DirectorySync.Infrastructure.Logging;

namespace DirectorySync.Infrastructure.Http;

/// <summary>
/// Http logger with the <see cref="StartupLogger"/> usage.
/// </summary>
public class HttpCloudInteractionLogger : DelegatingHandler
{
    /// <summary>
    /// Creates new instance of a <see cref="HttpCloudInteractionLogger"/> with a <see cref="MfTraceIdHeaderSetter"/>.
    /// </summary>
    public HttpCloudInteractionLogger()
    {
        var tracer = new MfTraceIdHeaderSetter
        {
            InnerHandler = new HttpClientHandler()
        };

        InnerHandler = tracer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CloudInteractionLogger.Debug("Sending {Method} request to {Url} with {Body}", 
            request.Method, 
            request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        var respBody = string.Empty;
        if (response?.Content != null)
        {
            respBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        CloudInteractionLogger.Debug("Got {HttpCode} response from {Url} with {Body}", 
            response?.StatusCode, 
            request.RequestUri, 
            respBody);

        return response;
    }
}
