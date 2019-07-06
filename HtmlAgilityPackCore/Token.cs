using System;
using System.Collections.Generic;
using System.Linq;

namespace HtmlAgilityPackCore
{
    public class Token
    {
        public ReadOnlyMemory<char> Filter { get; set; }
        public List<Token> SubTokens { get; set; }

        public Token(string word)
        {
            if (string.IsNullOrEmpty(word))
                throw new ArgumentNullException(nameof(word));

            var tokens = SplitTokens(word.AsMemory()).ToList();

            Filter = tokens.First();
            SubTokens = tokens.Skip(1).Select(i => new Token(i.ToString())).ToList(); // TODO
        }

        private static List<ReadOnlyMemory<char>> SplitTokens(ReadOnlyMemory<char> token)
        {
            Func<char, bool> isNameToken = (c) => char.IsLetterOrDigit(c) || c == '-'|| c == '_';
            var rt = new List<ReadOnlyMemory<char>>();
           
            int start = 0;
            bool isPrefix = true;
            bool isOpeningBracket = false;
            char closeBracket = '\0';
            for (int i = 0; i < token.Length; i++)
            {
                if (isOpeningBracket)
                {
                    if (token.Span[i] == closeBracket)
                    {
                        isOpeningBracket = false;
                        isPrefix = true;
                        rt.Add(token.Slice(start, i - start + 1));
                        start = i + 1;
                    }

                    continue;
                }

                if (token.Span[i] == '(')
                {
                    closeBracket = ')';
                    isOpeningBracket = true;
                }
                else if (token.Span[i] == '[')
                {
                    closeBracket = ']';
                    if (i != start)
                    {
                        rt.Add(token.Slice(start, i - start));
                        start = i;
                    }
                    isOpeningBracket = true;
                }
                else if (i == token.Length - 1)
                {
                    rt.Add(token.Slice(start, i - start + 1));
                }
                else if (!isNameToken(token.Span[i]) && !isPrefix)
                {
                    rt.Add(token.Slice(start, i - start));
                    start = i;
                }
                else if (isNameToken(token.Span[i]))
                    isPrefix = false;
            }

            return rt;
        }
    }
}