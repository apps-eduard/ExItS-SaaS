namespace ExItS.Platform.Application.Organizations;

public interface IPublicOrganizationIdGenerator
{
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default);
}
