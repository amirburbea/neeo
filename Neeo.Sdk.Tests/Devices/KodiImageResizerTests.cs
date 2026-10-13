using System;
using System.IO;
using Neeo.Drivers.Kodi;
using SkiaSharp;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public class KodiImageResizerTests
{
    private static byte[] CreateTestJpeg(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return data.ToArray();
    }

    [Fact]
    public void Resize_DownscalesImage_PreservingAspectRatio()
    {
        // 1920x1080 -> max 120 should become 120x68 (1080 * 120 / 1920 = 67.5 -> 68)
        byte[] original = KodiImageResizerTests.CreateTestJpeg(1920, 1080);

        byte[] resized = KodiImageResizer.Resize(original, 120);

        Assert.NotSame(original, resized);
        Assert.True(resized.Length < original.Length);

        using SKBitmap decoded = SKBitmap.Decode(resized);
        Assert.NotNull(decoded);
        Assert.Equal(120, decoded.Width);
        Assert.Equal(68, decoded.Height);
    }

    [Fact]
    public void Resize_FromStream_DownscalesDirectly()
    {
        byte[] original = KodiImageResizerTests.CreateTestJpeg(1920, 1080);
        using MemoryStream stream = new(original);

        byte[] resized = KodiImageResizer.Resize(stream, 120);

        Assert.True(resized.Length < original.Length);

        using SKBitmap decoded = SKBitmap.Decode(resized);
        Assert.NotNull(decoded);
        Assert.Equal(120, decoded.Width);
        Assert.Equal(68, decoded.Height);
    }

    [Fact]
    public void Resize_FromNonSeekableStream_DownscalesDirectly()
    {
        byte[] original = KodiImageResizerTests.CreateTestJpeg(1920, 1080);
        using MemoryStream stream = new(original);
        using NonSeekableStream nonSeekableStream = new(stream);

        byte[] resized = KodiImageResizer.Resize(nonSeekableStream, 120);

        Assert.True(resized.Length < original.Length);

        using SKBitmap decoded = SKBitmap.Decode(resized);
        Assert.NotNull(decoded);
        Assert.Equal(120, decoded.Width);
        Assert.Equal(68, decoded.Height);
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void Resize_TallImage_PreservesAspectRatio()
    {
        // 1000x2000 -> max 480 should become 240x480
        byte[] original = KodiImageResizerTests.CreateTestJpeg(1000, 2000);

        byte[] resized = KodiImageResizer.Resize(original, 480);

        Assert.NotSame(original, resized);

        using SKBitmap decoded = SKBitmap.Decode(resized);
        Assert.NotNull(decoded);
        Assert.Equal(240, decoded.Width);
        Assert.Equal(480, decoded.Height);
    }

    [Fact]
    public void Resize_ImageSmallerThanMaxDimension_PreservesDimensions()
    {
        byte[] original = KodiImageResizerTests.CreateTestJpeg(100, 80);

        byte[] resized = KodiImageResizer.Resize(original, 120);

        using SKBitmap decoded = SKBitmap.Decode(resized);
        Assert.NotNull(decoded);
        Assert.Equal(100, decoded.Width);
        Assert.Equal(80, decoded.Height);
    }

    [Fact]
    public void Resize_CorruptData_ThrowsInvalidOperationException()
    {
        byte[] corrupt = [1, 2, 3, 4, 5];

        Assert.Throws<InvalidOperationException>(() => KodiImageResizer.Resize(corrupt, 120));
    }

    [Fact]
    public void Resize_EmptyData_ReturnsEmptyBytes()
    {
        byte[] empty = [];

        byte[] result = KodiImageResizer.Resize(empty, 120);

        Assert.Same(empty, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Resize_InvalidMaxDimension_ThrowsArgumentOutOfRangeException(int invalidDimension)
    {
        byte[] original = KodiImageResizerTests.CreateTestJpeg(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => KodiImageResizer.Resize(original, invalidDimension));

        using MemoryStream stream = new(original);
        Assert.Throws<ArgumentOutOfRangeException>(() => KodiImageResizer.Resize(stream, invalidDimension));
    }
}
