using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
#if ANDROID
using Android.Graphics;
#endif

namespace ExItS.PinoyBusinessPOS.Maui.Services;

public sealed record ProductImagePickResult(bool Succeeded, byte[]? Bytes = null, string? FileName = null, string? ErrorKey = null);

public interface IProductImagePicker
{
    Task<ProductImagePickResult> CaptureAsync(CancellationToken ct = default);

    Task<ProductImagePickResult> PickAsync(CancellationToken ct = default);
}

/// <summary>
/// MAUI MediaPicker for one primary product image. Optional Android downsample (longest side ~1600px)
/// is client optimization only; the server remains authoritative.
/// </summary>
public sealed class MauiProductImagePicker : IProductImagePicker
{
    public const int LongestSidePx = 1600;

    public Task<ProductImagePickResult> CaptureAsync(CancellationToken ct = default) =>
        CaptureOrPickAsync(useCamera: true, ct);

    public Task<ProductImagePickResult> PickAsync(CancellationToken ct = default) =>
        CaptureOrPickAsync(useCamera: false, ct);

    private static async Task<ProductImagePickResult> CaptureOrPickAsync(bool useCamera, CancellationToken ct)
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
                return new ProductImagePickResult(false, ErrorKey: "Catalog_Image_CameraDenied");
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                return new ProductImagePickResult(false, ErrorKey: "Catalog_Image_CameraUnavailable");
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
            return new ProductImagePickResult(false, ErrorKey: "Catalog_Image_CameraDenied");
        }
        catch (FeatureNotSupportedException)
        {
            return new ProductImagePickResult(
                false,
                ErrorKey: useCamera ? "Catalog_Image_CameraUnavailable" : "Catalog_Image_GalleryUnavailable");
        }

        if (photo is null)
        {
            return new ProductImagePickResult(false, ErrorKey: "Catalog_Image_Cancelled");
        }

        await using var stream = await photo.OpenReadAsync().ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        var bytes = MaybeDownsample(buffer.ToArray());
        var name = string.IsNullOrWhiteSpace(photo.FileName) ? "image.jpg" : photo.FileName;
        return new ProductImagePickResult(true, bytes, name);
    }

    private static byte[] MaybeDownsample(byte[] bytes)
    {
#if ANDROID
        try
        {
            var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, bounds);
            var longest = Math.Max(bounds.OutWidth, bounds.OutHeight);
            if (longest <= LongestSidePx || longest <= 0)
            {
                return bytes;
            }

            var sample = 1;
            while (longest / sample > LongestSidePx)
            {
                sample *= 2;
            }

            var decode = new BitmapFactory.Options { InSampleSize = sample };
            using var bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, decode);
            if (bitmap is null)
            {
                return bytes;
            }

            using var output = new MemoryStream();
            var format = Bitmap.CompressFormat.Jpeg;
            if (format is null)
            {
                return bytes;
            }

            bitmap.Compress(format, 85, output);
            return output.ToArray();
        }
        catch (Exception)
        {
            return bytes;
        }
#else
        return bytes;
#endif
    }
}
