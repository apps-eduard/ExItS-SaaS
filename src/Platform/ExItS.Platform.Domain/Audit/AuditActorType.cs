namespace ExItS.Platform.Domain.Audit;

/// <summary>
/// Kind of actor that produced an audit record. DevelopmentOperator reflects the current
/// unauthenticated development stage (docs/engineering/authorization-matrix.md) and must never be
/// presented as production authorization.
/// </summary>
public enum AuditActorType
{
    DevelopmentOperator = 1,
    PlatformUser = 2,
    System = 3
}
