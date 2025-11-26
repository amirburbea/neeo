using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Server;

public sealed class SdkService(
    IEnumerable<IDeviceProvider> providers,
    IConfiguration configuration,
    TaskCompletionSource<ISdkEnvironment> environmentTaskSource,
    IHostApplicationLifetime applicationLifetime,
    ILogger<SdkService> logger
) : BackgroundService
{
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (environmentTaskSource.Task.IsCompletedSuccessfully)
            {
                logger.LogInformation("Stopping server...");
                ISdkEnvironment environment = await environmentTaskSource.Task.ConfigureAwait(false);
                await environment.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            AppDomain.CurrentDomain.UnhandledException -= SdkService.OnAppDomainUnhandledException;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Brain brain;
        try
        {
            brain = await GetBrainAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Brain discovery was cancelled!");
            environmentTaskSource.TrySetCanceled(stoppingToken);
            applicationLifetime.StopApplication();
            return;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to discover Brain. Shutting down...\n");
            environmentTaskSource.TrySetException(e);
            applicationLifetime.StopApplication();
            return;
        }
        AppDomain.CurrentDomain.UnhandledException += SdkService.OnAppDomainUnhandledException;
        logger.LogInformation("Using Brain {Name} at {Endpoint}...", brain.HostName, brain.ServiceEndPoint);
        ISdkEnvironment environment = await brain.StartServerAsync([.. providers], name: configuration.GetValue<string>("ServerName"), cancellationToken: stoppingToken).ConfigureAwait(false);
        environmentTaskSource.TrySetResult(environment);
        logger.LogInformation("Started server at address {Address}...", environment.HostAddress);
        logger.LogInformation("Brain WebUI is running at http://{IPAddress}:3200/eui", brain.IPAddress);
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);

        async ValueTask<Brain> GetBrainAsync()
        {
            if (configuration.GetValue<string>(nameof(Brain)) is { } text && IPAddress.TryParse(text, out IPAddress? address))
            {
                return new(address);
            }
            logger.LogInformation("Discovering Brain...");
            if (await Brain.DiscoverOneAsync(cancellationToken: stoppingToken).ConfigureAwait(false) is not { } brain)
            {
                throw new ApplicationException("Discovery failed, host stopping.");
            }
            return brain;
        }
    }

    private static void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        SdkService.PrintException((Exception)e.ExceptionObject, e.IsTerminating);
    }

    private static void PrintException(Exception exception, bool isTerminating)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"🚨 Unhandled Exception:");
        Console.WriteLine($"IsTerminating: {isTerminating}");
        Console.WriteLine($"Type: {exception.GetType().FullName}");
        Console.WriteLine($"Message: {exception.Message}");
        Console.WriteLine($"StackTrace:\n{exception.StackTrace}");
        Console.ResetColor();
    }
}
