using Microsoft.Extensions.Options;

namespace DirectorySync.Infrastructure.Configurations;

internal sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var fileName = options.LiteDbFileName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(fileName))
        {
            return ValidateOptionsResult.Fail("Storage:LiteDbFileName must be non-empty.");
        }

        // Reject both separators so Windows-style config values fail validation on Linux too.
        if (fileName.Contains('/') || fileName.Contains('\\'))
        {
            return ValidateOptionsResult.Fail("Storage:LiteDbFileName must be a file name only (no directory separators).");
        }

        return ValidateOptionsResult.Success;
    }
}
