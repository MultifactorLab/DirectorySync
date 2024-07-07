using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor;

public record CreatedUser(string Identity, MultifactorUserId Id);
