using System;
using System.Collections.Generic;
using System.Linq;

namespace Artmann.Terminal
{
    // Engine-agnostic state and key handling for a console input line: command and
    // argument suggestions, the inline ghost completion, history navigation, reverse
    // search and readline-style edits. The view forwards keys and text changes, and
    // draws the state when the events fire.
    public class ConsolePresenter
    {
        private const int MaxSuggestions = 8;

        private readonly CommandConsole _console;
        private readonly CommandArgumentResolver _argumentResolver;
        private readonly HistoryFilter _historyFilter = new();
        private readonly ReverseSearchModel _reverseSearch = new();
        private readonly List<(string Name, string Description)> _suggestions = new();

        public string Input { get; private set; } = string.Empty;
        public int Cursor { get; private set; }
        public bool IsReadOnly { get; private set; }

        public IReadOnlyList<(string Name, string Description)> Suggestions => _suggestions;
        public bool SuggestionsVisible => _suggestions.Count > 0;
        public int SelectedSuggestion { get; private set; } = -1;

        // Text before the word being completed, taken from the text the list was built for.
        public string SuggestionPrefix { get; private set; } = string.Empty;

        // The whole line the ghost completes Input to, or null. Input is always a prefix of it.
        public string GhostCompletion { get; private set; }

        // The presenter replaced Input, Cursor or IsReadOnly. Apply them without
        // raising the view's own change event.
        public event Action InputReplaced;

        // The suggestion list was rebuilt or hidden.
        public event Action SuggestionsChanged;

        // The selected suggestion or the ghost completion changed.
        public event Action SelectionChanged;

        public ConsolePresenter(CommandConsole console)
        {
            _console = console;
            _argumentResolver = new CommandArgumentResolver(console);
        }

        private string SelectedCompletion
        {
            get
            {
                if (SelectedSuggestion >= 0 && SelectedSuggestion < _suggestions.Count)
                {
                    return SuggestionPrefix + _suggestions[SelectedSuggestion].Name;
                }

                return _suggestions.Count > 0 ? SuggestionPrefix + _suggestions[0].Name : null;
            }
        }

        // The user edited the text.
        public void OnInputChanged(string text, int cursor)
        {
            Input = text;
            Cursor = cursor;
            OnTextChanged();
        }

        public KeyResult HandleKey(TerminalKey key, KeyModifiers modifiers, char character, int cursor)
        {
            Cursor = Math.Max(0, Math.Min(cursor, Input.Length));

            var ctrl = (modifiers & KeyModifiers.Ctrl) != 0;
            var shift = (modifiers & KeyModifiers.Shift) != 0;

            if (HandleReverseSearchKey(key, ctrl, character))
            {
                return KeyResult.Handled;
            }

            if (HandleLineEditKey(key, ctrl))
            {
                return KeyResult.Handled;
            }

            if (HandleGhostKey(key))
            {
                return KeyResult.Handled;
            }

            if (HandleSuggestionKey(key, shift))
            {
                return KeyResult.Handled;
            }

            if (key == TerminalKey.Escape)
            {
                HideSuggestions();
                return KeyResult.Blur;
            }

            if (key == TerminalKey.Tab)
            {
                return KeyResult.Handled;
            }

            if (HandleHistoryKey(key))
            {
                return KeyResult.Handled;
            }

            if (key == TerminalKey.Return)
            {
                Submit();
                return KeyResult.Submitted;
            }

            return KeyResult.Unhandled;
        }

        // Clicked a suggestion row.
        public void AcceptSuggestion(int index)
        {
            if (index < 0 || index >= _suggestions.Count)
            {
                return;
            }

            ReplaceCurrentWord(_suggestions[index].Name + " ");
            HideSuggestions();
        }

        // Pointer entered a suggestion row.
        public void HoverSuggestion(int index)
        {
            SelectSuggestion(index);
        }

        private void Submit()
        {
            var text = Input.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                _console.Execute(text);
            }

            SetInput(string.Empty, 0, notify: true);
        }

