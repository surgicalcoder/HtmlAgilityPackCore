using System;
using System.Text;

namespace HtmlAgilityPackCore
{
    internal class EncodingFoundException : Exception
    {
        private Encoding _encoding;

        internal EncodingFoundException(Encoding encoding)
        {
            _encoding = encoding;
        }

        internal Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}