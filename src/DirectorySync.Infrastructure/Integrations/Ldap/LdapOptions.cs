using DirectorySync.Infrastructure.Integrations.Ldap;
using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

public class LdapOptions
{
    [Required]
    public required string Path { get; set; }
    
    public string? SearchBase { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }

    [Range(1, 5000)]
    public int PageSize { get; set; } = 500;
}
