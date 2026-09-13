using DynamicsCrmLab.Cli;
using DynamicsCrmLab.Cli.Commands;
using DynamicsCrmLab.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

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
catch (InvalidOperationException exception)
{
    // Almost always a missing or wrong connection setting.
    await Console.Error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
    return 1;
}
