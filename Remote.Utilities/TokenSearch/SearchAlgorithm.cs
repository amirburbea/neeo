using System.Collections.Generic;

namespace Remote.Utilities.TokenSearch
{
    public delegate int SearchAlgorithm(string hayStack, IEnumerable<string> needles);
}