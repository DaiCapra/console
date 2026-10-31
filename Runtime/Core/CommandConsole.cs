using System;
using System.Collections.Generic;
using System.Linq;

namespace Artmann.Terminal
{
    // Command registry, output buffer and command history.
    public class CommandConsole
    {
        private class Command
        {
            public readonly string Name;
            public readonly string Description;
            public readonly Action<string[]> Handler;
            public readonly ArgumentSuggester Suggester;

            public Command(string name, string description, Action<string[]> handler, ArgumentSuggester suggester)
            {
                Name = name;
                Description = description;
                Handler = handler;
                Suggester = suggester;
            }
        }

        private const int MaxHistory = 100;
        private const int MaxLines = 500;

        private readonly IHistoryStore _historyStore;
        private readonly Dictionary<string, Command> _commands = new();
        private readonly List<string> _lines = new();
        private readonly List<string> _history = new();

        public IReadOnlyList<string> Lines => _lines;
        public IReadOnlyList<string> History => _history;
        public event Action<string> OnLine;
        public event Action OnCleared;

        public CommandConsole(IHistoryStore historyStore = null)
        {
            _historyStore = historyStore;

            LoadHistory();
            Register("clear", "Clear the output text area.", _ => ClearOutput());
            Register("clear-history", "Clear the command history.", _ => ClearHistory());
        }

        public IEnumerable<(string Name, string Description)> GetSuggestions(string prefix)
        {
            return _commands.Values
                .Where(c => c.Name.StartsWith(prefix.ToLower()))
                .OrderBy(c => c.Name)
                .Select(c => (c.Name, c.Description));
        }

        public void Register(string name, string description, Action<string[]> handler, ArgumentSuggester suggester = null)
        {
            var key = name.ToLower();
            _commands[key] = new Command(key, description, handler, suggester);
        }

        public ArgumentSuggester GetArgumentSuggester(string commandName)
        {
            return _commands.TryGetValue(commandName.ToLower(), out var command) ? command.Suggester : null;
        }

        public void Execute(string input)
        {
            var trimmed = input.Trim();

            Log($"> {input}");
            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            // Recorded before running, failed commands included, so a typo can be
            // recalled and fixed. clear-history then wipes its own entry too.
            AddToHistory(trimmed);

            var name = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            if (!_commands.TryGetValue(name, out var command))
            {
                Log($"Unknown command: {name}");
                return;
            }

            try
            {
                command.Handler(args);
            }
            catch (Exception e)
            {
                Log($"Error: {e.Message}");
            }
        }

        public void Log(string text)
        {
            if (_lines.Count >= MaxLines)
            {
                _lines.RemoveAt(0);
            }

            _lines.Add(text);
            OnLine?.Invoke(text);
        }

        // Logs why a command was rejected.
        public void Fail(string message)
        {
            Log(message);
        }

        public string FindHistorySuggestion(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            for (var i = _history.Count - 1; i >= 0; i--)
            {
                var entry = _history[i];
                if (entry.Length > prefix.Length && entry.StartsWith(prefix))
                {
                    return entry;
                }
            }

            return null;
        }

        public void ClearOutput()
        {
            _lines.Clear();
            OnCleared?.Invoke();
        }

        public void ClearHistory()
        {
            _history.Clear();
            _historyStore?.Clear();
            Log("History cleared.");
        }

        private void AddToHistory(string trimmed)
        {
            if (string.IsNullOrEmpty(trimmed) || (_history.Count > 0 && _history[_history.Count - 1] == trimmed))
            {
                return;
            }

            if (_history.Count >= MaxHistory)
            {
                _history.RemoveAt(0);
            }

            _history.Add(trimmed);
            _historyStore?.Save(string.Join("\n", _history));
        }

        private void LoadHistory()
        {
            var saved = _historyStore?.Load();
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            _history.AddRange(saved.Split('\n').Where(s => !string.IsNullOrEmpty(s)));
        }
    }
}
