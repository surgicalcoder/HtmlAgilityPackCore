using System;

namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents an HTML text node.
    /// </summary>
    public class HtmlText : HtmlNodeBase
    {
        /// <summary>
        /// Gets the name of a text node. It is actually defined as '#text'.
        /// </summary>
        public const string HtmlNodeTypeName = "#text";

        private ReadOnlyMemory<char> _text;

        internal HtmlText(HtmlDocument ownerdocument, int index)
            : base(HtmlNodeType.Text, ownerdocument, index)
        {
            Name = HtmlNodeTypeName;
        }

        /// <summary>
        /// Gets or Sets the text of the node.
        /// </summary>
        public ReadOnlyMemory<char> Text
        {
            get => _text;
            set
            {
                _text = value;
                IsChanged = true;
            }
        }

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object. In the case of a text node, it is equals to OuterHtml.
        /// </summary>
        public override ReadOnlyMemory<char> InnerHtml
        {
            get => Text;
            set => Text = value;
        }

        /// <summary>
        /// Gets or Sets the object and its content in HTML.
        /// </summary>
        public override string OuterHtml
        {
            get { return Text; }
        }

        protected override string GetCurrentNodeText()
        {
            string s = Text;
            if (ParentNode.Name != "pre")
            {
                // Make some test...
                s = s.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            }
            return s;
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="deep">true to recursively clone the subtree under the specified node; false to clone only the node itself.</param>
        /// <returns>The cloned node.</returns>
        public override HtmlNodeBase Clone(bool deep)
        {
            HtmlText node = base.Clone(deep) as HtmlText;
            if (node != null)
            {
                node.Text = Text;
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

            HtmlText normalSrc = node as HtmlText;
            if (normalSrc == null)
            {
                return;
            }

            Text = normalSrc.Text;
        }
    }
}