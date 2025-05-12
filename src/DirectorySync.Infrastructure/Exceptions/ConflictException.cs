namespace DirectorySync.Infrastructure.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException() : base("A conflict occurred while processing the request.") { }

        public ConflictException(string message) : base(message) { }

        public ConflictException(string message, Exception innerException) : base(message, innerException) { }
    }
}
