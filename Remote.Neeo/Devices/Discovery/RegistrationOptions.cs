namespace Remote.Neeo.Devices.Discovery
{
    public abstract class RegistrationOptions
    {
        protected RegistrationOptions(RegistrationType type)
        {
            this.Type = type;
        }

        public RegistrationType Type { get; }
    }
}
