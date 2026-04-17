using WiseApi.Client.Http;
using WiseApi.Client.Models.Balances;

namespace WiseApi.Client.Services;

/// <inheritdoc cref="IBalancesApi" />
public sealed class BalancesApi : IBalancesApi
{
    private readonly WiseHttpClient _http;

    /// <summary>Create a new <see cref="BalancesApi"/>.</summary>
    public BalancesApi(WiseHttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Balance>> ListAsync(
        long profileId,
        IReadOnlyCollection<BalanceType>? types = null,
        CancellationToken cancellationToken = default)
    {
        var typesQuery = types is null || types.Count == 0
            ? "STANDARD"
            : string.Join(',', types.Select(FormatBalanceType));

        var uri = $"/v4/profiles/{profileId}/balances?types={typesQuery}";
        return _http.GetAsync<IReadOnlyList<Balance>>(uri, cancellationToken);
    }

    private static string FormatBalanceType(BalanceType type) => type switch
    {
        BalanceType.Standard => "STANDARD",
        BalanceType.Savings => "SAVINGS",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown balance type."),
    };

    /// <inheritdoc />
    public Task<Balance> GetAsync(long profileId, long balanceId, CancellationToken cancellationToken = default)
        => _http.GetAsync<Balance>($"/v4/profiles/{profileId}/balances/{balanceId}", cancellationToken);

    /// <inheritdoc />
    public Task<Balance> CreateAsync(
        long profileId,
        CreateBalanceRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Type == BalanceType.Savings && string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A name is required when creating a SAVINGS balance.", nameof(request));
        }

        var headers = new Dictionary<string, string>
        {
            [WiseHttpClient.IdempotencyHeader] = (idempotencyKey ?? Guid.NewGuid()).ToString("D"),
        };

        return _http.PostJsonAsync<CreateBalanceRequest, Balance>(
            $"/v4/profiles/{profileId}/balances",
            request,
            headers,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(long profileId, long balanceId, CancellationToken cancellationToken = default)
        => _http.DeleteAsync($"/v4/profiles/{profileId}/balances/{balanceId}", headers: null, cancellationToken);
}
