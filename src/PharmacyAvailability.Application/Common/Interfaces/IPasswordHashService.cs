using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Application.Common.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password, string passwordHash);
}
