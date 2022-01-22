using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Api.Rest;

public interface IStartupStep
{
    



    Task OnStartAsync(CancellationToken cancellationToken = default);
}