namespace OnlinePortfolio.Api.Models;

public sealed record ApiErrorResponse(
    string Title,
    int Status,
    string Detail,
    string TraceId);
