namespace OnlinePortfolio.Api.Services;

public interface IEmailService
{
    Task SendInviteAsync(string toEmail, string tenantDisplayName, string inviteLink, CancellationToken ct = default);
}
