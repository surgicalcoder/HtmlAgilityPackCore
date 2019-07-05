using System;

namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents an HTML text node.
    /// </summary>
    public class HtmlTextNode : HtmlNode
    {
        private ReadOnlyMemory<char> _text;

        internal HtmlTextNode(HtmlDocument ownerdocument, int index) : base(HtmlNodeType.Text, ownerdocument, index)
        {
        }

        #region Properties

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object. In the case of a text node, it is equals to OuterHtml.
        /// </summary>
        public override ReadOnlyMemory<char> InnerHtml
        {
            get => OuterHtml;
            set => _text = value;
        }

        /// <summary>
        /// Gets or Sets the object and its content in HTML.
        /// </summary>
        public override ReadOnlyMemory<char> OuterHtml
        {
            get
            {
                return _text.IsEmpty ? base.OuterHtml : _text;
            }
        }

        /// <summary>
        /// Gets or Sets the text of the node.
        /// </summary>
        public ReadOnlyMemory<char> Text
        {
            get
            {
                return _text.IsEmpty ? base.OuterHtml : _text;
            }
            set
            {
                _text = value;
                SetChanged();
            }
        }

        #endregion
    }
}