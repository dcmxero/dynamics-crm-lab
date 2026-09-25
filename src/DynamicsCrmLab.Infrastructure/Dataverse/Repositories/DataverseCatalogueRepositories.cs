using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Customers;
using DynamicsCrmLab.Domain.Equipments;
using DynamicsCrmLab.Domain.Technicians;
using DynamicsCrmLab.Schema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Infrastructure.Dataverse.Repositories;

/// <summary>
/// Reads customers from Dataverse.
/// </summary>
/// <param name="client">The connection rows are read from.</param>
public sealed class DataverseCustomerRepository(IDataverseClient client) : ICustomerRepository
{
    /// <inheritdoc/>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await client
            .RetrieveAsync(
                ContactSchema.EntityName,
                id,
                new ColumnSet([.. ContactSchema.ReadColumns]),
                cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : new Customer(
                record.Id,
                record.GetAttributeValue<string>(ContactSchema.FullName) ?? "(unnamed)",
                record.GetAttributeValue<string>(ContactSchema.Email) ?? string.Empty);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Customer>> SearchAsync(
        string? startingWith,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ContactSchema.EntityName)
        {
            ColumnSet = new ColumnSet([.. ContactSchema.ReadColumns]),
            TopCount = maxCount,
            Orders = { new OrderExpression(ContactSchema.FullName, OrderType.Ascending) }
        };

        if (!string.IsNullOrWhiteSpace(startingWith))
        {
            // BeginsWith rather than Like: the caller is typing a name, not
            // writing a pattern, so a % they happen to type is a character.
            query.Criteria.AddCondition(
                ContactSchema.FullName,
                ConditionOperator.BeginsWith,
                startingWith);
        }

        var page = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        return [.. page.Entities.Select(record => new Customer(
            record.Id,
            record.GetAttributeValue<string>(ContactSchema.FullName) ?? "(unnamed)",
            record.GetAttributeValue<string>(ContactSchema.Email) ?? string.Empty))];
    }
}

/// <summary>
/// Reads equipment from Dataverse.
/// </summary>
/// <param name="client">The connection rows are read from.</param>
public sealed class DataverseEquipmentRepository(IDataverseClient client) : IEquipmentRepository
{
    /// <inheritdoc/>
    public async Task<Equipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await client
            .RetrieveAsync(
                EquipmentSchema.EntityName,
                id,
                new ColumnSet([.. EquipmentSchema.ReadColumns]),
                cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : new Equipment(
                record.Id,
                record.GetAttributeValue<string>(EquipmentSchema.SerialNumber) ?? "(no serial number)",
                record.GetAttributeValue<EntityReference>(EquipmentSchema.Customer)?.Id ?? Guid.Empty);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Equipment>> ListForCustomerAsync(
        Guid customerId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EquipmentSchema.EntityName)
        {
            ColumnSet = new ColumnSet([.. EquipmentSchema.ReadColumns]),
            TopCount = maxCount,
            Orders = { new OrderExpression(EquipmentSchema.SerialNumber, OrderType.Ascending) },
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression(EquipmentSchema.Customer, ConditionOperator.Equal, customerId)
                }
            }
        };

        var page = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        return [.. page.Entities.Select(record => new Equipment(
            record.Id,
            record.GetAttributeValue<string>(EquipmentSchema.SerialNumber) ?? "(no serial number)",
            record.GetAttributeValue<EntityReference>(EquipmentSchema.Customer)?.Id ?? Guid.Empty))];
    }
}

/// <summary>
/// Reads technicians from Dataverse.
/// </summary>
/// <param name="client">The connection rows are read from.</param>
public sealed class DataverseTechnicianRepository(IDataverseClient client) : ITechnicianRepository
{
    /// <inheritdoc/>
    public async Task<Technician?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await client
            .RetrieveAsync(
                TechnicianSchema.EntityName,
                id,
                new ColumnSet([.. TechnicianSchema.ReadColumns]),
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : ToDomain(record);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Technician>> ListAvailableAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(TechnicianSchema.EntityName)
        {
            ColumnSet = new ColumnSet([.. TechnicianSchema.ReadColumns]),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(TechnicianSchema.IsAvailable, ConditionOperator.Equal, true)
                }
            },
            TopCount = maxCount
        };

        var page = await client.RetrieveMultipleAsync(query, cancellationToken).ConfigureAwait(false);

        return [.. page.Entities.Select(ToDomain)];
    }

    private static Technician ToDomain(Entity record) =>
        new(record.Id,
            record.GetAttributeValue<string>(TechnicianSchema.FullName) ?? "(unnamed)",
            record.GetAttributeValue<bool>(TechnicianSchema.IsAvailable));
}
