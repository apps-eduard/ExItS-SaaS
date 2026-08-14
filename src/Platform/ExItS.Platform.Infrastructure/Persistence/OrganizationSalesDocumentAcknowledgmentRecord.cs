namespace ExItS.Platform.Infrastructure.Persistence;

internal sealed class OrganizationSalesDocumentAcknowledgmentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset AcknowledgedAtUtc { get; set; }
    public string? ContentKey { get; set; }
}
