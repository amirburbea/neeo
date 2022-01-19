using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neeo.Api;

public interface INotificationMapping
{
}

internal sealed class NotificationMapping : INotificationMapping
{
    private readonly IApiClient _client;

    public NotificationMapping(IApiClient client)
    {
        this._client = client;
    }
}
