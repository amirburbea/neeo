using System;
using System.Collections.Generic;
using System.Linq;

namespace Remote.Neeo.Devices
{
    public sealed class DeviceBuilder
    {
        private readonly HashSet<string> _additionalSearchTokens = new(StringComparer.OrdinalIgnoreCase);

        private DeviceBuilder(string name)
        {
            Validator.ValidateStringLength(name, prefix: "Device name");
            this.Identifier = $"apt-{UniqueName.Generate(this.Name = name)}";
        }

        public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

        public int DriverVersion
        {
            get;
            private set;
        }

        public DeviceIcon Icon { get; private set; }

        public string Identifier { get; }

        public string Manufacturer { get; private set; } = "NEEO";

        public string Name { get; }

        public string? SpecificName { get; private set; }

        public DeviceType Type { get; init; }

        public static DeviceBuilder BuildDevice(string name, DeviceType type = DeviceType.Accessory) => new(name) { Type = type };

        public DeviceBuilder AddAdditionalSearchToken(string text)
        {
            this._additionalSearchTokens.Add(text);
            return this;
        }

        public DeviceBuilder AddButton(string name, string? label = default)
        {

            return this;
        }

        public DeviceBuilder AddButtonGroup(ButtonGroup group)
        {
            foreach (string name in ButtonGroupAttribute.GetNames(group))
            {
                this.AddButton(name);
            }
            return this;
        }

        public DeviceBuilder ClearAdditionalSearchTokens()
        {
            this._additionalSearchTokens.Clear();
            return this;
        }

        public DeviceBuilder SetDriverVersion(byte version)
        {
            this.DriverVersion = version;
            return this;
        }

        public DeviceBuilder SetIcon(DeviceIcon icon)
        {
            this.Icon = icon;
            return this;
        }

        public DeviceBuilder SetManufacturer(string manufacturer = "NEEO")
        {
            Validator.ValidateStringLength(this.Manufacturer = manufacturer);
            return this;
        }

        public DeviceBuilder SetSpecificName(string? specificName)
        {
            if (specificName != null)
            {
                Validator.ValidateStringLength(specificName);
            }
            this.SpecificName = specificName;
            return this;
        }
    }
}
