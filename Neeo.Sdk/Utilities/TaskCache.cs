using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

public sealed class TaskCache<TValue>
{
    private readonly TimeSpan _cacheTime;
    private ValueContainer? _container;
    private Task<TValue>? _task;

    public TaskCache(int cacheMilliseconds = 10000, string? uniqueIdentifier = default)
    {
        this._cacheTime = TimeSpan.FromMilliseconds(cacheMilliseconds);
        this.UniqueIdentifier = uniqueIdentifier ?? DateTime.Now.ToString();
    }

    public string UniqueIdentifier { get; }

    public async ValueTask<TValue> GetValueAsync(Func<Task<TValue>>? taskFactory = default)
    {
        if (this._container is { Value: TValue value, Age: TimeSpan age } && age < this._cacheTime)
        {
            return value;
        }
        if (this._task != null)
        {
            return await this._task.ConfigureAwait(false);
        }
        this._task = (taskFactory ?? throw new ArgumentNullException(nameof(taskFactory))).Invoke();
        try
        {
            value = await this._task.ConfigureAwait(false);
        }
        finally
        {
            this._task = null;
        }
        this._container = new(value);
        return value;
    }

    private record ValueContainer(TValue Value)
    {
        public TimeSpan Age => DateTime.Now - this.Time;

        public DateTime Time { get; } = DateTime.Now;
    }
}