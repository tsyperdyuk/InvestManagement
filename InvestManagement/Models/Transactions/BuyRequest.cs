namespace InvestManagement.Api.Models.Transactions;

public sealed record BuyRequest(Guid SecurityId, decimal Quantity);