using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Application.Pharmacies;
using PharmacyAvailability.Contracts.Pharmacies;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Pharmacies;

public class PharmacyService : IPharmacyService
{
    private readonly PharmacyDbContext _context;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ILogger<PharmacyService> _logger;

    public PharmacyService(
        PharmacyDbContext context,
        IPasswordHashService passwordHasher,
        ILogger<PharmacyService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PharmacyDto> RegisterPharmacyAsync(RegisterPharmacyRequest request, CancellationToken ct = default)
    {
        var normLicense = request.LicenseNumber.Trim().ToUpperInvariant();
        var normEmail = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _context.Pharmacies.AnyAsync(p => p.LicenseNumber == normLicense, ct))
        {
            throw new DomainValidationException($"License number '{normLicense}' is already registered.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == normEmail, ct))
        {
            throw new DomainValidationException($"Email '{normEmail}' is already registered.");
        }

        var coordinates = new Coordinates(request.Latitude, request.Longitude);

        // New registrations always start as Pending for PlatformAdmin verification
        var pharmacy = new Pharmacy(
            request.Name,
            normLicense,
            request.Address,
            request.City,
            coordinates,
            request.Phone,
            request.Email,
            request.OpeningHours,
            initialStatus: PharmacyStatus.Pending);

        // Add initial main branch
        pharmacy.AddBranch(
            name: $"{request.Name} - Main Branch",
            address: request.Address,
            city: request.City,
            coordinates: coordinates,
            phone: request.Phone,
            openingHours: request.OpeningHours,
            branchCode: "MAIN",
            isActive: true);

        _context.Pharmacies.Add(pharmacy);

        // Create initial pharmacy administrator user
        var adminPhone = string.IsNullOrWhiteSpace(request.AdminPhone) ? request.Phone : request.AdminPhone;
        var dummyAdmin = new User(normEmail, "temp", request.AdminFullName, UserRole.PharmacyAdmin, adminPhone, pharmacy.Id);
        var passwordHash = _passwordHasher.HashPassword(dummyAdmin, request.AdminPassword);
        var adminUser = new User(
            normEmail,
            passwordHash,
            request.AdminFullName,
            UserRole.PharmacyAdmin,
            adminPhone,
            pharmacy.Id);
        adminUser.SetVerificationStatus(true, true);

        _context.Users.Add(adminUser);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pharmacy registered successfully: {PharmacyId} ({Name}) - Status: Pending",
            pharmacy.Id, pharmacy.Name);

        return MapToPharmacyDto(pharmacy, 1);
    }

    public async Task<IReadOnlyList<PharmacyDto>> GetActivePharmaciesAsync(string? city = null, CancellationToken ct = default)
    {
        var query = _context.Pharmacies
            .AsNoTracking()
            .Where(p => p.Status == PharmacyStatus.Active);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim();
            query = query.Where(p => p.City == normalizedCity);
        }

        var pharmacies = await query
            .Select(p => new
            {
                Pharmacy = p,
                BranchesCount = p.Branches.Count(b => b.IsActive)
            })
            .OrderBy(x => x.Pharmacy.Name)
            .ToListAsync(ct);

        return pharmacies.Select(x => MapToPharmacyDto(x.Pharmacy, x.BranchesCount)).ToList();
    }

    public async Task<PharmacyDetailDto?> GetPharmacyDetailsAsync(Guid id, bool activeBranchesOnly = true, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .AsNoTracking()
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (pharmacy == null)
            return null;

        // If queried publicly, pending or suspended pharmacies are hidden
        if (activeBranchesOnly && pharmacy.Status != PharmacyStatus.Active)
            return null;

        var branches = pharmacy.Branches
            .Where(b => !activeBranchesOnly || b.IsActive)
            .Select(b => MapToBranchDto(b, pharmacy.Name))
            .ToList();

        return new PharmacyDetailDto(
            MapToPharmacyDto(pharmacy, branches.Count),
            branches);
    }

