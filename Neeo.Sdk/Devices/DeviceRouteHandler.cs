using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked via a custom REST route to be handled by the device driver.
/// 
/// This can be used to allow the driver to host web content (such as dynamically generated images)
/// without requiring setting up another HTTP endpoint.
/// </summary>
/// <param name="request">The current HTTP request.</param>
/// <param name="path">The path substring (the substring of the true path after the URI prefix returned via a <see cref="UriPrefixCallback"/>).</param>
/// <param name="cancellationToken">Token to monitor for cancelled requests.</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task<ActionResult> DeviceRouteHandler(HttpRequest request, string path, CancellationToken cancellationToken);