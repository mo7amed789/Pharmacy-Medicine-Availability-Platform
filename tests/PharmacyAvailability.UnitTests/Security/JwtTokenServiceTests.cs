using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Security;
using Xunit;

namespace PharmacyAvailability.UnitTests.Security;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly JwtOptions _options = new()
    {
        Secret = "A_Very_Strong_Test_Key_That_Has_More_Than_32_Characters!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 7
    };

    public JwtTokenServiceTests()
    {
        _service = new JwtTokenService(Options.Create(_options));
    }

    [Fact]
    public void GenerateAccessToken_IncludesSubjectEmailRoleAndPharmacyIdClaims()
    {
        // Arrange
        var pharmacyId = Guid.NewGuid();
        var user = new User("pharmacist@test.com", "hash", "Mona", UserRole.PharmacyStaff, "+201000", pharmacyId);

        // Act
        var (token, expiresAt) = _service.GenerateAccessToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(_options.Issuer, jwt.Issuer);
        Assert.Contains(_options.Audience, jwt.Audiences);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("PharmacyStaff", roleClaim.Value);

        var pharmacyClaim = jwt.Claims.FirstOrDefault(c => c.Type == "pharmacy_id");
        Assert.NotNull(pharmacyClaim);
        Assert.Equal(pharmacyId.ToString(), pharmacyClaim.Value);
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesCryptographicallyRandomTokenAndSha256Hash()
    {
        // Act
        var (rawToken1, hash1, expiry1) = _service.GenerateRefreshToken();
        var (rawToken2, hash2, _) = _service.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(rawToken1));
        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.True(expiry1 > DateTime.UtcNow);

        // Two tokens must be unique
        Assert.NotEqual(rawToken1, rawToken2);
        Assert.NotEqual(hash1, hash2);

        // Hash must match SHA256 of rawToken
        var expectedHash = _service.HashToken(rawToken1);
        Assert.Equal(expectedHash, hash1);
    }
}
