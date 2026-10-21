using Inventory.Application.Contracts;
using Inventory.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Inventory.Infrastructure.Services
{
    public sealed class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private const string ExchangeName = "orderflow.events";
        private const string QueueName = "inventory.order-created";
        private const string RoutingKey = "order.created";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__Username") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest"
            };

            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(options: new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), cancellationToken: stoppingToken);


                await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

                await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                        var orderCreated = JsonSerializer.Deserialize<OrderCreated>(json)
                            ?? throw new JsonException("The OrderCreated message is empty.");

                        logger.LogInformation(
                            "Received order {OrderId} with {ItemCount} items.",
                            orderCreated.OrderId,
                            orderCreated.Items.Count);

                        using var scope = scopeFactory.CreateScope();

                        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                        var result = await productRepository.ReserveStockAsync(orderCreated, stoppingToken);

                        if (result.IsAccepted)
                        {
                            await PublishResultAsync(channel, "stock.reserved", result.Reservation!, stoppingToken);

                            logger.LogInformation("Stock reserved for order {OrderId}.", orderCreated.OrderId);
                        }
                        else
                        {
                            await PublishResultAsync(channel, "stock.rejected", result.Rejection!, stoppingToken);

                            logger.LogWarning("Failed to reserve stock for order {OrderId}.", orderCreated.OrderId);
                        }

                        await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to process OrderCreated message.");

                        await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

                logger.LogInformation("Consuming messages from queue {QueueName}.", QueueName);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("RabbitMQ consumer is stopping.");
            }
        }

        private async Task PublishResultAsync<T>(IChannel channel, string routingKey, T message, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = routingKey
            };

            await channel.BasicPublishAsync(exchange: ExchangeName, routingKey: routingKey, mandatory: true, basicProperties: properties, body: body, cancellationToken: cancellationToken);
        }
    }
}
