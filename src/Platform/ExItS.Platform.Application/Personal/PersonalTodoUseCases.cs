using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalTodoDto(
    Guid Id,
    Guid OwnerUserIdentityId,
    string Title,
    string? Notes,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? ReminderAtUtc,
    string Priority,
    string Status,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int Version);

public sealed record CreatePersonalTodoRequest(
    string Title,
    string? Notes,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? ReminderAtUtc,
    string? Priority,
    string? RelatedEntityType,
    Guid? RelatedEntityId);

public sealed record UpdatePersonalTodoRequest(
    string Title,
    string? Notes,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? ReminderAtUtc,
    string? Priority,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    int? ExpectedVersion);

public sealed record PersonalTodoVersionRequest(int? ExpectedVersion);

internal static class PersonalTodoAccess
{
    public static async Task<ApplicationResult<PersonalTodo>> RequireOwnedAsync(
        PlatformUserId ownerUserIdentityId,
        PersonalTodoId todoId,
        IPersonalTodoRepository todos,
        CancellationToken cancellationToken)
    {
        var todo = await todos.GetByIdAsync(todoId, cancellationToken).ConfigureAwait(false);
        if (todo is null)
        {
            return ApplicationResult<PersonalTodo>.Failure(
                ApplicationErrorCodes.PersonalTodoNotFound,
                "Personal to-do was not found.");
        }

        if (!todo.IsOwnedBy(ownerUserIdentityId))
        {
            return ApplicationResult<PersonalTodo>.Failure(
                ApplicationErrorCodes.PersonalTodoUnauthorized,
                "Personal to-do is not owned by this account.");
        }

        return ApplicationResult<PersonalTodo>.Success(todo);
    }

    public static PersonalTodoDto ToDto(PersonalTodo todo) =>
        new(
            todo.Id.Value,
            todo.OwnerUserIdentityId.Value,
            todo.Title,
            todo.Notes,
            todo.DueAtUtc,
            todo.ReminderAtUtc,
            todo.Priority.ToString(),
            todo.Status.ToString(),
            todo.RelatedEntityType is PersonalTodoRelatedEntityType.None
                ? null
                : todo.RelatedEntityType.ToString(),
            todo.RelatedEntityId,
            todo.CreatedAtUtc,
            todo.UpdatedAtUtc,
            todo.CompletedAtUtc,
            todo.Version);

    public static ApplicationResult<(PersonalTodoPriority Priority, PersonalTodoRelatedEntityType RelatedType)>
        ParseMetadata(string? priorityRaw, string? relatedEntityTypeRaw)
    {
        var priority = PersonalTodoPriority.None;
        if (!string.IsNullOrWhiteSpace(priorityRaw)
            && !Enum.TryParse(priorityRaw, ignoreCase: true, out priority))
        {
            return ApplicationResult<(PersonalTodoPriority, PersonalTodoRelatedEntityType)>.Failure(
                DomainErrorCodes.InvalidPersonalTodo,
                "Priority must be None, Low, Normal, or High.");
        }

        var relatedType = PersonalTodoRelatedEntityType.None;
        if (!string.IsNullOrWhiteSpace(relatedEntityTypeRaw)
            && !Enum.TryParse(relatedEntityTypeRaw, ignoreCase: true, out relatedType))
        {
            return ApplicationResult<(PersonalTodoPriority, PersonalTodoRelatedEntityType)>.Failure(
                DomainErrorCodes.InvalidPersonalTodo,
                "Related entity type is invalid.");
        }

        return ApplicationResult<(PersonalTodoPriority, PersonalTodoRelatedEntityType)>.Success(
            (priority, relatedType));
    }

    public static ApplicationResult<PersonalTodoDto> MapMutationFailure(DomainException ex)
    {
        if (ex.ErrorCode == DomainErrorCodes.PersonalTodoConcurrencyConflict)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                ex.Message);
        }

        return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
    }
}

public sealed class CreatePersonalTodo
{
    private readonly IPersonalTodoRepository _todos;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalTodo(
        IPersonalTodoRepository todos,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _todos = todos;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        CreatePersonalTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var meta = PersonalTodoAccess.ParseMetadata(request.Priority, request.RelatedEntityType);
        if (!meta.IsSuccess)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(meta.ErrorCode!, meta.ErrorMessage!);
        }

