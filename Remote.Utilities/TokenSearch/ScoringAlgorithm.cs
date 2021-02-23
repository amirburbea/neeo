using System.Collections.Generic;

namespace Remote.Utilities.TokenSearch
{
    public delegate int ScoringAlgorithm(string text, IEnumerable<string> searchTokens);
}