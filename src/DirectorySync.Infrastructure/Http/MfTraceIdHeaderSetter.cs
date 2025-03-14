﻿namespace DirectorySync.Infrastructure.Http;

public class MfTraceIdHeaderSetter : DelegatingHandler
{
    private const string _key = "mf-trace-id";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var trace = $"ds-{ActivityContext.Current.ActivityId}";
        if (!request.Headers.Contains(_key))
        {
            request.Headers.Add(_key, trace);
        }

        var resp = await base.SendAsync(request, cancellationToken);
        if (!resp.Headers.Contains(_key))
        {
            resp.Headers.Add(_key, trace);
        }

        return resp;
    }
}
