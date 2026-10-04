namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Platform-agnostic abstraction over host application identity and version metadata.
/// </summary>
public interface IAppInfoService
{
    string AppName { get; }
    string Version { get; }
    string EnvironmentName { get; }
}
