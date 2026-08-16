using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed class CreatePaymentAttempt
{
    private readonly ISaleRepository _sales;
    private readonly IPaymentAttemptRepository _attempts;
    private readonly IPaymentGateway _gateway;
    private readonly ISaleStockService _saleStock;
    private readonly IPosUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IConfiguration _config;
    private readonly ILogger<CreatePaymentAttempt> _logger;

    public CreatePaymentAttempt(
        ISaleRepository sales,
        IPaymentAttemptRepository attempts,
        IPaymentGateway gateway,
        ISaleStockService saleStock,
        IPosUnitOfWork uow,
        IClock clock,
        IConfiguration config,
        ILogger<CreatePaymentAttempt> logger)
    {
        _sales = sales;
        _attempts = attempts;
        _gateway = gateway;
        _saleStock = saleStock;
        _uow = uow;
        _clock = clock;
        _config = config;
        _logger = logger;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid saleId,
        CreatePaymentAttemptRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var sale = await _sales.GetByIdAsync(orgId, SaleId.From(saleId), cancellationToken)
                .ConfigureAwait(false);
            if (sale is null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    ApplicationErrorCodes.SaleNotFound,
                    "Sale was not found.");
            }

            if (sale.Status != SaleStatus.AwaitingPayment)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.SaleNotAwaitingPayment,
                    "Payment attempts can only be created for sales awaiting payment.");
            }

            var existingKey = await _attempts
                .GetByIdempotencyKeyAsync(orgId, request.IdempotencyKey.Trim(), cancellationToken)
                .ConfigureAwait(false);
            if (existingKey is not null)
            {
                if (existingKey.Status == PaymentAttemptStatus.Created
                    && string.IsNullOrWhiteSpace(existingKey.ProviderReference)
                    && existingKey.Method is PaymentAttemptMethod.Card or PaymentAttemptMethod.GCash)
                {
                    return await AttachOrRecoverElectronicSessionAsync(
                            sale,
                            existingKey,
                            actorId,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(existingKey));
            }

            var active = await _attempts.FindActiveForSaleAsync(orgId, sale.Id, cancellationToken)
                .ConfigureAwait(false);
            if (active is not null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    ApplicationErrorCodes.PaymentAttemptConflict,
                    "An active payment attempt already exists for this sale. Cancel it or wait for completion.");
            }

            var now = _clock.UtcNow;
            if (request.ManualGCashTransfer)
            {
                if (!IsManualGCashTransferEnabled(_config))
                {
                    return ApplicationResult<PaymentAttemptDto>.Failure(
                        ApplicationErrorCodes.ManualGCashTransferDisabled,
                        "Manual GCash transfer is not enabled for this environment.");
                }

                if (await _attempts
                        .ExistsExternalReferenceAsync(orgId, request.ExternalReference ?? string.Empty, null, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<PaymentAttemptDto>.Failure(
                        DomainErrorCodes.DuplicatePaymentAttemptExternalReference,
                        "This external GCash reference was already submitted.");
                }

                var manual = PaymentAttempt.CreateManualGCashTransfer(
                    orgId,
                    sale.Id,
                    sale.Total,
                    "PHP",
                    request.ExternalReference ?? string.Empty,
                    request.IdempotencyKey,
                    actorId,
                    now);

                await _attempts.AddAsync(manual, cancellationToken).ConfigureAwait(false);
                await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(manual));
            }

            var method = ParseElectronicMethod(request.Method);
            if ((method == PaymentAttemptMethod.Card && sale.PaymentMethod != SalePaymentMethod.Card)
                || (method == PaymentAttemptMethod.GCash && sale.PaymentMethod != SalePaymentMethod.GCash))
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.InvalidPaymentAttemptMethod,
                    "Payment attempt method must match the sale payment method.");
            }

            // Prior terminal attempt may have released reservation — re-hold before a new attempt.
            if (sale.StockReservationState == SaleStockReservationState.Released)
            {
                await _saleStock
                    .EnsureAvailableForSaleAsync(orgId, sale, cancellationToken)
                    .ConfigureAwait(false);
                await _saleStock
                    .ReserveForAwaitingPaymentAsync(sale, actorId, now, cancellationToken)
                    .ConfigureAwait(false);
                await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
                await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var attempt = PaymentAttempt.CreateElectronic(
                orgId,
                sale.Id,
                method,
                sale.Total,
                "PHP",
                request.IdempotencyKey,
                actorId,
                now);

            // Durable Created outside the gateway call (never hold inventory locks across provider I/O).
            await _attempts.AddAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Payment attempt created (Created) org={OrganizationId} sale={SaleId} attempt={AttemptId}",
                orgId.Value,
                sale.Id.Value,
                attempt.Id.Value);

            return await AttachOrRecoverElectronicSessionAsync(sale, attempt, actorId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<PaymentAttemptDto>> AttachOrRecoverElectronicSessionAsync(
        Sale sale,
        PaymentAttempt attempt,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var createRequest = new PaymentGatewayCreateRequest(
            attempt.OrganizationId.Value,
            attempt.SaleId.Value,
            attempt.Id.Value,
            attempt.Method.ToString(),
            attempt.Amount,
            attempt.Currency,
            attempt.IdempotencyKey);

        try
        {
            var session = await _gateway.CreateSessionAsync(createRequest, cancellationToken)
                .ConfigureAwait(false);
            attempt.AttachProviderSession(
                session.ProviderReference,
                session.CheckoutUrl,
                session.DeepLink,
                session.QrPayload,
                now);
            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Payment session attached org={OrganizationId} sale={SaleId} attempt={AttemptId} providerRef={ProviderReference}",
                attempt.OrganizationId.Value,
                attempt.SaleId.Value,
                attempt.Id.Value,
                attempt.ProviderReference);

            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        catch (PaymentGatewayException ex) when (!ex.SessionMayExist)
        {
            if (string.Equals(ex.ErrorCode, DomainErrorCodes.PaymentGatewayTimeout, StringComparison.Ordinal))
            {
                // Timeout before create — leave Created for retry; keep reservation.
                _logger.LogInformation(
                    "Payment gateway timeout before create org={OrganizationId} sale={SaleId} attempt={AttemptId}",
                    attempt.OrganizationId.Value,
                    attempt.SaleId.Value,
                    attempt.Id.Value);
                return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
            }

            // Definite failure — mark failed and release reservation.
            attempt.MarkFailedLocally(ex.ErrorCode, ex.Message, now);
            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _saleStock.ReleaseIfReservedAsync(sale, now, cancellationToken).ConfigureAwait(false);
            await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Payment gateway definite failure released reservation org={OrganizationId} sale={SaleId} attempt={AttemptId}",
                attempt.OrganizationId.Value,
                attempt.SaleId.Value,
                attempt.Id.Value);

            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PaymentGatewayException ex) when (ex.SessionMayExist)
        {
            var expectedRef = $"fake_{attempt.Id.Value:N}";
            var recovered = await _gateway.GetSessionAsync(expectedRef, cancellationToken)
                .ConfigureAwait(false);
            if (recovered is null)
            {
                _logger.LogInformation(
                    "Payment gateway timeout after create; session not recovered org={OrganizationId} attempt={AttemptId}",
                    attempt.OrganizationId.Value,
                    attempt.Id.Value);
                return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
            }

            attempt.AttachProviderSession(
                recovered.ProviderReference,
                recovered.CheckoutUrl,
                recovered.DeepLink,
                recovered.QrPayload,
                now);
            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Payment session recovered after timeout org={OrganizationId} sale={SaleId} attempt={AttemptId}",
                attempt.OrganizationId.Value,
                attempt.SaleId.Value,
                attempt.Id.Value);

            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        finally
        {
            _ = actorId;
        }
    }

    private static PaymentAttemptMethod ParseElectronicMethod(string? method)
    {
        if (string.Equals(method, nameof(PaymentAttemptMethod.Card), StringComparison.OrdinalIgnoreCase))
        {
            return PaymentAttemptMethod.Card;
        }

        if (string.Equals(method, nameof(PaymentAttemptMethod.GCash), StringComparison.OrdinalIgnoreCase))
        {
            return PaymentAttemptMethod.GCash;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidPaymentAttemptMethod,
            "Method must be Card or GCash (or use ManualGCashTransfer).");
    }

    private static bool IsManualGCashTransferEnabled(IConfiguration config) =>
        bool.TryParse(config["PosPayments:EnableManualGCashTransfer"], out var enabled) && enabled;
}

