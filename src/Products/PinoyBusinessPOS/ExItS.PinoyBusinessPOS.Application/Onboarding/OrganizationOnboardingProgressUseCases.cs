using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;

namespace ExItS.PinoyBusinessPOS.Application.Onboarding;

public sealed record OrganizationOnboardingProgressDto(
    Guid OrganizationId,
    string OrganizationSetupStatus,
    string BusinessSetupStatus,
    string ProductTemplateStatus,
    string OverallStatus,
    Guid? PrimaryBusinessTypeId,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record EnsureOrganizationOnboardingProgressRequest(Guid? PrimaryBusinessTypeId = null);

public sealed record UpdateOrganizationOnboardingProgressRequest(
    string? OrganizationSetupStatus = null,
    string? BusinessSetupStatus = null,
    string? ProductTemplateStatus = null,
    string? OverallStatus = null,
    Guid? PrimaryBusinessTypeId = null);

public static class OrganizationOnboardingProgressMapper
{
    public static OrganizationOnboardingProgressDto Map(OrganizationOnboardingProgress progress) =>
        new(
            progress.OrganizationId.Value,
            progress.OrganizationSetupStatus.ToString(),
            progress.BusinessSetupStatus.ToString(),
            progress.ProductTemplateStatus.ToString(),
            progress.OverallStatus.ToString(),
            progress.PrimaryBusinessTypeId,
            progress.UpdatedAtUtc,
            progress.CreatedAtUtc);
}

public sealed class GetOrganizationOnboardingProgress
{
    private readonly IOrganizationOnboardingProgressRepository _repository;

    public GetOrganizationOnboardingProgress(IOrganizationOnboardingProgressRepository repository) =>
        _repository = repository;

    public async Task<ApplicationResult<OrganizationOnboardingProgressDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var progress = await _repository
            .GetByOrganizationIdAsync(PosOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        if (progress is null)
        {
            return ApplicationResult<OrganizationOnboardingProgressDto>.Failure(
                ApplicationErrorCodes.OnboardingProgressNotFound,
                "Organization onboarding progress was not found.");
        }

        return ApplicationResult<OrganizationOnboardingProgressDto>.Success(
            OrganizationOnboardingProgressMapper.Map(progress));
    }
}

public sealed class EnsureOrganizationOnboardingProgress
{
    private readonly IOrganizationOnboardingProgressRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnsureOrganizationOnboardingProgress(
        IOrganizationOnboardingProgressRepository repository,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationOnboardingProgressDto>> ExecuteAsync(
        Guid organizationId,
        EnsureOrganizationOnboardingProgressRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var existing = await _repository
            .GetByOrganizationIdAsync(orgId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ApplicationResult<OrganizationOnboardingProgressDto>.Success(
                OrganizationOnboardingProgressMapper.Map(existing));
        }

        try
        {
            var created = OrganizationOnboardingProgress.Create(
                orgId,
                request?.PrimaryBusinessTypeId,
                _clock.UtcNow);
            await _repository.AddAsync(created, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationOnboardingProgressDto>.Success(
                OrganizationOnboardingProgressMapper.Map(created));
        }
        catch (PersistenceConflictException)
        {
            var raced = await _repository
                .GetByOrganizationIdAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
            if (raced is not null)
            {
                return ApplicationResult<OrganizationOnboardingProgressDto>.Success(
                    OrganizationOnboardingProgressMapper.Map(raced));
            }

            return ApplicationResult<OrganizationOnboardingProgressDto>.Failure(
                ApplicationErrorCodes.OnboardingProgressConcurrencyConflict,
                "Organization onboarding progress could not be created because of a concurrent request. Retry.");
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOnboardingProgressDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateOrganizationOnboardingProgress
{
    private readonly IOrganizationOnboardingProgressRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateOrganizationOnboardingProgress(
        IOrganizationOnboardingProgressRepository repository,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationOnboardingProgressDto>> ExecuteAsync(
        Guid organizationId,
        UpdateOrganizationOnboardingProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var progress = await _repository
            .GetByOrganizationIdAsync(orgId, cancellationToken)
            .ConfigureAwait(false);
        if (progress is null)
        {
            return ApplicationResult<OrganizationOnboardingProgressDto>.Failure(
                ApplicationErrorCodes.OnboardingProgressNotFound,
                "Organization onboarding progress was not found.");
        }

        var now = _clock.UtcNow;
        try
        {
            if (request.OrganizationSetupStatus is not null)
            {
                progress.MarkOrganizationSetup(ParseStepStatus(request.OrganizationSetupStatus), now);
            }

            if (request.BusinessSetupStatus is not null)
            {
                progress.MarkBusinessSetup(ParseStepStatus(request.BusinessSetupStatus), now);
            }

            if (request.ProductTemplateStatus is not null)
            {
                progress.MarkProductTemplate(ParseStepStatus(request.ProductTemplateStatus), now);
            }

            if (request.PrimaryBusinessTypeId.HasValue)
            {
                progress.SetPrimaryBusinessTypeId(request.PrimaryBusinessTypeId, now);
            }

            if (request.OverallStatus is not null)
            {
                ApplyOverallStatus(progress, request.OverallStatus, now);
            }

            await _repository.UpdateAsync(progress, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationOnboardingProgressDto>.Success(
                OrganizationOnboardingProgressMapper.Map(progress));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationOnboardingProgressDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static OnboardingStepStatus ParseStepStatus(string value)
    {
        if (!Enum.TryParse<OnboardingStepStatus>(value.Trim(), ignoreCase: true, out var status)
            || status is not (OnboardingStepStatus.Completed or OnboardingStepStatus.Skipped))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingStepStatus,
                "Step status must be Completed or Skipped.");
        }

        return status;
    }

    private static void ApplyOverallStatus(
        OrganizationOnboardingProgress progress,
        string value,
        DateTimeOffset utcNow)
    {
        if (!Enum.TryParse<OnboardingOverallStatus>(value.Trim(), ignoreCase: true, out var status)
            || status is not (OnboardingOverallStatus.FinishedLater or OnboardingOverallStatus.Completed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingOverallStatusTransition,
                "Overall status must be FinishedLater or Completed.");
        }

        if (status == OnboardingOverallStatus.FinishedLater)
        {
            progress.MarkFinishedLater(utcNow);
        }
        else
        {
            progress.MarkCompleted(utcNow);
        }
    }
}
