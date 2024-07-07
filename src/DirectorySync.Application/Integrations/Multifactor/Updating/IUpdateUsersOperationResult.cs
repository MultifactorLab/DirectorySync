﻿using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Updating;

public interface IUpdateUsersOperationResult
{
    ReadOnlyCollection<MultifactorUserId> UpdatedUsers { get; }
}
