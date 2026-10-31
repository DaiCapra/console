using System.Collections.Generic;
using NUnit.Framework;

namespace Artmann.Terminal.Tests
{
    [TestFixture]
    public class LineEditOpsTests
    {
        [Test]
        public void DeleteWordBack_From_End_Removes_Last_Word()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack("give wood plank", 15);
            Assert.That(text, Is.EqualTo("give wood "));
            Assert.That(cursor, Is.EqualTo(10));
        }

        [Test]
        public void DeleteWordBack_Skips_Trailing_Spaces()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack("give wood ", 10);
            Assert.That(text, Is.EqualTo("give "));
            Assert.That(cursor, Is.EqualTo(5));
        }

        [Test]
        public void DeleteWordBack_From_Middle_Keeps_Suffix()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack("give wood plank", 9);
            Assert.That(text, Is.EqualTo("give  plank"));
            Assert.That(cursor, Is.EqualTo(5));
        }

        [Test]
        public void DeleteWordBack_At_Start_Is_NoOp()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack("hello", 0);
            Assert.That(text, Is.EqualTo("hello"));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void DeleteWordBack_Empty_Is_NoOp()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack(string.Empty, 0);
            Assert.That(text, Is.EqualTo(string.Empty));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void KillToStart_From_End_Clears_All()
        {
            var (text, cursor) = LineEditOps.KillToStart("hello world", 11);
            Assert.That(text, Is.EqualTo(string.Empty));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void KillToStart_From_Middle_Keeps_Suffix()
        {
            var (text, cursor) = LineEditOps.KillToStart("hello world", 6);
            Assert.That(text, Is.EqualTo("world"));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void KillToStart_At_Start_Is_NoOp()
        {
            var (text, cursor) = LineEditOps.KillToStart("hello", 0);
            Assert.That(text, Is.EqualTo("hello"));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void KillToEnd_From_Start_Clears_All()
        {
            var (text, cursor) = LineEditOps.KillToEnd("hello world", 0);
            Assert.That(text, Is.EqualTo(string.Empty));
            Assert.That(cursor, Is.EqualTo(0));
        }

        [Test]
        public void KillToEnd_From_Middle_Keeps_Prefix()
        {
            var (text, cursor) = LineEditOps.KillToEnd("hello world", 5);
            Assert.That(text, Is.EqualTo("hello"));
            Assert.That(cursor, Is.EqualTo(5));
        }

        [Test]
        public void KillToEnd_At_End_Is_NoOp()
        {
            var (text, cursor) = LineEditOps.KillToEnd("hello", 5);
            Assert.That(text, Is.EqualTo("hello"));
            Assert.That(cursor, Is.EqualTo(5));
        }

        [Test]
        public void Cursor_Past_End_Is_Clamped()
        {
            var (text, cursor) = LineEditOps.DeleteWordBack("foo", 999);
            Assert.That(text, Is.EqualTo(string.Empty));
            Assert.That(cursor, Is.EqualTo(0));
        }
    }
}
