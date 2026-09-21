using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Puts enough records in place for the screens to show something.
/// </summary>
/// <remarks>
/// A fresh environment has no customers, no equipment and no technicians, so
/// every screen renders empty and nothing can be exercised end to end. This
/// seeds the smallest set that makes the client worth opening.
///
/// Records are matched by their natural key before being written, so running
/// this twice leaves one of each rather than duplicates.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
/// <param name="logger">Reports what was created and what was already there.</param>
internal sealed class SampleDataSeeder(IDataverseClient client, ILogger<SampleDataSeeder> logger)
{
    private static readonly (string First, string Last, string Email)[] Customers =
    [
        ("Acme", "Foods", "service@acme.example"),
        ("Riverside", "Hotels", "maintenance@riverside.example")
    ];

    private static readonly (string Name, bool Available)[] Technicians =
    [
        ("Peter Kovac", true),
        ("Zuzana Bielikova", true),
        ("Ivan Hruska", false)
    ];

    /// <summary>
    /// Creates the sample records that are not there yet.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes once the records are in place.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (name, available) in Technicians)
        {
            await EnsureAsync(
                TechnicianSchema.EntityName,
                TechnicianSchema.FullName,
                name,
                () => new Entity(TechnicianSchema.EntityName)
                {
                    [TechnicianSchema.FullName] = name,
                    [TechnicianSchema.IsAvailable] = available
                },
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var (first, last, email) in Customers)
        {
            var customerId = await EnsureAsync(
                ContactSchema.EntityName,
                ContactSchema.Email,
                email,
                () => new Entity(ContactSchema.EntityName)
                {
                    ["firstname"] = first,
                    ["lastname"] = last,
                    [ContactSchema.Email] = email
                },
                cancellationToken).ConfigureAwait(false);

            var serialNumber = $"SN-{last.ToUpperInvariant()}-0001";

            await EnsureAsync(
                EquipmentSchema.EntityName,
                EquipmentSchema.SerialNumber,
                serialNumber,
                () => new Entity(EquipmentSchema.EntityName)
                {
                    [EquipmentSchema.SerialNumber] = serialNumber,
                    [EquipmentSchema.Customer] =
                        new EntityReference(ContactSchema.EntityName, customerId)
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Guid> EnsureAsync(
        string entityName,
        string matchColumn,
        string matchValue,
        Func<Entity> build,
        CancellationToken cancellationToken)
    {
        var query = new QueryExpression(entityName)
        {
            ColumnSet = new ColumnSet(false),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression(matchColumn, ConditionOperator.Equal, matchValue) }
            },
            TopCount = 1
        };

        var found = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        if (found.Entities.Count > 0)
        {
            ProvisioningLog.RecordExists(logger, entityName, matchValue);

            return found.Entities[0].Id;
        }

        var id = await client.CreateAsync(build(), cancellationToken).ConfigureAwait(false);

        ProvisioningLog.RecordCreated(logger, entityName, matchValue);

        return id;
    }
}