public sealed class CancelPaymentAttempt
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISaleRepository _sales;
    private readonly ISaleStockService _saleStock;
    private readonly IPosUnitOfWork _uow;
    private readonly IClock _clock;

    public CancelPaymentAttempt(
        IPaymentAttemptRepository attempts,
        ISaleRepository sales,
        ISaleStockService saleStock,
        IPosUnitOfWork uow,
        IClock clock)
    {
        _attempts = attempts;
        _sales = sales;
        _saleStock = saleStock;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid attemptId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = actorId;
            var orgId = PosOrganizationId.From(organizationId);
            var attempt = await _attempts
                .GetByIdAsync(orgId, PaymentAttemptId.From(attemptId), cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.PaymentAttemptNotFound,
                    "Payment attempt was not found.");
            }

            var now = _clock.UtcNow;
            attempt.Cancel(now, "Cancelled by cashier");
            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);

            var sale = await _sales.GetByIdAsync(orgId, attempt.SaleId, cancellationToken)
                .ConfigureAwait(false);
            if (sale is not null)
            {
                await _saleStock.ReleaseIfReservedAsync(sale, now, cancellationToken).ConfigureAwait(false);
                await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
            }

            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class GetPaymentAttempt
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISaleRepository _sales;
    private readonly ISaleStockService _saleStock;
    private readonly IClock _clock;
    private readonly IPosUnitOfWork _uow;

    public GetPaymentAttempt(
        IPaymentAttemptRepository attempts,
        ISaleRepository sales,
        ISaleStockService saleStock,
        IClock clock,
        IPosUnitOfWork uow)
    {
        _attempts = attempts;
        _sales = sales;
        _saleStock = saleStock;
        _clock = clock;
        _uow = uow;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var attempt = await _attempts
            .GetByIdAsync(orgId, PaymentAttemptId.From(attemptId), cancellationToken)
            .ConfigureAwait(false);
        if (attempt is null)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(
                DomainErrorCodes.PaymentAttemptNotFound,
                "Payment attempt was not found.");
        }

        var now = _clock.UtcNow;
        var prior = attempt.Status;
        attempt.ExpireIfDue(now);
        if (attempt.Status == PaymentAttemptStatus.Expired && prior != PaymentAttemptStatus.Expired)
        {
            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            var sale = await _sales.GetByIdAsync(orgId, attempt.SaleId, cancellationToken)
                .ConfigureAwait(false);
            if (sale is not null)
            {
                await _saleStock.ReleaseIfReservedAsync(sale, now, cancellationToken).ConfigureAwait(false);
                await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
            }

            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
    }
}

