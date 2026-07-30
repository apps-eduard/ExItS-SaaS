using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Single source of truth for the connectivity/API status shown in both the shell's compact top
/// bar badges and the Home page's detailed status cards, so the two never disagree and the POS
/// API is not polled redundantly by multiple components.
/// </summary>
public sealed class PosStatusState : IDisposable
{
    private readonly IConnectivityService _connectivity;
    private readonly IPosApiClient _apiClient;

    public PosStatusState(IConnectivityService connectivity, IPosApiClient apiClient)
    {
        _connectivity = connectivity;
        _apiClient = apiClient;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public ConnectivityStatus Connectivity { get; private set; } = ConnectivityStatus.Unknown;

    public ApiCallStatus? ApiStatus { get; private set; }

    public string? ApiHealthText { get; private set; }

    public bool IsCheckingApi { get; private set; }

    /// <summary>Raised whenever connectivity or API status changes; subscribers should call <c>StateHasChanged</c>.</summary>
    public event Func<Task>? Changed;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Connectivity = await _connectivity.IsConnectedAsync(ct).ConfigureAwait(false)
            ? ConnectivityStatus.Online
            : ConnectivityStatus.Offline;
        await RefreshApiHealthAsync(ct).ConfigureAwait(false);
    }

    public async Task RefreshApiHealthAsync(CancellationToken ct = default)
    {
        IsCheckingApi = true;
        await NotifyAsync().ConfigureAwait(false);

        var result = await _apiClient.GetHealthAsync(ct).ConfigureAwait(false);
        ApiStatus = result.Status;
        ApiHealthText = result.IsSuccess ? result.Data?.Status : result.Error?.Detail ?? result.Error?.Title;
        IsCheckingApi = false;

        await NotifyAsync().ConfigureAwait(false);
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityStatus status)
    {
        Connectivity = status;
        await NotifyAsync().ConfigureAwait(false);
    }

    private async Task NotifyAsync()
    {
        if (Changed is not null)
        {
            await Changed.Invoke().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
