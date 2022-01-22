using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Api.Rest;

public interface IShutdownStep
{
    Task OnShutdownAsync(CancellationToken cancellationToken = default);
}