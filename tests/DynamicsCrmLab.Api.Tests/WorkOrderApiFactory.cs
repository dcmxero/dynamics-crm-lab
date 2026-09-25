using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Domain.Customers;
using DynamicsCrmLab.Domain.Equipments;
using DynamicsCrmLab.Domain.Technicians;
using DynamicsCrmLab.Domain.WorkOrders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DynamicsCrmLab.Api.Tests;

/// <summary>
/// Hosts the API with the Dataverse repositories swapped for in-memory ones, so
/// the routes, the contract and the status codes can be exercised without an
/// environment.
/// </summary>
internal sealed class WorkOrderApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://tests.dynamicscrmlab.invalid/";
    private const string Audience = "api://tests";

    /// <summary>
    /// Signs the tokens the tests present. A symmetric key keeps the tests off
    /// the network: they still travel the real bearer token pipeline, they just
    /// trust a key the test owns instead of a tenant's published one.
    /// </summary>
    private static readonly SymmetricSecurityKey SigningKey =
        new(RandomNumberGenerator.GetBytes(32)) { KeyId = "tests" };

    public InMemoryStore Store { get; } = new();

    /// <summary>
    /// Creates a client that calls as somebody who consented to the API.
    /// </summary>
    /// <param name="scope">
    /// The scope to put in the token, or <see langword="null"/> to leave it out.
    /// </param>
    /// <returns>A client that sends the bearer token on every request.</returns>
    public HttpClient CreateClientForCaller(string? scope = CallerAuthentication.Scope)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenFor(scope));

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dataverse:Url", "https://contoso.crm4.dynamics.com");

        builder.ConfigureServices(services =>
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Without this the handler would try to download the tenant's
                // signing keys, which a test has no business doing.
                options.Configuration = new OpenIdConnectConfiguration();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = SigningKey
                };
            });

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

    private static string TokenFor(string? scope)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["oid"] = "6f9b1c5e-0b2a-4a2f-8f8e-9a1f0c3d5e77",
            ["name"] = "Peter Kovac"
        };

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims["scp"] = scope;
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}

internal sealed class InMemoryStore
    : IWorkOrderRepository, ICustomerRepository, IEquipmentRepository, ITechnicianRepository
{
    private readonly Dictionary<Guid, WorkOrder> _workOrders = [];
    private readonly HashSet<Guid> _raced = [];
    private readonly Dictionary<Guid, Customer> _customers = [];
    private readonly Dictionary<Guid, Equipment> _equipment = [];
    private readonly Dictionary<Guid, Technician> _technicians = [];

    public Customer AddCustomer(string name = "Acme Foods")
    {
        var customer = new Customer(Guid.NewGuid(), name, "service@acme.example");
        _customers[customer.Id] = customer;
        return customer;
    }

    public Equipment AddEquipment(Guid customerId, string serialNumber = "SN-0001")
    {
        var equipment = new Equipment(Guid.NewGuid(), serialNumber, customerId);
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

    /// <summary>
    /// Makes the next write to this job behave as though somebody else had
    /// changed it first.
    /// </summary>
    public void ChangedByEveryoneElse(Guid workOrderId) => _raced.Add(workOrderId);

    Task<Guid> IWorkOrderRepository.AddAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        _workOrders[workOrder.Id] = workOrder;
        return Task.FromResult(workOrder.Id);
    }

    Task<WorkOrder?> IWorkOrderRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_workOrders.GetValueOrDefault(id));

    Task IWorkOrderRepository.UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (_raced.Remove(workOrder.Id))
        {
            throw new ConcurrencyException();
        }

        _workOrders[workOrder.Id] = workOrder;
        return Task.CompletedTask;
    }

    Task<Page<WorkOrder>> IWorkOrderRepository.ListByStatusAsync(
        WorkOrderStatus status,
        int maxCount,
        string? cursor,
        CancellationToken cancellationToken)
    {
        // The cursor is the count already handed out, which is all a list in
        // memory needs to carry on from.
        var alreadyRead = int.TryParse(cursor, out var parsed) ? parsed : 0;

        var matching = _workOrders.Values.Where(order => order.Status == status).ToList();
        IReadOnlyList<WorkOrder> page = [.. matching.Skip(alreadyRead).Take(maxCount)];

        var next = alreadyRead + page.Count < matching.Count
            ? (alreadyRead + page.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        return Task.FromResult(new Page<WorkOrder>(page, next));
    }

    Task<Customer?> ICustomerRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.GetValueOrDefault(id));

    Task<IReadOnlyList<Customer>> ICustomerRepository.SearchAsync(
        string? startingWith,
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Customer> matching =
        [
            .. _customers.Values
                .Where(customer => string.IsNullOrWhiteSpace(startingWith)
                                   || customer.Name.StartsWith(startingWith, StringComparison.OrdinalIgnoreCase))
                .OrderBy(customer => customer.Name, StringComparer.Ordinal)
                .Take(maxCount)
        ];

        return Task.FromResult(matching);
    }

    Task<Equipment?> IEquipmentRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_equipment.GetValueOrDefault(id));

    Task<IReadOnlyList<Equipment>> IEquipmentRepository.ListForCustomerAsync(
        Guid customerId,
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Equipment> owned =
        [
            .. _equipment.Values
                .Where(equipment => equipment.CustomerId == customerId)
                .OrderBy(equipment => equipment.SerialNumber, StringComparer.Ordinal)
                .Take(maxCount)
        ];

        return Task.FromResult(owned);
    }

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
