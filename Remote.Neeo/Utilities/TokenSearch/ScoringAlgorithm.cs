using System.Collections.Generic;

namespace Remote.Neeo.Utilities.TokenSearch;

public delegate int ScoringAlgorithm(string text, IEnumerable<string> searchTokens);
