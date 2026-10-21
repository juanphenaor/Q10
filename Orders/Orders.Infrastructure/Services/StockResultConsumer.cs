
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Application.Contracts;
using Orders.Application.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Orders.Infrastructure.Services;

public sealed class StockResultConsumer(IServiceScopeFactory scopeFactory, ILogger<StockResultConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "orderflow.events";
    private const string QueueName = "orders.stock-results";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
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

            await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: "stock.reserved", cancellationToken: stoppingToken);

            await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: "stock.rejected", cancellationToken: stoppingToken);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(
                        eventArgs.Body.ToArray());

                    using var scope = scopeFactory.CreateScope();

                    var repository = scope.ServiceProvider
                        .GetRequiredService<IOrderRepository>();

                    switch (eventArgs.RoutingKey)
                    {
                        case "stock.reserved":
                            {
                                var message =
                                    JsonSerializer.Deserialize<StockReserved>(json)
                                    ?? throw new JsonException(
                                        "The StockReserved message is empty.");

                                var updated = await repository.ConfirmAsync(
                                    message,
                                    stoppingToken);

                                if (!updated)
                                {
                                    logger.LogWarning(
                                        "Order {OrderId} was not confirmed because it does not exist or is no longer pending.",
                                        message.OrderId);
                                }

                                break;
                            }

                        case "stock.rejected":
                            {
                                var message =
                                    JsonSerializer.Deserialize<StockRejected>(json)
                                    ?? throw new JsonException(
                                        "The StockRejected message is empty.");

                                var updated = await repository.RejectAsync(
                                    message,
                                    stoppingToken);

                                if (!updated)
                                {
                                    logger.LogWarning(
                                        "Order {OrderId} was not rejected because it does not exist or is no longer pending.",
                                        message.OrderId);
                                }

                                break;
                            }

                        default:
                            throw new InvalidOperationException(
                                $"Unsupported routing key: {eventArgs.RoutingKey}");
                    }

                    await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to process stock result message with routing key {RoutingKey}.", eventArgs.RoutingKey);

                    await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            logger.LogInformation("Consuming stock results from queue {QueueName}.", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Stock result consumer is stopping.");
        }
    }
}
