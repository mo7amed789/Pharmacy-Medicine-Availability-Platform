using System.Text.RegularExpressions;
using FluentValidation;
using PharmacyAvailability.Contracts.Auth;

namespace PharmacyAvailability.Application.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{8,15}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .Matches(EmailRegex).WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(PhoneRegex).WithMessage("Phone number must be a valid international or local number (8 to 15 digits).")
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .Matches(EmailRegex).WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class VerifyCodeRequestValidator : AbstractValidator<VerifyCodeRequest>
{
    private static readonly Regex SixDigitsRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    public VerifyCodeRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Verification type is required.")
            .Must(t => string.Equals(t, "Email", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(t, "Phone", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Verification type must be either 'Email' or 'Phone'.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Matches(SixDigitsRegex).WithMessage("Verification code must be exactly 6 digits.");
    }
}

public class SendVerificationRequestValidator : AbstractValidator<SendVerificationRequest>
{
    public SendVerificationRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Verification type is required.")
            .Must(t => string.Equals(t, "Email", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(t, "Phone", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Verification type must be either 'Email' or 'Phone'.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class RevokeTokenRequestValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

/// <summary>Egyptian mobile numbers only: 01[0125] plus 8 digits, with optional +20 or 20 prefix.</summary>
public static class EgyptPhone
{
    private static readonly Regex Pattern = new(@"^(?:\+?20|0)?(1[0125][0-9]{8})$", RegexOptions.Compiled);

    public static bool TryNormalize(string? input, out string e164)
    {
        e164 = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var cleaned = new string(input.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '(' && c != ')').ToArray());
        var m = Pattern.Match(cleaned);
        if (!m.Success) return false;
        e164 = "+20" + m.Groups[1].Value;
        return true;
    }

    /// <summary>Internal login name for phone-first accounts. The .invalid TLD can never receive mail.</summary>
    public static string PlaceholderEmail(string e164) => e164.TrimStart('+') + "@phone.invalid";
}

public class PhoneRegisterRequestValidator : AbstractValidator<PhoneRegisterRequest>
{
    public PhoneRegisterRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(p => EgyptPhone.TryNormalize(p, out _)).WithMessage("Enter a valid Egyptian mobile number, for example 01012345678.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.");
        RuleFor(x => x.FullName)
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");
    }
}

public class PhoneLoginRequestValidator : AbstractValidator<PhoneLoginRequest>
{
    public PhoneLoginRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(p => EgyptPhone.TryNormalize(p, out _)).WithMessage("Enter a valid Egyptian mobile number, for example 01012345678.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
