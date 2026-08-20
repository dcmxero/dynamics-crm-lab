# dynamics-crm-lab

Learning project for Microsoft Dataverse / Dynamics 365 CRM development.

The modelled domain is a field service company: customers, equipment,
work orders and technicians.

## Requirements

- .NET SDK 10

## Conventions

- shared build settings live in `Directory.Build.props`
- .NET analysers run at `latest-all` and warnings fail the build
- public members carry XML documentation
