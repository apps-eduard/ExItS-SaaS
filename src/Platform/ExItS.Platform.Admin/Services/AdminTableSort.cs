using AntDesign;
using AntDesign.TableModels;

namespace ExItS.Platform.Admin.Services;

/// <summary>Shared helpers for Ant Design Blazor RemoteDataSource sort extraction.</summary>
public static class AdminTableSort
{
    public static (string? SortBy, bool SortDesc) Extract<T>(
        QueryModel<T> queryModel,
        Func<string?, string?> mapField)
    {
        var activeSort = queryModel.SortModel
            .FirstOrDefault(s => s.SortDirection != SortDirection.None);

        if (activeSort is null)
        {
            return (null, false);
        }

        var sortBy = mapField(activeSort.FieldName);
        if (sortBy is null)
        {
            return (null, false);
        }

        return (sortBy, activeSort.SortDirection == SortDirection.Descending);
    }

    public static async Task ApplyChangeAsync<T>(
        QueryModel<T> queryModel,
        Func<string?, string?> mapField,
        Func<string?> getSortBy,
        Action<string?> setSortBy,
        Func<bool> getSortDesc,
        Action<bool> setSortDesc,
        Func<int> getPage,
        Action<int> setPage,
        Func<Task> reloadAsync)
    {
        var newPage = queryModel.PageIndex >= 1 ? queryModel.PageIndex : 1;
        var (newSortBy, newSortDesc) = Extract(queryModel, mapField);
        var sortChanged = !string.Equals(getSortBy(), newSortBy, StringComparison.Ordinal) || getSortDesc() != newSortDesc;
        var pageChanged = getPage() != newPage;

        if (!sortChanged && !pageChanged)
        {
            return;
        }

        if (sortChanged)
        {
            setSortBy(newSortBy);
            setSortDesc(newSortDesc);
            setPage(1);
        }
        else
        {
            setPage(newPage);
        }

        // Stay on the Blazor sync context — ConfigureAwait(false) breaks circuit JS/state after hard refresh.
        await reloadAsync();
    }
}
