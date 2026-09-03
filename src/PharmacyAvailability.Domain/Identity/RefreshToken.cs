using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Identity;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Required by EF Core
    protected RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainValidationException("UserId is required.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainValidationException("TokenHash is required.");
        if (expiresAt <= DateTime.UtcNow)
            throw new DomainValidationException("Expiration date must be in the future.");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
