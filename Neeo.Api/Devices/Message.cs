namespace Neeo.Api.Devices;

public record class Message(string Name, object Value, bool? Raw = default);
