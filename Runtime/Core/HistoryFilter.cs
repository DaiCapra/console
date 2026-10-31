using System.Collections.Generic;

namespace Artmann.Terminal
{
    public class HistoryFilter
    {
        private int _index = -1;
        private string _draft = string.Empty;
        private string _filter = string.Empty;

        public bool IsActive => _index != -1;

        public void Reset()
        {
            _index = -1;
            _draft = string.Empty;
            _filter = string.Empty;
        }

        public string MoveUp(string currentInput, IReadOnlyList<string> history)
        {
            if (_index == -1)
            {
                _draft = currentInput;
                _filter = currentInput;
            }

            var matches = GetMatches(history);
            if (matches.Count == 0)
            {
                return null;
            }

            _index = _index == -1
                ? matches.Count - 1
                : (_index > 0 ? _index - 1 : 0);

            return matches[_index];
        }

        public string MoveDown(IReadOnlyList<string> history)
        {
            if (_index == -1)
            {
                return null;
            }

            var matches = GetMatches(history);
            if (_index < matches.Count - 1)
            {
                _index++;
                return matches[_index];
            }

            _index = -1;
            return _draft;
        }

        private List<string> GetMatches(IReadOnlyList<string> history)
        {
            var result = new List<string>(history.Count);
            for (var i = 0; i < history.Count; i++)
            {
                var entry = history[i];
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(_filter) && !entry.StartsWith(_filter))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }
    }
}
