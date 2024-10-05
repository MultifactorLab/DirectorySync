using DirectorySync.Infrastructure.Http;

namespace DirectorySync.Exceptions;

[Serializable]
internal class PullCloudConfigException : Exception
{
    public HttpClientResponse? Response { get; }
        
    public PullCloudConfigException() { }
        
    public PullCloudConfigException(string message) : base(message) { }
        
    public PullCloudConfigException(string message, HttpClientResponse response) : base(message) {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
    public PullCloudConfigException(string message, Exception inner) : base(message, inner) { }
        
    public PullCloudConfigException(string message, HttpClientResponse response, Exception inner) : base(message, inner)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}
