using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

// ReSharper disable InconsistentNaming
namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents an HTML node.
    /// </summary>
    [DebuggerDisplay("Name: {OriginalName}")]
    public abstract partial class HtmlNodeBase
    {
        internal const string DepthLevelExceptionMessage = "The document is too complex to parse";

        private ReadOnlyMemory<char> optimizedName;

        private ReadOnlyMemory<char> _outerHtml;
        private int? _innerlength = null;
        private int? _outerlength = null;
        private bool _isChanged = false;

        /// <summary>
        /// Gets a collection of flags that define specific behaviors for specific element nodes.
        /// The table contains a DictionaryEntry list with the lowercase tag name as the Key, and a combination of HtmlElementFlags as the Value.
        /// </summary>
        public static Dictionary<string, HtmlElementFlag> ElementsFlags;

        /// <summary>
        /// Initialize HtmlNode. Builds a list of all tags that have special allowances
        /// </summary>
        static HtmlNodeBase()
        {
            // tags whose content may be anything
            ElementsFlags = new Dictionary<string, HtmlElementFlag>(StringComparer.OrdinalIgnoreCase);
            ElementsFlags.Add("script", HtmlElementFlag.CData);
            ElementsFlags.Add("style", HtmlElementFlag.CData);
            ElementsFlags.Add("noxhtml", HtmlElementFlag.CData); // can't found.
            ElementsFlags.Add("textarea", HtmlElementFlag.CData);
            ElementsFlags.Add("title", HtmlElementFlag.CData);

            // tags that can not contain other tags
            ElementsFlags.Add("base", HtmlElementFlag.Empty);
            ElementsFlags.Add("link", HtmlElementFlag.Empty);
            ElementsFlags.Add("meta", HtmlElementFlag.Empty);
            ElementsFlags.Add("isindex", HtmlElementFlag.Empty);
            ElementsFlags.Add("hr", HtmlElementFlag.Empty);
            ElementsFlags.Add("col", HtmlElementFlag.Empty);
            ElementsFlags.Add("img", HtmlElementFlag.Empty);
            ElementsFlags.Add("param", HtmlElementFlag.Empty);
            ElementsFlags.Add("embed", HtmlElementFlag.Empty);
            ElementsFlags.Add("frame", HtmlElementFlag.Empty);
            ElementsFlags.Add("wbr", HtmlElementFlag.Empty);
            ElementsFlags.Add("bgsound", HtmlElementFlag.Empty);
            ElementsFlags.Add("spacer", HtmlElementFlag.Empty);
            ElementsFlags.Add("keygen", HtmlElementFlag.Empty);
            ElementsFlags.Add("area", HtmlElementFlag.Empty);
            ElementsFlags.Add("input", HtmlElementFlag.Empty);
            ElementsFlags.Add("basefont", HtmlElementFlag.Empty);
            ElementsFlags.Add("source", HtmlElementFlag.Empty);
            ElementsFlags.Add("form", HtmlElementFlag.CanOverlap);

            //// they sometimes contain, and sometimes they don 't...
            //ElementsFlags.Add("option", HtmlElementFlag.Empty);

            // tag whose closing tag is equivalent to open tag:
            // <p>bla</p>bla will be transformed into <p>bla</p>bla
            // <p>bla<p>bla will be transformed into <p>bla<p>bla and not <p>bla></p><p>bla</p> or <p>bla<p>bla</p></p>
            //<br> see above
            ElementsFlags.Add("br", HtmlElementFlag.Empty | HtmlElementFlag.Closed);

            if (!HtmlDocument.DisableBehaviorTagP)
            {
                ElementsFlags.Add("p", HtmlElementFlag.Empty | HtmlElementFlag.Closed);
            }
        }

        /// <summary>
        /// Initializes HtmlNode, providing type, owner and where it exists in a collection
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ownerdocument"></param>
        /// <param name="index"></param>
        public HtmlNodeBase(HtmlNodeType type, HtmlDocument ownerdocument, int index)
        {
            NodeType = type;
            OwnerDocument = ownerdocument;
            OuterStartIndex = index;
        }

        /// <summary>
        /// Gets or sets this node's name.
        /// </summary>
        public virtual ReadOnlySpan<char> Name
        {
            get
            {
                if (optimizedName.IsEmpty)
                {
                    if (OriginalName.IsEmpty)
                    {
                        Name = OwnerDocument.Text.Slice(NameStartIndex, Namelength).Span;
                    }

                    if (OriginalName.IsEmpty)
                    {
                        optimizedName = ReadOnlyMemory<char>.Empty;
                    }
                    else
                    {
                        Span<char> t = stackalloc char[OriginalName.Length];
                        OriginalName.Span.ToLowerInvariant(t);
                        return t.ToArray();
                    }
                }

                return optimizedName.Span;
            }
            internal set
            {
                OriginalName = value.ToArray();
                optimizedName = null;
            }
        }

        /// <summary>
        /// The original unaltered name of the tag
        /// </summary>
        public ReadOnlyMemory<char> OriginalName { get; private set; }

        /// <summary>
        /// Gets the <see cref="HtmlDocument"/> to which this node belongs.
        /// </summary>
        public HtmlDocument OwnerDocument { get; private set; }

        /// <summary>
        /// Gets the type of this node.
        /// </summary>
        public HtmlNodeType NodeType { get; private set; }

        /// <summary>
        /// Gets or Sets the object and its content in HTML.
        /// </summary>
        public virtual ReadOnlyMemory<char> OuterHtml
        {
            get
            {
                if (IsChanged)
                {
                    UpdateHtml();
                    return _outerHtml;
                }

                if (!_outerHtml.IsNullOrWhiteSpace())
                {
                    return _outerHtml;
                }

                if (OuterStartIndex < 0 || OuterLength < 0)
                {
                    return ReadOnlyMemory<char>.Empty;
                }

                return OwnerDocument.Text.Slice(OuterStartIndex, OuterLength);
            }
        }

        public int OuterStartIndex { get; private set; }

        /// <summary>
        /// Gets the length of the entire node, opening and closing tag included.
        /// </summary>
        public int OuterLength
        {
            get
            {
                if (_outerlength != null)
                {
                    return _outerlength.Value;
                }
                else
                {
                    return OuterHtml.Length;
                }
            }
            internal set { _outerlength = value; }
        }

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object.
        /// </summary>
        public virtual ReadOnlyMemory<char> InnerHtml { get; set; }

        /// <summary>
        /// Gets the text between the start and end tags of the object.
        /// </summary>
        public virtual ReadOnlyMemory<char> InnerText
        {
            get
            {
                var sb = new StringBuilder();
                bool isDisplayScriptingText;

                string name = Name.ToString(); //todo
                if (name != null)
                {
                    name = name.ToLowerInvariant();
                    isDisplayScriptingText = (name == "head" || name == "script" || name == "style");
                }
                else
                {
                    isDisplayScriptingText = false;
                }

                InternalInnerText(sb, isDisplayScriptingText);
                return sb.ToString().AsMemory(); // todo
            }
        }

        /// <summary>
        /// Gets the stream position of the area between the opening and closing tag of the node, relative to the start of the document.
        /// </summary>
        public int InnerStartIndex { get; internal set; }

        /// <summary>
        /// Gets the length of the area between the opening and closing tag of the node.
        /// </summary>
        public int InnerLength
        {
            get
            {
                if (_innerlength != null)
                {
                    return _innerlength.Value;
                }
                else
                {
                    return InnerHtml.Length;
                }
            }
            internal set { _innerlength = value; }
        }

        /// <summary>
        /// Gets the line number of this node in the document.
        /// </summary>
        public int Line { get; internal set; }

        /// <summary>
        /// Gets the column number of this node in the document.
        /// </summary>
        public int LinePosition { get; internal set; }

        /// <summary>
        /// Gets the HTML node immediately following this element.
        /// </summary>
        public HtmlNodeBase NextSibling { get; internal set; }

        /// <summary>
        /// Gets the parent of this node (for nodes that can have parents).
        /// </summary>
        public IHtmlNodeContainer ParentNode { get; internal set; }

        /// <summary>
        /// Gets the node immediately preceding this node.
        /// </summary>
        public HtmlNodeBase PreviousSibling { get; internal set; }

        /// <summary>
        /// Gets the stream position of this node in the document, relative to the start of the document.
        /// </summary>
        public int StreamPosition { get; internal set; }

        /// <summary>
        /// Gets a valid XPath string that points to this node
        /// </summary>
        public virtual string XPath
        {
            get { return string.Format("{0}{1}", GetBasePath(), GetRelativeXpath()); }
        }

        /// <summary>
        /// The depth of the node relative to the opening root html element. This value is used to determine if a document has to many nested html nodes which can cause stack overflows
        /// </summary>
        public int Depth { get; set; }

        internal int Namelength { get; set; }
        internal int NameStartIndex { get; set; }
        internal HtmlNodeBase PrevWithSameName { get; set; }
        internal bool StartTag { get; set; }
        internal bool IsImplicitEnd { get; set; }
        internal bool IsHideInnerText { get; set; }

        /// <summary>
        /// Indicates whether the InnerHtml and the OuterHtml must be regenerated.
        /// </summary>
        public bool IsChanged
        {
            get { return _isChanged; }
            set
            {
                _isChanged = value;
                if (value && ParentNode != null)
                {
                    ParentNode.IsChanged = true;
                }
            }
        }

        protected virtual void InternalInnerText(StringBuilder sb, bool isDisplayScriptingText)
        {
            sb.Append(GetCurrentNodeText());
        }

        /// <summary>Gets direct inner text.</summary>
        /// <returns>The direct inner text.</returns>
        public virtual string GetDirectInnerText()
        {
            return GetCurrentNodeText();
        }

        protected virtual string GetCurrentNodeText()
        {
            return string.Empty;
        }

        internal virtual void AppendDirectInnerText(StringBuilder sb)
        {
            string currentNodeText = GetCurrentNodeText();
            if (!string.IsNullOrWhiteSpace(currentNodeText))
            {
                sb.Append(GetCurrentNodeText());
            }
        }

        internal virtual void AppendInnerText(StringBuilder sb, bool isShowHideInnerText)
        {
            string currentNodeText = GetCurrentNodeText();
            if (!string.IsNullOrWhiteSpace(currentNodeText))
            {
                sb.Append(GetCurrentNodeText());
            }
        }

        /// <summary>
        /// Creates a duplicate of the node
        /// </summary>
        /// <returns>The cloned node.</returns>
        public HtmlNodeBase Clone()
        {
            return Clone(true);
        }

        /// <summary>
        /// Creates a duplicate of the node and changes its name at the same time.
        /// </summary>
        /// <param name="newName">The new name of the cloned node. May not be <c>null</c>.</param>
        /// <returns>The cloned node.</returns>
        public HtmlNodeBase Clone(string newName)
        {
            return Clone(newName, true);
        }

        /// <summary>
        /// Creates a duplicate of the node and changes its name at the same time.
        /// </summary>
        /// <param name="newName">The new name of the cloned node. May not be null.</param>
        /// <param name="deep">true to recursively clone the subtree under the specified node; false to clone only the node itself.</param>
        /// <returns>The cloned node.</returns>
        public HtmlNodeBase Clone(string newName, bool deep)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentNullException(nameof(newName));
            }

            HtmlNodeBase node = Clone(deep);
            node.Name = newName;
            return node;
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="deep">true to recursively clone the subtree under the specified node; false to clone only the node itself.</param>
        /// <returns>The cloned node.</returns>
        public virtual HtmlNodeBase Clone(bool deep)
        {
            HtmlNodeBase node = HtmlNodeFactory.Create(OwnerDocument, NodeType);
            node.Name = OriginalName.Span;
            return node;
        }

        /// <summary>
        /// Creates a duplicate of the node and the subtree under it.
        /// </summary>
        /// <param name="node">The node to duplicate. May not be <c>null</c>.</param>
        public void CopyFrom(HtmlNodeBase node)
        {
            CopyFrom(node, true);
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="node">The node to duplicate. May not be <c>null</c>.</param>
        /// <param name="deep">true to recursively clone the subtree under the specified node, false to clone only the node itself.</param>
        public virtual void CopyFrom(HtmlNodeBase node, bool deep)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Name = node.Name;
        }

        /// <summary>
        /// Returns a collection of all ancestor nodes of this element.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IHtmlNodeContainer> Ancestors()
        {
            IHtmlNodeContainer node = ParentNode;
            if (node != null)
            {
                yield return node; //return the immediate parent node

                //now look at it's parent and walk up the tree of parents
                while (node.ParentNode != null)
                {
                    yield return node.ParentNode;
                    node = node.ParentNode;
                }
            }
        }

        /// <summary>
        /// Get Ancestors with matching name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<IHtmlNodeContainer> Ancestors(string name)
        {
            for (IHtmlNodeContainer n = ParentNode; n != null; n = n.ParentNode)
                if (n.Name == name)
                    yield return n;
        }

        /// <summary>
        /// Returns a collection of all ancestor nodes of this element.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> AncestorsAndSelf()
        {
            for (HtmlNodeBase n = this; n != null; n = n.ParentNode as HtmlNodeBase)
                yield return n;
        }

        /// <summary>
        /// Gets all anscestor nodes and the current node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> AncestorsAndSelf(string name)
        {
            for (HtmlNodeBase n = this; n != null; n = n.ParentNode as HtmlNodeBase)
                if (n.Name.Equals(name))
                    yield return n;
        }

        /// <summary>
        /// Sets the parent Html node and properly determines the current node's depth using the parent node's depth.
        /// </summary>
        public void SetParent(IHtmlNodeContainer parent)
        {
            if (parent == null)
                return;

            ParentNode = parent;
            if (OwnerDocument.OptionMaxNestedChildNodes > 0)
            {
                Depth = parent.Depth + 1;
                if (Depth > OwnerDocument.OptionMaxNestedChildNodes)
                    throw new Exception(string.Format("Document has more than {0} nested tags. This is likely due to the page not closing tags properly.", OwnerDocument.OptionMaxNestedChildNodes));
            }
        }

        /// <summary>
        /// Removes node from parent collection
        /// </summary>
        public void Remove()
        {
            if (ParentNode != null)
            {
                ParentNode.ChildNodes.Remove(this);
            }
        }

        /// <summary>
        /// Saves the current node to the specified TextWriter.
        /// </summary>
        /// <param name="outText">The TextWriter to which you want to save.</param>
        /// <param name="level">identifies the level we are in starting at root with 0</param>
        public virtual void WriteTo(TextWriter outText, int level = 0)
        {
            outText.Write(OuterHtml);
        }

        /// <summary>
        /// Saves the current node to a string.
        /// </summary>
        /// <returns>The saved string.</returns>
        public string WriteTo()
        {
            using (StringWriter sw = new StringWriter())
            {
                WriteTo(sw);
                sw.Flush();
                return sw.ToString();
            }
        }

        /// <summary>
        /// Saves all the content of the node to a string.
        /// </summary>
        /// <returns>The saved string.</returns>
        public ReadOnlyMemory<char> WriteContentTo()
        {
            using (var sw = new StringWriter())
            {
                WriteContentTo(sw);
                sw.Flush();
                return sw.ToString().AsMemory();
            }
        }

        /// <summary>
        /// Saves all the content of the node to the specified TextWriter.
        /// </summary>
        /// <param name="outText">The TextWriter to which you want to save.</param>
        /// <param name="level">Identifies the level we are in starting at root with 0</param>
        public virtual void WriteContentTo(TextWriter outText, int level = 0)
        {
            if (outText == null)
            {
                throw new ArgumentNullException(nameof(outText));
            }

            outText.Write(OuterHtml);
        }

        protected void UpdateHtml()
        {
            InnerHtml = WriteContentTo();
            _outerHtml = WriteTo().AsMemory(); //todo
            IsChanged = false;
        }

        internal void UpdateLastNode()
        {
            HtmlNodeBase newLast = null;
            if (PrevWithSameName == null || !PrevWithSameName.StartTag)
            {
                if (OwnerDocument.Openednodes != null)
                {
                    foreach (var openNode in OwnerDocument.Openednodes)
                    {
                        if ((openNode.Key < OuterStartIndex || openNode.Key > (OuterStartIndex + OuterLength)) && MemoryExtensions.Equals(openNode.Value.OriginalName, OriginalName))
                        {
                            if (newLast == null && openNode.Value.StartTag)
                            {
                                newLast = openNode.Value;
                            }
                            else if (newLast != null && newLast.InnerStartIndex < openNode.Key && openNode.Value.StartTag)
                            {
                                newLast = openNode.Value;
                            }
                        }
                    }
                }
            }
            else
            {
                newLast = PrevWithSameName;
            }


            if (newLast != null)
            {
                OwnerDocument.Lastnodes[newLast.Name.ToString()] = newLast; //todo
            }
        }

        protected virtual string GetBasePath()
        {
            if (ParentNode == null)
            {
                return "/";
            }
            else
            {
                return ParentNode.XPath + "/";
            }
        }

        protected virtual string GetRelativeXpath() //todo
        {
            if (ParentNode == null)
                return Name.ToString();

            int i = 1;
            foreach (HtmlNodeBase node in ParentNode.ChildNodes)
            {
                if (node.Name != Name) continue;

                if (node == this)
                    break;

                i++;
            }

            return $"{Name.ToString()}[{i.ToString()}]"; //todo

        }

        /// <summary>
        /// Determines if an element node can be kept overlapped.
        /// </summary>
        /// <param name="name">The name of the element node to check. May not be <c>null</c>.</param>
        /// <returns>true if the name is the name of an element node that can be kept overlapped, <c>false</c> otherwise.</returns>
        public static bool CanOverlapElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlElementFlag flag;
            if (!ElementsFlags.TryGetValue(name, out flag))
            {
                return false;
            }

            return (flag & HtmlElementFlag.CanOverlap) != 0;
        }

        /// <summary>
        /// Determines if an element node is a CDATA element node.
        /// </summary>
        /// <param name="name">The name of the element node to check. May not be null.</param>
        /// <returns>true if the name is the name of a CDATA element node, false otherwise.</returns>
        public static bool IsCDataElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlElementFlag flag;
            if (!ElementsFlags.TryGetValue(name, out flag))
            {
                return false;
            }

            return (flag & HtmlElementFlag.CData) != 0;
        }

        /// <summary>
        /// Determines if an element node is closed.
        /// </summary>
        /// <param name="name">The name of the element node to check. May not be null.</param>
        /// <returns>true if the name is the name of a closed element node, false otherwise.</returns>
        public static bool IsClosedElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlElementFlag flag;
            if (!ElementsFlags.TryGetValue(name, out flag))
            {
                return false;
            }

            return (flag & HtmlElementFlag.Closed) != 0;
        }

        /// <summary>
        /// Determines if an element node is defined as empty.
        /// </summary>
        /// <param name="name">The name of the element node to check. May not be null.</param>
        /// <returns>true if the name is the name of an empty element node, false otherwise.</returns>
        public static bool IsEmptyElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                return true;
            }

            // <!DOCTYPE ...
            if ('!' == name[0])
            {
                return true;
            }

            // <?xml ...
            if ('?' == name[0])
            {
                return true;
            }

            HtmlElementFlag flag;
            if (!ElementsFlags.TryGetValue(name, out flag))
            {
                return false;
            }

            return (flag & HtmlElementFlag.Empty) != 0;
        }

        /// <summary>
        /// Determines if a text corresponds to the closing tag of an node that can be kept overlapped.
        /// </summary>
        /// <param name="text">The text to check. May not be null.</param>
        /// <returns>true or false.</returns>
        public static bool IsOverlappedClosingElement(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            // min is </x>: 4
            if (text.Length <= 4)
                return false;

            if ((text[0] != '<') ||
                (text[text.Length - 1] != '>') ||
                (text[1] != '/'))
                return false;

            string name = text.Substring(2, text.Length - 3);
            return CanOverlapElement(name);
        }
    }
}