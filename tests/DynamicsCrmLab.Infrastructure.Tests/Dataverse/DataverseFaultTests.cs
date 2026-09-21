using System.ServiceModel;
using DynamicsCrmLab.Infrastructure.Dataverse;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DynamicsCrmLab.Infrastructure.Tests.Dataverse;

public sealed class DataverseFaultTests
{
    [Fact]
    public void IsRecordNotFound_RecognisesTheCodeThePlatformUsesForAMissingRow()
    {
        var fault = Fault(unchecked((int)0x80040217));

        DataverseFault.IsRecordNotFound(fault).Should().BeTrue();
    }

    [Fact]
    public void IsRecordNotFound_LeavesEveryOtherFailureAlone()
    {
        // 0x80040265 is a plug-in reporting a business rule, which must keep
        // travelling up rather than be read as a missing row.
        var fault = Fault(unchecked((int)0x80040265));

        DataverseFault.IsRecordNotFound(fault).Should().BeFalse();
    }

    [Fact]
    public void IsRecordNotFound_IsFalseWhenThereIsNoFault()
    {
        DataverseFault.IsRecordNotFound(null).Should().BeFalse();
    }

    private static FaultException<OrganizationServiceFault> Fault(int errorCode) =>
        new(new OrganizationServiceFault { ErrorCode = errorCode }, "Dataverse");
}
