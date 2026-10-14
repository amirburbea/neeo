using System;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Examples;

/// <summary>
/// Serves the JPEG images embedded in this assembly (see the <c>Images</c> folder), e.g. <c>{prefix}puppy.jpg</c>.
/// </summary>
internal static class EmbeddedImages
{
    public static ActionResult HandleRequest(string path) => path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
        && typeof(EmbeddedImages).Assembly.GetManifestResourceStream(path) is { } stream
        ? new FileStreamResult(stream, "image/jpeg")
        : new NotFoundResult();
}
