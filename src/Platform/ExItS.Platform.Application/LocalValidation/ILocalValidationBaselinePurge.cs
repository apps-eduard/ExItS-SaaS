namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Clears Local Validation / Development transactional data while retaining catalog and
/// built-in role definitions. Used only for the PlatformAdministratorsOnly onboarding baseline.
/// </summary>
public interface ILocalValidationBaselinePurge
{
    Task PurgeTransactionalDataAsync(CancellationToken cancellationToken = default);
}
