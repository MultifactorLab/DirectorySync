using DirectorySync.Application.Models.Options;
using LiteDB;

namespace DirectorySync.Infrastructure.Dto.LiteDb
{
    public class SyncSettingsPersistenceModel
    {
        public const string SyncSettingsId = "sync-settings";
        [BsonId]
        public string Id { get; private set; } = SyncSettingsId;
        
        public GroupMappingPersistenceModel[] GroupMapping { get; set; } = [];

        public static SyncSettingsPersistenceModel FromDomainModel(SyncSettings model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new SyncSettingsPersistenceModel()
            {
                GroupMapping = model.DirectoryGroupMappings
                    .Select(GroupMappingPersistenceModel.FromDomainModel)
                    .ToArray()
            };
        }

        public static SyncSettings ToDomainModel(SyncSettingsPersistenceModel dbModel)
        {
            ArgumentNullException.ThrowIfNull(dbModel);

            return new SyncSettings
            {
                DirectoryGroupMappings = dbModel.GroupMapping
                    .Select(GroupMappingPersistenceModel.ToDomainModel)
                    .ToArray()
            };
        }
    }
}
