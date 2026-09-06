using FluentValidation.TestHelper;
using PharmacyAvailability.Application.Validation;
using PharmacyAvailability.Contracts.Auth;
using Xunit;

namespace PharmacyAvailability.UnitTests.Validation;

public class AuthValidatorsTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly VerifyCodeRequestValidator _verifyCodeValidator = new();
    private readonly SendVerificationRequestValidator _sendVerificationValidator = new();

    [Fact]
    public void RegisterRequest_ValidData_PassesValidation()
    {
        var model = new RegisterRequest("test@example.com", "Password123!", "Ahmed Ali", "+201012345678");
        var result = _registerValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    public void RegisterRequest_InvalidEmail_FailsValidation(string email)
    {
        var model = new RegisterRequest(email, "Password123!", "Ahmed Ali", "+201012345678");
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void RegisterRequest_InvalidPassword_FailsValidation(string password)
    {
        var model = new RegisterRequest("test@example.com", password, "Ahmed Ali", "+201012345678");
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")] // too short for phone
    public void RegisterRequest_InvalidPhone_FailsValidation(string phone)
    {
        var model = new RegisterRequest("test@example.com", "Password123!", "Ahmed Ali", phone);
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void LoginRequest_ValidData_PassesValidation()
    {
        var model = new LoginRequest("test@example.com", "Password123!");
        var result = _loginValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginRequest_EmptyFields_FailsValidation()
    {
        var model = new LoginRequest("", "");
        var result = _loginValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("Email", "123456", true)]
    [InlineData("Phone", "654321", true)]
    [InlineData("InvalidType", "123456", false)]
    [InlineData("Email", "12345", false)] // 5 digits
    [InlineData("Email", "1234567", false)] // 7 digits
    [InlineData("Email", "abcdef", false)] // non-digits
    public void VerifyCodeRequest_ValidationScenarios(string type, string code, bool isValid)
    {
        var model = new VerifyCodeRequest(type, code);
        var result = _verifyCodeValidator.TestValidate(model);

        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Email", true)]
    [InlineData("Phone", true)]
    [InlineData("SMS", false)]
    [InlineData("", false)]
    public void SendVerificationRequest_ValidationScenarios(string type, bool isValid)
    {
        var model = new SendVerificationRequest(type);
        var result = _sendVerificationValidator.TestValidate(model);

        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}
