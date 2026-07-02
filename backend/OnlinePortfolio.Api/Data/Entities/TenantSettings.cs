namespace OnlinePortfolio.Api.Data.Entities;

public class TenantSettings
{
    public Guid TenantId { get; set; }

    public string? ContactEmail { get; set; }

    public string? Bio { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
