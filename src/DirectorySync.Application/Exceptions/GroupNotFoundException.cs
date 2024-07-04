namespace DirectorySync.Application.Exceptions;

[Serializable]
public class GroupNotFoundException : Exception
{
    public GroupNotFoundException() { }
    public GroupNotFoundException(string message) : base(message) { }
    public GroupNotFoundException(string message, Exception inner) : base(message, inner) { }
}