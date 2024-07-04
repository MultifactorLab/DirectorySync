using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

public class LdapOptions
{
    [Required]
    public required string Name { get; set; }
    
    public string? Container { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }
}
