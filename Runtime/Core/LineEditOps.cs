namespace Artmann.Terminal
{
    public static class LineEditOps
    {
        public static (string text, int cursor) DeleteWordBack(string text, int cursor)
        {
            cursor = Clamp(cursor, text);
            if (cursor == 0)
            {
                return (text, 0);
            }

            var i = cursor;
            while (i > 0 && char.IsWhiteSpace(text[i - 1]))
            {
                i--;
            }

            while (i > 0 && !char.IsWhiteSpace(text[i - 1]))
            {
                i--;
            }

            return (text[..i] + text[cursor..], i);
        }

        public static (string text, int cursor) KillToStart(string text, int cursor)
        {
            cursor = Clamp(cursor, text);
            return cursor == 0 ? (text, 0) : (text[cursor..], 0);
        }

        public static (string text, int cursor) KillToEnd(string text, int cursor)
        {
            cursor = Clamp(cursor, text);
            return cursor >= text.Length ? (text, cursor) : (text[..cursor], cursor);
        }

        private static int Clamp(int cursor, string text)
        {
            if (cursor < 0)
            {
                return 0;
            }

            return cursor > text.Length ? text.Length : cursor;
        }
    }
}
