using System.Collections.Generic;
using HtmlAgilityPackCore.Nodes;

namespace HtmlAgilityPackCore.Selectors
{
    internal class AllSelector : CssSelector
    {
        public override string Token => "*";

        protected internal override IEnumerable<HtmlNode> FilterCore(IEnumerable<HtmlNode> currentNodes)
        {
            return currentNodes;
        }
    }
}