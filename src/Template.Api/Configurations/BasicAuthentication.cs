using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Template.Application.Abstractions.Messaging;
using Template.Application.Features.Users;
using Template.Domain.Results;

namespace Template.Api.Configurations;

/// <summary>
/// HTTP Basic authentication (RFC 7617). Credentials are only accepted over HTTPS and are checked
/// against PBKDF2 password hashes through <see cref="AuthenticateUserQuery"/>.
/// </summary>
public sealed class BasicAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Basic";

    private readonly IQueryHandler<AuthenticateUserQuery, AuthenticatedUser> _authenticateUser;

    public BasicAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IQueryHandler<AuthenticateUserQuery, AuthenticatedUser> authenticateUser)
        : base(options, logger, encoder)
    {
        _authenticateUser = authenticateUser;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out AuthenticationHeaderValue? header) ||
            !SchemeName.Equals(header.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        // Basic credentials are only base64 encoded, so never accept them over plain HTTP.
        if (!Request.IsHttps)
        {
            return AuthenticateResult.Fail("HTTPS is required for Basic authentication.");
        }

        if (!TryDecode(header.Parameter, out string username, out string password))
        {
            return AuthenticateResult.Fail("Invalid Basic authentication header.");
        }

        Result<AuthenticatedUser> result = await _authenticateUser.Handle(
            new AuthenticateUserQuery(username, password), Context.RequestAborted);

        if (result.IsFailure)
        {
            return AuthenticateResult.Fail(result.Error.Description);
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, result.Value.Id.ToString()),
            new(ClaimTypes.Name, result.Value.Username)
        ];

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"{SchemeName} realm=\"Template.Api\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool TryDecode(string? parameter, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (string.IsNullOrWhiteSpace(parameter)) return false;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
        }
        catch (FormatException)
        {
            return false;
        }

        int separator = decoded.IndexOf(':');
        if (separator <= 0) return false;

        username = decoded[..separator];
        password = decoded[(separator + 1)..];
        return true;
    }
}
