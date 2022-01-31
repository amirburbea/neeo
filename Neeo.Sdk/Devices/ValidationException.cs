using System;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An exception that is raised in response to an invalid device or list configuration.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Creates a new <see cref="ValidationException"/> with the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The message for the exception.</param>
    internal ValidationException(string message)
        : base(message)
    {
    }
}