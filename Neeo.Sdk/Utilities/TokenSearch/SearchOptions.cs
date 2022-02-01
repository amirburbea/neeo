using System;

namespace Neeo.Sdk.Utilities.TokenSearch;

internal sealed class SearchOptions<T>
    where T : notnull, IComparable<T>
{



    public string[]? SearchProperties { get; set; }
}
