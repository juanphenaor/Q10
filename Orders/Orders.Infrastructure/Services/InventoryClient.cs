using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Contracts;
using Orders.Application.Exceptions;

namespace Orders.Infrastructure.Services;

public sealed class InventoryClient(HttpClient httpClient) : IInventoryClient
{
    private const string ValidateSkusPath = "api/inventory/validate-skus";

    public async Task<bool> AllSkusExistAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(ValidateSkusPath, new ValidateSkusRequest(skus), cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new InventoryUnavailableException(exception);
        }
    }

    private sealed record ValidateSkusRequest(IReadOnlyCollection<string> Skus);
}
