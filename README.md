# Console

[![Tests](https://github.com/DaiCapra/console/actions/workflows/tests.yml/badge.svg?branch=master)](https://github.com/DaiCapra/console/actions/workflows/tests.yml)

Engine-agnostic in-game command console. Plain C#, no engine references.

- **Core**: `CommandConsole` holds commands, output lines and history. Commands can suggest their arguments. History is saved through an optional `IHistoryStore`.
- **Presentation**: `ConsolePresenter` holds the input line state and handles keys: suggestions, inline ghost completion, history (Up/Down), reverse search (Ctrl+R) and line edits (Ctrl+W/U/K/L). Your view forwards keys as `TerminalKey` and draws the state.

## Install (Unity)

Add to `Packages/manifest.json`:

```json
"com.artmann.console": "https://github.com/DaiCapra/console.git"
```

## Usage

```csharp
var console = new CommandConsole(historyStore);
console.Register("say", "Print text.", args => console.Log(string.Join(" ", args)));

var presenter = new ConsolePresenter(console);
presenter.InputReplaced += () => { /* set input text, cursor, read-only */ };
presenter.SuggestionsChanged += () => { /* rebuild suggestion rows */ };
presenter.SelectionChanged += () => { /* highlight row, draw GhostCompletion */ };

// From the view:
presenter.OnInputChanged(text, cursor);
var result = presenter.HandleKey(TerminalKey.Tab, KeyModifiers.None, '\0', cursor);
```

## Tests

```
dotnet test Tests~/Artmann.Terminal.Tests.csproj
```
