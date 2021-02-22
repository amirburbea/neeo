using System;

namespace Remote.Neeo.Devices.Discovery
{
    public readonly struct RegistrationOptions
    {
        public RegistrationOptions(string headerText, string description)
        {
            this.HeaderText = headerText ?? throw new ArgumentNullException(nameof(headerText));
            this.Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public string Description { get; }

        public string HeaderText { get; }
    }
}