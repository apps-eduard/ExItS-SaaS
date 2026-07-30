namespace ExItS.Platform.Domain.Audit;

/// <summary>Recorded outcome of an audited action.</summary>
public enum AuditOutcome
{
    Succeeded = 1,
    Denied = 2,
    Failed = 3
}
