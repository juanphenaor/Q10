namespace Orders.Application.Contracts;

public interface IInventoryClient
{
    Task<bool> AllSkusExistAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
}
