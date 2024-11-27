using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Shared.Integrations.Ldap
{
    public class LdapOptions
    {
        [Required]
        public string Path { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Range(1, 5000)]
        public int PageSize { get; set; } = 500;
    }
}
