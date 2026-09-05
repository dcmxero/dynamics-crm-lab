using DynamicsCrmLab.Infrastructure.Dataverse;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class DataverseConnectionStringBuilderTests
{
    private static readonly Uri EnvironmentUrl = new("https://contoso.crm4.dynamics.com");

    [Fact]
    public void Build_UsesInteractiveSignInByDefault()
    {
        var connectionString = DataverseConnectionStringBuilder.Build(new DataverseOptions
        {
            Url = EnvironmentUrl
        });

        connectionString.Should().Contain("AuthType=OAuth");
        connectionString.Should().Contain("contoso.crm4.dynamics.com");
    }

    [Fact]
    public void Build_CachesTheTokenSoSignInIsNotRepeated()
    {
        var connectionString = DataverseConnectionStringBuilder.Build(new DataverseOptions
        {
            Url = EnvironmentUrl,
            TokenCachePath = "./.cache"
        });

        connectionString.Should().Contain("TokenCacheStorePath=./.cache");
    }

    [Fact]
    public void Build_UsesTheApplicationRegistrationWhenAskedTo()
    {
        var connectionString = DataverseConnectionStringBuilder.Build(new DataverseOptions
        {
            Url = EnvironmentUrl,
            AuthMode = DataverseAuthMode.ClientSecret,
            ClientId = "11111111-1111-1111-1111-111111111111",
            ClientSecret = "s3cret"
        });

        connectionString.Should().StartWith("AuthType=ClientSecret");
        connectionString.Should().Contain("ClientId=11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Build_RejectsClientSecretModeWithoutCredentials()
    {
        var act = () => DataverseConnectionStringBuilder.Build(new DataverseOptions
        {
            Url = EnvironmentUrl,
            AuthMode = DataverseAuthMode.ClientSecret
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*ClientId*");
    }

    [Fact]
    public void Build_RejectsMissingEnvironmentAddress()
    {
        var act = () => DataverseConnectionStringBuilder.Build(new DataverseOptions());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Dataverse:Url*");
    }
}
