using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Common.Interfaces;

namespace PharmacyAvailability.Infrastructure.Security;

public class DevelopmentVerificationSender : IVerificationSender
{
    private readonly ILogger<DevelopmentVerificationSender> _logger;

    public DevelopmentVerificationSender(ILogger<DevelopmentVerificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailCodeAsync(string email, string code, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n=======================================================\n" +
            "[EMAIL VERIFICATION] Sent to: {Email}\n" +
            "Verification Code: {Code}\n" +
            "Expires in: 10 minutes\n" +
            "=======================================================",
            email, code);

        return Task.CompletedTask;
    }

    public Task SendPhoneCodeAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n=======================================================\n" +
            "[SMS / PHONE VERIFICATION] Sent to: {PhoneNumber}\n" +
            "Verification Code: {Code}\n" +
            "Expires in: 10 minutes\n" +
            "=======================================================",
            phoneNumber, code);

        return Task.CompletedTask;
    }
}
