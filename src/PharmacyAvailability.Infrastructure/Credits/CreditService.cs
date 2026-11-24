using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Application.Credits;
using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Contracts.Credits;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Credits;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Credits;

public class CreditService : ICreditService
{
    private readonly PharmacyDbContext _db;
    private readonly CreditOptions _options;

    public CreditService(PharmacyDbContext db, IOptions<CreditOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<MyCreditDto> GetMyAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await GetOrCreateAccountAsync(userId, ct);
        return new MyCreditDto(CreditAccount.FormatCode(account.PublicCode), account.Balance,
            account.FreeQueriesRemaining, _options.ChargingEnabled);
    }

    public async Task<MeteredSearchOutcome> SearchMeteredAsync(
        Guid? userId,
        Func<CancellationToken, Task<AvailabilitySearchResult>> search,
        CancellationToken ct = default)
    {
        CreditAccount? account = null;

        if (_options.ChargingEnabled)
        {
            if (userId is null)
                throw new UnauthorizedAccessException("Sign in to search. Searches use your account balance.");

            var user = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.IsPhoneVerified }).FirstOrDefaultAsync(ct)
                ?? throw new UnauthorizedAccessException("Account not found.");
            if (!user.IsPhoneVerified)
                throw new ForbiddenAccessException("Verify your phone number before searching.");

