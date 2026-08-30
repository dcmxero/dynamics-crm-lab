using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Customers;
using DynamicsCrmLab.Domain.Equipments;

namespace DynamicsCrmLab.Application.Tests.Fakes;

internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<Guid, Customer> _stored = [];

    public Customer Add(string name = "Acme Foods")
    {
        var customer = new Customer(Guid.NewGuid(), name, "service@acme.example");
        _stored[customer.Id] = customer;
        return customer;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stored.GetValueOrDefault(id));
}

internal sealed class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly Dictionary<Guid, Equipment> _stored = [];

    public Equipment Add(Guid customerId, string serialNumber = "SN-0001")
    {
        var equipment = new Equipment(Guid.NewGuid(), serialNumber, customerId);
        _stored[equipment.Id] = equipment;
        return equipment;
    }

    public Task<Equipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stored.GetValueOrDefault(id));
}
