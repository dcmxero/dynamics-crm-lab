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
  DynamicsCrmLab.Provisioning  creates the schema in an empty environment
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
| `StartWorkOrder` | records that the technician has started on it |
| `CloseWorkOrder` | finishes it and records what was done |

A listing answers with a page and a cursor rather than a bare array. The cursor
is opaque: it carries the paging cookie the platform issues, so a caller cannot
assemble one, and a stale one starts the listing again rather than failing. A
page number and a row count to skip would read a row twice, or miss one, as soon
as a job is raised between two requests.

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
| `WorkOrderLifecyclePlugin` | Create of `dcl_workorder`, and Update with filtering attribute `dcl_status`, PreOperation, synchronous, pre image of the stage, technician and resolution |
| `ClosedWorkOrderPlugin` | Update of `dcl_workorder`, no filtering attributes, PreOperation, synchronous, pre image of the stage and number |
| `ClosedWorkOrderLinesPlugin` | Create, Update and Delete of `dcl_workorderline`, PreOperation, synchronous, pre image `dcl_workorderid` |
| `WorkOrderPricingPlugin` | Create, Update and Delete of `dcl_workorderline`, PostOperation, synchronous, pre image `dcl_workorderid` |
| `WorkOrderClosedNotificationPlugin` | Update of `dcl_workorder`, filtering attribute `dcl_status`, PostOperation, **asynchronous**, pre image `dcl_status` and `dcl_number` |

The lifecycle rules run where the data lives, not only where the application
does. A bulk edit, a flow, or somebody in the maker portal writing straight to
the table would otherwise be able to close a job nobody started or reopen one
that is finished. The plug-in does not restate the rules: it rebuilds the stage
the job came from into the aggregate and asks the aggregate to make the move, so
the platform gives the same answer the API gives.

Guarding only the move turned out to leave three ways round it, all of which a
review reproduced against a live environment. A job created straight at a later
stage never moves at all. An update that empties the stage is not a move either,
and it stranded the record: every later change reads the stage it came from and
found nothing there. And an update that leaves a closed job closed while
emptying its resolution is not a stage change, so the stage guard was never
asked. Hence the second guard, which is asked about every column: closing a job
is a statement to the customer, and the record has to be shut to everything, not
only to going backwards.

For the same reason a charge is judged by both jobs an update concerns. Moved
off a closed job it would leave that job cheaper than what was agreed, and the
pricing step would dutifully write the smaller total.

One consequence is deliberate and worth knowing: a closed job can no longer be
deleted either, because deleting it cascades to its charges and they are shut.

Pricing is triggered by the line rather than by the job: a job is saved before
its lines exist, so a step on the job would add up an empty list. The
notification runs asynchronously because nobody saving the form needs to wait
for a follow-up task to exist.

Money is rounded halves away from zero, matching what the money column does,
because the store is the record. Rounding differently would make the same price
worth a different total depending on whether it arrived through the application
or was written straight to the table.

The sandbox loads the assembly it is given and nothing beside it, so the
plug-ins travel as a **plug-in package** carrying the domain with them. The
package name has to start with the publisher prefix.

Registration is code rather than clicks. `DynamicsCrmLab.Provisioning` uploads
the package, declares each step beside the plug-in it runs, and removes steps
the code no longer declares, so an environment can be rebuilt without anyone
remembering what was set in the registration tool.

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
dotnet user-secrets set "AzureAd:ClientSecret" "..."
dotnet run        # OpenAPI document at /openapi/v1.json
```

| Route | Purpose |
|---|---|
| `GET /api/customers?name=&take=` | customers whose name begins with what has been typed |
| `GET /api/customers/{id}/equipment` | the equipment registered to one customer |
| `GET /api/work-orders?status=&take=&cursor=` | one page of jobs that have reached a stage |
| `GET /api/work-orders/{id}` | one job in full |
| `POST /api/work-orders` | raise a job |
| `POST /api/work-orders/{id}/assignment` | put a technician on it |
| `POST /api/work-orders/{id}/start` | record that work has begun |
| `POST /api/work-orders/{id}/closure` | finish it |

Failures come back as problem details. A missing record is 404; a request that
is well formed but which the state of the job does not allow is 422; a request
that arrived after somebody else changed the same job is 409.

### Asking twice

Every use case reads a job, asks the aggregate to make a move and writes the
result back, and between the read and the write somebody else can have moved the
same job on. The version the row carried when it was read travels with the write
and the platform refuses it if the row has moved since, so the second of two
people closing the same job is told rather than quietly replacing the first
account of what was done.

Raising is different: there is nothing to have moved on, but a client that times
out and retries would raise the job twice. A request may name itself with an
`Idempotency-Key` header; the name is a unique key on the table, so the second
write is refused by the store rather than by a check a fast enough retry could
slip past, and the repeat is answered with the job the first request raised, as
200 rather than 201. A caller who names nothing gets a name of their own, so the
rule is the same for every request instead of something only some of them obey.

### Who is calling

Every route is closed. A request carries a bearer token from the tenant, and the
token has to carry the scope this API publishes: a token issued for some other
application is a valid token for somebody, but it is not consent to work with
these work orders. A call with no token is 401, a call with a token that does
not carry the scope is 403.

The API then reaches Dataverse as that person rather than as itself, by
exchanging their token for one the environment accepts. Their own security roles
decide what they may read and write, and the environment records the change
against their name instead of against one service account. The connection is
therefore opened per request: a shared one would hand the second caller the
first caller's access.

Two application registrations stand behind this. The API exposes a scope named
`access_as_user` and holds a delegated permission on Dynamics CRM; the client
holds a permission on that scope. The API alone has a secret, because the
exchange is what needs one.

## The web client

`web/` is an Angular 22 application in zoneless mode: standalone components,
signals for state, Material for the widgets, and one lazily loaded chunk per
screen.

```bash
cd web
npm install
npm start           # proxies /api to the backend on https://localhost:7134

