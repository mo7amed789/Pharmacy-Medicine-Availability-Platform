using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Identity;

public class VerificationCode : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public VerificationType Type { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public int AttemptCount { get; private set; }
    public const int MaxAttempts = 5;

    // Required by EF Core
    protected VerificationCode() { }

    public VerificationCode(Guid userId, string code, VerificationType type, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainValidationException("UserId is required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainValidationException("Verification code is required.");
        if (expiresAt <= DateTime.UtcNow)
            throw new DomainValidationException("Expiration date must be in the future.");

        UserId = userId;
        Code = code.Trim();
        Type = type;
        ExpiresAt = expiresAt;
        IsUsed = false;
        AttemptCount = 0;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired && AttemptCount < MaxAttempts;

    public bool Verify(string inputCode)
    {
        if (IsUsed)
            throw new DomainValidationException("This verification code has already been used.");
        if (IsExpired)
            throw new DomainValidationException("This verification code has expired.");
        if (AttemptCount >= MaxAttempts)
            throw new DomainValidationException("Maximum verification attempts exceeded. Please request a new code.");

        AttemptCount++;

        if (string.Equals(Code, inputCode?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            IsUsed = true;
            return true;
        }

        return false;
    }

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
