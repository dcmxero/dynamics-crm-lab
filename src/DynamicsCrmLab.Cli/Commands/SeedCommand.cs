using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Cli.Commands;

/// <summary>
/// Puts enough believable data in an environment to show the application.
/// </summary>
/// <remarks>
/// A freshly provisioned environment has the tables and none of the rows, so
/// the first thing anybody sees is an empty list and a form they cannot fill
/// in. Writing the rows by hand is a quarter of an hour nobody should spend
/// twice, and doing it from here means the same data every time rather than
/// whatever was typed on the day.
///
/// Records already present are left alone, so the command can be run against an
/// environment that has been used without duplicating what is there.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
internal sealed class SeedCommand(IDataverseClient client) : ICliCommand
{
    private static readonly (string Name, string Email)[] Customers =
    [
        ("Northwind Catering", "facilities@northwind.example"),
        ("Southgate Hotels", "maintenance@southgate.example"),
        ("Riverside Bakery", "owner@riverside.example")
    ];

    private static readonly (string Customer, string SerialNumber)[] Equipment =
    [
        ("Northwind Catering", "WF-2200-014873"),
        ("Northwind Catering", "WF-2200-014901"),
        ("Northwind Catering", "CH-4000-007712"),
        ("Southgate Hotels", "CH-4000-008120"),
        ("Southgate Hotels", "AC-1150-330294"),
        ("Riverside Bakery", "OV-9000-551037")
    ];

    private static readonly string[] Technicians =
    [
        "Peter Kovac",
        "Jana Bartosova",
        "Milan Durica"
    ];

    /// <inheritdoc/>
    public string Name => "seed";

    /// <inheritdoc/>
    public string Usage => "seed - put the customers, equipment and technicians an environment needs to be shown";

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var customers = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var (name, email) in Customers)
        {
            customers[name] = await EnsureAsync(
                ContactSchema.EntityName,
                ContactSchema.FullName,
                name,
                () => new Entity(ContactSchema.EntityName)
                {
                    // The platform assembles fullname from the parts, so the
                    // parts are what a contact is written with.
                    ["firstname"] = FirstOf(name),
                    ["lastname"] = LastOf(name),
                    [ContactSchema.Email] = email
                },
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var (customer, serialNumber) in Equipment)
        {
            await EnsureAsync(
                EquipmentSchema.EntityName,
                EquipmentSchema.SerialNumber,
                serialNumber,
                () => new Entity(EquipmentSchema.EntityName)
                {
                    [EquipmentSchema.SerialNumber] = serialNumber,
                    [EquipmentSchema.Customer] =
                        new EntityReference(ContactSchema.EntityName, customers[customer])
                },
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var technician in Technicians)
        {
            await EnsureAsync(
                TechnicianSchema.EntityName,
                TechnicianSchema.FullName,
                technician,
                () => new Entity(TechnicianSchema.EntityName)
                {
                    [TechnicianSchema.FullName] = technician,
                    [TechnicianSchema.IsAvailable] = true
                },
                cancellationToken).ConfigureAwait(false);
        }

        await Console.Out
            .WriteLineAsync(
                $"  {Customers.Length} customers, {Equipment.Length} units, {Technicians.Length} technicians")
            .ConfigureAwait(false);

        return 0;
    }

    private static string FirstOf(string name) => name.Split(' ')[0];

    private static string LastOf(string name) => string.Join(' ', name.Split(' ').Skip(1));

    /// <summary>
    /// Returns the record with this name, writing it first if it is not there.
    /// </summary>
    private async Task<Guid> EnsureAsync(
        string entityName,
        string nameColumn,
        string name,
        Func<Entity> build,
        CancellationToken cancellationToken)
    {
        var existing = await client.RetrieveMultipleAsync(
            new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                Criteria =
                {
                    Conditions = { new ConditionExpression(nameColumn, ConditionOperator.Equal, name) }
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (existing.Entities.Count > 0)
        {
            await Console.Out.WriteLineAsync($"  {name} is already there").ConfigureAwait(false);

            return existing.Entities[0].Id;
        }

        var id = await client.CreateAsync(build(), cancellationToken).ConfigureAwait(false);

        await Console.Out.WriteLineAsync($"  {name}").ConfigureAwait(false);

        return id;
    }
}
