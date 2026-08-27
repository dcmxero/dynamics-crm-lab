# dynamics-crm-lab

Learning project for Microsoft Dataverse / Dynamics 365 CRM development.

The modelled domain is a field service company: customers, equipment,
work orders and technicians.

## Layout

```
src/
  DynamicsCrmLab.Domain     business rules, no dependencies
tests/
  DynamicsCrmLab.Domain.Tests
```

The domain layer references nothing at all. Rules live inside the entities, so
a work order cannot be put into a state the business would not allow, and none
of it needs Dataverse to run.

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
