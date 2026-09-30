using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Contracts;
using InvestManagement.Infrastructure.Data;
using System.Text.Json;

namespace InvestManagement.Infrastructure.Messaging;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly InvestManagementDbContext _dbContext;

    public OutboxWriter(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(TransactionCreated message)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = message.EventId,
            Type = "TransactionCreated.v1",
            Payload = JsonSerializer.Serialize(message),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.OutboxMessages.Add(outboxMessage);
    }

    public void Add(ProcessTransaction message)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "ProcessTransaction.v1",
            Payload = JsonSerializer.Serialize(message),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.OutboxMessages.Add(outboxMessage);
    }
}
