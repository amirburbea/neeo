﻿using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// A callback invoked by the NEEO Brain to check whether registration has been previously
/// performed successfully.
/// <para />
/// If the task result is <see langword="true"/> then the NEEO Brain will skip registration.
/// </summary>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<bool> QueryIsRegistered();
