namespace Joby.Application.Security;

/// <summary>Custom JWT claim types used by the API.</summary>
public static class JwtClaimTypes
{
    /// <summary>Carries the root admin user id while the subject is an impersonated user.</summary>
    public const string Impersonator = "impersonator";
}
