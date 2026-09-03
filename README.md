# dynamics-crm-lab

Learning project for Microsoft Dataverse / Dynamics 365 CRM development.

The modelled domain is a field service company: customers, equipment,
work orders and technicians.

## Layout

```
src/
  DynamicsCrmLab.Domain        business rules, no dependencies
  DynamicsCrmLab.Application   use cases and the ports they talk to
tests/
  DynamicsCrmLab.Domain.Tests
  DynamicsCrmLab.Application.Tests
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

## Requirements

- .NET SDK 10

## Building and testing

```bash
dotnet build
dotnet test
```

The tests need no environment, no configuration and no connection.

## Conventions

- shared build settings live in `Directory.Build.props`
- package versions are managed centrally in `Directory.Packages.props`
- .NET analysers run at `latest-all` and warnings fail the build
- public members carry XML documentation
- work that reaches a store is asynchronous and takes a `CancellationToken`
