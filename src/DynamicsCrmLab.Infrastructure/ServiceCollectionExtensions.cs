using DynamicsCrmLab.Application.Abstractions;
using DynamicsCrmLab.Application.UseCases.AssignWorkOrder;
using DynamicsCrmLab.Application.UseCases.CloseWorkOrder;
using DynamicsCrmLab.Application.UseCases.RaiseWorkOrder;
using DynamicsCrmLab.Application.UseCases.StartWorkOrder;
using DynamicsCrmLab.Application.UseCases.ViewWorkOrders;
using DynamicsCrmLab.Infrastructure.Dataverse;
using DynamicsCrmLab.Infrastructure.Dataverse.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicsCrmLab.Infrastructure;

/// <summary>
/// Provides the registrations a host needs to run the use cases.
/// </summary>
/// <remarks>
/// The host calls these two methods and stays unaware of the concrete types
/// behind the ports.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the use case handlers.
    /// </summary>
    /// <param name="services">The container to add to.</param>
    /// <returns>The same container, to allow chaining.</returns>
    public static IServiceCollection AddWorkOrderUseCases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<RaiseWorkOrderHandler>();
        services.AddScoped<AssignWorkOrderHandler>();
        services.AddScoped<StartWorkOrderHandler>();
        services.AddScoped<CloseWorkOrderHandler>();
        services.AddScoped<ListWorkOrdersHandler>();
        services.AddScoped<GetWorkOrderHandler>();

        return services;
    }

    /// <summary>
    /// Registers the Dataverse connection and the repositories behind the ports.
    /// </summary>
    /// <param name="services">The container to add to.</param>
    /// <param name="configuration">The configuration the settings are bound from.</param>
    /// <returns>The same container, to allow chaining.</returns>
    public static IServiceCollection AddDataverse(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DataverseOptions>()
            .Bind(configuration.GetSection(DataverseOptions.SectionName))
            .ValidateDataAnnotations();

        // The connection is expensive to open and safe to share, so it is opened once.
        services.AddSingleton<IDataverseClient, DataverseClient>();

        services.AddScoped<IWorkOrderRepository, DataverseWorkOrderRepository>();
        services.AddScoped<ICustomerRepository, DataverseCustomerRepository>();
        services.AddScoped<IEquipmentRepository, DataverseEquipmentRepository>();
        services.AddScoped<ITechnicianRepository, DataverseTechnicianRepository>();

        return services;
    }
}
