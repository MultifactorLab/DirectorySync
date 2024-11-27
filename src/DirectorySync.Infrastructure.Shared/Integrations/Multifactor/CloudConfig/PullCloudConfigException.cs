using DirectorySync.Infrastructure.Shared.Http;
using System;

namespace DirectorySync.Infrastructure.Shared.Integrations.Multifactor.CloudConfig
{
    public class PullCloudConfigException : Exception
    {
        public HttpClientResponse Response { get; }

        public PullCloudConfigException() { }

        public PullCloudConfigException(string message) : base(message) { }

        public PullCloudConfigException(string message, HttpClientResponse response) : base(message)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }
        public PullCloudConfigException(string message, Exception inner) : base(message, inner) { }
    }
}