# To sign in against your own tenant, copy
# src/environments/environment.local.example.ts to environment.local.ts,
# fill in your two registrations, then:
npm run start:tenant

npm run lint
npm test            # unit tests
npm run e2e         # Playwright, including an axe scan of every screen
npm run build
```

A job is raised by choosing, not by typing: the customer is searched for by name
and the units offered are that customer's own, because a job cannot be raised
against somebody else's equipment. A customer with exactly one unit has it
chosen for them.

Signing in happens before any of it. The client sends people to the tenant,
attaches the token it gets back to calls to its own API and to nothing else, and
shows who is signed in. The guard on the route is about not opening a page that
cannot load; what may actually be read and written is settled by the API.

Every failed call goes through one interceptor that reduces problem details to
a single shape, so no component digs around in an error body. A 422 is shown as
the job answering back rather than as a fault: a rule the current state does not
allow is not the same thing as something going wrong. A 401 asks for a fresh
sign-in, a 403 says to ask somebody for access, because trying again would not
help.

The Playwright suite stubs the API at the network layer, so it exercises the
client on its own, and it is served from a build with no tenant configured:
there is no API behind it for a token to be any use to. What the server does
with a request is covered by the API tests instead.

## The model-driven app

`Field Service` is the app the solution carries: work orders, equipment,
technicians and the customers behind them, with the lifecycle plug-ins holding
the rules underneath. The work order form binds the stage column to the code
component below rather than to the stock choice control.

An app, a form layout and a site map are the parts of this that are made by
clicking rather than by writing, which is what the unpacked solution in
`solutions/` is for: they are reviewed and moved between environments as files
like everything else.

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

## Setting up an environment

The tables are created by a tool rather than clicked together in the maker
portal. A schema that exists only as a sequence of clicks cannot be reviewed,
repeated on a second environment, or rebuilt after someone deletes a column.

```bash
cd src/DynamicsCrmLab.Provisioning
dotnet user-secrets set "Dataverse:Url" "https://your-org.crm4.dynamics.com"

dotnet run              # publisher, solution, tables, columns, relationships, keys
dotnet run -- --seed    # and a couple of customers, units and technicians
```

Every step checks before it writes, so running it again does nothing.

What it creates:

| Table | Columns |
|---|---|
| `dcl_equipment` | serial number, customer lookup |
| `dcl_technician` | name, available |
| `dcl_workorder` | number, stage, resolution, total price, three lookups, alternate key on the number |
| `dcl_workorderline` | description, quantity, unit price, work order lookup |

The stage values are stated rather than left to the publisher option value
prefix, because they have to match the domain enum.

What still has to be done in the portal, because there is no sane way to do it
from code: business rules, the business process flow, security roles and the
model-driven app.

## Getting the solution in and out of Dataverse

A solution zip is opaque to git, so the unpacked form is what lives here, under
`solutions/DynamicsCrmLab`. It carries the tables, the PCF control, the plug-in
package and the registered steps with their images, which is what makes an
import reproducible without anyone repeating a sequence of clicks.

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

Two things do not travel in the solution and have to exist in the target
environment before anything works: an application user for the registration the
tools sign in as, with a security role, and the data itself. A managed import
carrying the tables, the plug-in package and its steps has been through a
freshly created environment and the rules refuse there what they refuse at
home, which is the point of shipping the rules with the schema.

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
dotnet run -- seed          # customers, equipment and technicians to show it with
dotnet run -- raise <customerId> <equipmentId> "Technician labour" 2 45 [requestKey]
dotnet run -- assign <workOrderId>
dotnet run -- start <workOrderId>
dotnet run -- close <workOrderId> "Replaced the compressor seal."
```

The first run opens a browser to sign in and caches the token under
`%LOCALAPPDATA%/DynamicsCrmLab`, which every tool here shares, so signing in once
covers the console application, the API and the provisioning tool. For a service
or a pipeline, switch to an Entra ID application registration instead:

```bash
dotnet user-secrets set "Dataverse:AuthMode" "ClientSecret"
dotnet user-secrets set "Dataverse:ClientId" "..."
dotnet user-secrets set "Dataverse:ClientSecret" "..."
```

That registration also needs an application user with a security role in the
target environment, otherwise it can reach the API but sees no data.

Two application registrations stand behind the API itself, and nothing in this
repository names them: the API reads `AzureAd:TenantId`, `AzureAd:ClientId`,
`AzureAd:Audience` and `AzureAd:ClientSecret` from the same secret store, and the
client reads its own from an uncommitted `environment.local.ts`. A checkout
carries neither, so it runs unsigned-in until somebody points it at a tenant of
their own.

A client secret expires. When the API starts answering every call with a failure
to acquire a token, that is usually all it is.

## Conventions

- shared build settings live in `Directory.Build.props`
- package versions are managed centrally in `Directory.Packages.props`
- .NET analysers run at `latest-all` and warnings fail the build
- public members carry XML documentation
- work that reaches a store is asynchronous and takes a `CancellationToken`
