using Azure.Messaging.ServiceBus;
using InvestManagement.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Messaging;

public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(IServiceScopeFactory scopeFactory, ServiceBusSender sender, ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox worker started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SendPendingMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to process outbox");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Finishing work.
        }
    }

    private async Task SendPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<InvestManagementDbContext>();

        var now = DateTime.UtcNow;

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.ProcessedAtUtc == null &&
                (message.NextAttemptAtUtc == null ||
                 message.NextAttemptAtUtc <= now))
            .OrderBy(message => message.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            message.Attempts++;

            try
            {
                var serviceBusMessage = new ServiceBusMessage(
                    message.Payload)
                {
                    MessageId = message.Id.ToString(),
                    Subject = message.Type,
                    ContentType = "application/json"
                };

                await _sender.SendMessageAsync(
                    serviceBusMessage,
                    cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.NextAttemptAtUtc = null;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.NextAttemptAtUtc =
                    DateTime.UtcNow.AddSeconds(30);

                _logger.LogWarning(
                    exception,
                    "Failed to send message {MessageId}, attempt {Attempt}",
                    message.Id,
                    message.Attempts);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (message.ProcessedAtUtc is not null)
            {
                _logger.LogInformation(
                    "Outbox message {MessageId} sent",
                     message.Id);
            }
        }
    }
}
