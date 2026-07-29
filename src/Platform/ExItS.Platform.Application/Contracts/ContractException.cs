namespace ExItS.Platform.Application.Contracts;

public static class ContractErrorCodes
{
    public const string InvalidContractEnvelope = "contract.envelope.invalid";
    public const string UnsupportedContractVersion = "contract.version.unsupported";
    public const string InvalidSourceVersion = "contract.source_version.invalid";
    public const string DuplicateMessage = "contract.message.duplicate";
    public const string OlderProjectionVersion = "contract.projection.older_version";
    public const string ProjectionVersionGap = "contract.projection.version_gap";
    public const string ProjectionConflict = "contract.projection.conflict";
    public const string InvalidOrganizationMapping = "contract.organization_mapping.invalid";
    public const string ProductCodeMismatch = "contract.product_code.mismatch";
    public const string ReconciliationRequired = "contract.reconciliation.required";
    public const string SensitiveDataNotAllowed = "contract.sensitive_data.not_allowed";
    public const string InvalidContractVersion = "contract.version.invalid";
}

public sealed class ContractException : Exception
{
    public string ErrorCode { get; }

    public ContractException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ErrorCode = errorCode;
    }
}
