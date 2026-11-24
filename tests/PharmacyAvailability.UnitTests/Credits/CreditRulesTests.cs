using PharmacyAvailability.Application.Validation;
using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Domain.Credits;
using PharmacyAvailability.Infrastructure.Credits;
using Xunit;

namespace PharmacyAvailability.UnitTests.Credits;

public class CreditRulesTests
{
    private static MedicineAvailabilityDto Item(string status, string freshness) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Brand", "Generic", "500mg", "Tablet", Guid.NewGuid(), "P", Guid.NewGuid(),
        "B", "Addr", "City", "000", null, 0, 0, null, 1, status, 10m, "EGP", freshness, DateTime.UtcNow);

    private static AvailabilitySearchResult Result(params MedicineAvailabilityDto[] items) =>
        new(items, 1, 20, items.Length, 1, false, false);

    [Fact]
    public void Empty_result_is_NoResults_and_not_chargeable()
        => Assert.Equal(QueryOutcome.NoResults, CreditService.Classify(Result()));

    [Fact]
    public void Only_unknown_stock_is_Unknown()
        => Assert.Equal(QueryOutcome.Unknown, CreditService.Classify(Result(Item("Unknown", "Fresh"), Item("Unknown", "Fresh"))));

    [Fact]
    public void Only_stale_data_is_Unknown()
        => Assert.Equal(QueryOutcome.Unknown, CreditService.Classify(Result(Item("InStock", "Stale"))));

    [Fact]
    public void One_known_fresh_row_is_Success()
        => Assert.Equal(QueryOutcome.Success, CreditService.Classify(Result(Item("Unknown", "Fresh"), Item("InStock", "Fresh"))));

    [Theory]
    [InlineData(" tr-123 ", "TR-123")]
    [InlineData("abc", "ABC")]
    public void Transfer_reference_is_trimmed_and_uppercased(string input, string expected)
        => Assert.Equal(expected, CreditService.NormalizeReference(input));

    [Fact]
    public void Generated_codes_are_valid_and_unambiguous()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = CreditAccount.GenerateCode();
            Assert.Equal(CreditAccount.CodeLength, code.Length);
            Assert.True(CreditAccount.TryNormalizeCode(code, out _));
            Assert.DoesNotContain('O', code); Assert.DoesNotContain('0', code);
            Assert.DoesNotContain('I', code); Assert.DoesNotContain('1', code);
        }
    }

    [Theory]
    [InlineData("abcd-efgh", "ABCDEFGH")]
    [InlineData("ABCD EFGH", "ABCDEFGH")]
    public void Code_input_is_normalized(string input, string expected)
    {
        Assert.True(CreditAccount.TryNormalizeCode(input, out var n));
        Assert.Equal(expected, n);
        Assert.Equal("ABCD-EFGH", CreditAccount.FormatCode(n));
    }

    [Theory]
    [InlineData("")] [InlineData("ABCD")] [InlineData("ABCDEFG0")] [InlineData("ABCDEFGHJ")]
    public void Bad_codes_are_rejected(string input) => Assert.False(CreditAccount.TryNormalizeCode(input, out _));

    [Theory]
    [InlineData("01012345678", "+201012345678")]
    [InlineData("+20 101 234 5678", "+201012345678")]
    [InlineData("201112345678", "+201112345678")]
    [InlineData("01512345678", "+201512345678")]
    public void Egyptian_mobile_numbers_normalize(string input, string expected)
    {
        Assert.True(EgyptPhone.TryNormalize(input, out var e164));
        Assert.Equal(expected, e164);
    }

    [Theory]
    [InlineData("01312345678")] [InlineData("0101234567")] [InlineData("+14155550123")] [InlineData("abc")] [InlineData("")]
    public void Non_egyptian_or_malformed_numbers_are_rejected(string input) => Assert.False(EgyptPhone.TryNormalize(input, out _));
}
