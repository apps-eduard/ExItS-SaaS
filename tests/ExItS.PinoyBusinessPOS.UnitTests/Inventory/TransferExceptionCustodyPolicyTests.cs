using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class TransferExceptionCustodyPolicyTests
{
    [Theory]
    [InlineData(ReceiveDiscrepancyOtherReason.WrongItem, true)]
    [InlineData(ReceiveDiscrepancyOtherReason.WrongVariant, true)]
    [InlineData(ReceiveDiscrepancyOtherReason.Expired, false)]
    [InlineData(ReceiveDiscrepancyOtherReason.PackagingIssue, false)]
    public void RestoresDirectlyToSellableOnSourceReceive_matchesReason(string reason, bool expected) =>
        Assert.Equal(expected, TransferExceptionCustodyPolicy.RestoresDirectlyToSellableOnSourceReceive(reason));

    [Theory]
    [InlineData(ReceiveDiscrepancyOtherReason.WrongItem, true)]
    [InlineData(ReceiveDiscrepancyOtherReason.WrongVariant, true)]
    [InlineData(ReceiveDiscrepancyOtherReason.Expired, false)]
    public void RequiresActualProduct_matchesReason(string reason, bool expected) =>
        Assert.Equal(expected, TransferExceptionCustodyPolicy.RequiresActualProduct(reason));

    [Fact]
    public void ResolveDecision_forcesReturn_forWrongItem_evenWhenKeepRequested()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TransferExceptionCustodyPolicy.ResolveDecision(
                ReceiveDiscrepancyOtherReason.WrongItem,
                InventoryTransferExceptionCustodyDecision.KeepAtDestination));

        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferExceptionCustodyDecision, ex.ErrorCode);
    }

    [Fact]
    public void ResolveDecision_requiresDecision_forPackagingIssue()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TransferExceptionCustodyPolicy.ResolveDecision(
                ReceiveDiscrepancyOtherReason.PackagingIssue,
                requested: null));

        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferExceptionCustodyDecision, ex.ErrorCode);
    }

    [Fact]
    public void ResolveDecision_allowsKeep_forQualityIssue()
    {
        var decision = TransferExceptionCustodyPolicy.ResolveDecision(
            ReceiveDiscrepancyOtherReason.QualityIssue,
            InventoryTransferExceptionCustodyDecision.KeepAtDestination);

        Assert.Equal(InventoryTransferExceptionCustodyDecision.KeepAtDestination, decision);
    }
}
