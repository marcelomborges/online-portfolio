namespace OnlinePortfolio.Api.Data;

public static class AppRoles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string Owner = "Owner";
    public const string Editor = "Editor";

    public static readonly IReadOnlyList<string> All = [PlatformAdmin, Owner, Editor];
}
