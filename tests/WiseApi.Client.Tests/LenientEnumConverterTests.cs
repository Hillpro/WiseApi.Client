using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WiseApi.Client.Models.Balances;
using WiseApi.Client.Models.Rates;
using WiseApi.Client.Serialization;

namespace WiseApi.Client.Tests;

/// <summary>
/// Wise adds enum values without bumping endpoint versions. A response to a request that already
/// succeeded (e.g. a balance conversion) must still deserialize when it carries a value this
/// client has never seen.
/// </summary>
public sealed class LenientEnumConverterTests
{
    // Enums that never appear in a Wise response body, so they stay strict.
    private static readonly HashSet<Type> RequestOnlyEnums =
    [
        typeof(RateGrouping),                // query-string only
        typeof(Models.Profiles.ProfileType), // [JsonIgnore]'d; the "type" discriminator drives deserialization
    ];

    public static TheoryData<Type> ModelEnums()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(BalanceType).Assembly.GetTypes()
                     .Where(t => t.IsEnum && t.IsPublic && t.Namespace?.StartsWith("WiseApi.Client.Models", StringComparison.Ordinal) == true)
                     .Where(t => !RequestOnlyEnums.Contains(t)))
        {
            data.Add(type);
        }

        return data;
    }

    /// <summary>
    /// The converter only falls back for enums declaring <c>Unknown</c>. Keeping it at value 0 also
    /// means a field Wise omits reads as <c>Unknown</c> rather than a plausible real value. The
    /// converter is opted into per enum (source-generator friendly), so the attribute is checked too.
    /// </summary>
    [Theory]
    [MemberData(nameof(ModelEnums))]
    public void Response_enums_declare_Unknown_as_zero_and_use_lenient_converter(Type enumType)
    {
        Assert.True(Enum.IsDefined(enumType, "Unknown"), $"{enumType.Name} has no Unknown member.");
        Assert.Equal(0, Convert.ToInt32(Enum.Parse(enumType, "Unknown"), System.Globalization.CultureInfo.InvariantCulture));

        var attribute = enumType.GetCustomAttribute<JsonConverterAttribute>();
        Assert.True(
            attribute?.ConverterType == typeof(LenientEnumConverter<>).MakeGenericType(enumType),
            $"{enumType.Name} lacks [JsonConverter(typeof(LenientEnumConverter<{enumType.Name}>))].");
    }

    [Fact]
    public void Omitted_non_nullable_enum_reads_as_Unknown()
    {
        var movement = JsonSerializer.Deserialize("""{"id":1}""", WiseJsonContext.Default.BalanceMovement);

        Assert.NotNull(movement);
        Assert.Equal(BalanceMovementType.Unknown, movement.Type);
        Assert.Equal(BalanceMovementState.Unknown, movement.State);
    }

    [Fact]
    public void Null_non_nullable_enum_reads_as_Unknown()
    {
        var movement = JsonSerializer.Deserialize("""{"id":1,"type":null,"state":null}""", WiseJsonContext.Default.BalanceMovement);

        Assert.NotNull(movement);
        Assert.Equal(BalanceMovementType.Unknown, movement.Type);
        Assert.Equal(BalanceMovementState.Unknown, movement.State);
    }

    [Fact]
    public void Null_nullable_enum_stays_null()
    {
        var balance = JsonSerializer.Deserialize(
            """{"id":1,"currency":"EUR","type":"STANDARD","investmentState":null,"amount":{"value":1,"currency":"EUR"}}""",
            WiseJsonContext.Default.Balance);

        Assert.NotNull(balance);
        Assert.Null(balance.InvestmentState);
    }

    [Fact]
    public void Unrecognised_value_reads_as_Unknown()
    {
        var movement = JsonSerializer.Deserialize(
            """{"id":1,"type":"CARD_SPEND","state":"FAILED"}""",
            WiseJsonContext.Default.BalanceMovement);

        Assert.NotNull(movement);
        Assert.Equal(BalanceMovementType.Unknown, movement.Type);
        Assert.Equal(BalanceMovementState.Unknown, movement.State);
    }

    [Fact]
    public void Unrecognised_value_reads_as_Unknown_for_nullable_enum()
    {
        var value = JsonSerializer.Deserialize<InvestmentState?>("\"FROZEN\"", WiseJsonDefaults.Options);
        Assert.Equal(InvestmentState.Unknown, value);
    }

    [Theory]
    [InlineData("\"NOT_INVESTED\"", InvestmentState.NotInvested)]
    [InlineData("\"not_invested\"", InvestmentState.NotInvested)]
    [InlineData("\"UNKNOWN\"", InvestmentState.Unknown)]
    public void Reads_known_values_case_insensitively(string json, InvestmentState expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<InvestmentState>(json, WiseJsonDefaults.Options));
    }

    [Fact]
    public void Writes_screaming_snake_case()
    {
        var json = JsonSerializer.Serialize(new CreateBalanceRequest("EUR", BalanceType.Savings, "Trip"), WiseJsonContext.Default.CreateBalanceRequest);
        Assert.Contains("\"type\":\"SAVINGS\"", json);
    }

    [Fact]
    public void Enum_without_Unknown_member_stays_strict()
    {
        var options = new JsonSerializerOptions { Converters = { new LenientEnumConverter<RateGrouping>() } };

        Assert.Equal(RateGrouping.Hour, JsonSerializer.Deserialize<RateGrouping>("\"HOUR\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RateGrouping>("\"WEEK\"", options));
    }

    [Fact]
    public void Rejects_integer_values()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<BalanceType>("0", WiseJsonDefaults.Options));
    }
}