        private bool HandleReverseSearchKey(TerminalKey key, bool ctrl, char character)
        {
            var history = _console.History;

            if (!_reverseSearch.IsActive)
            {
                if (ctrl && key == TerminalKey.R)
                {
                    _reverseSearch.Begin(Input);
                    IsReadOnly = true;
                    RenderReverseSearch();
                    return true;
                }

                return false;
            }

            if (key == TerminalKey.Escape)
            {
                ExitReverseSearch(_reverseSearch.End(accept: false));
                return true;
            }

            // Falls through so the accepted line is submitted.
            if (key == TerminalKey.Return)
            {
                ExitReverseSearch(_reverseSearch.End(accept: true));
                return false;
            }

            if (ctrl && key == TerminalKey.R)
            {
                _reverseSearch.Step(history);
                RenderReverseSearch();
                return true;
            }

            if (key == TerminalKey.Backspace)
            {
                _reverseSearch.Backspace(history);
                RenderReverseSearch();
                return true;
            }

            var printable = character != '\0' && !char.IsControl(character);
            if (printable)
            {
                _reverseSearch.AddChar(character, history);
                RenderReverseSearch();
                return true;
            }

            ExitReverseSearch(_reverseSearch.End(accept: true));
            return true;
        }

        private void RenderReverseSearch()
        {
            var display = _reverseSearch.Display;
            SetInput(display, display.Length, notify: false);
        }

        private void ExitReverseSearch(string finalText)
        {
            IsReadOnly = false;
            SetInput(finalText, finalText.Length, notify: false);
        }

        private bool HandleLineEditKey(TerminalKey key, bool ctrl)
        {
            if (!ctrl)
            {
                return false;
            }

            switch (key)
            {
                case TerminalKey.W:
                case TerminalKey.Space:
                case TerminalKey.Backspace:
                    ApplyEdit(LineEditOps.DeleteWordBack(Input, Cursor));
                    return true;
                case TerminalKey.U:
                    ApplyEdit(LineEditOps.KillToStart(Input, Cursor));
                    return true;
                case TerminalKey.K:
                    ApplyEdit(LineEditOps.KillToEnd(Input, Cursor));
                    return true;
                case TerminalKey.L:
                    _console.ClearOutput();
                    return true;
            }

            return false;
        }

        private void ApplyEdit((string text, int cursor) result)
        {
            SetInput(result.text, result.cursor, notify: true);
        }

        private bool HandleGhostKey(TerminalKey key)
        {
            if (string.IsNullOrEmpty(GhostCompletion))
            {
                return false;
            }

            if (key != TerminalKey.Right && key != TerminalKey.End)
            {
                return false;
            }

            if (Cursor != Input.Length)
            {
                return false;
            }

            var accepted = key == TerminalKey.Right ? NextGhostWord() : GhostCompletion;
            SetInput(accepted, accepted.Length, notify: true);
            return true;
        }

        // Right arrow takes one word at a time. The trailing space is included so the
        // next press continues into the following word.
        private string NextGhostWord()
        {
            var space = GhostCompletion.IndexOf(' ', Input.Length);
            return space < 0 ? GhostCompletion : GhostCompletion.Substring(0, space + 1);
        }

        private bool HandleSuggestionKey(TerminalKey key, bool shift)
        {
            if (!SuggestionsVisible)
            {
                return false;
            }

            if (key == TerminalKey.Up)
            {
                SelectSuggestion(Math.Max(0, SelectedSuggestion - 1));
                return true;
            }

            if (key == TerminalKey.Down)
            {
                SelectSuggestion(Math.Min(_suggestions.Count - 1, SelectedSuggestion + 1));
                return true;
            }

            if (key != TerminalKey.Tab)
            {
                return false;
            }

            // A single match completes outright and closes the list. Several matches
            // cycle instead. Both leave a trailing space so the arguments can be typed.
            if (_suggestions.Count == 1)
            {
                AcceptSuggestion(0);
                return true;
            }

            var step = shift ? -1 : 1;
            var count = _suggestions.Count;
            var next = SelectedSuggestion < 0
                ? (step > 0 ? 0 : count - 1)
                : (SelectedSuggestion + step + count) % count;

            SelectedSuggestion = next;
            ReplaceCurrentWord(_suggestions[next].Name + " ");
            return true;
        }