public sealed class ProcessPaymentWebhook
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISaleRepository _sales;
    private readonly ISaleStockService _saleStock;
    private readonly ICatalogProductRepository _products;
    private readonly IPaymentGateway _gateway;
    private readonly IPosUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ILogger<ProcessPaymentWebhook> _logger;

    public ProcessPaymentWebhook(
        IPaymentAttemptRepository attempts,
        ISaleRepository sales,
        ISaleStockService saleStock,
        ICatalogProductRepository products,
        IPaymentGateway gateway,
        IPosUnitOfWork uow,
        IClock clock,
        ILogger<ProcessPaymentWebhook> logger)
    {
        _attempts = attempts;
        _sales = sales;
        _saleStock = saleStock;
        _products = products;
        _gateway = gateway;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        string provider,
        string? signatureHeader,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.Equals(provider, _gateway.ProviderCode, StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    ApplicationErrorCodes.PaymentProviderUnsupported,
                    $"Unsupported payment provider '{provider}'.");
            }

            if (!_gateway.ValidateWebhookSignature(signatureHeader, rawBody))
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.PaymentWebhookSignatureInvalid,
                    "Webhook signature is invalid.");
            }

            var evt = _gateway.ParseWebhook(rawBody);
            var attempt = await _attempts
                .GetByProviderReferenceAsync(evt.Provider, evt.ProviderReference, cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.PaymentAttemptNotFound,
                    "No payment attempt matches the provider reference.");
            }

            var now = _clock.UtcNow;
            var priorStatus = attempt.Status;
            attempt.ExpireIfDue(now);
            var status = evt.Status.Trim();
            if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                attempt.MarkPaidFromProvider(evt.EventSequence, now, evt.CardBrand, evt.CardLastFour);
            }
            else if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(status, "Declined", StringComparison.OrdinalIgnoreCase))
            {
                attempt.MarkFailedFromProvider(evt.EventSequence, evt.FailureCode, evt.FailureMessage, now);
            }
            else if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                attempt.Cancel(now, evt.FailureMessage ?? "Cancelled by provider");
            }
            else if (string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                attempt.MarkExpiredFromProvider(evt.EventSequence, evt.FailureMessage, now);
            }

            if (attempt.Status == PaymentAttemptStatus.Paid)
            {
                await FinalizeSaleIfNeededAsync(attempt, now, cancellationToken).ConfigureAwait(false);
            }
            else if ((attempt.Status is PaymentAttemptStatus.Failed
                      or PaymentAttemptStatus.Cancelled
                      or PaymentAttemptStatus.Expired)
                     && priorStatus != attempt.Status)
            {
                var sale = await _sales.GetByIdAsync(attempt.OrganizationId, attempt.SaleId, cancellationToken)
                    .ConfigureAwait(false);
                if (sale is not null)
                {
                    await _saleStock.ReleaseIfReservedAsync(sale, now, cancellationToken).ConfigureAwait(false);
                    await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Payment terminal non-paid released reservation org={OrganizationId} sale={SaleId} attempt={AttemptId} status={Status}",
                        attempt.OrganizationId.Value,
                        attempt.SaleId.Value,
                        attempt.Id.Value,
                        attempt.Status);
                }
            }

            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task FinalizeSaleIfNeededAsync(
        PaymentAttempt attempt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sale = await _sales.GetByIdAsync(attempt.OrganizationId, attempt.SaleId, cancellationToken)
            .ConfigureAwait(false);
        if (sale is null || sale.Status == SaleStatus.Completed)
        {
            return;
        }

        var safeRef = attempt.ProviderReference ?? attempt.ExternalReference;
        sale.FinalizeAfterPayment(safeRef, now);

        var products = await _products
            .ListByIdsAsync(attempt.OrganizationId, sale.Lines.Select(l => l.ProductId).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var byId = products.ToDictionary(p => p.Id.Value);

        if (sale.StockReservationState == SaleStockReservationState.Reserved)
        {
            await _saleStock
                .ConsumeReservedForPaidAsync(sale, byId, attempt.CreatedBy, now, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _saleStock
                .DeductForSaleAsync(attempt.OrganizationId, sale, byId, attempt.CreatedBy, now, cancellationToken)
                .ConfigureAwait(false);
            if (sale.StockReservationState == SaleStockReservationState.Released)
            {
                sale.MarkStockConsumed(now);
            }
        }

        await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Sale finalized after Paid webhook org={OrganizationId} sale={SaleId} attempt={AttemptId} stockState={StockState}",
            attempt.OrganizationId.Value,
            attempt.SaleId.Value,
            attempt.Id.Value,
            sale.StockReservationState);
    }
}

