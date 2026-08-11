namespace InvestManagement.Application.Transactions.Models;

public sealed record BuyRequest(Guid SecurityId, decimal Quantity);