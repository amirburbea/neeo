using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    public readonly struct SuccessResult
    {
        [JsonConstructor]
        public SuccessResult(bool success) => this.Success = success;

        public bool Success { get; }
    }
}