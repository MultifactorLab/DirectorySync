﻿using DirectorySync.Infrastructure.Logging;

namespace DirectorySync.Infrastructure.Http;

public class HttpTracerWihFallbackLoging : DelegatingHandler
{
    public HttpTracerWihFallbackLoging()
    {
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var reqBody = string.Empty;
        if (request.Content != null)
        {
            reqBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        FallbackLogger.Information("Sending {Method} request to {Url} with {Body}", request.Method, request.RequestUri, reqBody);

        var response = await base.SendAsync(request, cancellationToken);

        var respBody = string.Empty;
        if (response?.Content != null)
        {
            respBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        FallbackLogger.Information("Got {HttpCode} response from {Url} with {Body}", response?.StatusCode, request.RequestUri, respBody);

        return response;
    }
}
