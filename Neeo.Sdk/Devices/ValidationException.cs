using System;

namespace Neeo.Sdk.Devices;

public sealed class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
