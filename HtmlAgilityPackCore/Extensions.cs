using System;

namespace HtmlAgilityPackCore
{
    public static class Extensions
    {

        public static bool IsNullOrWhiteSpace(this ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty) return true;
            
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value.Span[i])) return false;
            }

            return true;
        }

        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> s)
        {
            if (s.IsEmpty)
                return s;

            var span = s.Span;
            var start = 0;

            for (; start < s.Length; start++)
            {
                if (!char.IsWhiteSpace(span[start]))
                {
                    break;
                }
            }

            if (start == s.Length)
            {
                return ReadOnlyMemory<char>.Empty;
            }

            var count = 1;

            for (; count + start < s.Length; count++)
            {
                if (char.IsWhiteSpace(span[count]))
                {
                    break;
                }
            }

            return s.Slice(start, count);
        }
    }
}