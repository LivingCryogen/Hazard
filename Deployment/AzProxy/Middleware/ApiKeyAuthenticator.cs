using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AzProxy.Middleware;

// Custom Authenticator for API Key authentication - used for lone developer admic access
// If project scope grows, move to JWT or Oauth
public class ApiKeyAuthenticator(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config) 
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly string _adminKey = config["APIKey"] ?? string.Empty;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_adminKey))
            return Task.FromResult(AuthenticateResult.Fail("No admin key configured."));

        // If Api Key is correct, authenticate as Admin
        if (Request.Headers.TryGetValue("APIKey", out var givenKey)
            && givenKey == _adminKey)
        {
            var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        else         
            return Task.FromResult(AuthenticateResult.Fail("Invalid Admin Key."));
    }
}
