using Microsoft.AspNetCore.SignalR;

namespace Orders.Api.Hubs
{
    public sealed class OrdersHub : Hub
    {
        public static string GetOrderGroupName(Guid orderId) => $"order-{orderId}";

        public async Task SubscribeToOrder(Guid orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetOrderGroupName(orderId));
        }
    }
}
