using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Options;
using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Adapters.Options
{
    public class SyncSettingsOptions : ISyncSettingsOptions
    {
        private readonly object _locker = new();
        private string[] _requiredLdapAttributes = [];
        
        public SyncSettings Current => _monitor.CurrentValue;
        private readonly IOptionsMonitor<SyncSettings> _monitor;

        public SyncSettingsOptions(IOptionsMonitor<SyncSettings> monitor)
        {
            _monitor = monitor;
            _requiredLdapAttributes = Map(monitor.CurrentValue.PropertyMapping).ToArray();
            monitor.OnChange(x => 
            { 
                lock (_locker)
                {
                    _requiredLdapAttributes = Map(x.PropertyMapping).ToArray();
                }
            });
        }
        
        public string[] GetRequiredAttributeNames()
        {
            lock (_locker)
            {
                return _requiredLdapAttributes;
            }
        }
        
        private IEnumerable<string> Map(PropsMapping options)
        {
            yield return options.IdentityAttribute;

            if (!string.IsNullOrWhiteSpace(options.NameAttribute))
            {
                yield return options.NameAttribute;
            }

            foreach (var emailAttrName in options.EmailAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                yield return emailAttrName;
            }

            foreach (var phoneAttrName in options.PhoneAttributes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                yield return phoneAttrName;
            }
        }
    }
}
