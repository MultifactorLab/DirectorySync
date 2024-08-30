using DirectorySync.Infrastructure.Http;

namespace DirectorySync.Exceptions;

[Serializable]
internal class PullMultifactorSettingsException : Exception
{
    public HttpClientResponse? Response { get; }
        
    public PullMultifactorSettingsException() { }
        
    public PullMultifactorSettingsException(string message) : base(message) { }
        
    public PullMultifactorSettingsException(string message, HttpClientResponse response) : base(message) {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
    public PullMultifactorSettingsException(string message, Exception inner) : base(message, inner) { }
        
    public PullMultifactorSettingsException(string message, HttpClientResponse response, Exception inner) : base(message, inner)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}
