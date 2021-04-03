using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Remote.Neeo.Notifications
{
    public interface INotificationService
    {
        Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    }

    internal sealed class NotificationService : INotificationService
    {
        private readonly IApiClient _client;
        private readonly Dictionary<string, NotificationMessage> _cache = new();
        private readonly ILogger<NotificationService> _logger;
        private int _queueSize;

        public NotificationService(IApiClient client, ILogger<NotificationService> logger)
        {
            (this._client, this._logger) = (client, logger);
        }

        public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            if (message.Type == null)
            {
                this._logger.LogWarning("Ignored: Uninitialized message.");
                return false;
            }
            if (this.IsDuplicate(message))
            {
                this._logger.LogWarning("Ignored: Duplicate message.");
                return false;
            }
            if (this._queueSize >= Constants.MaxQueueSize)
            {
                this._logger.LogDebug("Ignored: Max Queue Size Reached.");
                return false;
            }
            this._logger.LogDebug("Sending ({type},{data})", message.Type, message.Data);
            this._queueSize++;
            bool success;
            try
            {
                SuccessResult result = await this._client.PostAsync<NotificationMessage, SuccessResult>(UrlPaths.Notifications, message, cancellationToken).ConfigureAwait(false);
                if (!(success = result.Success))
                {
                    this._logger.LogWarning("Failed to send notification - Brain rejected.");
                }
                else
                {
                    this.UpdateCache(message);
                }
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Failed to send notification.");
                success = false;
            }
            finally
            {
                this.DecreaseQueueSize();
            }
            return success;
        }

        private void DecreaseQueueSize()
        {
            if (this._queueSize != 0)
            {
                this._queueSize--;
            }
        }

        private bool IsDuplicate(NotificationMessage message)
        {
            return this._cache.TryGetValue(message.Type, out NotificationMessage other) && other.Equals(message);
        }

        private void UpdateCache(NotificationMessage message)
        {
            if (this._cache.Count > Constants.MaxCachedEntries)
            {
                this._logger.LogInformation("Clearing notification cache. Cache size exceeded.");
                this._cache.Clear();
            }
            this._cache[message.Type] = message;
        }

        private static class Constants
        {
            public const int MaxCachedEntries = 50;
            public const int MaxQueueSize = 20;
        }
    }
}