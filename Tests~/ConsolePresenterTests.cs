using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class ConsolePresenterTests
    {
        private CommandConsole _console;
        private ConsolePresenter _presenter;
        private List<string> _executed;

        [SetUp]
        public void SetUp()
        {
            _console = new CommandConsole();
            _executed = new List<string>();
            _console.Register("give", "Give an item.", args => _executed.Add("give " + string.Join(" ", args)), SuggestItems);
            _console.Register("gift", "Gift.", _ => _executed.Add("gift"));
            _console.Register("raid", "Spawn a raid.", _ => _executed.Add("raid"));
            _presenter = new ConsolePresenter(_console);
        }

        private static IEnumerable<(string Name, string Description)> SuggestItems(int argIndex, string partial)
        {
            if (argIndex == 0)
            {
                return new[] { ("wood", "wood"), ("stone", "stone") };
            }

            return Enumerable.Empty<(string, string)>();
        }

        private void Type(string text)
        {
            _presenter.OnInputChanged(text, text.Length);
        }

        private KeyResult Press(TerminalKey key, KeyModifiers modifiers = KeyModifiers.None, char character = '\0')
        {
            return _presenter.HandleKey(key, modifiers, character, _presenter.Cursor);
        }

        [Test]
        public void Typing_Shows_Matching_Commands()
        {
            Type("gi");

            Assert.That(_presenter.SuggestionsVisible, Is.True);
            Assert.That(_presenter.Suggestions.Select(s => s.Name), Is.EqualTo(new[] { "gift", "give" }));
        }

        [Test]
        public void Typing_After_Space_Shows_Argument_Suggestions()
        {
            Type("give ");

            Assert.That(_presenter.Suggestions.Select(s => s.Name), Is.EquivalentTo(new[] { "wood", "stone" }));
        }

        [Test]
        public void Tab_With_Single_Match_Completes_And_Hides()
        {
            Type("ra");

            var result = Press(TerminalKey.Tab);

            Assert.That(result, Is.EqualTo(KeyResult.Handled));
            Assert.That(_presenter.Input, Is.EqualTo("raid "));
            Assert.That(_presenter.Cursor, Is.EqualTo(5));
            Assert.That(_presenter.SuggestionsVisible, Is.False);
        }

        [Test]
        public void Tab_Cycles_Matches()
        {
            Type("gi");

            Press(TerminalKey.Tab);
            Assert.That(_presenter.Input, Is.EqualTo("gift "));

            Press(TerminalKey.Tab);
            Assert.That(_presenter.Input, Is.EqualTo("give "));

            Press(TerminalKey.Tab, KeyModifiers.Shift);
            Assert.That(_presenter.Input, Is.EqualTo("gift "));
        }

        [Test]
        public void Ghost_Completes_Command_Name()
        {
            Type("ra");

            Assert.That(_presenter.GhostCompletion, Is.EqualTo("raid"));
        }

        [Test]
        public void End_Accepts_Whole_Ghost()
        {
            _console.Execute("give wood 10");
            Type("give wood");

            var result = Press(TerminalKey.End);

            Assert.That(result, Is.EqualTo(KeyResult.Handled));
            Assert.That(_presenter.Input, Is.EqualTo("give wood 10"));
        }

        [Test]
        public void Right_Accepts_Ghost_One_Word_At_A_Time()
        {
            _console.Execute("give wood 10");
            Type("give w");

            Press(TerminalKey.Right);

            Assert.That(_presenter.Input, Is.EqualTo("give wood"));
        }

        [Test]
        public void Right_Does_Nothing_When_Cursor_Not_At_End()
        {
            Type("ra");

            var result = _presenter.HandleKey(TerminalKey.Right, KeyModifiers.None, '\0', 1);

            Assert.That(result, Is.EqualTo(KeyResult.Unhandled));
            Assert.That(_presenter.Input, Is.EqualTo("ra"));
        }

        [Test]
        public void Up_And_Down_Browse_History()
        {
            _console.Execute("raid");
            _console.Execute("gift");

            Press(TerminalKey.Up);
            Assert.That(_presenter.Input, Is.EqualTo("gift"));

            Press(TerminalKey.Up);
            Assert.That(_presenter.Input, Is.EqualTo("raid"));

            Press(TerminalKey.Down);
            Assert.That(_presenter.Input, Is.EqualTo("gift"));
        }

        [Test]
        public void Return_Executes_And_Clears_Input()
        {
            Type("raid");

            var result = Press(TerminalKey.Return);

            Assert.That(result, Is.EqualTo(KeyResult.Submitted));
            Assert.That(_executed, Is.EqualTo(new[] { "raid" }));
            Assert.That(_presenter.Input, Is.Empty);
            Assert.That(_presenter.SuggestionsVisible, Is.False);
        }

        [Test]
        public void Escape_Hides_Suggestions_And_Blurs()
        {
            Type("gi");

            var result = Press(TerminalKey.Escape);

            Assert.That(result, Is.EqualTo(KeyResult.Blur));
            Assert.That(_presenter.SuggestionsVisible, Is.False);
        }

        [Test]
        public void Ctrl_W_Deletes_Word_Back()
        {
            Type("give wood");

            Press(TerminalKey.W, KeyModifiers.Ctrl);

            Assert.That(_presenter.Input, Is.EqualTo("give "));
        }

        [Test]
        public void Ctrl_U_And_Ctrl_K_Kill_Around_Cursor()
        {
            Type("give wood");
            _presenter.HandleKey(TerminalKey.U, KeyModifiers.Ctrl, '\0', 5);
            Assert.That(_presenter.Input, Is.EqualTo("wood"));

            Type("give wood");
            _presenter.HandleKey(TerminalKey.K, KeyModifiers.Ctrl, '\0', 4);
            Assert.That(_presenter.Input, Is.EqualTo("give"));
        }

        [Test]
        public void Ctrl_R_Finds_And_Submits_History_Match()
        {
            _console.Execute("give stone 5");
            _console.Execute("raid");

            Press(TerminalKey.R, KeyModifiers.Ctrl);
            Assert.That(_presenter.IsReadOnly, Is.True);

            Press(TerminalKey.Other, KeyModifiers.None, 's');
            Press(TerminalKey.Other, KeyModifiers.None, 't');
            Assert.That(_presenter.Input, Does.Contain("give stone 5"));

            var result = Press(TerminalKey.Return);

            Assert.That(result, Is.EqualTo(KeyResult.Submitted));
            Assert.That(_presenter.IsReadOnly, Is.False);
            Assert.That(_executed.Last(), Is.EqualTo("give stone 5"));
        }

        [Test]
        public void Ctrl_R_Escape_Restores_Input()
        {
            _console.Execute("raid");
            Type("gi");

            Press(TerminalKey.R, KeyModifiers.Ctrl);
            Press(TerminalKey.Other, KeyModifiers.None, 'r');
            Press(TerminalKey.Escape);

            Assert.That(_presenter.Input, Is.EqualTo("gi"));
        }

        [Test]
        public void Click_Accepts_Suggestion()
        {
            Type("give w");

            _presenter.AcceptSuggestion(0);

            Assert.That(_presenter.Input, Is.EqualTo("give wood "));
            Assert.That(_presenter.SuggestionsVisible, Is.False);
        }
    }
}
