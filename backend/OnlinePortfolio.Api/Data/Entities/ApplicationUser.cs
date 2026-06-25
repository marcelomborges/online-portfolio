using Microsoft.AspNetCore.Identity;

namespace OnlinePortfolio.Api.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? InvitedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public Tenant? Tenant { get; set; }

    public ApplicationUser? InvitedByUser { get; set; }
}
