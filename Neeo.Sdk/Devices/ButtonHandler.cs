﻿using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A callback which is invoked in response to buttons being pressed on the NEEO remote
/// in order to allow the driver to respond accordingly.
/// <para />
/// </summary>
/// <param name="deviceId">The id associated with the device.</param>
/// <param name="button">
/// The name of the button being pressed.
/// <para/>
/// Note that <see cref="KnownButton.GetKnownButton"/> may be able to translate this into a <see cref="KnownButtons"/> enumerated value.
/// </param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task ButtonHandler(string deviceId, string button);