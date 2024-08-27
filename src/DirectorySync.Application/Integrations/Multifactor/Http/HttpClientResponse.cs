using System.Collections.ObjectModel;
using System.Net;
using System.Text;

namespace DirectorySync.Application.Integrations.Multifactor.Http;

/// <summary>
/// Http response wrapper with the deserialized response body.
/// </summary>
/// <typeparam name="T"></typeparam>
public class HttpClientResponse<T> : HttpClientResponse
{
    /// <summary>
    /// Deserialized response body.
    /// </summary>
    public T? Model { get; }

    internal HttpClientResponse(HttpStatusCode statusCode, string? content, T? model, IDictionary<string, string[]> headers) 
        : base(statusCode, content, headers)
    {
        Model = model;
    }
}

/// <summary>
/// Wraps the http response.
/// </summary>
public class HttpClientResponse
{
    /// <summary>
    /// Http response status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Raw response body.
    /// </summary>
    public string? Content { get; }

    /// <summary>
    /// Http response headers.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ResponseHeaders { get; }

    /// <summary>
    /// Returns true if <see cref="StatusCode"/> is in the range 200-299; otherwise, false.
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    internal HttpClientResponse(HttpStatusCode statusCode, string? content, IDictionary<string, string[]> headers)
    {
        StatusCode = statusCode;
        Content = content;
        ResponseHeaders = new ReadOnlyDictionary<string, string[]>(headers);
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"StatusCode: {(int)StatusCode} {StatusCode}");
        sb.AppendLine();

        if (ResponseHeaders.Count != 0)
        {
            sb.AppendLine("Headers:");
            foreach (var header in ResponseHeaders)
            {
                sb.AppendLine($"  {header.Key}: {string.Join(", ", "header.Value")}");
            }
        }
        
        if (Content is not null)
        {
            sb.AppendLine($"Content:");
            sb.Append(Content);
        }

        return sb.ToString();
    }
}
