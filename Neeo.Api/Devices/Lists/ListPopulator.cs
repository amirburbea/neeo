using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neeo.Api.Devices.Lists;




public delegate Task ListPopulator(string deviceId, object parameters, IListBuilder listBuilder);
