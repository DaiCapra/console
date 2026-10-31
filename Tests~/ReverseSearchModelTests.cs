using System.Collections.Generic;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class ReverseSearchModelTests
    {
        private static readonly List<string> History = new()
        {
            "give wood 10",
            "raid 5",
            "give stone 20",
            "dummy",
            "give iron 15"
        };

        [Test]
        public void Begin_Captures_Saved_Input_And_Activates()
        {
            var model = new ReverseSearchModel();
            model.Begin("draft");
            Assert.That(model.IsActive, Is.True);
            Assert.That(model.SavedInput, Is.EqualTo("draft"));
            Assert.That(model.Query, Is.EqualTo(string.Empty));
            Assert.That(model.Match, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Narrows_To_Most_Recent_Match()
        {
            var model = new ReverseSearchModel();
            model.Begin(string.Empty);
            model.AddChar('g', History);
            model.AddChar('i', History);
            model.AddChar('v', History);
            model.AddChar('e', History);
            Assert.That(model.Match, Is.EqualTo("give iron 15"));
        }

        [Test]
        public void Step_Advances_To_Older_Matching_Entry()
        {
            var model = new ReverseSearchModel();
            model.Begin(string.Empty);
            model.AddChar('g', History);
            model.AddChar('i', History);
            model.AddChar('v', History);
            model.AddChar('e', History);
            model.Step(History);
            Assert.That(model.Match, Is.EqualTo("give stone 20"));
            model.Step(History);
            Assert.That(model.Match, Is.EqualTo("give wood 10"));
        }

        [Test]
        public void Shrinks_Query_Resets_Match()
        {
            var model = new ReverseSearchModel();
            model.Begin(string.Empty);
            model.AddChar('g', History);
            model.AddChar('i', History);
            model.AddChar('v', History);
            model.AddChar('e', History);
            model.Step(History);
            model.Step(History);
            model.Backspace(History);
            Assert.That(model.Query, Is.EqualTo("giv"));
            Assert.That(model.MatchIndex, Is.EqualTo(0));
            Assert.That(model.Match, Is.EqualTo("give iron 15"));
        }

        [Test]
        public void Backspace_On_Empty_Query_Is_NoOp()
        {
            var model = new ReverseSearchModel();
            model.Begin("draft");
            model.Backspace(History);
            Assert.That(model.Query, Is.EqualTo(string.Empty));
            Assert.That(model.IsActive, Is.True);
        }

        [Test]
        public void No_Match_Leaves_Match_Empty()
        {
            var model = new ReverseSearchModel();
            model.Begin(string.Empty);
            model.AddChar('z', History);
            Assert.That(model.Match, Is.EqualTo(string.Empty));
        }

        [Test]
        public void End_Accept_True_Returns_Match()
        {
            var model = new ReverseSearchModel();
            model.Begin("draft");
            model.AddChar('d', History);
            model.AddChar('u', History);
            var result = model.End(accept: true);
            Assert.That(result, Is.EqualTo("dummy"));
            Assert.That(model.IsActive, Is.False);
        }

        [Test]
        public void End_Accept_False_Returns_Saved_Input()
        {
            var model = new ReverseSearchModel();
            model.Begin("draft");
            model.AddChar('d', History);
            var result = model.End(accept: false);
            Assert.That(result, Is.EqualTo("draft"));
        }

        [Test]
        public void No_Match_Falls_Back_To_Saved_Input()
        {
            var model = new ReverseSearchModel();
            model.Begin("draft");
            model.AddChar('z', History);
            var result = model.End(accept: true);
            Assert.That(result, Is.EqualTo("draft"));
        }

        [Test]
        public void Display_Formats_As_Reverse_I_Search()
        {
            var model = new ReverseSearchModel();
            model.Begin(string.Empty);
            model.AddChar('g', History);
            model.AddChar('i', History);
            model.AddChar('v', History);
            model.AddChar('e', History);
            Assert.That(model.Display, Is.EqualTo("(reverse-i-search) \"give\": give iron 15"));
        }
    }
}
