namespace Orders.Application.Contracts
{
    public interface IOrderNotifier
    {
        Task NotifyStatusChangedAsync(Guid orderId, string status, CancellationToken cancellationToken);
    }
}
