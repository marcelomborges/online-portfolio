using Microsoft.Extensions.Options;
using OnlinePortfolio.Api.Options;
using Resend;

namespace OnlinePortfolio.Api.Services;

public sealed class ResendEmailService(
    IResend resend,
    IOptions<ResendOptions> resendOpts,
    IOptions<AppOptions> appOpts,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly ResendOptions _resend = resendOpts.Value;
    private readonly AppOptions _app = appOpts.Value;

    public async Task SendInviteAsync(
        string toEmail,
        string tenantDisplayName,
        string inviteLink,
        CancellationToken ct = default)
    {
        var message = new EmailMessage
        {
            From = $"{_resend.FromName} <{_resend.FromEmail}>",
            Subject = "Convite para o Online Portfolio",
            HtmlBody = $"""
                <p>Olá,</p>
                <p>Você foi convidado para gerenciar o portfólio <strong>{tenantDisplayName}</strong> no Online Portfolio.</p>
                <p><a href="{inviteLink}">Clique aqui para aceitar o convite e definir sua senha.</a></p>
                <p>Se o botão não funcionar, copie e cole este endereço no navegador:</p>
                <p>{inviteLink}</p>
                <p>Este link é válido por 24 horas.</p>
                """,
        };
        message.To.Add(toEmail);

        await resend.EmailSendAsync(message, ct);
        logger.LogInformation("Invite email sent to {Email} for tenant {Tenant}", toEmail, tenantDisplayName);
    }
}
