namespace ProteinTracker.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "ProteinTracker.Api";
    public string Audience { get; set; } = "ProteinTracker.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