        try
        {
            var (priority, relatedType) = meta.Value;
            var todo = PersonalTodo.Create(
                ownerUserIdentityId,
                request.Title,
                _clock.UtcNow,
                request.Notes,
                request.DueAtUtc,
                request.ReminderAtUtc,
                priority,
                relatedType,
                request.RelatedEntityId);

            await _todos.AddAsync(todo, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalTodoCreated,
                nameof(PersonalTodo),
                todo.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal to-do '{todo.Title}' created.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(todo));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListPersonalTodos
{
    private readonly IPersonalTodoRepository _todos;

    public ListPersonalTodos(IPersonalTodoRepository todos) => _todos = todos;

    public async Task<IReadOnlyList<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var list = await _todos.ListByOwnerAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
        return list.Select(PersonalTodoAccess.ToDto).ToList();
    }
}

public sealed class GetPersonalTodo
{
    private readonly IPersonalTodoRepository _todos;

    public GetPersonalTodo(IPersonalTodoRepository todos) => _todos = todos;

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid todoId,
        CancellationToken cancellationToken = default)
    {
        var access = await PersonalTodoAccess
            .RequireOwnedAsync(ownerUserIdentityId, PersonalTodoId.From(todoId), _todos, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Value is null)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(access.Value));
    }
}

public sealed class UpdatePersonalTodo
{
    private readonly IPersonalTodoRepository _todos;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalTodo(
        IPersonalTodoRepository todos,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _todos = todos;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid todoId,
        UpdatePersonalTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await PersonalTodoAccess
            .RequireOwnedAsync(ownerUserIdentityId, PersonalTodoId.From(todoId), _todos, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Value is null)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        var meta = PersonalTodoAccess.ParseMetadata(request.Priority, request.RelatedEntityType);
        if (!meta.IsSuccess)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(meta.ErrorCode!, meta.ErrorMessage!);
        }

        var todo = access.Value;
        try
        {
            var (priority, relatedType) = meta.Value;
            todo.Update(
                request.Title,
                request.Notes,
                request.DueAtUtc,
                request.ReminderAtUtc,
                priority,
                relatedType,
                request.RelatedEntityId,
                _clock.UtcNow,
                request.ExpectedVersion);

            await _todos.UpdateAsync(todo, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalTodoUpdated,
                nameof(PersonalTodo),
                todo.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal to-do '{todo.Title}' updated.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(todo));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return PersonalTodoAccess.MapMutationFailure(ex);
        }
    }
}

public sealed class CompletePersonalTodo
{
    private readonly IPersonalTodoRepository _todos;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompletePersonalTodo(
        IPersonalTodoRepository todos,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _todos = todos;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid todoId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var access = await PersonalTodoAccess
            .RequireOwnedAsync(ownerUserIdentityId, PersonalTodoId.From(todoId), _todos, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Value is null)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        var todo = access.Value;
        try
        {
            todo.Complete(_clock.UtcNow, expectedVersion);
            await _todos.UpdateAsync(todo, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalTodoCompleted,
                nameof(PersonalTodo),
                todo.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal to-do '{todo.Title}' completed.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(todo));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return PersonalTodoAccess.MapMutationFailure(ex);
        }
    }
}

public sealed class ReopenPersonalTodo
{
    private readonly IPersonalTodoRepository _todos;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReopenPersonalTodo(
        IPersonalTodoRepository todos,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _todos = todos;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid todoId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var access = await PersonalTodoAccess
            .RequireOwnedAsync(ownerUserIdentityId, PersonalTodoId.From(todoId), _todos, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Value is null)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        var todo = access.Value;
        try
        {
            todo.Reopen(_clock.UtcNow, expectedVersion);
            await _todos.UpdateAsync(todo, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalTodoReopened,
                nameof(PersonalTodo),
                todo.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal to-do '{todo.Title}' reopened.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(todo));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return PersonalTodoAccess.MapMutationFailure(ex);
        }
    }
}

public sealed class CancelPersonalTodo
{
    private readonly IPersonalTodoRepository _todos;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelPersonalTodo(
        IPersonalTodoRepository todos,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _todos = todos;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalTodoDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid todoId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var access = await PersonalTodoAccess
            .RequireOwnedAsync(ownerUserIdentityId, PersonalTodoId.From(todoId), _todos, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Value is null)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        var todo = access.Value;
        try
        {
            todo.Cancel(_clock.UtcNow, expectedVersion);
            await _todos.UpdateAsync(todo, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalTodoCancelled,
                nameof(PersonalTodo),
                todo.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Personal to-do '{todo.Title}' cancelled.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalTodoDto>.Success(PersonalTodoAccess.ToDto(todo));
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PersonalTodoDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return PersonalTodoAccess.MapMutationFailure(ex);
        }
    }
}
