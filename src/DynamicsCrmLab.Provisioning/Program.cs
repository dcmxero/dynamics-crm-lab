using DynamicsCrmLab.Infrastructure;
using DynamicsCrmLab.Provisioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

builder.Services.AddDataverse(builder.Configuration);
builder.Services.AddScoped<SolutionProvisioner>();
builder.Services.AddScoped<OrganizationLanguage>();
builder.Services.AddScoped<PluginRegistrar>();
builder.Services.AddScoped<SchemaProvisioner>();
builder.Services.AddScoped<SampleDataSeeder>();

using var host = builder.Build();

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var seedRequested = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);
var pluginPackage = PluginPackagePath(args);

try
{
    await services.GetRequiredService<SolutionProvisioner>()
        .EnsureAsync(cancellation.Token)
        .ConfigureAwait(false);

    await services.GetRequiredService<SchemaProvisioner>()
        .ApplyAsync(cancellation.Token)
        .ConfigureAwait(false);

    await services.GetRequiredService<PluginRegistrar>()
        .RegisterAsync(pluginPackage, cancellation.Token)
        .ConfigureAwait(false);

    if (seedRequested)
    {
        await services.GetRequiredService<SampleDataSeeder>()
            .SeedAsync(cancellation.Token)
            .ConfigureAwait(false);
    }

    // Nothing is usable on a form until the customizations are published.
    await services.GetRequiredService<SolutionProvisioner>()
        .PublishAsync(cancellation.Token)
        .ConfigureAwait(false);

    await Console.Out.WriteLineAsync("Done.").ConfigureAwait(false);

    return 0;
}
catch (OptionsValidationException)
{
    await Console.Error.WriteLineAsync(
        """
        No Dataverse environment is configured. Point the tool at yours with:

          dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"

        A free environment: https://aka.ms/PowerAppsDevPlan
        """).ConfigureAwait(false);

    return 1;
}
catch (OperationCanceledException)
{
    await Console.Out.WriteLineAsync("Cancelled.").ConfigureAwait(false);

    return 130;
}
catch (InvalidOperationException exception)
{
    await Console.Error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);

    return 1;
}

/// <summary>
/// Marks the assembly that user secrets are stored against.
/// </summary>
internal sealed partial class Program
{
    /// <summary>
    /// Works out which plug-in package to upload.
    /// </summary>
    /// <remarks>
    /// The release build sitting next to the source is what a developer has to
    /// hand; a pipeline passes the path of what it just built.
    /// </remarks>
    /// <param name="args">The command line the tool was started with.</param>
    /// <returns>The path of the package to upload.</returns>
    private static string PluginPackagePath(string[] args)
    {
        const string Option = "--plugins";

        var index = Array.FindIndex(args, argument => argument.Equals(Option, StringComparison.OrdinalIgnoreCase));

        if (index >= 0 && index + 1 < args.Length)
        {
            return args[index + 1];
        }

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "DynamicsCrmLab.Plugins",
            "bin",
            "Release",
            "dcl_DynamicsCrmLab.Plugins.1.0.0.nupkg"));
    }
}
