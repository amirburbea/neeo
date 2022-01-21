namespace Neeo.Api.Devices.Discovery;

public record struct IsRegisteredResponse(bool Registered)
{
    public static implicit operator IsRegisteredResponse(bool registered) => new(registered);
}