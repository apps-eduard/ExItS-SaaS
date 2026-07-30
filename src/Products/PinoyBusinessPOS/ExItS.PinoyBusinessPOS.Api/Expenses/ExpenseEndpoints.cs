using System.Globalization;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Api.Expenses;

/// <summary>
/// Organization-scoped store expense endpoints (P8-WP05). Development-stage only: organization
/// scope comes from <c>X-Pos-Organization-Id</c>, the actor from <c>X-Dev-Platform-User-Id</c>, and
/// cross-organization access returns 404 (fail closed). Online-only — no offline expense queue.
/// </summary>
internal static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        MapCategoryEndpoints(app.MapGroup("/api/v1/pos/expense-categories"));
        MapExpenseGroup(app.MapGroup("/api/v1/pos/expenses"));
        return app;
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            ExpenseCategoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseCategoryStatus(status, out var parsedStatus, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListAsync(organizationId, parsedStatus, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreatePosExpenseCategoryRequest body,
            CreateExpenseCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.Name, body.CategoryId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                c =>
                {
                    var dto = ExpenseCategoryQueryService.Map(c);
                    return Results.Created($"/api/v1/pos/expense-categories/{dto.CategoryId:D}", dto);
                });
        });

        group.MapGet("/{categoryId:guid}", async (
            HttpRequest request,
            Guid categoryId,
            ExpenseCategoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var category = await queries.GetByIdAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return category is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.ExpenseCategoryNotFound,
                    "Expense category was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(category);
        });

        group.MapPut("/{categoryId:guid}", async (
            HttpRequest request,
            Guid categoryId,
            UpdatePosExpenseCategoryRequest body,
            UpdateExpenseCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, categoryId, body.Name, body.ExpectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ExpenseCategoryQueryService.Map(c)));
        });

        group.MapPost("/{categoryId:guid}/deactivate", async (
            HttpRequest request,
            Guid categoryId,
            DeactivateExpenseCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ExpenseCategoryQueryService.Map(c)));
        });

        group.MapPost("/{categoryId:guid}/reactivate", async (
            HttpRequest request,
            Guid categoryId,
            ReactivateExpenseCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ExpenseCategoryQueryService.Map(c)));
        });
    }

    private static void MapExpenseGroup(RouteGroupBuilder group)
    {
        group.MapGet("/summary", async (
            HttpRequest request,
            string? fromDate,
            string? toDate,
            ExpenseSummaryService summary,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseDate(fromDate, "fromDate", out var parsedFrom, out problem)
                || !TryParseDate(toDate, "toDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            var result = await summary
                .GetSummaryAsync(organizationId, parsedFrom, parsedTo, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? paymentMethod,
            Guid? categoryId,
            string? fromDate,
            string? toDate,
            string? expenseNumber,
            int? page,
            int? pageSize,
            ExpenseQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParsePaymentMethod(paymentMethod, out var parsedMethod, out problem)
                || !TryParseDate(fromDate, "fromDate", out var parsedFrom, out problem)
                || !TryParseDate(toDate, "toDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            ExpenseCategoryId? parsedCategory = null;
            if (categoryId is not null)
            {
                try
                {
                    parsedCategory = ExpenseCategoryId.From(categoryId.Value);
                }
                catch (DomainException ex)
                {
                    return PosApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
                }
            }

            var filter = new ExpenseFilter(parsedStatus, parsedMethod, parsedCategory, parsedFrom, parsedTo, expenseNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            RecordExpenseRequest body,
            RecordExpense useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.ExpenseCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.CategoryId,
                        body.PaymentMethod,
                        body.Amount,
                        body.Description,
                        body.ExpenseDate,
                        actorId,
                        body.Payee,
                        body.GCashReference,
                        body.ExpenseId,
                        ct2),
                    e => ExpenseQueryService.Map(e),
                    dto => Results.Created($"/api/v1/pos/expenses/{dto.ExpenseId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{expenseId:guid}", async (
            HttpRequest request,
            Guid expenseId,
            ExpenseQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            var expense = await queries.GetByIdAsync(organizationId, expenseId, ct).ConfigureAwait(false);
            return expense is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.ExpenseNotFound,
                    "Expense was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(expense);
        });

        group.MapPost("/{expenseId:guid}/void", async (
            HttpRequest request,
            Guid expenseId,
            VoidExpenseRequest body,
            VoidExpense useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageExpenses, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, expenseId, body.Reason, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, e => Results.Ok(ExpenseQueryService.Map(e)));
        });
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseCategoryStatus(string? status, out ExpenseCategoryStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<ExpenseCategoryStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidExpenseCategoryStatus,
                $"Unrecognized expense category status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseStatus(string? status, out ExpenseStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<ExpenseStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidExpenseStatus,
                $"Unrecognized expense status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParsePaymentMethod(
        string? paymentMethod,
        out ExpensePaymentMethod? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return true;
        }

        if (!ExpensePaymentMethods.TryParse(paymentMethod, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidExpensePaymentMethod,
                $"Unrecognized payment method '{paymentMethod}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseDate(string? value, string name, out DateOnly? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.DomainViolation,
                $"Invalid {name} '{value}'. Use YYYY-MM-DD.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = date;
        return true;
    }
}
