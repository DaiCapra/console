using System.Collections.Generic;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class HistoryFilterTests
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
        public void MoveUp_With_Empty_Input_Returns_Newest()
        {
            var filter = new HistoryFilter();
            var text = filter.MoveUp(string.Empty, History);
            Assert.That(text, Is.EqualTo("give iron 15"));
            Assert.That(filter.IsActive, Is.True);
        }

        [Test]
        public void MoveUp_Cycles_To_Older_Entries()
        {
            var filter = new HistoryFilter();
            filter.MoveUp(string.Empty, History);
            var second = filter.MoveUp(string.Empty, History);
            var third = filter.MoveUp(string.Empty, History);
            Assert.That(second, Is.EqualTo("dummy"));
            Assert.That(third, Is.EqualTo("give stone 20"));
        }

        [Test]
        public void MoveUp_With_Prefix_Filters_Matching_Entries()
        {
            var filter = new HistoryFilter();
            var first = filter.MoveUp("give", History);
            var second = filter.MoveUp("give", History);
            var third = filter.MoveUp("give", History);
            Assert.That(first, Is.EqualTo("give iron 15"));
            Assert.That(second, Is.EqualTo("give stone 20"));
            Assert.That(third, Is.EqualTo("give wood 10"));
        }

        [Test]
        public void MoveUp_Past_Oldest_Clamps()
        {
            var filter = new HistoryFilter();
            for (var i = 0; i < 10; i++)
            {
                filter.MoveUp(string.Empty, History);
            }

            var text = filter.MoveUp(string.Empty, History);
            Assert.That(text, Is.EqualTo("give wood 10"));
        }

        [Test]
        public void MoveDown_Past_Newest_Restores_Draft()
        {
            var filter = new HistoryFilter();
            filter.MoveUp("give", History);
            var text = filter.MoveDown(History);
            Assert.That(text, Is.EqualTo("give"));
            Assert.That(filter.IsActive, Is.False);
        }

        [Test]
        public void Returns_Null_When_Inactive()
        {
            var filter = new HistoryFilter();
            var text = filter.MoveDown(History);
            Assert.That(text, Is.Null);
        }

        [Test]
        public void Reset_Clears_State()
        {
            var filter = new HistoryFilter();
            filter.MoveUp("give", History);
            filter.Reset();
            Assert.That(filter.IsActive, Is.False);
            var text = filter.MoveUp(string.Empty, History);
            Assert.That(text, Is.EqualTo("give iron 15"));
        }

        [Test]
        public void MoveUp_With_Empty_History_Returns_Null()
        {
            var filter = new HistoryFilter();
            var text = filter.MoveUp(string.Empty, new List<string>());
            Assert.That(text, Is.Null);
            Assert.That(filter.IsActive, Is.False);
        }

        [Test]
        public void No_Matches_Returns_Null()
        {
            var filter = new HistoryFilter();
            var text = filter.MoveUp("zzz", History);
            Assert.That(text, Is.Null);
        }
    }
}
