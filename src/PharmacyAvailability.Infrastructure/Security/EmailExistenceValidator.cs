using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Common.Interfaces;

namespace PharmacyAvailability.Infrastructure.Security;

public class EmailExistenceValidator : IEmailExistenceValidator
{
    private readonly bool _isDevelopment;
    private readonly ILogger<EmailExistenceValidator> _logger;

    // List of popular disposable and temporary email domains
    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "tempmail.com", "temp-mail.org", "tempmail.net",
        "10minutemail.com", "10minutemail.net",
        "mailinator.com", "mailinator2.com", "mailinater.com",
        "guerrillamail.com", "guerrillamail.net", "guerrillamail.org",
        "sharklasers.com", "grr.la", "guerrillamailblock.com",
        "dispostable.com", "yopmail.com", "yopmail.fr", "yopmail.net",
        "trashmail.com", "trashmail.net", "trashmail.org",
        "throwawaymail.com", "burnermail.io", "fakeinbox.com",
        "getairmail.com", "maildrop.cc", "fakemailgenerator.com",
        "mohmal.com", "mohmal.im", "crazymailing.com", "nada.ltd"
    };

    // Major reputable mail providers that always exist
    private static readonly HashSet<string> KnownValidDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "hotmail.com", "live.com", "msn.com",
        "yahoo.com", "yahoo.co.uk", "yahoo.fr", "ymail.com",
        "icloud.com", "me.com", "mac.com",
        "proton.me", "protonmail.com",
        "zoho.com", "aol.com"
    };

    public EmailExistenceValidator(ILogger<EmailExistenceValidator> logger)
    {
        _logger = logger;
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        _isDevelopment = string.IsNullOrWhiteSpace(env) ||
                         env.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
                         env.Equals("Testing", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<EmailValidationResult> ValidateEmailExistenceAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new EmailValidationResult(false, "Email address cannot be empty.");
        }

        var parts = email.Trim().Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return new EmailValidationResult(false, "Invalid email address format.");
        }

        var localPart = parts[0];
        var domain = parts[1].ToLowerInvariant().Trim();

        // 1. Check for disposable / temporary email domains
        if (DisposableDomains.Contains(domain))
        {
            _logger.LogWarning("Email rejected: Disposable domain '{Domain}' detected.", domain);
            return new EmailValidationResult(
                false,
                "Temporary or disposable email domains are not allowed. Please provide a real, personal or business email address.");
        }

        // 2. Allow known major providers instantly without remote DNS overhead
        if (KnownValidDomains.Contains(domain))
        {
            return new EmailValidationResult(true);
        }

        // 3. Allow test/mock domains
        if (domain.EndsWith(".test") || domain.Equals("example.com") || domain.Equals("test.com") || domain.Equals("localhost"))
        {
            return new EmailValidationResult(true);
        }

        // 4. Perform live DNS resolution to verify the domain exists in real life
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, ct);
            if (addresses == null || addresses.Length == 0)
            {
                _logger.LogWarning("Email rejected: Domain '{Domain}' has no active DNS host records.", domain);
                return new EmailValidationResult(
                    false,
                    $"The email domain '{domain}' does not exist on the internet. Please provide a valid, active email address.");
            }

            return new EmailValidationResult(true);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Email rejected: Domain '{Domain}' could not be resolved via DNS.", domain);
            return new EmailValidationResult(
                false,
                $"The email domain '{domain}' does not exist on the internet. Please provide a valid, active email address.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS check encountered an unexpected error for domain '{Domain}'.", domain);
            if (_isDevelopment)
            {
                return new EmailValidationResult(true);
            }

            return new EmailValidationResult(
                false,
                $"Unable to verify that domain '{domain}' exists. Please verify your email address.");
        }
    }
}
