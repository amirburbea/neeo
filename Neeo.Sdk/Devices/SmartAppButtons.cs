using System;
using System.Collections.Generic;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// The NEEO Brain has special rendering for launcher buttons of several known SmartTV applications.
/// </summary>
[Flags]
public enum SmartAppButtons
{
    /// <summary>
    /// &quot;AMAZON&quot;
    /// </summary>
    [Text("AMAZON")]
    Amazon = 1,

    /// <summary>
    /// &quot;CRACKLE&quot;
    /// </summary>
    [Text("CRACKLE")]
    Crackle = 1 << 1,

    /// <summary>
    /// &quot;GOOGLE PLAY&quot;
    /// </summary>
    [Text("GOOGLE PLAY")]
    GooglePlay = 1 << 2,

    /// <summary>
    /// &quot;HULU&quot;
    /// </summary>
    [Text("HULU")]
    Hulu = 1 << 3,

    /// <summary>
    /// &quot;HULU PLUS&quot;
    /// </summary>
    [Text("HULU PLUS")]
    HuluPlus = 1 << 4,

    /// <summary>
    /// &quot;NETFLIX&quot;
    /// </summary>
    [Text("NETFLIX")]
    Netflix = 1 << 5,

    /// <summary>
    /// &quot;INPUT SPOTIFY&quot;
    /// </summary>
    [Text("INPUT SPOTIFY")]
    Spotify = 1 << 6,

    /// <summary>
    /// &quot;VIMEO&quot;
    /// </summary>
    [Text("VIMEO")]
    Vimeo = 1 << 7,

    /// <summary>
    /// &quot;VUDU&quot;
    /// </summary>
    [Text("VUDU")]
    Vudu = 1 << 8,

    /// <summary>
    /// &quot;YOU TUBE&quot;
    /// </summary>
    [Text("YOU TUBE")]
    YouTube = 1 << 9
}

/// <summary>
/// Contains <see langword="static"/> methods for interacting with the <see cref="SmartAppButtons"/> enumeration.
/// </summary>
public static class SmartAppButton
{
    /// <summary>
    /// Gets the button names in the specified combination of <paramref name="buttons"/>.
    /// </summary>
    /// <param name="buttons">The (potentially flagged) <see cref="SmartAppButtons"/> value.</param>
    /// <returns>The collection of button names.</returns>
    public static IEnumerable<string> GetNames(SmartAppButtons buttons) => FlaggedEnumerations<SmartAppButtons>.GetNames(buttons);

    /// <summary>
    /// Attempts to get the associated <see cref="SmartAppButtons"/> value for a button name.
    /// </summary>
    /// <param name="name">The name of the button.</param>
    /// <returns><see cref="KnownButtons"/> value if found, otherwise <c>null</c>.</returns>
    public static SmartAppButtons? TryResolve(string name) => FlaggedEnumerations<SmartAppButtons>.TryResolve(name);
}