using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Personal;

public sealed class PersonalUtangMigrationBatchId : IEquatable<PersonalUtangMigrationBatchId>
{
    public Guid Value { get; }

    private PersonalUtangMigrationBatchId(Guid value) => Value = value;

    public static PersonalUtangMigrationBatchId New() => new(Guid.NewGuid());

    public static PersonalUtangMigrationBatchId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangMigrationBatchId,
                "Migration batch id is required.");
        }

        return new PersonalUtangMigrationBatchId(value);
    }

    public bool Equals(PersonalUtangMigrationBatchId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalUtangMigrationBatchId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

public sealed class PersonalUtangMigrationItemId : IEquatable<PersonalUtangMigrationItemId>
{
    public Guid Value { get; }

    private PersonalUtangMigrationItemId(Guid value) => Value = value;

    public static PersonalUtangMigrationItemId New() => new(Guid.NewGuid());

    public static PersonalUtangMigrationItemId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangMigrationItemId,
                "Migration item id is required.");
        }

        return new PersonalUtangMigrationItemId(value);
    }

    public bool Equals(PersonalUtangMigrationItemId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalUtangMigrationItemId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

/// <summary>
/// Point-in-time Personal Utang → Business Credit migration batch (ADR-020). No continuous sync.
/// </summary>
public sealed class PersonalUtangMigrationBatch
{
    public PersonalUtangMigrationBatchId Id { get; }
    public PlatformUserId OwnerUserIdentityId { get; }
    public PlatformOrganizationId DestinationOrganizationId { get; }
    public string DestinationProductCode { get; }
    public string? IdempotencyKey { get; private set; }
    public PersonalUtangMigrationBatchStatus Status { get; private set; }
    public DateTimeOffset EffectiveMigrationDateUtc { get; }
    public bool IncludeContact { get; }
    public bool IncludeOpeningBalance { get; }
    public bool IncludeSelectedHistory { get; }
    public bool IncludeDueDatesAndNotes { get; }
    public PersonalUtangSourceDisposition SourceDisposition { get; }
    public bool LinkedParticipantConsentAcknowledged { get; }
    public Guid ConfirmationToken { get; }
    public DateTimeOffset PreviewedAtUtc { get; }
    public DateTimeOffset? ExecutedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    private PersonalUtangMigrationBatch(
        PersonalUtangMigrationBatchId id,
        PlatformUserId ownerUserIdentityId,
        PlatformOrganizationId destinationOrganizationId,
        string destinationProductCode,
        string? idempotencyKey,
        PersonalUtangMigrationBatchStatus status,
        DateTimeOffset effectiveMigrationDateUtc,
        bool includeContact,
        bool includeOpeningBalance,
        bool includeSelectedHistory,
        bool includeDueDatesAndNotes,
        PersonalUtangSourceDisposition sourceDisposition,
        bool linkedParticipantConsentAcknowledged,
        Guid confirmationToken,
        DateTimeOffset previewedAtUtc,
        DateTimeOffset? executedAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OwnerUserIdentityId = ownerUserIdentityId;
        DestinationOrganizationId = destinationOrganizationId;
        DestinationProductCode = destinationProductCode;
        IdempotencyKey = idempotencyKey;
        Status = status;
        EffectiveMigrationDateUtc = effectiveMigrationDateUtc;
        IncludeContact = includeContact;
        IncludeOpeningBalance = includeOpeningBalance;
        IncludeSelectedHistory = includeSelectedHistory;
        IncludeDueDatesAndNotes = includeDueDatesAndNotes;
        SourceDisposition = sourceDisposition;
        LinkedParticipantConsentAcknowledged = linkedParticipantConsentAcknowledged;
        ConfirmationToken = confirmationToken;
        PreviewedAtUtc = previewedAtUtc;
        ExecutedAtUtc = executedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public static PersonalUtangMigrationBatch CreatePreview(
        PlatformUserId ownerUserIdentityId,
        PlatformOrganizationId destinationOrganizationId,
        string destinationProductCode,
        DateTimeOffset effectiveMigrationDateUtc,
        bool includeContact,
        bool includeOpeningBalance,
        bool includeSelectedHistory,
        bool includeDueDatesAndNotes,
        PersonalUtangSourceDisposition sourceDisposition,
        bool linkedParticipantConsentAcknowledged,
        DateTimeOffset utcNow,
        string? idempotencyKey = null,
        PersonalUtangMigrationBatchId? id = null)
    {
        ArgumentNullException.ThrowIfNull(ownerUserIdentityId);
        ArgumentNullException.ThrowIfNull(destinationOrganizationId);
        EnsureUtc(utcNow);
        EnsureUtc(effectiveMigrationDateUtc);

        if (!includeContact && !includeOpeningBalance && !includeSelectedHistory && !includeDueDatesAndNotes)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangMigrationSelectionRequired,
                "At least one migration include option is required.");
        }

        var product = NormalizeProductCode(destinationProductCode);
        return new PersonalUtangMigrationBatch(
            id ?? PersonalUtangMigrationBatchId.New(),
            ownerUserIdentityId,
            destinationOrganizationId,
            product,
            NormalizeOptionalKey(idempotencyKey),
            PersonalUtangMigrationBatchStatus.Previewed,
            effectiveMigrationDateUtc,
            includeContact,
            includeOpeningBalance,
            includeSelectedHistory,
            includeDueDatesAndNotes,
            sourceDisposition,
            linkedParticipantConsentAcknowledged,
            Guid.NewGuid(),
            utcNow,
            executedAtUtc: null,
            utcNow);
    }

    public static PersonalUtangMigrationBatch Rehydrate(
        PersonalUtangMigrationBatchId id,
        PlatformUserId ownerUserIdentityId,
        PlatformOrganizationId destinationOrganizationId,
        string destinationProductCode,
        string? idempotencyKey,
        PersonalUtangMigrationBatchStatus status,
        DateTimeOffset effectiveMigrationDateUtc,
        bool includeContact,
        bool includeOpeningBalance,
        bool includeSelectedHistory,
        bool includeDueDatesAndNotes,
        PersonalUtangSourceDisposition sourceDisposition,
        bool linkedParticipantConsentAcknowledged,
        Guid confirmationToken,
        DateTimeOffset previewedAtUtc,
        DateTimeOffset? executedAtUtc,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            ownerUserIdentityId,
            destinationOrganizationId,
            destinationProductCode,
            idempotencyKey,
            status,
            effectiveMigrationDateUtc,
            includeContact,
            includeOpeningBalance,
            includeSelectedHistory,
            includeDueDatesAndNotes,
            sourceDisposition,
            linkedParticipantConsentAcknowledged,
            confirmationToken,
            previewedAtUtc,
            executedAtUtc,
            createdAtUtc);

    public void BindIdempotencyKey(string idempotencyKey)
    {
        IdempotencyKey = NormalizeOptionalKey(idempotencyKey)
            ?? throw new DomainException(
                DomainErrorCodes.PersonalUtangMigrationSelectionRequired,
                "Idempotency key is required.");
    }

    public void MarkExecuted(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalUtangMigrationBatchStatus.Executed)
        {
            return;
        }

        if (Status != PersonalUtangMigrationBatchStatus.Previewed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangMigrationStatusTransition,
                $"Cannot execute a migration batch in status {Status}.");
        }

        Status = PersonalUtangMigrationBatchStatus.Executed;
        ExecutedAtUtc = utcNow;
    }

    public void MarkFailed(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalUtangMigrationBatchStatus.Executed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangMigrationStatusTransition,
                "Executed migration batches cannot be marked failed.");
        }

        Status = PersonalUtangMigrationBatchStatus.Failed;
    }

    private static string NormalizeProductCode(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, "Destination product is required.");
        }

        var trimmed = productCode.Trim().ToLowerInvariant();
        if (trimmed.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, "Destination product code is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        return trimmed.Length > 128 ? trimmed[..128] : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalUtangMigrationItem
{
    public PersonalUtangMigrationItemId Id { get; }
    public PersonalUtangMigrationBatchId BatchId { get; }
    public PersonalUtangMigrationSourceType SourceType { get; }
    public Guid SourceRecordId { get; }
    public PersonalUtangMigrationDestinationType? DestinationType { get; private set; }
    public Guid? DestinationRecordId { get; private set; }
    public decimal? OpeningBalanceAmount { get; private set; }
    public string? CurrencyCode { get; private set; }
    public string? NotesSnapshot { get; private set; }
    public DateTimeOffset? DueDateUtc { get; private set; }
    public string? HistoryEntryIdsCsv { get; private set; }
    public PersonalUtangMigrationItemStatus Status { get; private set; }
    public string? BlockedReason { get; private set; }

    private PersonalUtangMigrationItem(
        PersonalUtangMigrationItemId id,
        PersonalUtangMigrationBatchId batchId,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        PersonalUtangMigrationDestinationType? destinationType,
        Guid? destinationRecordId,
        decimal? openingBalanceAmount,
        string? currencyCode,
        string? notesSnapshot,
        DateTimeOffset? dueDateUtc,
        string? historyEntryIdsCsv,
        PersonalUtangMigrationItemStatus status,
        string? blockedReason)
    {
        Id = id;
        BatchId = batchId;
        SourceType = sourceType;
        SourceRecordId = sourceRecordId;
        DestinationType = destinationType;
        DestinationRecordId = destinationRecordId;
        OpeningBalanceAmount = openingBalanceAmount;
        CurrencyCode = currencyCode;
        NotesSnapshot = notesSnapshot;
        DueDateUtc = dueDateUtc;
        HistoryEntryIdsCsv = historyEntryIdsCsv;
        Status = status;
        BlockedReason = blockedReason;
    }

    public static PersonalUtangMigrationItem CreatePreview(
        PersonalUtangMigrationBatchId batchId,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        decimal? openingBalanceAmount,
        string? currencyCode,
        string? notesSnapshot,
        DateTimeOffset? dueDateUtc,
        string? historyEntryIdsCsv,
        PersonalUtangMigrationItemStatus status = PersonalUtangMigrationItemStatus.Previewed,
        string? blockedReason = null,
        PersonalUtangMigrationItemId? id = null)
    {
        ArgumentNullException.ThrowIfNull(batchId);
        if (sourceRecordId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangMigrationSelectionRequired,
                "Source record id is required.");
        }

        return new PersonalUtangMigrationItem(
            id ?? PersonalUtangMigrationItemId.New(),
            batchId,
            sourceType,
            sourceRecordId,
            destinationType: null,
            destinationRecordId: null,
            openingBalanceAmount,
            currencyCode,
            notesSnapshot,
            dueDateUtc,
            historyEntryIdsCsv,
            status,
            blockedReason);
    }

    public static PersonalUtangMigrationItem Rehydrate(
        PersonalUtangMigrationItemId id,
        PersonalUtangMigrationBatchId batchId,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        PersonalUtangMigrationDestinationType? destinationType,
        Guid? destinationRecordId,
        decimal? openingBalanceAmount,
        string? currencyCode,
        string? notesSnapshot,
        DateTimeOffset? dueDateUtc,
        string? historyEntryIdsCsv,
        PersonalUtangMigrationItemStatus status,
        string? blockedReason) =>
        new(
            id,
            batchId,
            sourceType,
            sourceRecordId,
            destinationType,
            destinationRecordId,
            openingBalanceAmount,
            currencyCode,
            notesSnapshot,
            dueDateUtc,
            historyEntryIdsCsv,
            status,
            blockedReason);

    public void MarkMigrated(
        PersonalUtangMigrationDestinationType destinationType,
        Guid destinationRecordId)
    {
        if (destinationRecordId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangMigrationSelectionRequired,
                "Destination record id is required.");
        }

        DestinationType = destinationType;
        DestinationRecordId = destinationRecordId;
        Status = PersonalUtangMigrationItemStatus.Migrated;
        BlockedReason = null;
    }

    public void MarkBlocked(string reason)
    {
        Status = PersonalUtangMigrationItemStatus.Blocked;
        BlockedReason = string.IsNullOrWhiteSpace(reason) ? "Blocked" : reason.Trim();
    }
}
