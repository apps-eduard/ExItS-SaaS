namespace ExItS.PinoyLoanManager.Application.Access;

public interface IPlmOperationalAccessGuard
{
    ValueTask<PlmOperationalAccessDecision> EvaluateAsync(CancellationToken cancellationToken = default);
}
