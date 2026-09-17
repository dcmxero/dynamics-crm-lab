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
  DynamicsCrmLab.Plugins       plug-ins that run inside Dataverse
  DynamicsCrmLab.Schema        Dataverse logical names shared by both sides
pcf/
  WorkOrderStatusTrack         status track control for model-driven forms
tests/
  DynamicsCrmLab.Domain.Tests
  DynamicsCrmLab.Application.Tests
  DynamicsCrmLab.Infrastructure.Tests
  DynamicsCrmLab.Plugins.Tests
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

## Plug-ins

Dataverse runs plug-ins on .NET Framework 4.6.2, so the domain is built for both
`net10.0` and `netstandard2.0` and the plug-ins use the same rules as the console
application rather than a second copy that would drift.

| Plug-in | Registration |
|---|---|
| `WorkOrderPricingPlugin` | Create and Update of `dcl_workorder`, PreOperation, synchronous |
| `WorkOrderClosedNotificationPlugin` | Update of `dcl_workorder`, filtering attribute `dcl_status`, PostOperation, **asynchronous**, pre image `dcl_status` and `dcl_number` |

Pricing runs in PreOperation and writes onto the target, so the platform saves
the value with the rest of the record instead of a second update that could
retrigger the plug-in. The notification runs asynchronously because nobody
saving the form needs to wait for a follow-up task to exist.

Tests run against an in-memory Dataverse, so they need no environment either.

## Custom UI

`pcf/WorkOrderStatusTrack` is a Power Apps component framework control that
shows the lifecycle of a job on the form. The stage logic is a plain module with
no React in it and is unit tested; the component only draws what it is given.

```bash
cd pcf/WorkOrderStatusTrack
npm install
npm test
npm run build
npm start        # harness in the browser
```

Reaching into the form DOM from JavaScript would be unsupported and would break
on any platform update, which is what a control like this avoids.

## Requirements

- .NET SDK 10
- Node.js 20 or newer, for the PCF control

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
