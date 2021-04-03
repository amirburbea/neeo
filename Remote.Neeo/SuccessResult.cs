using System.Text.Json.Serialization;

namespace Remote.Neeo {
    /// <summary>
    /// A struct indicating success. Used a standard return type for NEEO Brain APIs.
    /// </summary>
    public readonly struct SuccessResult {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuccessResult"/> struct.
        /// </summary>
        /// <param name="success">A value indicating success of the operation.</param>
        [JsonConstructor]
        public SuccessResult(bool success) => this.Success = success;

        /// <summary>
        /// Gets a value indicating success of the operation.
        /// </summary>
        public bool Success { get; }
    }
}