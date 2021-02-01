using System;

namespace Remote.Utilities
{
    public sealed class DataEventArgs<TData> : EventArgs
    {
        public DataEventArgs(TData data)
        {
            this.Data = data;
        }

        public TData Data { get; }
    }
}
