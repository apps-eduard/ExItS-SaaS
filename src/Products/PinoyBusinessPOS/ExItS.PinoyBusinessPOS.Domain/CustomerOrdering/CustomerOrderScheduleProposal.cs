using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Seller-proposed fulfillment schedule awaiting customer accept/decline.
/// Stored on <see cref="CustomerOrder"/> without changing <see cref="CustomerOrderStatus"/>.
/// </summary>
public sealed class CustomerOrderScheduleProposal
{
    public const int NoteMaxLength = 512;
    public const int LocalMaxLength = 32;
    public const int TimeZoneMaxLength = 64;

    public Guid ProposalId { get; }
    public string Local { get; }
    public string TimeZoneId { get; }
    public DateTimeOffset AtUtc { get; }
    public string? Note { get; }
    public Guid ProposedBy { get; }
    public DateTimeOffset ProposedAtUtc { get; }

    public CustomerOrderScheduleProposal(
        Guid proposalId,
        string local,
        string timeZoneId,
        DateTimeOffset atUtc,
        string? note,
        Guid proposedBy,
        DateTimeOffset proposedAtUtc)
    {
        if (proposalId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Schedule proposal id is required.");
        }

        SaleMoney.EnsureUtc(atUtc);
        SaleMoney.EnsureUtc(proposedAtUtc);
        if (proposedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderActor,
                "Schedule proposal actor is required.");
        }

        var trimmedLocal = (local ?? string.Empty).Trim();
        var trimmedZone = (timeZoneId ?? string.Empty).Trim();
        if (trimmedLocal.Length is 0 or > LocalMaxLength
            || trimmedZone.Length is 0 or > TimeZoneMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Schedule proposal time is not valid for this store.");
        }

        string? trimmedNote = null;
        if (!string.IsNullOrWhiteSpace(note))
        {
            trimmedNote = note.Trim();
            if (trimmedNote.Length > NoteMaxLength)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                    $"Schedule proposal note must be at most {NoteMaxLength} characters.");
            }
        }

        ProposalId = proposalId;
        Local = trimmedLocal;
        TimeZoneId = trimmedZone;
        AtUtc = atUtc;
        Note = trimmedNote;
        ProposedBy = proposedBy;
        ProposedAtUtc = proposedAtUtc;
    }
}
