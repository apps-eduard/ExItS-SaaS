using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Application.Commercial;

public interface IUtangCapabilityEvaluator
{
    bool IsAllowed(UtangCapability capability);
}

/// <summary>Evaluates Utang capabilities from the current auth session commercial fields.</summary>
public sealed class UtangCapabilityEvaluator(ICurrentUserContext currentUser) : IUtangCapabilityEvaluator
{
    public bool IsAllowed(UtangCapability capability)
    {
        var session = currentUser.Session;
        if (session is null)
        {
            return false;
        }

        return UtangCapabilityPolicy.IsAllowed(
            capability,
            session.SubscriptionStatus,
            session.EnabledFeatureCodes);
    }
}
