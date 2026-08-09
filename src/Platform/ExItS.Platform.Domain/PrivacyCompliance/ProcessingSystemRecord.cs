using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.PrivacyCompliance;

/// <summary>Registry entry describing a processing system for privacy readiness documentation.</summary>
public sealed class ProcessingSystemRecord
{
    public Guid Id { get; }
    public string Code { get; }
    public string SystemName { get; private set; }
    public string Purpose { get; private set; }
    public string DataSubjects { get; private set; }
    public string PersonalDataCategories { get; private set; }
    public string? SensitiveDataCategories { get; private set; }
    public string StorageLocation { get; private set; }
    public string? RecipientsProcessors { get; private set; }
    public string? RetentionSummary { get; private set; }
    public string? SecurityControls { get; private set; }
    public string Owner { get; private set; }
    public ProcessingSystemPiaStatus PiaStatus { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ProcessingSystemRecord(
        Guid id,
        string code,
        string systemName,
        string purpose,
        string dataSubjects,
        string personalDataCategories,
        string? sensitiveDataCategories,
        string storageLocation,
        string? recipientsProcessors,
        string? retentionSummary,
        string? securityControls,
        string owner,
        ProcessingSystemPiaStatus piaStatus,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        SystemName = systemName;
        Purpose = purpose;
        DataSubjects = dataSubjects;
        PersonalDataCategories = personalDataCategories;
        SensitiveDataCategories = sensitiveDataCategories;
        StorageLocation = storageLocation;
        RecipientsProcessors = recipientsProcessors;
        RetentionSummary = retentionSummary;
        SecurityControls = securityControls;
        Owner = owner;
        PiaStatus = piaStatus;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ProcessingSystemRecord Create(
        string code,
        string systemName,
        string purpose,
        string dataSubjects,
        string personalDataCategories,
        string storageLocation,
        string owner,
        DateTimeOffset utcNow,
        string? sensitiveDataCategories = null,
        string? recipientsProcessors = null,
        string? retentionSummary = null,
        string? securityControls = null,
        ProcessingSystemPiaStatus piaStatus = ProcessingSystemPiaStatus.NotStarted,
        Guid? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        return new ProcessingSystemRecord(
            id ?? Guid.NewGuid(),
            NormalizeCode(code),
            Require(systemName, 200),
            Require(purpose, 2000),
            Require(dataSubjects, 1000),
            Require(personalDataCategories, 2000),
            Optional(sensitiveDataCategories, 2000),
            Require(storageLocation, 500),
            Optional(recipientsProcessors, 1000),
            Optional(retentionSummary, 1000),
            Optional(securityControls, 2000),
            Require(owner, 120),
            piaStatus,
            utcNow,
            utcNow);
    }

    public static ProcessingSystemRecord Rehydrate(
        Guid id,
        string code,
        string systemName,
        string purpose,
        string dataSubjects,
        string personalDataCategories,
        string? sensitiveDataCategories,
        string storageLocation,
        string? recipientsProcessors,
        string? retentionSummary,
        string? securityControls,
        string owner,
        ProcessingSystemPiaStatus piaStatus,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            code,
            systemName,
            purpose,
            dataSubjects,
            personalDataCategories,
            sensitiveDataCategories,
            storageLocation,
            recipientsProcessors,
            retentionSummary,
            securityControls,
            owner,
            piaStatus,
            createdAtUtc,
            updatedAtUtc);

    public void SetPiaStatus(ProcessingSystemPiaStatus status, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        PiaStatus = status;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(DomainErrorCodes.InvalidProcessingSystemCode, "System code is required.");
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string Require(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidProcessingSystemField, "Required field missing.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException(DomainErrorCodes.InvalidProcessingSystemField, "Field too long.");
        }

        return trimmed;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException(DomainErrorCodes.InvalidProcessingSystemField, "Field too long.");
        }

        return trimmed;
    }
}
