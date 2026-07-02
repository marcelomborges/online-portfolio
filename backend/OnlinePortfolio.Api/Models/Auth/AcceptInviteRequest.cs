using System.ComponentModel.DataAnnotations;

namespace OnlinePortfolio.Api.Models.Auth;

public sealed record AcceptInviteRequest(
    [Required, EmailAddress, MaxLength(320)]
    string Email,

    [Required]
    string Token,

    [Required, MinLength(8)]
    string NewPassword);
