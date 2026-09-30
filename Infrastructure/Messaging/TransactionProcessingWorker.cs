using Azure.Messaging.ServiceBus;
using InvestManagement.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InvestManagement.Infrastructure.Messaging;

public sealed class TransactionProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<TransactionProcessingWorker> _logger;

    public TransactionProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<TransactionProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await using var processor = _client.CreateProcessor(
            _options.TopicName,
            _options.TransactionProcessingSubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

        try
        {
            await processor.StartProcessingAsync(stoppingToken);

            _logger.LogInformation(
                "Transaction processing worker started");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Graceful application shutdown.
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
        }
    }

    private async Task HandleMessageAsync(
        ProcessMessageEventArgs args)
    {
        if (args.Message.Subject != "ProcessTransaction.v1")
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "UnsupportedMessageType",
                deadLetterErrorDescription: "Expected ProcessTransaction.v1.",
                cancellationToken: args.CancellationToken);

            return;
        }

        ProcessTransaction? command;

        try
        {
            command = args.Message.Body
                .ToObjectFromJson<ProcessTransaction>();
        }
        catch (JsonException)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "InvalidPayload",
                deadLetterErrorDescription: "Message body is not a valid command.",
                cancellationToken: args.CancellationToken);

            return;
        }

        if (command is null || command.OperationId == Guid.Empty)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "InvalidOperationId",
                deadLetterErrorDescription: "OperationId is required.",
                cancellationToken: args.CancellationToken);

            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();

        var operationProcessor = scope.ServiceProvider
            .GetRequiredService<TransactionOperationProcessor>();

        await operationProcessor.ProcessAsync(
            command.OperationId,
            args.CancellationToken);

        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);
    }

    private Task HandleErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processing error. Source: {ErrorSource}",
            args.ErrorSource);

        return Task.CompletedTask;
    }
}
