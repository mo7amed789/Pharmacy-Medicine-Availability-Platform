namespace PharmacyAvailability.Application.Common.Interfaces;

public interface IEmailExistenceValidator
{
    Task<EmailValidationResult> ValidateEmailExistenceAsync(string email, CancellationToken ct = default);
}

public record EmailValidationResult(
    bool IsValid,
    string? ErrorMessage = null);