    public async Task<IReadOnlyList<PharmacyBranchDto>> GetActiveBranchesAsync(string? city = null, CancellationToken ct = default)
    {
        var query = _context.PharmacyBranches
            .AsNoTracking()
            .Include(b => b.Pharmacy)
            .Where(b => b.IsActive && b.Pharmacy!.Status == PharmacyStatus.Active);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim();
            query = query.Where(b => b.City == normalizedCity);
        }

        var branches = await query
            .OrderBy(b => b.Pharmacy!.Name)
            .ThenBy(b => b.Name)
            .ToListAsync(ct);

        return branches.Select(b => MapToBranchDto(b, b.Pharmacy!.Name)).ToList();
    }

    public async Task<PharmacyBranchDto?> GetBranchByIdAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await _context.PharmacyBranches
            .AsNoTracking()
            .Include(b => b.Pharmacy)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (branch == null || !branch.IsActive || branch.Pharmacy?.Status != PharmacyStatus.Active)
            return null;

        return MapToBranchDto(branch, branch.Pharmacy.Name);
    }

    public async Task<PharmacyDto> UpdatePharmacyProfileAsync(Guid pharmacyId, UpdatePharmacyProfileRequest request, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{pharmacyId}' was not found.");

        pharmacy.UpdateProfile(
            request.Name,
            request.Address,
            request.City,
            request.Phone,
            request.Email,
            request.OpeningHours);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Pharmacy profile updated: {PharmacyId}", pharmacyId);

        return MapToPharmacyDto(pharmacy, pharmacy.Branches.Count);
    }

    public async Task<IReadOnlyList<PharmacyBranchDto>> GetBranchesForPharmacyAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies.FindAsync(new object[] { pharmacyId }, ct);
        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{pharmacyId}' was not found.");

        var branches = await _context.PharmacyBranches
            .AsNoTracking()
            .Where(b => b.PharmacyId == pharmacyId)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

        return branches.Select(b => MapToBranchDto(b, pharmacy.Name)).ToList();
    }

    public async Task<PharmacyBranchDto> CreateBranchAsync(Guid pharmacyId, CreateBranchRequest request, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{pharmacyId}' was not found.");

        var branch = pharmacy.AddBranch(
            request.Name,
            request.Address,
            request.City,
            new Coordinates(request.Latitude, request.Longitude),
            request.Phone,
            request.OpeningHours,
            request.BranchCode,
            request.IsActive);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Branch created: {BranchId} under pharmacy {PharmacyId}", branch.Id, pharmacyId);

        return MapToBranchDto(branch, pharmacy.Name);
    }

    public async Task<PharmacyBranchDto> UpdateBranchAsync(
        Guid pharmacyId,
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken ct = default)
    {
        var branch = await _context.PharmacyBranches
            .Include(b => b.Pharmacy)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.PharmacyId == pharmacyId, ct);

        if (branch == null)
            throw new KeyNotFoundException($"Branch with ID '{branchId}' was not found for this pharmacy.");

        branch.UpdateDetails(
            request.Name,
            request.Address,
            request.City,
            request.Phone,
            request.OpeningHours,
            request.BranchCode);

        branch.UpdateLocation(new Coordinates(request.Latitude, request.Longitude));

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Branch updated: {BranchId}", branchId);

        return MapToBranchDto(branch, branch.Pharmacy!.Name);
    }

    public async Task<PharmacyBranchDto> UpdateBranchStatusAsync(
        Guid pharmacyId,
        Guid branchId,
        bool isActive,
        CancellationToken ct = default)
    {
        var branch = await _context.PharmacyBranches
            .Include(b => b.Pharmacy)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.PharmacyId == pharmacyId, ct);

        if (branch == null)
            throw new KeyNotFoundException($"Branch with ID '{branchId}' was not found for this pharmacy.");

        if (isActive)
            branch.Activate();
        else
            branch.Deactivate();

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Branch status toggled: {BranchId} - IsActive: {IsActive}", branchId, isActive);

        return MapToBranchDto(branch, branch.Pharmacy!.Name);
    }

    public async Task<PharmacyStaffDto> AddStaffAsync(Guid pharmacyId, InviteStaffRequest request, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies.FindAsync(new object[] { pharmacyId }, ct);
        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{pharmacyId}' was not found.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role) ||
            (role != UserRole.PharmacyStaff && role != UserRole.PharmacyAdmin))
        {
            throw new DomainValidationException($"Staff role '{request.Role}' must be PharmacyStaff or PharmacyAdmin.");
        }

        var normEmail = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == normEmail, ct))
            throw new DomainValidationException($"Email '{normEmail}' is already registered.");

        var staffPhone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber : pharmacy.Phone;
        var dummyUser = new User(normEmail, "temp", request.FullName, role, staffPhone, pharmacyId);
        var passwordHash = _passwordHasher.HashPassword(dummyUser, request.Password);

        var user = new User(
            normEmail,
            passwordHash,
            request.FullName,
            role,
            staffPhone,
            pharmacyId);
        user.SetVerificationStatus(true, true);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Staff user {UserId} created for pharmacy {PharmacyId} with role {Role}",
            user.Id, pharmacyId, user.Role);

        return new PharmacyStaffDto(
            user.Id,
            pharmacyId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt);
    }

    public async Task<IReadOnlyList<PharmacyStaffDto>> GetStaffAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.PharmacyId == pharmacyId)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(u => new PharmacyStaffDto(
            u.Id,
            pharmacyId,
            u.Email,
            u.FullName,
            u.PhoneNumber,
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<PharmacyDto>> GetAllPharmaciesAsync(PharmacyStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.Pharmacies.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var pharmacies = await query
            .Select(p => new
            {
                Pharmacy = p,
                BranchesCount = p.Branches.Count
            })
            .OrderBy(x => x.Pharmacy.Name)
            .ToListAsync(ct);

        return pharmacies.Select(x => MapToPharmacyDto(x.Pharmacy, x.BranchesCount)).ToList();
    }

    public async Task<PharmacyDto> ApprovePharmacyAsync(Guid id, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{id}' was not found.");

        pharmacy.Approve();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pharmacy {PharmacyId} approved by PlatformAdmin", id);
        return MapToPharmacyDto(pharmacy, pharmacy.Branches.Count);
    }

    public async Task<PharmacyDto> SuspendPharmacyAsync(Guid id, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{id}' was not found.");

        pharmacy.Suspend();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pharmacy {PharmacyId} suspended by PlatformAdmin", id);
        return MapToPharmacyDto(pharmacy, pharmacy.Branches.Count);
    }

    public async Task<PharmacyDto> ReactivatePharmacyAsync(Guid id, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{id}' was not found.");

        pharmacy.Reactivate();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pharmacy {PharmacyId} reactivated by PlatformAdmin", id);
        return MapToPharmacyDto(pharmacy, pharmacy.Branches.Count);
    }

    public async Task<PharmacyDto> DeactivatePharmacyAsync(Guid id, CancellationToken ct = default)
    {
        var pharmacy = await _context.Pharmacies
            .Include(p => p.Branches)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{id}' was not found.");

        pharmacy.Deactivate();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Pharmacy {PharmacyId} deactivated by PlatformAdmin", id);
        return MapToPharmacyDto(pharmacy, pharmacy.Branches.Count);
    }

    private static PharmacyDto MapToPharmacyDto(Pharmacy p, int branchesCount) =>
        new(
            p.Id,
            p.Name,
            p.LicenseNumber,
            p.Address,
            p.City,
            p.Latitude,
            p.Longitude,
            p.Phone,
            p.Email,
            p.OpeningHours,
            p.Status.ToString(),
            branchesCount,
            p.CreatedAt);

    private static PharmacyBranchDto MapToBranchDto(PharmacyBranch b, string pharmacyName) =>
        new(
            b.Id,
            b.PharmacyId,
            pharmacyName,
            b.Name,
            b.BranchCode,
            b.Address,
            b.City,
            b.Latitude,
            b.Longitude,
            b.Phone,
            b.OpeningHours,
            b.IsActive,
            b.CreatedAt);
}
