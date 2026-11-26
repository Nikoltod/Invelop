using Invelop.Contacts.Api;
using Invelop.Contacts.Api.Validation;
using Invelop.Contacts.Application;
using Invelop.Contacts.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string DevelopmentCorsPolicy = "DevelopmentSpa";

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. See the README for setup.");

// TimeProvider rather than DateTime.Today, so the date-of-birth rules are testable at their
// boundaries. The validators and the domain factories both take it as a dependency.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();

// The Angular dev server runs on its own origin. In the single-host deployment described in the
// README this policy is simply not applied, because there is no cross-origin request to permit.
builder.Services.AddCors(options => options.AddPolicy(
    DevelopmentCorsPolicy,
    policy => policy
        .WithOrigins(builder.Configuration["Spa:Origin"] ?? "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);
    app.MapOpenApi();
}

// Off by default: the README has the reviewer run `dotnet ef database update` so the migrations
// are visible rather than magic. The Playwright run turns it on, because its throwaway database
// is created and discarded within the test run and nobody is watching it happen.
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).ExcludeFromDescription();
app.MapContactEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can drive the real pipeline via WebApplicationFactory.</summary>
public partial class Program;
