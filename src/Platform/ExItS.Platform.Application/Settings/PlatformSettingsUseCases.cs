using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Settings;

namespace ExItS.Platform.Application.Settings;

public sealed record PlatformGeneralSettingsDto(
    string PlatformDisplayName,
    string? SupportEmail,
    string? BrandingLogoUrl,
    string? BrandingPrimaryColor,
    string? BrandingAccentColor,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedByActorId);

public sealed record UpdatePlatformGeneralSettingsRequest(
    string PlatformDisplayName,
    string? SupportEmail,
    string? BrandingLogoUrl,
    string? BrandingPrimaryColor,
    string? BrandingAccentColor,
    int? ExpectedVersion);

public sealed record PlatformEmailSettingsDto(
    string ProviderMode,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    bool PasswordConfigured,
    string FromDisplayName,
    string FromAddress,
    string SecurityMode,
    string? AdminPublicBaseUrl,
    bool IsConfigured,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedByActorId);

public sealed record UpdatePlatformEmailSettingsRequest(
    string ProviderMode,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    bool ReplacePassword,
    string? SmtpPassword,
    string FromDisplayName,
    string FromAddress,
    string SecurityMode,
    string? AdminPublicBaseUrl,
    int? ExpectedVersion);

public sealed record PlatformEmailTestRequest(string RecipientEmail);

public sealed record PlatformEmailTestResultDto(bool Succeeded, string Message);

public sealed record PlatformRegionalSettingsDto(
    string DefaultTimeZoneId,
    string DefaultLocale,
    string DefaultCurrencyCode,
    string DefaultCountryCode,
    string? DateFormat,
    string? TimeFormat,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedByActorId);

public sealed record UpdatePlatformRegionalSettingsRequest(
    string DefaultTimeZoneId,
    string DefaultLocale,
    string DefaultCurrencyCode,
    string DefaultCountryCode,
    string? DateFormat,
    string? TimeFormat,
    int? ExpectedVersion);

public sealed class GetPlatformGeneralSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;

    public GetPlatformGeneralSettings(PlatformSettingsProvisioner provisioner) => _provisioner = provisioner;

    public async Task<ApplicationResult<PlatformGeneralSettingsDto>> ExecuteAsync(
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformGeneralSettingsDto>.Success(PlatformSettingsMappings.MapGeneral(settings));
    }
}

public sealed class UpdatePlatformGeneralSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;
    private readonly IPlatformSettingsRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public UpdatePlatformGeneralSettings(
        PlatformSettingsProvisioner provisioner,
        IPlatformSettingsRepository repository,
        IPlatformUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock)
    {
        _provisioner = provisioner;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformGeneralSettingsDto>> ExecuteAsync(
        UpdatePlatformGeneralSettingsRequest request,
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        try
        {
            settings.UpdateGeneral(
                request.PlatformDisplayName,
                request.SupportEmail,
                PlatformBrandingDefaults.Create(
                    request.BrandingLogoUrl,
                    request.BrandingPrimaryColor,
                    request.BrandingAccentColor),
                _clock.UtcNow,
                actor.ActorIdentifier,
                request.ExpectedVersion);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformGeneralSettingsDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _repository.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditWriter.WriteAsync(
            actor,
            PlatformAuditActions.PlatformSettingsGeneralUpdated,
            nameof(PlatformSettings),
            PlatformSettings.SingletonId.ToString(),
            AuditOutcome.Succeeded,
            summary: "Platform general settings updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformGeneralSettingsDto>.Success(PlatformSettingsMappings.MapGeneral(settings));
    }
}

public sealed class GetPlatformEmailSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;

    public GetPlatformEmailSettings(PlatformSettingsProvisioner provisioner) => _provisioner = provisioner;

    public async Task<ApplicationResult<PlatformEmailSettingsDto>> ExecuteAsync(
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformEmailSettingsDto>.Success(PlatformSettingsMappings.MapEmail(settings));
    }
}

public sealed class UpdatePlatformEmailSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;
    private readonly IPlatformSettingsRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IPlatformSettingsSecretProtector _secretProtector;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public UpdatePlatformEmailSettings(
        PlatformSettingsProvisioner provisioner,
        IPlatformSettingsRepository repository,
        IPlatformUnitOfWork unitOfWork,
        IPlatformSettingsSecretProtector secretProtector,
        IAuditWriter auditWriter,
        IClock clock)
    {
        _provisioner = provisioner;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _secretProtector = secretProtector;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformEmailSettingsDto>> ExecuteAsync(
        UpdatePlatformEmailSettingsRequest request,
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        if (!PlatformSettingsMappings.TryParseProviderMode(request.ProviderMode, out var providerMode))
        {
            return ApplicationResult<PlatformEmailSettingsDto>.Failure(
                ApplicationErrorCodes.InvalidPlatformSettings,
                "Email provider mode is not supported.");
        }

        if (!PlatformSettingsMappings.TryParseSecurityMode(request.SecurityMode, out var securityMode))
        {
            return ApplicationResult<PlatformEmailSettingsDto>.Failure(
                ApplicationErrorCodes.InvalidPlatformSettings,
                "SMTP security mode is not supported.");
        }

        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        var passwordConfiguredAfterUpdate = settings.SmtpPasswordConfigured;
        if (request.ReplacePassword)
        {
            passwordConfiguredAfterUpdate = !string.IsNullOrWhiteSpace(request.SmtpPassword);
        }

        try
        {
            settings.UpdateEmail(
                providerMode,
                request.SmtpHost,
                request.SmtpPort,
                request.SmtpUsername,
                request.ReplacePassword,
                passwordConfiguredAfterUpdate,
                request.FromDisplayName,
                request.FromAddress,
                securityMode,
                request.AdminPublicBaseUrl,
                _clock.UtcNow,
                actor.ActorIdentifier,
                request.ExpectedVersion);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformEmailSettingsDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _repository.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        if (request.ReplacePassword)
        {
            if (string.IsNullOrWhiteSpace(request.SmtpPassword))
            {
                await _repository.ClearSmtpPasswordAsync(settings.Id, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _repository.UpdateSmtpPasswordAsync(
                    settings.Id,
                    _secretProtector.Protect(request.SmtpPassword),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditWriter.WriteAsync(
            actor,
            PlatformAuditActions.PlatformSettingsEmailUpdated,
            nameof(PlatformSettings),
            PlatformSettings.SingletonId.ToString(),
            AuditOutcome.Succeeded,
            summary: "Platform email settings updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformEmailSettingsDto>.Success(PlatformSettingsMappings.MapEmail(settings));
    }
}

public sealed class SendPlatformEmailTest
{
    private readonly IPlatformEmailDeliveryResolver _deliveryResolver;
    private readonly IPlatformEmailTestSender _testSender;
    private readonly IAuditWriter _auditWriter;

    public SendPlatformEmailTest(
        IPlatformEmailDeliveryResolver deliveryResolver,
        IPlatformEmailTestSender testSender,
        IAuditWriter auditWriter)
    {
        _deliveryResolver = deliveryResolver;
        _testSender = testSender;
        _auditWriter = auditWriter;
    }

    public async Task<ApplicationResult<PlatformEmailTestResultDto>> ExecuteAsync(
        PlatformEmailTestRequest request,
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipient = Domain.Identity.PlatformUser.NormalizeEmail(request.RecipientEmail);
            var delivery = await _deliveryResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
            if (!delivery.IsConfigured)
            {
                return ApplicationResult<PlatformEmailTestResultDto>.Failure(
                    ApplicationErrorCodes.PlatformEmailNotConfigured,
                    "Platform email delivery is not configured.");
            }

            await _testSender.SendTestEmailAsync(recipient, delivery, cancellationToken).ConfigureAwait(false);
            await _auditWriter.WriteAsync(
                actor,
                PlatformAuditActions.PlatformSettingsEmailTestSent,
                nameof(PlatformSettings),
                PlatformSettings.SingletonId.ToString(),
                AuditOutcome.Succeeded,
                summary: $"Platform test email sent to {recipient}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PlatformEmailTestResultDto>.Success(
                new PlatformEmailTestResultDto(true, "Test email sent."));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformEmailTestResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult<PlatformEmailTestResultDto>.Failure(
                ApplicationErrorCodes.PlatformEmailTestFailed,
                ex.Message);
        }
    }
}

public sealed class GetPlatformRegionalSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;

    public GetPlatformRegionalSettings(PlatformSettingsProvisioner provisioner) => _provisioner = provisioner;

    public async Task<ApplicationResult<PlatformRegionalSettingsDto>> ExecuteAsync(
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformRegionalSettingsDto>.Success(PlatformSettingsMappings.MapRegional(settings));
    }
}

public sealed class UpdatePlatformRegionalSettings
{
    private readonly PlatformSettingsProvisioner _provisioner;
    private readonly IPlatformSettingsRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public UpdatePlatformRegionalSettings(
        PlatformSettingsProvisioner provisioner,
        IPlatformSettingsRepository repository,
        IPlatformUnitOfWork unitOfWork,
        IAuditWriter auditWriter,
        IClock clock)
    {
        _provisioner = provisioner;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformRegionalSettingsDto>> ExecuteAsync(
        UpdatePlatformRegionalSettingsRequest request,
        PlatformActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var settings = await _provisioner.EnsureAsync(actor.ActorIdentifier, cancellationToken).ConfigureAwait(false);
        try
        {
            settings.UpdateRegional(
                request.DefaultTimeZoneId,
                request.DefaultLocale,
                request.DefaultCurrencyCode,
                request.DefaultCountryCode,
                request.DateFormat,
                request.TimeFormat,
                _clock.UtcNow,
                actor.ActorIdentifier,
                request.ExpectedVersion);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformRegionalSettingsDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _repository.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _auditWriter.WriteAsync(
            actor,
            PlatformAuditActions.PlatformSettingsRegionalUpdated,
            nameof(PlatformSettings),
            PlatformSettings.SingletonId.ToString(),
            AuditOutcome.Succeeded,
            summary: "Platform regional settings updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformRegionalSettingsDto>.Success(PlatformSettingsMappings.MapRegional(settings));
    }
}

internal static class PlatformSettingsMappings
{
    internal static PlatformGeneralSettingsDto MapGeneral(PlatformSettings settings) =>
        new(
            settings.PlatformDisplayName ?? "ExItS",
            settings.SupportEmail,
            settings.Branding.LogoUrl,
            settings.Branding.PrimaryColor,
            settings.Branding.AccentColor,
            settings.Version,
            settings.UpdatedAtUtc,
            settings.UpdatedByActorId);

    internal static PlatformEmailSettingsDto MapEmail(PlatformSettings settings) =>
        new(
            settings.EmailProviderMode.ToString(),
            settings.SmtpHost,
            settings.SmtpPort,
            settings.SmtpUsername,
            settings.SmtpPasswordConfigured,
            settings.FromDisplayName ?? "ExItS",
            settings.FromAddress ?? string.Empty,
            settings.SmtpSecurityMode.ToString(),
            settings.AdminPublicBaseUrl,
            settings.IsEmailDeliveryConfigured && settings.SmtpPasswordConfigured,
            settings.Version,
            settings.UpdatedAtUtc,
            settings.UpdatedByActorId);

    internal static PlatformRegionalSettingsDto MapRegional(PlatformSettings settings) =>
        new(
            settings.DefaultTimeZoneId ?? "UTC",
            settings.DefaultLocale ?? "en-US",
            settings.DefaultCurrencyCode ?? "USD",
            settings.DefaultCountryCode ?? "US",
            settings.DateFormat,
            settings.TimeFormat,
            settings.Version,
            settings.UpdatedAtUtc,
            settings.UpdatedByActorId);

    internal static bool TryParseProviderMode(string raw, out PlatformEmailProviderMode mode) =>
        Enum.TryParse(raw, ignoreCase: true, out mode) && Enum.IsDefined(mode);

    internal static bool TryParseSecurityMode(string raw, out PlatformSmtpSecurityMode mode) =>
        Enum.TryParse(raw, ignoreCase: true, out mode) && Enum.IsDefined(mode);
}
