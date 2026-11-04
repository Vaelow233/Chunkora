using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Chunkora.Server.Agents.Authentication;

public class AgentTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AgentTokenValidator validator) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AgentToken";
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return AuthenticateResult.NoResult();
        }
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header) || 
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || 
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.Fail("Invalid Authorization header");
        }
        var node = await validator.ValidateAsync(header.Parameter);
        if (node is null)
        {
            return AuthenticateResult.Fail("Invalid or expired access token");
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, node.Id.ToString()),
            new Claim(ClaimTypes.Name, node.Name),
            new Claim("principal_type", "agent")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}