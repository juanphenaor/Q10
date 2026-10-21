using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Mappers;

namespace Orders.Application.Handlers;

public sealed class GetOrdersHandler(IOrderRepository orderRepository)
{
    public async Task<IReadOnlyList<OrderResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetAllAsync(cancellationToken);

        return orders.Select(OrderMapper.ToResponse).ToArray();
    }
}
