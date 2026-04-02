namespace DirectorySync.Application.Models.ValueObjects;

/// <summary>
/// Неизменяемый идентификатор объекта в каталоге (например AD <c>objectGUID</c>), передаётся в облако для сопоставления записей при смене логина.
/// </summary>
public static class ExternalDirectoryObjectId
{
    /// <summary>
    /// Каноническое строковое представление GUID для API (формат "D", нижний регистр не требуется — GUID стандартный).
    /// </summary>
    public static string ToCanonicalString(DirectoryGuid directoryGuid)
    {
        ArgumentNullException.ThrowIfNull(directoryGuid);
        return directoryGuid.Value.ToString("D");
    }
}
