using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="IAppInfoService"/> wrapping
/// <see cref="Microsoft.Maui.ApplicationModel.AppInfo"/>. <see cref="EnvironmentName"/> is
/// configured at startup from the build configuration (DEBUG vs RELEASE), since MAUI has no
/// concept of ASP.NET Core hosting environments.
/// </summary>
public sealed class MauiAppInfoService(string environmentName) : IAppInfoService
{
    public string AppName => AppInfo.Current.Name;

    public string Version => $"{AppInfo.Current.VersionString}+{AppInfo.Current.BuildString}";

    public string EnvironmentName { get; } = environmentName;
}
