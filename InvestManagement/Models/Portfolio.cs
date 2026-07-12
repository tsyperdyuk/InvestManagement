namespace InvestManagement.Models;

public class Portfolio
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
