using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DynamicsCrmLab.Provisioning.Tests;

public sealed class OrganizationLanguageTests
{
    [Fact]
    public async Task BaseCodeAsync_ReturnsTheLanguageTheEnvironmentWasCreatedWith()
    {
        var organization = new Entity("organization") { ["languagecode"] = 1051 };
        var client = new StubDataverseClient(new EntityCollection { Entities = { organization } });

        var code = await new OrganizationLanguage(client).BaseCodeAsync();

        code.Should().Be(1051);
    }

    [Fact]
    public async Task BaseCodeAsync_AsksOnlyForTheLanguageColumnOfASingleRow()
    {
        var organization = new Entity("organization") { ["languagecode"] = 1033 };
        var client = new StubDataverseClient(new EntityCollection { Entities = { organization } });

        await new OrganizationLanguage(client).BaseCodeAsync();

        var query = client.Query.Should().BeOfType<QueryExpression>().Subject;
        query.EntityName.Should().Be("organization");
        query.TopCount.Should().Be(1);
        query.ColumnSet.Columns.Should().ContainSingle().Which.Should().Be("languagecode");
    }

    [Fact]
    public async Task BaseCodeAsync_FailsWhenTheEnvironmentReportsNoLanguage()
    {
        var client = new StubDataverseClient(new EntityCollection());

        var reading = async () => await new OrganizationLanguage(client).BaseCodeAsync();

        await reading.Should().ThrowAsync<InvalidOperationException>().WithMessage("*base language*");
    }
}
