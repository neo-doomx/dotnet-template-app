using Scalar.AspNetCore;
using Serilog;
using Template.Api.Configurations;
using Template.Application;
using Template.Infrastructure;
using Template.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddVersionedOpenApi();
builder.Services.AddSecurity();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();

    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(options => options.AddPreferredSecuritySchemes(BasicAuthentication.SchemeName));
}

app.MapEndpoints();

await app.RunAsync();
