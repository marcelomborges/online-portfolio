using System.ComponentModel.DataAnnotations;

namespace OnlinePortfolio.Api.Models.Platform;

public sealed record InviteUserRequest(
    [Required, EmailAddress, MaxLength(320)]
    string Email,

    [Required]
    string Role);
