using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.TestSupport;

internal sealed class FakePublicOrganizationIdGenerator : IPublicOrganizationIdGenerator
{
    private int _sequence = 1842;

    public Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        var id = $"ORG{_sequence.ToString("D6")}";
        _sequence++;
        PublicOrganizationIdRules.Normalize(id);
        return Task.FromResult(id);
    }
}
