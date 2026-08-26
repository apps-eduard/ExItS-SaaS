using ExItS.PinoyLoanManager.Application;
using ExItS.PinoyLoanManager.Domain;

namespace ExItS.PinoyLoanManager.UnitTests;

public sealed class ScaffoldAssemblyTests
{
    [Fact]
    public void Domain_and_application_assemblies_load()
    {
        Assert.Equal("ExItS.PinoyLoanManager.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
        Assert.Equal("ExItS.PinoyLoanManager.Application", typeof(ApplicationAssembly).Assembly.GetName().Name);
    }
}
