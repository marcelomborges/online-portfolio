using System.ComponentModel.DataAnnotations;

namespace OnlinePortfolio.Api.Models.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(320)]
    string Email,

    [Required, MinLength(1)]
    string Password);
