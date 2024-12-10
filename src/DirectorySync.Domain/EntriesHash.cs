using System.Security.Cryptography;
using System.Text;

namespace DirectorySync.Domain;

public class EntriesHash : ValueObject
{
    public string Value { get; }

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

        Value = hash;
    }
    
    /// <summary>
    /// Computes and returns EntriesHash from guids.
    /// </summary>
    /// <param name="guids">Guid collection.</param>
    public static EntriesHash Create(IEnumerable<DirectoryGuid> guids)
    {
        ArgumentNullException.ThrowIfNull(guids);

        var ordered = guids.Select(x => x.Value).OrderDescending();
        var joinedGuids = string.Join(';', ordered);
        var bytes = Encoding.UTF8.GetBytes(joinedGuids);
        var hash = SHA256.HashData(bytes);

        var value = BitConverter.ToString(hash).Replace("-", string.Empty);
        return new(value);
    }

    public override string ToString() => $"{nameof(EntriesHash)} '{Value}'";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
