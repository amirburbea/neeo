using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Examples;

/// <summary>
/// Serves the JPEG images embedded in this assembly (see the <c>Images</c> folder), e.g. <c>{prefix}puppy.jpg</c>.
/// Use as the route handler passed to <c>EnableDeviceRoute</c>.
/// </summary>
internal static class EmbeddedImages
{
    public static Task<ActionResult> HandleRequestAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        ActionResult result = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && typeof(EmbeddedImages).Assembly.GetManifestResourceStream(path) is { } stream
            ? new FileStreamResult(stream, "image/jpeg")
            : new NotFoundResult();
        return Task.FromResult(result);
    }
}
