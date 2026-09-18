using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Customers;
using DynamicsCrmLab.Domain.Equipments;
using DynamicsCrmLab.Domain.Technicians;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DynamicsCrmLab.Api.Tests;

/// <summary>
/// Hosts the API with the Dataverse repositories swapped for in-memory ones, so
/// the routes, the contract and the status codes can be exercised without an
/// environment.
/// </summary>
internal sealed class WorkOrderApiFactory : WebApplicationFactory<Program>
{
    public InMemoryStore Store { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dataverse:Url", "https://contoso.crm4.dynamics.com");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWorkOrderRepository>();
            services.RemoveAll<ICustomerRepository>();
            services.RemoveAll<IEquipmentRepository>();
            services.RemoveAll<ITechnicianRepository>();

            services.AddSingleton<IWorkOrderRepository>(Store);
            services.AddSingleton<ICustomerRepository>(Store);
            services.AddSingleton<IEquipmentRepository>(Store);
            services.AddSingleton<ITechnicianRepository>(Store);
        });
    }
}

internal sealed class InMemoryStore
    : IWorkOrderRepository, ICustomerRepository, IEquipmentRepository, ITechnicianRepository
{
    private readonly Dictionary<Guid, WorkOrder> _workOrders = [];
    private readonly Dictionary<Guid, Customer> _customers = [];
    private readonly Dictionary<Guid, Equipment> _equipment = [];
    private readonly Dictionary<Guid, Technician> _technicians = [];

    public Customer AddCustomer()
    {
        var customer = new Customer(Guid.NewGuid(), "Acme Foods", "service@acme.example");
        _customers[customer.Id] = customer;
        return customer;
    }

    public Equipment AddEquipment(Guid customerId)
    {
        var equipment = new Equipment(Guid.NewGuid(), "SN-0001", customerId);
        _equipment[equipment.Id] = equipment;
        return equipment;
    }

    public Technician AddTechnician(bool isAvailable = true)
    {
        var technician = new Technician(Guid.NewGuid(), "Peter Kovac", isAvailable);
        _technicians[technician.Id] = technician;
        return technician;
    }

    public WorkOrder AddWorkOrder(WorkOrder workOrder)
    {
        _workOrders[workOrder.Id] = workOrder;
        return workOrder;
    }

    Task<Guid> IWorkOrderRepository.AddAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        _workOrders[workOrder.Id] = workOrder;
        return Task.FromResult(workOrder.Id);
    }

    Task<WorkOrder?> IWorkOrderRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_workOrders.GetValueOrDefault(id));

    Task IWorkOrderRepository.UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        _workOrders[workOrder.Id] = workOrder;
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<WorkOrder>> IWorkOrderRepository.ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkOrder> matching =
            [.. _workOrders.Values.Where(order => order.Status == status).Take(maxCount)];

        return Task.FromResult(matching);
    }

    Task<Customer?> ICustomerRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.GetValueOrDefault(id));

    Task<Equipment?> IEquipmentRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_equipment.GetValueOrDefault(id));

    Task<Technician?> ITechnicianRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_technicians.GetValueOrDefault(id));

    Task<IReadOnlyList<Technician>> ITechnicianRepository.ListAvailableAsync(
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Technician> available =
            [.. _technicians.Values.Where(technician => technician.IsAvailable).Take(maxCount)];

        return Task.FromResult(available);
    }
}
