using System.Linq;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class CommandArgumentResolverTests
    {
        private CommandConsole _console;
        private CommandArgumentResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _console = new CommandConsole();
            _resolver = new CommandArgumentResolver(_console);

            _console.Register(
                "give",
                "Give an item.",
                _ => { },
                (argIndex, partial) => argIndex switch
                {
                    0 => new[] { ("wood", "wood"), ("stone", "stone"), ("Wooden Plank", "wooden plank") },
                    1 => new[] { ("10", "ten"), ("20", "twenty") },
                    _ => System.Linq.Enumerable.Empty<(string, string)>()
                });

            _console.Register(
                "raid",
                "Spawn a raid.",
                _ => { });
        }

        [Test]
        public void Empty_Text_Returns_No_Suggestions()
        {
            var matches = _resolver.Suggest(string.Empty).ToList();
            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void No_Space_Returns_No_Suggestions()
        {
            var matches = _resolver.Suggest("give").ToList();
            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Unknown_Command_Returns_No_Suggestions()
        {
            var matches = _resolver.Suggest("unknown wood").ToList();
            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Returns_No_Suggestions()
        {
            var matches = _resolver.Suggest("raid 5").ToList();
            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Trailing_Space_Uses_Next_Arg_Index()
        {
            var matches = _resolver.Suggest("give ").ToList();
            Assert.That(matches.Select(m => m.Name), Is.EquivalentTo(new[] { "wood", "stone", "Wooden Plank" }));
        }

        [Test]
        public void Partial_Last_Word_Filters_By_Prefix()
        {
            var matches = _resolver.Suggest("give wo").ToList();
            Assert.That(matches.Select(m => m.Name), Is.EquivalentTo(new[] { "wood", "Wooden Plank" }));
        }

        [Test]
        public void Prefix_Filter_Is_Case_Insensitive()
        {
            var matches = _resolver.Suggest("give WO").ToList();
            Assert.That(matches.Select(m => m.Name), Is.EquivalentTo(new[] { "wood", "Wooden Plank" }));
        }

        [Test]
        public void Routes_To_Arg_Index_1()
        {
            var matches = _resolver.Suggest("give wood ").ToList();
            Assert.That(matches.Select(m => m.Name), Is.EquivalentTo(new[] { "10", "20" }));
        }

        [Test]
        public void Third_Arg_Beyond_Suggester_Returns_Empty()
        {
            var matches = _resolver.Suggest("give wood 10 ").ToList();
            Assert.That(matches, Is.Empty);
        }
    }
}
