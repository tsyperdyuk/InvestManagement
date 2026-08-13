namespace InvestManagement.Api.Models.Transactions;

public sealed record SellRequest(Guid SecurityId, decimal Quantity);