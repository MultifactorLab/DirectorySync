using System.Security.Cryptography;
using System.Text;

namespace DirectorySync.Domain;

public record EntriesHash
{
    private readonly string _value;

    /// <summary>
    /// Creates EntriesHash from string representation.
    /// </summary>
    /// <param name="hash">Hash string.</param>
    /// <exception cref="ArgumentException"></exception>
    public EntriesHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(hash));
        }

        _value = hash;
    }
    
    /// <summary>
    /// Computes and returns EntriesHash from guids.
    /// </summary>
    /// <param name="guids">Guid collection.</param>
    public EntriesHash(IEnumerable<DirectoryGuid> guids)
    {
        ArgumentNullException.ThrowIfNull(guids);

        var joinedGuids = string.Join(';', guids.Select(x => (string)x));
        var bytes = Encoding.UTF8.GetBytes(joinedGuids);
        var hash = SHA256.HashData(bytes);

        _value = BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static implicit operator string(EntriesHash hash)
    {
        if (hash is null)
        {
            throw new InvalidCastException("Hash is null");
        }

        return hash._value;
    }

    public override string ToString() => $"{nameof(EntriesHash)} '{_value}'";
}
