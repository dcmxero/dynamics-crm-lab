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
  DynamicsCrmLab.Api           HTTP API the web client talks to
  DynamicsCrmLab.Cli           console front end
  DynamicsCrmLab.Plugins       plug-ins that run inside Dataverse
  DynamicsCrmLab.Schema        Dataverse logical names shared by both sides
web/                           Angular client
pcf/
  WorkOrderStatusTrack         status track control for model-driven forms
solutions/                     the Dataverse solution, unpacked
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

## The API

The web client does not talk to Dataverse directly. It talks to
`DynamicsCrmLab.Api`, which reuses the same use cases as the console
application.

That choice is deliberate. A browser holding a Dataverse token holds the full
permissions of the signed-in user, the OData shapes would have to be mapped a
second time in TypeScript, and the rules would either be duplicated there or
lost. Keeping a backend means the token stays on the server and the rules stay
in one place.

```bash
cd src/DynamicsCrmLab.Api
dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"
dotnet run        # OpenAPI document at /openapi/v1.json
```

| Route | Purpose |
|---|---|
| `GET /api/work-orders?status=&take=` | jobs that have reached a stage |
| `GET /api/work-orders/{id}` | one job in full |
| `POST /api/work-orders` | raise a job |
| `POST /api/work-orders/{id}/assignment` | put a technician on it |
| `POST /api/work-orders/{id}/closure` | finish it |

Failures come back as problem details. A missing record is 404; a request that
is well formed but which the state of the job does not allow is 422.

## The web client

`web/` is an Angular 22 application in zoneless mode: standalone components,
signals for state, Material for the widgets, and one lazily loaded chunk per
screen.

```bash
cd web
npm install
npm start           # proxies /api to the backend on https://localhost:7134

npm run lint
npm test            # unit tests
npm run e2e         # Playwright, including an axe scan of every screen
npm run build
```

Every failed call goes through one interceptor that reduces problem details to
a single shape, so no component digs around in an error body. A 422 is shown as
the job answering back rather than as a fault: a rule the current state does not
allow is not the same thing as something going wrong.

The Playwright suite stubs the API at the network layer, so it exercises the
client on its own. What the server does with a request is covered by the API
tests instead.

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

## Getting the solution in and out of Dataverse

A solution zip is opaque to git, so the unpacked form is what lives here:

```bash
pac auth create --environment https://your-org.crm4.dynamics.com

pac solution export --path ./out/DynamicsCrmLab.zip --name DynamicsCrmLab --managed false
pac solution unpack --zipfile ./out/DynamicsCrmLab.zip --folder ./solutions/DynamicsCrmLab --packagetype Unmanaged
```

Development happens against an **unmanaged** solution. Everything downstream
gets a **managed** build, which cannot be edited in place and can be removed
cleanly. Nothing is changed by hand in a downstream environment.

Values that differ per environment - addresses, keys, switches - belong in
environment variables rather than in the solution, and connections used by flows
in connection references.

Two workflows cover this:

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | push and pull request | builds and tests the .NET solution and the PCF control |
| `solution-release.yml` | manual | exports from development, commits the unpacked form, imports the managed build into test |

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
