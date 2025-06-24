using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Infrastructure.Adapters.LiteDb.Configuration;
using DirectorySync.Infrastructure.Dto.LiteDb;
using LiteDB;

namespace DirectorySync.Infrastructure.Adapters.LiteDb;

public class SyncSettingsLiteDb : ISyncSettingsDatabase
{
    private readonly ILiteCollection<SyncSettingsPersistenceModel> _collection;
    
    public SyncSettingsLiteDb(ILiteDbConnection connection)
    {
        _collection = connection.Database.GetCollection<SyncSettingsPersistenceModel>();
    }
    
    public SyncSettings? GetSyncSettings()
    {
        var settings = _collection.FindById(SyncSettingsPersistenceModel.SyncSettingsId);

        if (settings is null)
        {
            return null;
        }
        
        return  SyncSettingsPersistenceModel.ToDomainModel(settings);
    }

    public void SaveSettings(SyncSettings syncSettings)
    {
        ArgumentNullException.ThrowIfNull(syncSettings);
        
        var dbSetting = SyncSettingsPersistenceModel.FromDomainModel(syncSettings);
        
        _collection.Upsert(dbSetting);
    }
}
