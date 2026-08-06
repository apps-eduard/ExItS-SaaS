using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using ZXing;
using ZXing.Common;
#if ANDROID
using Android.Graphics;
#endif

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Result of a local QR capture/decode. Does not call Platform APIs — caller resolves the payload.
/// </summary>
public sealed record QrScanResult(bool Succeeded, string? Payload = null, string? ErrorKey = null);

/// <summary>
/// Captures or picks a QR image and decodes the ExItS public-identity payload locally.
/// </summary>
public interface IQrCodeScanService
{
    Task<QrScanResult> CaptureAsync(CancellationToken ct = default);

    Task<QrScanResult> PickFromGalleryAsync(CancellationToken ct = default);
}

/// <summary>
/// MAUI MediaPicker + ZXing still-image decode for Blazor Hybrid (no embedded live camera view).
/// </summary>
public sealed class MauiQrCodeScanService : IQrCodeScanService
{
    public Task<QrScanResult> CaptureAsync(CancellationToken ct = default) =>
        CaptureOrPickAsync(useCamera: true, ct);

    public Task<QrScanResult> PickFromGalleryAsync(CancellationToken ct = default) =>
        CaptureOrPickAsync(useCamera: false, ct);

    private static async Task<QrScanResult> CaptureOrPickAsync(bool useCamera, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (useCamera)
        {
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);
            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);
            }

            if (cameraStatus != PermissionStatus.Granted)
            {
                return new QrScanResult(false, ErrorKey: "Personal_ScanCameraDenied");
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                return new QrScanResult(false, ErrorKey: "Personal_ScanCameraUnavailable");
            }
        }

        FileResult? photo;
        try
        {
            photo = useCamera
                ? await MediaPicker.Default.CapturePhotoAsync().ConfigureAwait(false)
                : await MediaPicker.Default.PickPhotoAsync().ConfigureAwait(false);
        }
        catch (PermissionException)
        {
            return new QrScanResult(false, ErrorKey: "Personal_ScanCameraDenied");
        }
        catch (FeatureNotSupportedException)
        {
            return new QrScanResult(
                false,
                ErrorKey: useCamera ? "Personal_ScanCameraUnavailable" : "Personal_ScanGalleryUnavailable");
        }

        if (photo is null)
        {
            return new QrScanResult(false, ErrorKey: "Personal_ScanCancelled");
        }

        await using var stream = await photo.OpenReadAsync().ConfigureAwait(false);
        return DecodeStream(stream);
    }

    private static QrScanResult DecodeStream(Stream stream)
    {
#if ANDROID
        try
        {
            using var bitmap = BitmapFactory.DecodeStream(stream);
            if (bitmap is null)
            {
                return new QrScanResult(false, ErrorKey: "Personal_ScanDecodeFailed");
            }

            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixels = new int[width * height];
            bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);

            // RGBLuminanceSource expects packed RGBA bytes (4 bytes per pixel).
            var rgba = new byte[pixels.Length * 4];
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var offset = i * 4;
                rgba[offset] = (byte)((pixel >> 16) & 0xFF);     // R
                rgba[offset + 1] = (byte)((pixel >> 8) & 0xFF);  // G
                rgba[offset + 2] = (byte)(pixel & 0xFF);         // B
                rgba[offset + 3] = (byte)((pixel >> 24) & 0xFF); // A
            }

            var source = new RGBLuminanceSource(
                rgba,
                width,
                height,
                RGBLuminanceSource.BitmapFormat.RGBA32);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = [BarcodeFormat.QR_CODE],
                    TryHarder = true
                }
            };

            var result = reader.Decode(source);
            if (result is null || string.IsNullOrWhiteSpace(result.Text))
            {
                return new QrScanResult(false, ErrorKey: "Personal_ScanNoQrFound");
            }

            return new QrScanResult(true, Payload: result.Text.Trim());
        }
        catch
        {
            return new QrScanResult(false, ErrorKey: "Personal_ScanDecodeFailed");
        }
#else
        _ = stream;
        return new QrScanResult(false, ErrorKey: "Personal_ScanCameraUnavailable");
#endif
    }
}
