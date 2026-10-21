using Orders.Application.Contracts;

namespace Backend.Tests;

internal sealed class StubInventoryClient : IInventoryClient
{
    public bool Result { get; set; } = true;
    public string[]? RequestedSkus { get; private set; }

    public Task<bool> AllSkusExistAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
    {
        RequestedSkus = skus.ToArray();
        return Task.FromResult(Result);
    }
}
