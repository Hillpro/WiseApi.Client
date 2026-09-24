using System.Text.Json;
using System.Text.Json.Serialization;
using WiseApi.Client.Authentication.OAuth;
using WiseApi.Client.Models.Balances;
using WiseApi.Client.Models.MultiCurrencyAccounts;
using WiseApi.Client.Models.Profiles;
using WiseApi.Client.Models.Quotes;
using WiseApi.Client.Models.Rates;

namespace WiseApi.Client.Serialization;

/// <summary>
/// Source-generated JSON metadata for every request and response type the library sends or
/// receives. Services pass <c>WiseJsonContext.Default.X</c> to <see cref="Http.WiseHttpClient"/>,
/// so a model missing from this list is a compile error rather than a runtime failure under
/// Native AOT. The attribute below is the single source of truth for serializer settings.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    AllowOutOfOrderMetadataProperties = true,
    Converters = [typeof(LenientDateTimeOffsetConverter)])]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(IReadOnlyList<Profile>))]
[JsonSerializable(typeof(MultiCurrencyAccount))]
[JsonSerializable(typeof(MultiCurrencyAccountEligibility))]
[JsonSerializable(typeof(Balance))]
[JsonSerializable(typeof(IReadOnlyList<Balance>))]
[JsonSerializable(typeof(CreateBalanceRequest))]
[JsonSerializable(typeof(BalanceMovement))]
[JsonSerializable(typeof(BalanceMovementRequest))]
[JsonSerializable(typeof(Quote))]
[JsonSerializable(typeof(CreateQuoteRequest))]
[JsonSerializable(typeof(IReadOnlyList<Rate>))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(TokenClient.OAuthError))]
internal sealed partial class WiseJsonContext : JsonSerializerContext;
