using DynamicsCrmLab.Cli;
using DynamicsCrmLab.Cli.Commands;
using DynamicsCrmLab.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// The environment address and any secret stay outside the repository:
//   dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddWorkOrderUseCases();
builder.Services.AddDataverse(builder.Configuration);

builder.Services.AddScoped<ICliCommand, WhoAmICommand>();
builder.Services.AddScoped<ICliCommand, RaiseCommand>();
builder.Services.AddScoped<ICliCommand, AssignCommand>();
builder.Services.AddScoped<ICliCommand, CloseCommand>();
builder.Services.AddScoped<CommandDispatcher>();

using var host = builder.Build();

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

// Commands are scoped, so they are resolved from a scope rather than the root provider.
using var scope = host.Services.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<CommandDispatcher>();

try
{
    return await dispatcher.DispatchAsync(args, cancellation.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    await Console.Out.WriteLineAsync("Cancelled.").ConfigureAwait(false);
    return 130;
}
catch (OptionsValidationException)
{
    // Nothing is configured yet, so say what to run rather than printing a stack trace.
    await Console.Error.WriteLineAsync(
        """
        No Dataverse environment is configured. Point the tool at yours with:

          dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"

        A free environment: https://aka.ms/PowerAppsDevPlan
        """).ConfigureAwait(false);

    return 1;
}
catch (InvalidOperationException exception)
{
    // Almost always a wrong connection setting or a failed sign-in.
    await Console.Error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
    return 1;
}
