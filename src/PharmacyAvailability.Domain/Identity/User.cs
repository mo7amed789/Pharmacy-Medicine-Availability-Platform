using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Identity;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public Guid? PharmacyId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Required by EF Core
    protected User() { }

    public User(
        string email,
        string passwordHash,
        string fullName,
        UserRole role,
        string? phoneNumber = null,
        Guid? pharmacyId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainValidationException("Password hash is required.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Full name is required.");

        if ((role == UserRole.PharmacyAdmin || role == UserRole.PharmacyStaff) && !pharmacyId.HasValue)
        {
            throw new DomainValidationException("Pharmacy staff/admin users must be associated with a valid PharmacyId.");
        }

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Role = role;
        PharmacyId = pharmacyId;
        IsActive = true;
    }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Full name cannot be empty.");

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainValidationException("Password hash cannot be empty.");

        PasswordHash = newPasswordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokeAllRefreshTokens();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAt)
    {
        var token = new RefreshToken(Id, tokenHash, expiresAt);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeAllRefreshTokens()
    {
        var activeTokens = _refreshTokens.Where(t => t.IsActive);
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }
}
