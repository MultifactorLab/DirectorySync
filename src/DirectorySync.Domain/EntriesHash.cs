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
    public static EntriesHash Create(IEnumerable<DirectoryGuid> guids)
    {
        ArgumentNullException.ThrowIfNull(guids);

        var ordered = guids.Select(x => (string)x).OrderDescending();
        var joinedGuids = string.Join(';', ordered);
        var bytes = Encoding.UTF8.GetBytes(joinedGuids);
        var hash = SHA256.HashData(bytes);

        var value = BitConverter.ToString(hash).Replace("-", string.Empty);
        return new(value);
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
