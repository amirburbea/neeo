using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

public static class UriMethods
{
    public static Uri Combine(this Uri uri, string relativeUri) => Uri.TryCreate(uri, relativeUri, out Uri? result)
        ? result
        : uri;

    public static Uri Combine(this Uri uri, Uri relativeUri) => Uri.TryCreate(uri, relativeUri, out Uri? result)
        ? result
        : uri;
}
