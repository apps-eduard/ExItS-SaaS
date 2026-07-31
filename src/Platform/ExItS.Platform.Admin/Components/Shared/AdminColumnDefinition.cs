namespace ExItS.Platform.Admin.Components.Shared;

/// <summary>
/// Column metadata for <see cref="AdminDataTable{TItem}"/>.
/// Ordering and filtering remain page/server-authoritative; this is presentation only.
/// </summary>
public sealed record AdminColumnDefinition(
    string Key,
    string Title,
    bool Primary = false,
    bool Numeric = false,
    string? Width = null,
    bool Sortable = false);

public enum AdminSortDirection
{
    None,
    Ascending,
    Descending
}

public sealed record AdminSortState(string? ColumnKey, AdminSortDirection Direction);

public sealed record AdminFilterChip(string Label, string Value);
