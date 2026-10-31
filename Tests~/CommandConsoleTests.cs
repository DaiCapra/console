using System.Linq;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class CommandConsoleTests
    {
        private CommandConsole _console;

        [SetUp]
        public void SetUp()
        {
            _console = new CommandConsole();
        }

        [Test]
        public void GetSuggestions_Returns_Matching_Commands()
        {
            _console.Register("give", "Give an item.", _ => { });
            _console.Register("raid", "Spawn a raid.", _ => { });
            _console.Register("giveall", "Give all.", _ => { });

            var matches = _console.GetSuggestions("gi").Select(m => m.Name).ToList();

            Assert.That(matches, Is.EquivalentTo(new[] { "give", "giveall" }));
        }

        [Test]
        public void GetSuggestions_Is_Case_Insensitive()
        {
            _console.Register("Give", "x", _ => { });

            var matches = _console.GetSuggestions("GIV").Select(m => m.Name).ToList();

            Assert.That(matches, Is.EquivalentTo(new[] { "give" }));
        }

        [Test]
        public void GetSuggestions_Returns_Ordered_By_Name()
        {
            _console.Register("zebra", "z", _ => { });
            _console.Register("apple", "a", _ => { });
            _console.Register("mango", "m", _ => { });

            var matches = _console.GetSuggestions(string.Empty).Select(m => m.Name).ToList();

            Assert.That(matches, Is.EqualTo(new[] { "apple", "clear", "clear-history", "mango", "zebra" }));
        }

        [Test]
        public void Execute_Adds_Trimmed_Input_To_History()
        {
            _console.Register("noop", "x", _ => { });

            _console.Execute("noop");
            _console.Execute("noop");

            Assert.That(_console.History.Count, Is.EqualTo(1),
                "Consecutive duplicate entries should collapse.");
            Assert.That(_console.History[0], Is.EqualTo("noop"));
        }

        [Test]
        public void Execute_Records_Unknown_Command_In_History()
        {
            _console.Execute("give-item iron-pickaxe 1");

            Assert.That(_console.History, Is.EqualTo(new[] { "give-item iron-pickaxe 1" }));
        }

        [Test]
        public void Execute_Records_Throwing_Command_In_History()
        {
            _console.Register("boom", "x", _ => throw new System.InvalidOperationException("nope"));

            _console.Execute("boom");

            Assert.That(_console.History, Is.EqualTo(new[] { "boom" }));
        }

        [Test]
        public void Execute_Records_Failed_Command_In_History()
        {
            _console.Register("bad", "x", _ => _console.Fail("Usage: bad"));

            _console.Execute("bad");

            Assert.That(_console.History, Is.EqualTo(new[] { "bad" }));
        }

        [Test]
        public void Execute_Skips_History_For_Clear_History()
        {
            _console.Register("noop", "x", _ => { });
            _console.Execute("noop");

            _console.Execute("clear-history");

            Assert.That(_console.History, Is.Empty);
        }

        [Test]
        public void Execute_Records_Distinct_Entries()
        {
            _console.Register("a", "x", _ => { });
            _console.Register("b", "x", _ => { });

            _console.Execute("a");
            _console.Execute("b");
            _console.Execute("a");

            Assert.That(_console.History, Is.EqualTo(new[] { "a", "b", "a" }));
        }

        [Test]
        public void Finds_Most_Recent_Match()
        {
            _console.Register("give", "x", _ => { });

            _console.Execute("give wood 10");
            _console.Execute("give stone 20");
            _console.Execute("give iron 15");

            var match = _console.FindHistorySuggestion("give");
            Assert.That(match, Is.EqualTo("give iron 15"));
        }

        [Test]
        public void Returns_Null_For_Exact_Match()
        {
            _console.Register("give", "x", _ => { });

            _console.Execute("give");

            var match = _console.FindHistorySuggestion("give");
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Returns_Null_When_No_Match()
        {
            _console.Register("give", "x", _ => { });

            _console.Execute("give wood 10");

            var match = _console.FindHistorySuggestion("raid");
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Empty_Prefix_Returns_Null()
        {
            _console.Register("give", "x", _ => { });
            _console.Execute("give wood 10");

            var match = _console.FindHistorySuggestion(string.Empty);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Clears_And_Fires_OnCleared()
        {
            _console.Log("hello");
            _console.Log("world");
            Assert.That(_console.Lines.Count, Is.EqualTo(2));

            var fired = false;
            _console.OnCleared += () => fired = true;

            _console.ClearOutput();

            Assert.That(_console.Lines, Is.Empty);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void Returns_Registered_Suggester()
        {
            ArgumentSuggester suggester = (_, _) => new[] { ("wood", "wood") };
            _console.Register("give", "x", _ => { }, suggester);

            Assert.That(_console.GetArgumentSuggester("give"), Is.SameAs(suggester));
        }

        [Test]
        public void Null_For_Unknown_Command()
        {
            Assert.That(_console.GetArgumentSuggester("unknown"), Is.Null);
        }

        [Test]
        public void Suggester_Is_Case_Insensitive()
        {
            ArgumentSuggester suggester = (_, _) => Enumerable.Empty<(string, string)>();
            _console.Register("Give", "x", _ => { }, suggester);

            Assert.That(_console.GetArgumentSuggester("GIVE"), Is.SameAs(suggester));
        }
    }
}