public sealed class ReconcilePaymentAttempt
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISaleRepository _sales;
    private readonly ISaleStockService _saleStock;
    private readonly IPaymentGateway _gateway;
    private readonly IPosUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ILogger<ReconcilePaymentAttempt> _logger;

    public ReconcilePaymentAttempt(
        IPaymentAttemptRepository attempts,
        ISaleRepository sales,
        ISaleStockService saleStock,
        IPaymentGateway gateway,
        IPosUnitOfWork uow,
        IClock clock,
        ILogger<ReconcilePaymentAttempt> logger)
    {
        _attempts = attempts;
        _sales = sales;
        _saleStock = saleStock;
        _gateway = gateway;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var attempt = await _attempts
                .GetByIdAsync(orgId, PaymentAttemptId.From(attemptId), cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.PaymentAttemptNotFound,
                    "Payment attempt was not found.");
            }

            var now = _clock.UtcNow;
            var prior = attempt.Status;
            attempt.ExpireIfDue(now);
            if (attempt.Status == PaymentAttemptStatus.Expired && prior != PaymentAttemptStatus.Expired)
            {
                await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
                var sale = await _sales.GetByIdAsync(orgId, attempt.SaleId, cancellationToken)
                    .ConfigureAwait(false);
                if (sale is not null)
                {
                    await _saleStock.ReleaseIfReservedAsync(sale, now, cancellationToken).ConfigureAwait(false);
                    await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
                }

                await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
            }

            if (attempt.Status == PaymentAttemptStatus.Created
                && string.IsNullOrWhiteSpace(attempt.ProviderReference)
                && attempt.Method is PaymentAttemptMethod.Card or PaymentAttemptMethod.GCash)
            {
                var expectedRef = $"fake_{attempt.Id.Value:N}";
                var session = await _gateway.GetSessionAsync(expectedRef, cancellationToken)
                    .ConfigureAwait(false);
                if (session is null)
                {
                    try
                    {
                        session = await _gateway
                            .CreateSessionAsync(
                                new PaymentGatewayCreateRequest(
                                    attempt.OrganizationId.Value,
                                    attempt.SaleId.Value,
                                    attempt.Id.Value,
                                    attempt.Method.ToString(),
                                    attempt.Amount,
                                    attempt.Currency,
                                    attempt.IdempotencyKey),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (PaymentGatewayException ex) when (ex.SessionMayExist)
                    {
                        session = await _gateway.GetSessionAsync(expectedRef, cancellationToken)
                            .ConfigureAwait(false);
                        if (session is null)
                        {
                            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
                        }
                    }
                    catch (PaymentGatewayException ex)
                    {
                        return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
                    }
                }

                if (session is not null)
                {
                    attempt.AttachProviderSession(
                        session.ProviderReference,
                        session.CheckoutUrl,
                        session.DeepLink,
                        session.QrPayload,
                        now);
                    await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
                    await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Payment attempt reconciled session org={OrganizationId} attempt={AttemptId}",
                        orgId.Value,
                        attempt.Id.Value);
                }
            }

            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SimulatePaymentOutcome
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ProcessPaymentWebhook _webhook;
    private readonly IHostEnvironment _env;

    public SimulatePaymentOutcome(
        IPaymentAttemptRepository attempts,
        ProcessPaymentWebhook webhook,
        IHostEnvironment env)
    {
        _attempts = attempts;
        _webhook = webhook;
        _env = env;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid attemptId,
        string outcome,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment() && !_env.IsEnvironment("Testing") && !_env.IsEnvironment("Local"))
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(
                DomainErrorCodes.PaymentSimulatorDisabled,
                "Payment simulation endpoints are disabled outside Development/Testing.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var attempt = await _attempts
            .GetByIdAsync(orgId, PaymentAttemptId.From(attemptId), cancellationToken)
            .ConfigureAwait(false);
        if (attempt is null || attempt.ProviderReference is null)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(
                DomainErrorCodes.PaymentAttemptNotFound,
                "Payment attempt was not found or has no provider session.");
        }

        var status = outcome.Trim().ToLowerInvariant() switch
        {
            "success" or "paid" => "Paid",
            "decline" or "failed" => "Failed",
            "cancel" or "cancelled" => "Cancelled",
            "expire" or "expired" => "Expired",
            _ => null
        };
        if (status is null)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(
                ApplicationErrorCodes.PaymentSimulationOutcomeInvalid,
                "Outcome must be success, decline, cancel, or expire.");
        }

        var body = FakePaymentGateway.BuildWebhookBody(
            attempt.ProviderReference,
            status,
            eventSequence: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            failureCode: status == "Paid" ? null : status.ToLowerInvariant(),
            failureMessage: status == "Paid" ? null : $"Simulated {status}",
            cardBrand: attempt.Method == PaymentAttemptMethod.Card && status == "Paid" ? "Visa" : null,
            cardLastFour: attempt.Method == PaymentAttemptMethod.Card && status == "Paid" ? "4242" : null);
        var signature = FakePaymentGateway.ComputeSignature(body);
        return await _webhook.ExecuteAsync(FakePaymentGateway.ProviderCodeValue, signature, body, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class VerifyManualGCashTransfer
{
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISaleRepository _sales;
    private readonly ISaleStockService _saleStock;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _uow;
    private readonly IClock _clock;

    public VerifyManualGCashTransfer(
        IPaymentAttemptRepository attempts,
        ISaleRepository sales,
        ISaleStockService saleStock,
        ICatalogProductRepository products,
        IPosUnitOfWork uow,
        IClock clock)
    {
        _attempts = attempts;
        _sales = sales;
        _saleStock = saleStock;
        _products = products;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PaymentAttemptDto>> ExecuteAsync(
        Guid organizationId,
        Guid attemptId,
        Guid verifierId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var attempt = await _attempts
                .GetByIdAsync(orgId, PaymentAttemptId.From(attemptId), cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                return ApplicationResult<PaymentAttemptDto>.Failure(
                    DomainErrorCodes.PaymentAttemptNotFound,
                    "Payment attempt was not found.");
            }

            var now = _clock.UtcNow;
            attempt.VerifyManualTransfer(verifierId, reason, now);

            var sale = await _sales.GetByIdAsync(orgId, attempt.SaleId, cancellationToken)
                .ConfigureAwait(false);
            if (sale is not null && sale.Status == SaleStatus.AwaitingPayment)
            {
                sale.FinalizeAfterPayment(attempt.ExternalReference, now);
                var products = await _products
                    .ListByIdsAsync(orgId, sale.Lines.Select(l => l.ProductId).ToList(), cancellationToken)
                    .ConfigureAwait(false);
                var byId = products.ToDictionary(p => p.Id.Value);
                if (sale.StockReservationState == SaleStockReservationState.Reserved)
                {
                    await _saleStock
                        .ConsumeReservedForPaidAsync(sale, byId, verifierId, now, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _saleStock
                        .DeductForSaleAsync(orgId, sale, byId, verifierId, now, cancellationToken)
                        .ConfigureAwait(false);
                }

                await _sales.UpdateAsync(sale, cancellationToken).ConfigureAwait(false);
            }

            await _attempts.UpdateAsync(attempt, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PaymentAttemptDto>.Success(PaymentAttemptMaps.Map(attempt));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentAttemptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
