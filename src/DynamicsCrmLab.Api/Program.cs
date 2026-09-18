using DynamicsCrmLab.Api.WorkOrders;
using DynamicsCrmLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The environment address and any secret stay outside the repository.
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddWorkOrderUseCases();
builder.Services.AddDataverse(builder.Configuration);

builder.Services.AddOpenApi();

// Failures come back as problem details, so the client has one shape to handle
// rather than a mix of plain text, HTML and JSON.
builder.Services.AddProblemDetails();

const string AngularDevServer = "angular-dev-server";
builder.Services.AddCors(options => options.AddPolicy(
    AngularDevServer,
    policy => policy
        .WithOrigins(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(AngularDevServer);
}

app.MapWorkOrders();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Exposes the entry point so integration tests can host the application.
/// </summary>
internal sealed partial class Program;
