using System.Collections.Generic;

namespace Artmann.Terminal
{
    public class ReverseSearchModel
    {
        public bool IsActive { get; private set; }
        public string Query { get; private set; } = string.Empty;
        public string Match { get; private set; } = string.Empty;
        public string SavedInput { get; private set; } = string.Empty;
        public int MatchIndex { get; private set; }

        public string Display => $"(reverse-i-search) \"{Query}\": {Match}";

        public void Begin(string savedInput)
        {
            IsActive = true;
            SavedInput = savedInput;
            Query = string.Empty;
            MatchIndex = 0;
            Match = string.Empty;
        }

        public void AddChar(char c, IReadOnlyList<string> history)
        {
            Query += c;
            MatchIndex = 0;
            Refresh(history);
        }

        public void Backspace(IReadOnlyList<string> history)
        {
            if (Query.Length == 0)
            {
                return;
            }

            Query = Query[..^1];
            MatchIndex = 0;
            Refresh(history);
        }

        public void Step(IReadOnlyList<string> history)
        {
            MatchIndex++;
            Refresh(history);
        }

        public string End(bool accept)
        {
            IsActive = false;
            return accept && !string.IsNullOrEmpty(Match) ? Match : SavedInput;
        }

        private void Refresh(IReadOnlyList<string> history)
        {
            Match = FindMatch(history);
        }

        private string FindMatch(IReadOnlyList<string> history)
        {
            if (string.IsNullOrEmpty(Query))
            {
                return string.Empty;
            }

            var skip = MatchIndex;
            for (var i = history.Count - 1; i >= 0; i--)
            {
                if (!history[i].Contains(Query))
                {
                    continue;
                }

                if (skip == 0)
                {
                    return history[i];
                }

                skip--;
            }

            if (MatchIndex > 0)
            {
                MatchIndex--;
                return FindMatch(history);
            }

            return string.Empty;
        }
    }
}
