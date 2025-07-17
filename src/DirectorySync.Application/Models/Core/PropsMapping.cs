namespace DirectorySync.Application.Models.Core;

public class PropsMapping
{
    public string IdentityAttribute { get; set; } = string.Empty;
    public string NameAttribute { get; set; } = string.Empty;
    public string[] EmailAttributes { get; set; } = [];
    public string[] PhoneAttributes { get; set; } = [];
}
