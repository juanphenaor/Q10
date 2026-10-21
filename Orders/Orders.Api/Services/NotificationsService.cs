using Microsoft.AspNetCore.SignalR;
using Orders.Api.Hubs;
using Orders.Application.Contracts;

namespace Orders.Api.Services
{
    public class NotificationsService(IHubContext<OrdersHub> hubContext) : IOrderNotifier
    {
        public async Task NotifyStatusChangedAsync(Guid orderId, string status, CancellationToken cancellationToken)
        {
            await hubContext.Clients.Group(OrdersHub.GetOrderGroupName(orderId))
                .SendAsync(
                    "OrderStatusChanged",
                    new
                    {
                        OrderId = orderId,
                        Status = status
                    },
                    cancellationToken);
        }
    }
}
