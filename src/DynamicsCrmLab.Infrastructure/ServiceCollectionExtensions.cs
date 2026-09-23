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
    /// <param name="callerToken">
    /// How to obtain the token of whoever is holding the request. Needed only by
    /// <see cref="DataverseAuthMode.OnBehalfOf"/>, and only the host can answer
    /// it, so it is asked for here rather than assumed.
    /// </param>
    /// <returns>The same container, to allow chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the settings say to sign in as the caller and no way of
    /// obtaining their token was supplied.
    /// </exception>
    public static IServiceCollection AddDataverse(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IServiceProvider, IDataverseAccessToken>? callerToken = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DataverseOptions>()
            .Bind(configuration.GetSection(DataverseOptions.SectionName))
            .ValidateDataAnnotations();

        var authMode = configuration
            .GetSection(DataverseOptions.SectionName)
            .GetValue<DataverseAuthMode>(nameof(DataverseOptions.AuthMode));

        if (authMode is DataverseAuthMode.OnBehalfOf)
        {
            services.AddScoped(callerToken ?? throw new InvalidOperationException(
                "Signing in as the caller needs the host to supply their token."));

            // A connection carries the identity it signed in with, so one shared
            // connection would hand the second caller the first caller's access.
            services.AddScoped<IDataverseClient, DataverseClient>();
        }
        else
        {
            // One identity for the whole application, and the connection is
            // expensive to open, so it is opened once.
            services.AddSingleton<IDataverseClient, DataverseClient>();
        }

        // The currencies of an environment change when the environment is set
        // up, so what is read stays read, whoever read it.
        services.AddSingleton<CurrencyCache>();
        services.AddScoped<DataverseCurrencies>();

        services.AddScoped<IWorkOrderRepository, DataverseWorkOrderRepository>();
        services.AddScoped<ICustomerRepository, DataverseCustomerRepository>();
        services.AddScoped<IEquipmentRepository, DataverseEquipmentRepository>();
        services.AddScoped<ITechnicianRepository, DataverseTechnicianRepository>();

        return services;
    }
}
