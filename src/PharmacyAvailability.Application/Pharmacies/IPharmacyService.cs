using PharmacyAvailability.Contracts.Pharmacies;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Application.Pharmacies;

public interface IPharmacyService
{
    // Public / Customer
    Task<PharmacyDto> RegisterPharmacyAsync(RegisterPharmacyRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PharmacyDto>> GetActivePharmaciesAsync(string? city = null, CancellationToken ct = default);
    Task<PharmacyDetailDto?> GetPharmacyDetailsAsync(Guid id, bool activeBranchesOnly = true, CancellationToken ct = default);
    Task<IReadOnlyList<PharmacyBranchDto>> GetActiveBranchesAsync(string? city = null, CancellationToken ct = default);
    Task<PharmacyBranchDto?> GetBranchByIdAsync(Guid id, CancellationToken ct = default);

    // Pharmacy Tenant Management (PharmacyAdmin / PharmacyStaff)
    Task<PharmacyDto> UpdatePharmacyProfileAsync(Guid pharmacyId, UpdatePharmacyProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PharmacyBranchDto>> GetBranchesForPharmacyAsync(Guid pharmacyId, CancellationToken ct = default);
    Task<PharmacyBranchDto> CreateBranchAsync(Guid pharmacyId, CreateBranchRequest request, CancellationToken ct = default);
    Task<PharmacyBranchDto> UpdateBranchAsync(Guid pharmacyId, Guid branchId, UpdateBranchRequest request, CancellationToken ct = default);
    Task<PharmacyBranchDto> UpdateBranchStatusAsync(Guid pharmacyId, Guid branchId, bool isActive, CancellationToken ct = default);
    Task<PharmacyStaffDto> AddStaffAsync(Guid pharmacyId, InviteStaffRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PharmacyStaffDto>> GetStaffAsync(Guid pharmacyId, CancellationToken ct = default);

    // PlatformAdmin Oversight & Approval Workflow
    Task<IReadOnlyList<PharmacyDto>> GetAllPharmaciesAsync(PharmacyStatus? status = null, CancellationToken ct = default);
    Task<PharmacyDto> ApprovePharmacyAsync(Guid id, CancellationToken ct = default);
    Task<PharmacyDto> SuspendPharmacyAsync(Guid id, CancellationToken ct = default);
    Task<PharmacyDto> ReactivatePharmacyAsync(Guid id, CancellationToken ct = default);
    Task<PharmacyDto> DeactivatePharmacyAsync(Guid id, CancellationToken ct = default);
}
