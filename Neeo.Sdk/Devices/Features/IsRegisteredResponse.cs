namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Container for a value indicating if registration was previously performed in response to a call to <see cref="IRegistrationFeature.QueryIsRegisteredAsync"/>.
/// </summary>
/// <param name="Registered">A value indicating if registration was previously performed.</param>
public readonly record struct IsRegisteredResponse(bool Registered);
