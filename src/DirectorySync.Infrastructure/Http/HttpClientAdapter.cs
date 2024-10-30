using System.Text;
using System.Text.Json;

namespace DirectorySync.Infrastructure.Http;

public class HttpClientAdapter
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public HttpClientAdapter(HttpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<HttpClientResponse> GetAsync(string endpoint, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync(response);
    }

    public async Task<HttpClientResponse<T>> GetAsync<T>(string endpoint, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<HttpClientResponse> PostAsync(string endpoint, object? body = null, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync(response);
    }

    public async Task<HttpClientResponse<T>> PostAsync<T>(string endpoint, object? body = null, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<HttpClientResponse<T>> PutAsync<T>(string endpoint, object? body = null, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<HttpClientResponse<T>> PatchAsync<T>(string endpoint, object? body = null, Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<HttpClientResponse> DeleteAsync(string endpoint,
        object? body = null, 
        Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync(response);
    }

    public async Task<HttpClientResponse<T>> DeleteAsync<T>(string endpoint, 
        object? body = null, 
        Action<HttpRequestMessage>? configureRequest = null)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Content = body == null ? null : CreateJsonStringContent(body);
        configureRequest?.Invoke(request);

        using var response = await _client.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    private static async Task<HttpClientResponse> HandleResponseAsync(HttpResponseMessage response)
    {
        string? text = null;
        if (response.Content.Headers.ContentLength != 0)
        {
            text = await response.Content.ReadAsStringAsync();
        }
        return new HttpClientResponse(response.StatusCode, text, response.Headers.ToDictionary(k => k.Key, v => v.Value.ToArray()));
    }

    private async Task<HttpClientResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return new HttpClientResponse<T>(response.StatusCode, null, default, response.Headers.ToDictionary(k => k.Key, v => v.Value.ToArray()));
        }

        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            var text = await response.Content.ReadAsStringAsync();
            return new HttpClientResponse<T>(response.StatusCode, text, default, response.Headers.ToDictionary(k => k.Key, v => v.Value.ToArray()));
        }

        var (content, parsed) = await ParseJsonContentAsync<T>(response);
        return new HttpClientResponse<T>(response.StatusCode, content, parsed, response.Headers.ToDictionary(k => k.Key, v => v.Value.ToArray()));
    }

    private async Task<(string?, T?)> ParseJsonContentAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, default);
        }

        var dto = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        return (json, dto);
    }

    private StringContent CreateJsonStringContent(object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
