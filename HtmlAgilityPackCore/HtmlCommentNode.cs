using System;

namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents an HTML comment.
    /// </summary>
    public class HtmlCommentNode : HtmlNode
    {
        #region Fields

        private ReadOnlyMemory<char> _comment;

        #endregion

        #region Constructors

        internal HtmlCommentNode(HtmlDocument ownerdocument, int index)
            :
            base(HtmlNodeType.Comment, ownerdocument, index)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or Sets the comment text of the node.
        /// </summary>
        public ReadOnlyMemory<char> Comment
        {
            get
            {
                if (_comment.IsEmpty)
                {
                    return base.InnerHtml;
                }

                return _comment;
            }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object. In the case of a text node, it is equals to OuterHtml.
        /// </summary>
        public override ReadOnlyMemory<char> InnerHtml
        {
            get
            {
                if (_comment.IsEmpty)
                {
                    return base.InnerHtml;
                }

                return _comment;
            }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or Sets the object and its content in HTML.
        /// </summary>
        public override ReadOnlyMemory<char> OuterHtml
        {
            get
            {
                if (_comment.IsEmpty)
                {
                    return base.OuterHtml;
                }

                return $"<!--{_comment}-->".AsMemory();
            }
        }

        #endregion
    }
}