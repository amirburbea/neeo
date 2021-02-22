using System;
using System.Linq;

namespace Remote.Neeo.Devices.Descriptors
{
    /// <summary>
    /// Describes a Button for the NEEO Remote.
    /// </summary>
    public sealed record ButtonDescriptor : Descriptor, IComparable<ButtonDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonDescriptor"/> record.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <param name="label">An optional label to use in place of the name.</param>
        public ButtonDescriptor(string name, string? label = default)
            : base(name, label)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonDescriptor"/> record.
        /// </summary>
        /// <param name="button">
        /// The button to create.
        /// <para/>
        /// Note that the use of bitwise (flagged) combinations of <see cref="KnownButtons"/> is not allowed.
        /// </param>
        /// <param name="label">An optional label to use in place of the name.</param>
        public ButtonDescriptor(KnownButtons button, string? label = default)
            : base(KnownButton.GetNames(button).Single(), label)
        {
        }

        int IComparable<ButtonDescriptor>.CompareTo(ButtonDescriptor? other) => StringComparer.OrdinalIgnoreCase.Compare(this.Name, other?.Name);

        /// <summary>
        /// Implicitly create a <see cref="ButtonDescriptor"/> from a <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the button to create.</param>
        public static implicit operator ButtonDescriptor(string name) => new(name);
    }
}