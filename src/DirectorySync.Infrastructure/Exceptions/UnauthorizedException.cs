namespace DirectorySync.Infrastructure.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base("Unauthorized: Please provide valid credentials.") { }

        public UnauthorizedException(string message) : base(message) { }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
