
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Infrastructure.Context;
using RabbitMQ.Client;
using System.Text;

namespace Orders.Infrastructure.Services;

public sealed class OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const string ExchangeName = "orderflow.events";
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RabbitMQ__Username") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest"
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);

                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);

                await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);

                await channel.ExchangeDeclareAsync(
                    exchange: "orderflow.events",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await PublishPendingMessagesAsync(channel, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error publishing Outbox messages.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PublishPendingMessagesAsync(IChannel channel, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var messages = await context.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    MessageId = message.Id.ToString(),
                    Type = message.Type
                };

                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: message.Type,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish Outbox message {MessageId}.", message.Id);

                try
                {
                    message.Attempts++;
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveException)
                {
                    logger.LogError(saveException, "Failed to update Outbox message {MessageId}.", message.Id);
                }
            }
        }
    }
}
