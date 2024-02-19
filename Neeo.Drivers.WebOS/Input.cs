using System;
using System.Collections.Generic;

namespace Neeo.Drivers.WebOS;

public enum Input
{
    [Input("externalinput.av1")]
    AV,

    Hdmi1,
    Hdmi2,
    Hdmi3,
    Hdmi4,
    LiveTV,
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class InputAttribute(string name) : Attribute, INameAttribute
{
    public string Name { get; } = name;
}

public static class InputName
{
    private static readonly IReadOnlyDictionary<Input, string> _names = NameDictionary.Generate<Input, InputAttribute>();

    public static string Of(Input input) => InputName._names[input];
}
