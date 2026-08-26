namespace ExItS.PinoyLoanManager.Application.Access;

public enum PlmOperationalAccessDenialReason
{
    ContextUnavailable = 1,
    ActorMissing = 2,
    OrganizationMissing = 3,
    WrongProduct = 4,
    ProductAccessDenied = 5
}
