using System;

namespace Neeo.Api.Devices;

public sealed class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
