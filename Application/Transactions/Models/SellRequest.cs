namespace InvestManagement.Application.Transactions.Models;

public sealed record SellRequest(Guid SecurityId, decimal Quantity);