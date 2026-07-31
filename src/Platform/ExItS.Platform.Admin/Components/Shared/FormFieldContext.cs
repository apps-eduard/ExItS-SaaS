namespace ExItS.Platform.Admin.Components.Shared;

public sealed class FormFieldContext
{
    public required string FieldId { get; init; }
    public string? AriaDescribedBy { get; init; }
    public bool Invalid { get; init; }
    public bool Disabled { get; init; }
    public bool Required { get; init; }
}
