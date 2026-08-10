using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

/// <summary>Resolves business-type tokens (Guid, code, or normalized name) to persisted ids.</summary>
public static class BusinessTypeResolver
{
    public static async Task<IReadOnlyList<BusinessTypeId>> ResolveManyAsync(
        IBusinessTypeRepository repository,
        IReadOnlyList<string>? tokens,
        IReadOnlyList<Guid>? ids,
        CancellationToken cancellationToken = default)
    {
        var resolved = new List<BusinessTypeId>();

        if (ids is { Count: > 0 })
        {
            foreach (var id in ids)
            {
                var entity = await repository.GetByIdAsync(BusinessTypeId.From(id), cancellationToken)
                    .ConfigureAwait(false);
                if (entity is null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                        $"Unrecognized business type id '{id:D}'.");
                }

                resolved.Add(entity.Id);
            }
        }

        if (tokens is { Count: > 0 })
        {
            foreach (var token in tokens)
            {
                resolved.Add(await ResolveOneAsync(repository, token, cancellationToken).ConfigureAwait(false));
            }
        }

        return GlobalCatalogRules.NormalizeBusinessTypeIds(resolved);
    }

    public static async Task<BusinessTypeId> ResolvePrimaryAsync(
        IBusinessTypeRepository repository,
        Guid? id,
        string? codeOrName,
        CancellationToken cancellationToken = default)
    {
        if (id is Guid guid)
        {
            var byId = await repository.GetByIdAsync(BusinessTypeId.From(guid), cancellationToken)
                .ConfigureAwait(false);
            if (byId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    $"Unrecognized business type id '{guid:D}'.");
            }

            return GlobalCatalogRules.NormalizePrimaryBusinessTypeId(byId.Id);
        }

        if (string.IsNullOrWhiteSpace(codeOrName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Primary business type is required.");
        }

        return GlobalCatalogRules.NormalizePrimaryBusinessTypeId(
            await ResolveOneAsync(repository, codeOrName, cancellationToken).ConfigureAwait(false));
    }

    public static async Task<BusinessTypeId> ResolveOneAsync(
        IBusinessTypeRepository repository,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Business type cannot be blank.");
        }

        var trimmed = token.Trim();
        if (Guid.TryParse(trimmed, out var guid))
        {
            var byId = await repository.GetByIdAsync(BusinessTypeId.From(guid), cancellationToken)
                .ConfigureAwait(false);
            if (byId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    $"Unrecognized business type id '{guid:D}'.");
            }

            return byId.Id;
        }

        var byCode = await repository.GetByCodeAsync(trimmed, cancellationToken).ConfigureAwait(false);
        if (byCode is not null)
        {
            return byCode.Id;
        }

        var normalizedName = trimmed.ToUpperInvariant();
        // Collapse internal whitespace like NormalizeName for lookup.
        normalizedName = System.Text.RegularExpressions.Regex.Replace(normalizedName.Trim(), @"\s+", " ");
        var byName = await repository.FindByNormalizedNameAsync(normalizedName, cancellationToken)
            .ConfigureAwait(false);
        if (byName is not null)
        {
            return byName.Id;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidGlobalCatalogBusinessType,
            $"Unrecognized business type '{trimmed}'.");
    }

    public static async Task<IReadOnlyDictionary<Guid, string>> LoadCodeLookupAsync(
        IBusinessTypeRepository repository,
        IEnumerable<BusinessTypeId> ids,
        CancellationToken cancellationToken = default)
    {
        var guidSet = ids.Select(i => i.Value).Distinct().ToArray();
        if (guidSet.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var entities = await repository.GetByIdsAsync(guidSet, cancellationToken).ConfigureAwait(false);
        return entities.ToDictionary(e => e.Id.Value, e => e.Code);
    }

    public static async Task<IReadOnlyDictionary<Guid, string>> LoadAllCodeLookupAsync(
        IBusinessTypeRepository repository,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await repository
            .ListAsync(status: null, search: null, skip: 0, take: 10_000, cancellationToken)
            .ConfigureAwait(false);
        return items.ToDictionary(e => e.Id.Value, e => e.Code);
    }
}
