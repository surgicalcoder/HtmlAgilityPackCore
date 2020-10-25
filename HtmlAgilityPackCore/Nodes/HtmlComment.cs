using System;

namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents an HTML comment.
    /// </summary>
    public class HtmlComment : HtmlNodeBase
    {
        /// <summary>
        /// Gets the name of a comment node. It is actually defined as '#comment'.
        /// </summary>
        public const string HtmlNodeTypeName = "#comment";

        internal HtmlComment(HtmlDocument ownerdocument, int index)
            : base(HtmlNodeType.Comment, ownerdocument, index)
        {
            Name = HtmlNodeTypeName;
        }

        /// <summary>
        /// Gets or Sets the comment text of the node.
        /// </summary>
        public ReadOnlyMemory<char> Comment { get; set; }

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object. In the case of a text node, it is equals to OuterHtml.
        /// </summary>
        public override ReadOnlyMemory<char> InnerHtml
        {
            get => Comment;
            set => Comment = value;
        }

        /// <summary>
        /// Gets or Sets the object and its content in HTML.
        /// </summary>
        public override ReadOnlyMemory<char> OuterHtml => $"<!--{Comment}-->".AsMemory();

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="deep">true to recursively clone the subtree under the specified node; false to clone only the node itself.</param>
        /// <returns>The cloned node.</returns>
        public override HtmlNodeBase Clone(bool deep)
        {
            var node = base.Clone(deep) as HtmlComment;

            if (node != null)
            {
                node.Comment = Comment;
            }
            return node;
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="node">The node to duplicate. May not be <c>null</c>.</param>
        /// <param name="deep">true to recursively clone the subtree under the specified node, false to clone only the node itself.</param>
        public override void CopyFrom(HtmlNodeBase node, bool deep)
        {
            base.CopyFrom(node, deep);

            HtmlComment normalSrc = node as HtmlComment;
            if (normalSrc == null)
            {
                return;
            }

            Comment = normalSrc.Comment;
        }
    }
}