namespace PharmacyAvailability.Application.Common.Interfaces;

public interface IVerificationSender
{
    Task SendEmailCodeAsync(string email, string code, CancellationToken ct = default);
    Task SendPhoneCodeAsync(string phoneNumber, string code, CancellationToken ct = default);
}
