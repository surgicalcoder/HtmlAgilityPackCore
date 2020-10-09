namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents methods used to create a concrete HtmlNode instance.
    /// </summary>
    public static class HtmlNodeFactory
    {
        /// <summary>
        /// Create a specific type of HtmlNode
        /// </summary>
        /// <param name="ownerDoc">The owner document of this node</param>
        /// <param name="type">The type of this node</param>
        /// <param name="index"></param>
        public static HtmlNodeBase Create(HtmlDocument ownerDoc, HtmlNodeType type, int index)
        {
            switch (type)
            {
                case HtmlNodeType.Comment:
                    return new HtmlComment(ownerDoc, index);
                case HtmlNodeType.Text:
                    return new HtmlText(ownerDoc, index);
                case HtmlNodeType.Document:
                    return new HtmlDocumentNode(ownerDoc, index);
                default:
                    return new HtmlElement(ownerDoc, index);
            }
        }

        /// <summary>
        /// Create a specific type of HtmlNode
        /// </summary>
        /// <param name="ownerDoc">The owner document of this node</param>
        /// <param name="type">The type of this node</param>
        public static HtmlNodeBase Create(HtmlDocument ownerDoc, HtmlNodeType type)
        {
            return Create(ownerDoc, type, -1);
        }

        /// <summary>
        /// Creates an HTML node from a string representing literal HTML.
        /// </summary>
        /// <param name="html">The HTML text.</param>
        /// <returns>The newly created node instance.</returns>
        public static HtmlNodeBase CreateNode(string html)
        {
            // REVIEW: this is *not* optimum...
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlElement element = doc.DocumentNode.FirstChild as HtmlElement;
            while (element != null)
            {
                if (element.IsValid())
                {
                    return element;
                }
                element = element.NextSibling as HtmlElement;
            }

            return doc.DocumentNode.FirstChild;
        }
    }
}