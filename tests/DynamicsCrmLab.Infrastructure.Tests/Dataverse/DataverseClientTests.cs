using DynamicsCrmLab.Infrastructure.Dataverse;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class DataverseClientTests
{
    [Fact]
    public async Task SigningInAsTheCaller_SaysSoWhenNobodySuppliesTheirToken()
    {
        // Misconfiguration should read as misconfiguration. Without this the
        // first request would fail somewhere inside the connection instead.
        using var client = new DataverseClient(Settings(DataverseAuthMode.OnBehalfOf), accessToken: null);

        var connecting = async () => await client.RetrieveAsync("account", Guid.NewGuid(), new ColumnSet());

        await connecting.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*token*");
    }

    [Fact]
    public async Task SigningInWithASecret_SaysWhatIsMissing()
    {
        using var client = new DataverseClient(Settings(DataverseAuthMode.ClientSecret));

        var connecting = async () => await client.RetrieveAsync("account", Guid.NewGuid(), new ColumnSet());

        await connecting.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dataverse:ClientId*");
    }

    private static IOptions<DataverseOptions> Settings(DataverseAuthMode authMode) =>
        Options.Create(new DataverseOptions
        {
            Url = new Uri("https://contoso.crm4.dynamics.com"),
            AuthMode = authMode
        });
}
