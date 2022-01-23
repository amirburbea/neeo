﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neeo.Api.Devices;

namespace Remote.HodgePodge;

public interface IDeviceProvider
{
    IDeviceBuilder ProvideDevice();
}