            account = await GetOrCreateAccountAsync(userId.Value, ct);
            if (account.FreeQueriesRemaining <= 0 && account.Balance < _options.PointsPerQuery)
                throw new InsufficientCreditException("Not enough balance. Send your account code to top up.");
        }

        if (userId is not null && _options.DailyQueryCapPerUser > 0)
        {
            var dayStart = DateTime.UtcNow.Date;
            var today = await _db.QueryUsageLogs.CountAsync(l => l.UserId == userId && l.CreatedAt >= dayStart, ct);
            if (today >= _options.DailyQueryCapPerUser)
                throw new QueryLimitExceededException("Daily search limit reached. Try again tomorrow.");
        }

        var result = await search(ct);
        var outcome = Classify(result);

        var charged = 0;
        var usedFree = false;

        if (_options.ChargingEnabled && outcome == QueryOutcome.Success && account is not null)
        {
            (charged, usedFree) = await ChargeAsync(account.Id, ct);
        }

        _db.QueryUsageLogs.Add(new QueryUsageLog(userId, outcome, result.Items.Count, charged, usedFree));
        await _db.SaveChangesAsync(ct);

        return new MeteredSearchOutcome(result, charged);
    }

    public async Task<AdminCreditAccountDto> LookupAccountAsync(string publicCode, CancellationToken ct = default)
    {
        var account = await FindByCodeAsync(publicCode, ct);
        return await BuildAdminDtoAsync(account, ct);
    }

    public async Task<AdminCreditAccountDto> TopUpAsync(Guid adminUserId, CreditTopUpRequest request, CancellationToken ct = default)
    {
        if (request.Points < 1 || request.Points > _options.MaxTopUpPoints)
            throw new DomainValidationException($"Points must be between 1 and {_options.MaxTopUpPoints}.");

        var reference = NormalizeReference(request.TransferReference);
        var account = await FindByCodeAsync(request.PublicCode, ct);

        if (await _db.CreditLedgerEntries.AnyAsync(e => e.TransferReference == reference, ct))
            throw new ResourceConflictException("This transfer reference was already credited.");

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note is { Length: > 500 }) throw new DomainValidationException("Note is too long.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.CreditAccounts.Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance + request.Points), ct);
            var newBalance = await _db.CreditAccounts.AsNoTracking().Where(a => a.Id == account.Id).Select(a => a.Balance).SingleAsync(ct);

            _db.CreditLedgerEntries.Add(new CreditLedgerEntry(account.Id, CreditEntryType.TopUp, request.Points,
                newBalance, reference, adminUserId, note));
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A unique-index violation means another request credited the same transfer reference first.
            _db.ChangeTracker.Clear();
            if (await _db.CreditLedgerEntries.AsNoTracking().AnyAsync(e => e.TransferReference == reference, CancellationToken.None))
                throw new ResourceConflictException("This transfer reference was already credited.");
            throw;
        }

        _db.ChangeTracker.Clear();
        var fresh = await FindByCodeAsync(request.PublicCode, ct);
        return await BuildAdminDtoAsync(fresh, ct);
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        if (toUtc <= fromUtc) throw new DomainValidationException("The end of the range must be after the start.");
        if (toUtc - fromUtc > TimeSpan.FromDays(366)) throw new DomainValidationException("The range is too long.");

        var q = _db.QueryUsageLogs.AsNoTracking().Where(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc);
        var total = await q.CountAsync(ct);
        var success = await q.CountAsync(l => l.Outcome == QueryOutcome.Success, ct);
        var unknown = await q.CountAsync(l => l.Outcome == QueryOutcome.Unknown, ct);
        var none = await q.CountAsync(l => l.Outcome == QueryOutcome.NoResults, ct);
        var anon = await q.CountAsync(l => l.UserId == null, ct);
        var points = await q.SumAsync(l => (long?)l.PointsCharged, ct) ?? 0;
        var free = await q.CountAsync(l => l.UsedFreeQuery, ct);
        var users = await q.Where(l => l.UserId != null).Select(l => l.UserId).Distinct().CountAsync(ct);

        return new UsageSummaryDto(fromUtc, toUtc, total, success, unknown, none, anon, points, free, users);
    }

    // ---- helpers ----

    internal static QueryOutcome Classify(AvailabilitySearchResult result)
    {
        if (result.Items.Count == 0) return QueryOutcome.NoResults;
        var anyKnown = result.Items.Any(i =>
            !string.Equals(i.StockStatus, "Unknown", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(i.Freshness, "Stale", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(i.Freshness, "Outdated", StringComparison.OrdinalIgnoreCase));
        return anyKnown ? QueryOutcome.Success : QueryOutcome.Unknown;
    }

    internal static string NormalizeReference(string? reference)
    {
        var r = reference?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(r) || r.Length < 3 || r.Length > 64)
            throw new DomainValidationException("Transfer reference is required (3 to 64 characters).");
        return r;
    }

    private async Task<(int charged, bool usedFree)> ChargeAsync(Guid accountId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var usedFree = await _db.CreditAccounts.Where(a => a.Id == accountId && a.FreeQueriesRemaining > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.FreeQueriesRemaining, a => a.FreeQueriesRemaining - 1), ct);
        if (usedFree == 1)
        {
            await tx.CommitAsync(ct);
            return (0, true);
        }

        var cost = _options.PointsPerQuery;
        var debited = await _db.CreditAccounts.Where(a => a.Id == accountId && a.Balance >= cost)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance - cost), ct);
        if (debited == 0)
            throw new InsufficientCreditException("Not enough balance. Send your account code to top up.");

        var newBalance = await _db.CreditAccounts.AsNoTracking().Where(a => a.Id == accountId).Select(a => a.Balance).SingleAsync(ct);
        _db.CreditLedgerEntries.Add(new CreditLedgerEntry(accountId, CreditEntryType.QueryCharge, -cost, newBalance, null, null, null));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (cost, false);
    }

    private async Task<CreditAccount> GetOrCreateAccountAsync(Guid userId, CancellationToken ct)
    {
        var existing = await _db.CreditAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (existing is not null) return existing;

        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct))
            throw new KeyNotFoundException("User not found.");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var account = new CreditAccount(userId, CreditAccount.GenerateCode(), Math.Max(0, _options.FreeTrialQueries));
            _db.CreditAccounts.Add(account);
            try
            {
                await _db.SaveChangesAsync(ct);
                _db.Entry(account).State = EntityState.Detached;
                return account;
            }
            catch (DbUpdateException)
            {
                _db.Entry(account).State = EntityState.Detached;
                var raced = await _db.CreditAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
                if (raced is not null) return raced;
                // Otherwise the code collided: try another one.
            }
        }
        throw new InvalidOperationException("Could not allocate an account code.");
    }

    private async Task<CreditAccount> FindByCodeAsync(string publicCode, CancellationToken ct)
    {
        if (!CreditAccount.TryNormalizeCode(publicCode, out var code))
            throw new DomainValidationException("Invalid account code format.");
        return await _db.CreditAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.PublicCode == code, ct)
            ?? throw new KeyNotFoundException("No account with this code.");
    }

    private async Task<AdminCreditAccountDto> BuildAdminDtoAsync(CreditAccount account, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().Where(u => u.Id == account.UserId)
            .Select(u => new { u.FullName, u.PhoneNumber, u.IsPhoneVerified }).SingleAsync(ct);
        var entries = await _db.CreditLedgerEntries.AsNoTracking().Where(e => e.AccountId == account.Id)
            .OrderByDescending(e => e.CreatedAt).Take(20)
            .Select(e => new CreditLedgerEntryDto(e.Id, e.Type.ToString(), e.Points, e.BalanceAfter,
                e.TransferReference, e.PerformedByUserId, e.Note, e.CreatedAt))
            .ToListAsync(ct);
        return new AdminCreditAccountDto(CreditAccount.FormatCode(account.PublicCode), account.UserId, user.FullName,
            user.PhoneNumber, user.IsPhoneVerified, account.Balance, account.FreeQueriesRemaining, entries);
    }
}
