namespace DirectorySync.Application.Exceptions;

[Serializable]
public class UserIdUndefinedException : Exception
{
    public UserIdUndefinedException() { }
    public UserIdUndefinedException(string message) : base(message) { }
    public UserIdUndefinedException(string message, Exception inner) : base(message, inner) { }
}
