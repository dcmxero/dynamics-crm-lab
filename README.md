# dynamics-crm-lab

Learning project for Microsoft Dataverse / Dynamics 365 CRM development.

The modelled domain is a field service company: customers, equipment,
work orders and technicians.

## Layout

```
src/
  DynamicsCrmLab.Domain        business rules, no dependencies
  DynamicsCrmLab.Application   use cases and the ports they talk to
  DynamicsCrmLab.Infrastructure  Dataverse connection, mapping, repositories
  DynamicsCrmLab.Cli           console front end
tests/
  DynamicsCrmLab.Domain.Tests
  DynamicsCrmLab.Application.Tests
  DynamicsCrmLab.Infrastructure.Tests
```

The domain layer references nothing at all. Rules live inside the entities, so
a work order cannot be put into a state the business would not allow, and none
of it needs Dataverse to run.

The application layer orchestrates those rules behind repository interfaces it
defines itself. Nothing in it knows where the records are actually kept, which
is why the use cases can be tested against in-memory stores.

A broken business rule comes back as a failed `Result<T>` rather than an
exception - it is an ordinary answer to the request, not a program failure.

## Use cases

| Use case | What it does |
|---|---|
| `RaiseWorkOrder` | opens a job against a customer and their equipment |
| `AssignWorkOrder` | puts a technician on it, picking one when none is named |
| `CloseWorkOrder` | finishes it and records what was done |

## Dataverse

Tables carry the `dcl_` publisher prefix: `dcl_workorder`, `dcl_workorderline`,
`dcl_equipment` and `dcl_technician`. Customers use the stock `contact` table.

The repositories are the only place that knows any of that. They ask for the
columns they need rather than all of them, carry the paging cookie between
pages, and read the lines of a whole page in one query.

A free environment to point them at: [Power Apps Developer Plan](https://aka.ms/PowerAppsDevPlan).

## Requirements

- .NET SDK 10

## Building and testing

```bash
dotnet build
dotnet test
```

The tests need no environment, no configuration and no connection.

## Running against an environment

Nothing about the environment is committed. Point the tool at yours:

```bash
cd src/DynamicsCrmLab.Cli
dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"

dotnet run -- whoami
dotnet run -- raise <customerId> <equipmentId> "Technician labour" 2 45
dotnet run -- assign <workOrderId>
dotnet run -- close <workOrderId> "Replaced the compressor seal."
```

The first run opens a browser to sign in and caches the token, so later runs do
not ask again. For a service or a pipeline, switch to an Entra ID application
registration instead:

```bash
dotnet user-secrets set "Dataverse:AuthMode" "ClientSecret"
dotnet user-secrets set "Dataverse:ClientId" "..."
dotnet user-secrets set "Dataverse:ClientSecret" "..."
```

That registration also needs an application user with a security role in the
target environment, otherwise it can reach the API but sees no data.

## Conventions

- shared build settings live in `Directory.Build.props`
- package versions are managed centrally in `Directory.Packages.props`
- .NET analysers run at `latest-all` and warnings fail the build
- public members carry XML documentation
- work that reaches a store is asynchronous and takes a `CancellationToken`
