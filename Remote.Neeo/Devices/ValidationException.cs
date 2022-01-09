﻿using System;

namespace Remote.Neeo.Devices;

public sealed class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
