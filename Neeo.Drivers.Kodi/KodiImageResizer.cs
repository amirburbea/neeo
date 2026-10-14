using System;
using System.IO;
using SkiaSharp;

namespace Neeo.Drivers.Kodi;

internal static class KodiImageResizer
{
    public static byte[] Resize(byte[] imageData, int maxDimension, int quality = 80)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDimension, 0);
        if (imageData.Length == 0)
        {
            return imageData;
        }
        using MemoryStream stream = new(imageData);
        return KodiImageResizer.Resize(stream, maxDimension, quality);
    }

    public static byte[] Resize(Stream inputStream, int maxDimension, int quality = 80)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDimension, 0);
        using SKBitmap originalBitmap = SKBitmap.Decode(inputStream) ?? throw new InvalidOperationException("Failed to decode image from stream.");
        if (originalBitmap.Width <= maxDimension && originalBitmap.Height <= maxDimension)
        {
            using SKImage origImage = SKImage.FromBitmap(originalBitmap);
            using SKData origData = origImage.Encode(SKEncodedImageFormat.Jpeg, quality);
            return origData.ToArray();
        }
        int targetWidth;
        int targetHeight;
        if (originalBitmap.Width >= originalBitmap.Height)
        {
            targetWidth = maxDimension;
            targetHeight = Math.Max(1, (int)Math.Round((double)originalBitmap.Height * maxDimension / originalBitmap.Width));
        }
        else
        {
            targetHeight = maxDimension;
            targetWidth = Math.Max(1, (int)Math.Round((double)originalBitmap.Width * maxDimension / originalBitmap.Height));
        }
        SKImageInfo targetInfo = new(targetWidth, targetHeight, originalBitmap.ColorType, originalBitmap.AlphaType);
        using SKBitmap resizedBitmap = new(targetInfo);
        if (!originalBitmap.ScalePixels(resizedBitmap.PeekPixels(), new(SKFilterMode.Linear, SKMipmapMode.Linear)))
        {
            throw new InvalidOperationException("Failed to scale image pixels.");
        }
        using SKImage image = SKImage.FromBitmap(resizedBitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}
