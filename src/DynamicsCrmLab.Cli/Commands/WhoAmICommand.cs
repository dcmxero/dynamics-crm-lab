using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Reports who the application is signed in as.
/// </summary>
/// <remarks>
/// Besides create, read, update and delete, Dataverse exposes named operations
/// that go through Execute. WhoAmI is the cheapest of them and makes a good
/// check that the connection and the permissions are in order.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
internal sealed class WhoAmICommand(IDataverseClient client) : ICliCommand
{
    /// <inheritdoc/>
    public string Name => "whoami";

    /// <inheritdoc/>
    public string Usage => "whoami - report the signed-in user and the environment";

    /// <inheritdoc/>
    public async Task ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var response = (WhoAmIResponse)await client
            .ExecuteAsync(new WhoAmIRequest(), cancellationToken)
            .ConfigureAwait(false);

        var user = await client
            .RetrieveAsync(
                "systemuser",
                response.UserId,
                new ColumnSet("fullname", "internalemailaddress"),
                cancellationToken)
            .ConfigureAwait(false);

        await Console.Out.WriteLineAsync($"  user          {user.GetAttributeValue<string>("fullname")}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  email         {user.GetAttributeValue<string>("internalemailaddress")}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  user id       {response.UserId}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  business unit {response.BusinessUnitId}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  organization  {response.OrganizationId}").ConfigureAwait(false);
        await Console.Out.WriteLineAsync($"  work orders   {WorkOrderSchema.EntityName}").ConfigureAwait(false);
    }
}
