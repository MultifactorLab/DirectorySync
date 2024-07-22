﻿using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeleteUsersOperationResult
{
    ReadOnlyCollection<MultifactorIdentity> DeletedUsers { get; }
}
