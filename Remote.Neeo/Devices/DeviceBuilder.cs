using System;
using System.Collections.Generic;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Fluent interface for building device.
    /// </summary>
    public interface IDeviceBuilder
    {
        IReadOnlyCollection<string> AdditionalSearchTokens { get; }

        IButtonHandler? ButtonHandler { get; }

        IReadOnlyCollection<ButtonDescriptor> Buttons { get; }

        DelaysSpecifier? Delays { get; }

        uint DriverVersion { get; }

        IFavoritesHandler? FavoritesHandler { get; }

        DeviceIcon Icon { get; }

        string Identifier { get; }

        string Manufacturer { get; }

        string Name { get; }

        IPowerStateSensor? PowerStateSensor { get; }

        string? SpecificName { get; }

        DeviceType Type { get; }

        IDeviceBuilder AddAdditionalSearchToken(string text);

        IDeviceBuilder AddButton(string name, string? label = default);

        IDeviceBuilder AddButtonGroup(ButtonGroup group);

        IDeviceBuilder SetButtonHandler(IButtonHandler handler);

        IDeviceBuilder SetDelays(DelaysSpecifier delays);

        IDeviceBuilder SetDriverVersion(uint version);

        IDeviceBuilder SetFavoritesHandler(IFavoritesHandler handler);

        IDeviceBuilder SetIcon(DeviceIcon icon);

        IDeviceBuilder SetManufacturer(string manufacturer);

        IDeviceBuilder SetPowerStateSensor(IPowerStateSensor sensor);

        IDeviceBuilder SetSpecificName(string? specificName);

        internal IDeviceAdapter BuildAdapter();
    }

    internal sealed class DeviceBuilder : IDeviceBuilder
    {
        private readonly HashSet<string> _additionalSearchTokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ButtonDescriptor> _buttons = new();

        public DeviceBuilder(string name)
        {
            Validator.ValidateStringLength(this.Name = name ?? throw new ArgumentNullException(nameof(name)), prefix: nameof(name));
            this.Identifier = $"apt-{UniqueNameGenerator.Generate(name)}";
        }

        public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

        public IButtonHandler? ButtonHandler { get; private set; }

        public IReadOnlyCollection<ButtonDescriptor> Buttons => this._buttons;

        public DelaysSpecifier? Delays { get; private set; }

        public uint DriverVersion { get; private set; }

        public IFavoritesHandler? FavoritesHandler { get; private set; }

        public DeviceIcon Icon { get; private set; }

        public string Identifier { get; }

        public string Manufacturer { get; private set; } = "NEEO";

        public string Name { get; }

        public IPowerStateSensor? PowerStateSensor { get; private set; }

        public string? SpecificName { get; private set; }

        public DeviceType Type { get; init; }

        IDeviceBuilder IDeviceBuilder.AddAdditionalSearchToken(string text) => this.AddAdditionalSearchToken(text);

        public DeviceBuilder AddAdditionalSearchToken(string text)
        {
            this._additionalSearchTokens.Add(text);
            return this;
        }

        IDeviceBuilder IDeviceBuilder.AddButton(string name, string? label) => this.AddButton(name, label);

        public DeviceBuilder AddButton(string name, string? label = default)
        {
            this._buttons.Add(new(name, label));
            return this;
        }

        IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroup group) => this.AddButtonGroup(group);

        public DeviceBuilder AddButtonGroup(ButtonGroup group)
        {
            foreach (string name in ButtonGroupAttribute.GetNames(group))
            {
                this.AddButton(name);
            }
            return this;
        }

        IDeviceAdapter IDeviceBuilder.BuildAdapter() => null!;

        public DeviceBuilder SetButtonHandler(IButtonHandler handler)
        {
            this.ButtonHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetButtonHandler(IButtonHandler handler) => this.SetButtonHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetDelays(DelaysSpecifier delays) => this.SetDelays(delays);

        public DeviceBuilder SetDelays(DelaysSpecifier delays)
        {
            if (!this.Type.SupportsDelays())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support delays.");
            }
            this.Delays = delays;
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version) => this.SetDriverVersion(version);

        public DeviceBuilder SetDriverVersion(uint version)
        {
            this.DriverVersion = version;
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetFavoritesHandler(IFavoritesHandler handler) => this.SetFavoritesHandler(handler);

        public DeviceBuilder SetFavoritesHandler(IFavoritesHandler handler)
        {
            if (!this.Type.SupportsFavorites())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
            }
            this.FavoritesHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIcon icon) => this.SetIcon(icon);

        public DeviceBuilder SetIcon(DeviceIcon icon)
        {
            this.Icon = icon;
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer) => this.SetManufacturer(manufacturer);

        public DeviceBuilder SetManufacturer(string manufacturer = "NEEO")
        {
            Validator.ValidateStringLength(this.Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer)));
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetPowerStateSensor(IPowerStateSensor sensor) => this.SetPowerStateSensor(sensor);

        public DeviceBuilder SetPowerStateSensor(IPowerStateSensor sensor)
        {
            this.PowerStateSensor = sensor;
            return this;
        }

        IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName) => this.SetSpecificName(specificName);

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
