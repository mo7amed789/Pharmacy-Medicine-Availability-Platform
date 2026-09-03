namespace PharmacyAvailability.Contracts.Pharmacies;

public record PharmacyDto(
    Guid Id,
    string Name,
    string LicenseNumber,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    string Phone,
    string? Email,
    string? OpeningHours,
    string Status,
    int BranchesCount,
    DateTime CreatedAt);

public record PharmacyBranchDto(
    Guid Id,
    Guid PharmacyId,
    string PharmacyName,
    string Name,
    string? BranchCode,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    string Phone,
    string? OpeningHours,
    bool IsActive,
    DateTime CreatedAt);

public record PharmacyDetailDto(
    PharmacyDto Pharmacy,
    IReadOnlyList<PharmacyBranchDto> Branches);

public record RegisterPharmacyRequest(
    string Name,
    string LicenseNumber,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    string Phone,
    string? Email,
    string? OpeningHours,
    string AdminFullName,
    string AdminEmail,
    string AdminPassword,
    string? AdminPhone = null);

public record CreateBranchRequest(
    string Name,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    string Phone,
    string? OpeningHours = null,
    string? BranchCode = null,
    bool IsActive = true);

public record UpdateBranchRequest(
    string Name,
    string Address,
    string City,
    double Latitude,
    double Longitude,
    string Phone,
    string? OpeningHours = null,
    string? BranchCode = null);

public record UpdateBranchStatusRequest(
    bool IsActive);

public record UpdatePharmacyProfileRequest(
    string Name,
    string Address,
    string City,
    string Phone,
    string? Email,
    string? OpeningHours);

public record InviteStaffRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    string Role = "PharmacyStaff");

public record PharmacyStaffDto(
    Guid Id,
    Guid PharmacyId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt);