        private void SelectSuggestion(int index)
        {
            SelectedSuggestion = index;
            RefreshGhost();
            SelectionChanged?.Invoke();
        }

        private bool HandleHistoryKey(TerminalKey key)
        {
            string text;
            if (key == TerminalKey.Up)
            {
                text = _historyFilter.MoveUp(Input, _console.History);
            }
            else if (key == TerminalKey.Down)
            {
                text = _historyFilter.MoveDown(_console.History);
            }
            else
            {
                return false;
            }

            if (text == null)
            {
                return false;
            }

            SetInput(text, text.Length, notify: false);
            return true;
        }

        // Builds on the text the list was made for, so the next Tab replaces the
        // previous pick and its trailing space instead of adding after it.
        private void ReplaceCurrentWord(string replacement)
        {
            var value = SuggestionPrefix + replacement;
            SetInput(value, value.Length, notify: false);
        }

        // notify runs the same logic as a user edit: resets history browsing and
        // rebuilds the suggestions.
        private void SetInput(string text, int cursor, bool notify)
        {
            Input = text;
            Cursor = cursor;
            InputReplaced?.Invoke();

            if (notify)
            {
                OnTextChanged();
                return;
            }

            RefreshGhost();
            SelectionChanged?.Invoke();
        }

        private void OnTextChanged()
        {
            _historyFilter.Reset();
            UpdateSuggestions();
        }

        private void UpdateSuggestions()
        {
            var trimmed = Input.TrimStart();
            if (string.IsNullOrEmpty(trimmed))
            {
                HideSuggestions();
                return;
            }

            var matches = trimmed.Contains(' ')
                ? _argumentResolver.Suggest(trimmed)
                : _console.GetSuggestions(trimmed);

            _suggestions.Clear();
            _suggestions.AddRange(matches.Take(MaxSuggestions));
            if (_suggestions.Count == 0)
            {
                HideSuggestions();
                return;
            }

            SelectedSuggestion = -1;
            SuggestionPrefix = Input.Substring(0, Input.LastIndexOf(' ') + 1);
            RefreshGhost();
            SuggestionsChanged?.Invoke();
        }

        private void HideSuggestions()
        {
            _suggestions.Clear();
            SelectedSuggestion = -1;
            RefreshGhost();
            SuggestionsChanged?.Invoke();
        }

        private void RefreshGhost()
        {
            GhostCompletion = null;
            if (_reverseSearch.IsActive || string.IsNullOrEmpty(Input))
            {
                return;
            }

            var completion = ResolveGhost(SelectedCompletion);
            var extends = completion != null
                && completion.Length > Input.Length
                && completion.StartsWith(Input, StringComparison.OrdinalIgnoreCase);
            if (!extends)
            {
                return;
            }

            // Keep the typed casing so Input stays a prefix of the completion.
            GhostCompletion = Input + completion.Substring(Input.Length);
        }

        private string ResolveGhost(string preferred)
        {
            var lastSpace = Input.LastIndexOf(' ');
            var lastToken = Input.Substring(lastSpace + 1);
            var prefix = Input.Substring(0, lastSpace + 1);

            // Preferred is a whole line. Once Tab has inserted it, it no longer extends the input.
            var preferredExtends = !string.IsNullOrEmpty(preferred)
                && preferred.Length > Input.Length
                && preferred.StartsWith(Input, StringComparison.OrdinalIgnoreCase);
            if (preferredExtends)
            {
                return preferred;
            }

            if (lastSpace < 0)
            {
                var name = _console.GetSuggestions(lastToken)
                    .Select(s => s.Name)
                    .FirstOrDefault(n => n.Length > lastToken.Length);
                if (name != null)
                {
                    return name;
                }
            }
            else
            {
                var argName = _argumentResolver.Suggest(Input)
                    .Select(s => s.Name)
                    .FirstOrDefault(n => n.Length > lastToken.Length);
                if (argName != null)
                {
                    return prefix + argName;
                }
            }

            return _console.FindHistorySuggestion(Input);
        }
    }
}
