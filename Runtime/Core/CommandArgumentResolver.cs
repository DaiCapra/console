using System;
using System.Collections.Generic;
using System.Linq;

namespace Artmann.Terminal
{
    public class CommandArgumentResolver
    {
        private readonly CommandConsole _console;

        public CommandArgumentResolver(CommandConsole console)
        {
            _console = console;
        }

        public IEnumerable<(string Name, string Description)> Suggest(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Enumerable.Empty<(string, string)>();
            }

            var parts = text.Split(' ');
            if (parts.Length < 2)
            {
                return Enumerable.Empty<(string, string)>();
            }

            var commandName = parts[0];
            var suggester = _console.GetArgumentSuggester(commandName);
            if (suggester == null)
            {
                return Enumerable.Empty<(string, string)>();
            }

            var partial = parts[^1];
            var argIndex = parts.Length - 2;

            if (argIndex < 0)
            {
                return Enumerable.Empty<(string, string)>();
            }

            return suggester(argIndex, partial)
                .Where(s => string.IsNullOrEmpty(partial)
                    || s.Name.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
    }
}
