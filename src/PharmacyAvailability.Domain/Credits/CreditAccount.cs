using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Credits;

/// <summary>One credit balance per user. PublicCode is what the user sends the operator to be topped up.</summary>
public class CreditAccount : BaseEntity
{
    public const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int CodeLength = 8;

    public Guid UserId { get; private set; }
    public string PublicCode { get; private set; } = string.Empty;
    public long Balance { get; private set; }
    public int FreeQueriesRemaining { get; private set; }

    protected CreditAccount() { }

    public CreditAccount(Guid userId, string publicCode, int freeQueries)
    {
        if (userId == Guid.Empty) throw new DomainValidationException("User is required.");
        if (!TryNormalizeCode(publicCode, out var normalized))
            throw new DomainValidationException("Invalid account code.");
        if (freeQueries < 0) throw new DomainValidationException("Free queries cannot be negative.");
        UserId = userId;
        PublicCode = normalized;
        FreeQueriesRemaining = freeQueries;
    }

    public static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            chars[i] = CodeAlphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return new string(chars);
    }

    /// <summary>Accepts "abcd-efgh", "ABCD EFGH" and similar. Returns the 8 character canonical form.</summary>
    public static bool TryNormalizeCode(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var cleaned = new string(input.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (cleaned.Length != CodeLength || cleaned.Any(c => !CodeAlphabet.Contains(c))) return false;
        normalized = cleaned;
        return true;
    }

    public static string FormatCode(string canonical) =>
        canonical.Length == CodeLength ? $"{canonical[..4]}-{canonical[4..]}" : canonical;
}
