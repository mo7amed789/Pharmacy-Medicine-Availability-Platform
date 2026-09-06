using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Identity;
using Xunit;

namespace PharmacyAvailability.UnitTests.Domain;

public class VerificationCodeTests
{
    [Fact]
    public void CreateVerificationCode_ValidParameters_Succeeds()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var code = new VerificationCode(userId, "123456", VerificationType.Email, expiresAt);

        Assert.Equal(userId, code.UserId);
        Assert.Equal("123456", code.Code);
        Assert.Equal(VerificationType.Email, code.Type);
        Assert.Equal(expiresAt, code.ExpiresAt);
        Assert.False(code.IsUsed);
        Assert.Equal(0, code.AttemptCount);
        Assert.True(code.IsValid);
        Assert.False(code.IsExpired);
    }

    [Fact]
    public void CreateVerificationCode_EmptyCode_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new VerificationCode(Guid.NewGuid(), "", VerificationType.Email, DateTime.UtcNow.AddMinutes(10)));
    }

    [Fact]
    public void CreateVerificationCode_PastExpiration_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new VerificationCode(Guid.NewGuid(), "123456", VerificationType.Phone, DateTime.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void Verify_CorrectCode_ReturnsTrueAndMarksUsed()
    {
        var code = new VerificationCode(Guid.NewGuid(), "654321", VerificationType.Phone, DateTime.UtcNow.AddMinutes(10));

        var result = code.Verify("654321");

        Assert.True(result);
        Assert.True(code.IsUsed);
        Assert.Equal(1, code.AttemptCount);
        Assert.False(code.IsValid);
    }

    [Fact]
    public void Verify_IncorrectCode_ReturnsFalseAndIncrementsAttempts()
    {
        var code = new VerificationCode(Guid.NewGuid(), "654321", VerificationType.Phone, DateTime.UtcNow.AddMinutes(10));

        var result = code.Verify("000000");

        Assert.False(result);
        Assert.False(code.IsUsed);
        Assert.Equal(1, code.AttemptCount);
        Assert.True(code.IsValid);
    }

    [Fact]
    public void Verify_MaxAttemptsExceeded_ThrowsDomainValidationException()
    {
        var code = new VerificationCode(Guid.NewGuid(), "654321", VerificationType.Phone, DateTime.UtcNow.AddMinutes(10));

        for (int i = 0; i < VerificationCode.MaxAttempts; i++)
        {
            code.Verify("000000");
        }

        Assert.Throws<DomainValidationException>(() => code.Verify("654321"));
    }
}
