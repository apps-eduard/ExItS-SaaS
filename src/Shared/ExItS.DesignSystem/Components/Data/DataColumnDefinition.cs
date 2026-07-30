namespace ExItS.DesignSystem.Components.Data;

public sealed record DataColumnDefinition(string Key, string Title, bool Primary = false, string? Width = null);
