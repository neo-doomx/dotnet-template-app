using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;

namespace Template.Api.Configurations;

public static class Security
{
    public static void AddSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(BasicAuthentication.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthentication>(BasicAuthentication.SchemeName, null);

        services.AddAuthorization();

        // Limits password guessing: every client IP gets 60 requests per minute.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
    }
}
