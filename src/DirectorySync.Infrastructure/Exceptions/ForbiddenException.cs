namespace DirectorySync.Infrastructure.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("Access to the requested resource is forbidden.") { }

        public ForbiddenException(string message) : base(message) { }

        public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
    }
}
