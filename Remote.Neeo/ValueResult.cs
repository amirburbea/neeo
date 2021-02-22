using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    public readonly struct ValueResult
    {
        [JsonConstructor]
        public ValueResult(object? value) => this.Value = value;

        public object? Value { get; }
    }
}