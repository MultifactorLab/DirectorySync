using DirectorySync.Application.Models.ValueObjects;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace DirectorySync.Infrastructure.Adapters.Ldap.Abstractions
{
    public interface IDomainDiscoveryStrategy
    {
        public List<LdapDomain> FindForestDomains(ILdapConnection connection, ILdapSchema schema);
        public List<LdapDomain> FindForestTrusts(ILdapConnection connection, ILdapSchema schema);
    }
}
