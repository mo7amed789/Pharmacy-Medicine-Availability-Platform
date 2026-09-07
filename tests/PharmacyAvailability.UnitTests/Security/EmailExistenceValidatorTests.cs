using Microsoft.Extensions.Logging.Abstractions;
using PharmacyAvailability.Infrastructure.Security;
using Xunit;

namespace PharmacyAvailability.UnitTests.Security;

public class EmailExistenceValidatorTests
{
    private readonly EmailExistenceValidator _validator;

    public EmailExistenceValidatorTests()
    {
        _validator = new EmailExistenceValidator(NullLogger<EmailExistenceValidator>.Instance);
    }

    [Theory]
    [InlineData("patient@gmail.com")]
    [InlineData("doctor@outlook.com")]
    [InlineData("pharmacist@yahoo.com")]
    [InlineData("admin@hotmail.com")]
    [InlineData("user@icloud.com")]
    public async Task ValidateEmailExistence_KnownMajorDomains_ReturnsValid(string email)
    {
        var result = await _validator.ValidateEmailExistenceAsync(email);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("spammer@mailinator.com")]
    [InlineData("fake@tempmail.com")]
    [InlineData("test@10minutemail.com")]
    [InlineData("bot@guerrillamail.com")]
    [InlineData("temp@sharklasers.com")]
    [InlineData("trash@yopmail.com")]
    public async Task ValidateEmailExistence_DisposableDomains_ReturnsInvalid(string email)
    {
        var result = await _validator.ValidateEmailExistenceAsync(email);
        Assert.False(result.IsValid);
        Assert.Contains("disposable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("patient@test.com")]
    [InlineData("user@example.com")]
    [InlineData("test@local.test")]
    public async Task ValidateEmailExistence_TestDomains_ReturnsValid(string email)
    {
        var result = await _validator.ValidateEmailExistenceAsync(email);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-format")]
    [InlineData("noat.com")]
    [InlineData("@nodomain")]
    public async Task ValidateEmailExistence_InvalidFormat_ReturnsInvalid(string email)
    {
        var result = await _validator.ValidateEmailExistenceAsync(email);
        Assert.False(result.IsValid);
    }
}
