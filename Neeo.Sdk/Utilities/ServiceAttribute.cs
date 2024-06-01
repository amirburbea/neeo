using System;
using Microsoft.Extensions.DependencyInjection;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Annotation specifying that the decorated <see langword="class" /> should be registered a service.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceAttribute : Attribute
{
    /// <summary>
    /// Annotation specifying that the decorated <see langword="class" /> should be registered a service
    /// with the specified <paramref name="lifetime" />.
    /// </summary>
    /// <param name="lifetime">The service lifetime.</param>
    public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        : this(null, lifetime)
    {
    }

    /// <summary>
    /// Annotation specifying that the decorated <see langword="class" /> should be registered a service
    /// implementing <paramref name="serviceType"/> with the specified <paramref name="lifetime" />.
    /// </summary>
    /// <param name="serviceType">The type of the service to register the decorated <see langword="class" /> as an implementation of.</param>
    /// <param name="lifetime">The service lifetime.</param>
    public ServiceAttribute(Type? serviceType, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        (this.ServiceType, this.Lifetime) = (serviceType, lifetime);
    }

    /// <summary>
    /// The service lifetime.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Optional, the type of the service to register the decorated <see langword="class" /> as an implementation of.
    /// When <see langword="null"/>, the <see langword="class" /> is registered as both the implementation and service type.
    /// </summary>
    public Type? ServiceType { get; }
}
