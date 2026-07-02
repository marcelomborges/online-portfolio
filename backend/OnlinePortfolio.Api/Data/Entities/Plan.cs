namespace OnlinePortfolio.Api.Data.Entities;

public class Plan
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int MaxStorageMb { get; set; }

    public int MaxArtworks { get; set; }

    public bool CustomDomainAllowed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Tenant> Tenants { get; set; } = [];
}
