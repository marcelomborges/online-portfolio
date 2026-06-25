namespace OnlinePortfolio.Api.Data.Entities;

public class Tenant
{
    public Guid Id { get; set; }

    public Guid PlanId { get; set; }

    public required string Slug { get; set; }

    public required string DisplayName { get; set; }

    public string? CustomDomain { get; set; }

    public DateTimeOffset? CustomDomainVerifiedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Plan Plan { get; set; } = null!;

    public TenantSettings? Settings { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = [];
}
