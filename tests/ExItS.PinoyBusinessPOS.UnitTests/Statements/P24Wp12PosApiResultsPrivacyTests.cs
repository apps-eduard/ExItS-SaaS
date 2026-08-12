using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using Microsoft.AspNetCore.Http;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

/// <summary>P24-WP12: HTTP privacy/premium status mapping for linked-customer surfaces.</summary>
public sealed class P24Wp12PosApiResultsPrivacyTests
{
    [Theory]
    [InlineData(ApplicationErrorCodes.ReceiptNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ApplicationErrorCodes.LinkedCustomerNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ApplicationErrorCodes.CustomerNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ApplicationErrorCodes.LinkedCustomerDenied, StatusCodes.Status403Forbidden)]
    [InlineData(ApplicationErrorCodes.ExtendedHistoryRequired, StatusCodes.Status403Forbidden)]
    public void Privacy_and_premium_codes_map_to_expected_http_status(string errorCode, int expected)
    {
        Assert.Equal(expected, PosApiResults.MapStatusCode(errorCode));
    }

    [Fact]
    public void Extended_history_required_is_forbidden_not_not_found()
    {
        Assert.Equal(StatusCodes.Status403Forbidden, PosApiResults.MapStatusCode(ApplicationErrorCodes.ExtendedHistoryRequired));
        Assert.NotEqual(StatusCodes.Status404NotFound, PosApiResults.MapStatusCode(ApplicationErrorCodes.ExtendedHistoryRequired));
    }

    [Fact]
    public void Receipt_not_found_is_not_found_not_forbidden()
    {
        Assert.Equal(StatusCodes.Status404NotFound, PosApiResults.MapStatusCode(ApplicationErrorCodes.ReceiptNotFound));
        Assert.NotEqual(StatusCodes.Status403Forbidden, PosApiResults.MapStatusCode(ApplicationErrorCodes.ReceiptNotFound));
    }
}
