namespace DirectorySync.Application.Exceptions;

[Serializable]
public class IdentityAttributeNotDefinedException : Exception
{
    public IdentityAttributeNotDefinedException() { }
    public IdentityAttributeNotDefinedException(string message) : base(message) { }
    public IdentityAttributeNotDefinedException(string message, Exception inner) : base(message, inner) { }
}
