using System.Collections.Generic;

namespace Artmann.Terminal
{
    public delegate IEnumerable<(string Name, string Description)> ArgumentSuggester(int argIndex, string partial);
}
