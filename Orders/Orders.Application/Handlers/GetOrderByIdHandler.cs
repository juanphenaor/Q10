using Orders.Application.Contracts;
using Orders.Application.DTOs;
using Orders.Application.Mappers;

namespace Orders.Application.Handlers;

public sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
{
    public async Task<OrderResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);

        return order is null ? null : OrderMapper.ToResponse(order);
    }
}
