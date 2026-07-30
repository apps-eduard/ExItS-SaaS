namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Platform-agnostic abstraction over host application identity/version metadata. MAUI
/// implementations typically wrap <c>Microsoft.Maui.ApplicationModel.AppInfo</c>.
/// </summary>
public interface IAppInfoService
{
    string AppName { get; }
    string Version { get; }
    string EnvironmentName { get; }
}
