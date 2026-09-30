namespace InvestManagement.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    public string TopicName { get; set; } = string.Empty;

    public string TransactionProcessingSubscriptionName { get; set; } = string.Empty;
}