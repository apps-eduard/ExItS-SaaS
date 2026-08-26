using ExItS.PinoyLoanManager.Domain.Access;

namespace ExItS.PinoyLoanManager.UnitTests.Access;

public sealed class PlmProductIdentityTests
{
    [Fact]
    public void Pinoy_loan_manager_code_is_final_catalog_value()
    {
        Assert.Equal("pinoy-loan-manager", PlmProductIdentity.PinoyLoanManagerCode);
        Assert.Equal("pinoy-loan-manager", PlmProductIdentity.PinoyLoanManager.Code);
        Assert.True(PlmProductIdentity.IsPinoyLoanManager("Pinoy-Loan-Manager"));
        Assert.False(PlmProductIdentity.IsPinoyLoanManager("pinoy-business-pos"));
    }
}
