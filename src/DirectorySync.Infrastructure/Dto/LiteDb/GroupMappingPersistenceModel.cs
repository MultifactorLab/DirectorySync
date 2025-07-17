using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;

namespace DirectorySync.Infrastructure.Dto.LiteDb;

public class GroupMappingPersistenceModel
{
    public string DirectoryGroup { get; set; } = string.Empty;
    public string[] SignUpGroups { get; set; } = [];

    public static GroupMappingPersistenceModel FromDomainModel(GroupMapping model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new GroupMappingPersistenceModel { DirectoryGroup = model.DirectoryGroup, SignUpGroups = model.SignUpGroups };
    }

    public static GroupMapping ToDomainModel(GroupMappingPersistenceModel dbModel)
    {
        ArgumentNullException.ThrowIfNull(dbModel);
        
        return new GroupMapping { DirectoryGroup = dbModel.DirectoryGroup, SignUpGroups = dbModel.SignUpGroups };
    }
}
