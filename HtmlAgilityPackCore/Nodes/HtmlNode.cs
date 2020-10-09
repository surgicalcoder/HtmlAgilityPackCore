using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents a true HTML node.
    /// </summary>
    public abstract partial class HtmlNode : HtmlNodeBase, IHtmlNodeContainer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The type of this node</param>
        /// <param name="ownerDoc">The owner document of this node</param>
        /// <param name="index"></param>
        public HtmlNode(HtmlNodeType type, HtmlDocument ownerDoc, int index)
            : base(type, ownerDoc, index)
        {
            InitName();
            InitEndNode();

            Attributes = new HtmlAttributeCollection(this);
            ChildNodes = new HtmlNodeCollection(this);

            if (OwnerDocument.Openednodes != null)
            {
                if (!Closed)
                {
                    // we use the index as the key

                    // -1 means the node comes from public
                    if (-1 != index)
                    {
                        OwnerDocument.Openednodes.Add(index, this);
                    }
                }
            }

            // innerhtml and outerhtml must be calculated
            // -1 means the node comes from public
            if (-1 == index)
            {
                IsChanged = true;
            }
        }

        /// <summary>
        /// Gets the closing tag of the node, null if the node is self-closing.
        /// </summary>
        public HtmlNode EndNode { get; internal set; }

        /// <summary>
        /// Gets a value indicating if this node has been closed or not.
        /// </summary>
        public bool Closed => (EndNode != null);

        /// <summary>
        /// Gets a value indicating whether the current node has any attributes on the closing tag.
        /// </summary>
        public bool HasClosingAttributes
        {
            get
            {
                if ((EndNode == null) || (EndNode == this))
                {
                    return false;
                }

                return EndNode.HasAttributes;
            }
        }

        /// <summary>
        /// Gets the collection of HTML attributes for this node. May not be null.
        /// </summary>
        public HtmlAttributeCollection Attributes { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current node has any attributes.
        /// </summary>
        public bool HasAttributes
        {
            get
            {
                if (Attributes.Count <= 0)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the collection of HTML attributes for the closing tag. May not be null.
        /// </summary>
        public HtmlAttributeCollection ClosingAttributes
        {
            get { return !HasClosingAttributes ? new HtmlAttributeCollection(this) : EndNode.Attributes; }
        }

        /// <summary>
        /// Gets or sets the value of the 'id' HTML attribute. The document must have been parsed using the OptionUseIdAttribute set to true.
        /// </summary>
        public string Id
        {
            get
            {
                if (OwnerDocument.Nodesid == null)
                    throw new Exception(HtmlDocument.HtmlExceptionUseIdAttributeFalse);

                return GetId();
            }
            set
            {
                if (OwnerDocument.Nodesid == null)
                    throw new Exception(HtmlDocument.HtmlExceptionUseIdAttributeFalse);

                if (value == null)
                    throw new ArgumentNullException("value");

                SetId(value);
            }
        }

        /// <summary>
        /// Gets all the children of the node.
        /// </summary>
        public HtmlNodeCollection ChildNodes { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this node has any child nodes.
        /// </summary>
        public bool HasChildNodes
        {
            get
            {
                if (ChildNodes.Count <= 0)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the first child of the node.
        /// </summary>
        public HtmlNodeBase FirstChild
        {
            get { return !HasChildNodes ? null : ChildNodes[0]; }
        }

        /// <summary>
        /// Gets the last child of the node.
        /// </summary>
        public HtmlNodeBase LastChild
        {
            get { return !HasChildNodes ? null : ChildNodes[ChildNodes.Count - 1]; }
        }

        /// <summary>
        /// Gets or Sets the HTML between the start and end tags of the object.
        /// </summary>
        public override string InnerHtml
        {
            get
            {
                if (IsChanged)
                {
                    UpdateHtml();
                    return base.InnerHtml;
                }

                if (!string.IsNullOrWhiteSpace(base.InnerHtml))
                {
                    return base.InnerHtml;
                }

                if (InnerStartIndex < 0 || InnerLength < 0)
                {
                    return string.Empty;
                }

                return OwnerDocument.Text.Substring(InnerStartIndex, InnerLength);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException();
                }

                base.InnerHtml = value;

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(value);

                RemoveAllChildren();
                AppendChildren(doc.DocumentNode.ChildNodes);
            }
        }

        /// <summary>
        /// Initialize the Name property
        /// </summary>
        protected abstract void InitName();

        /// <summary>
        /// Initialize the end node field
        /// </summary>
        protected abstract void InitEndNode();

        protected override void InternalInnerText(StringBuilder sb, bool isDisplayScriptingText)
        {
            if (HasChildNodes)
            {
                AppendInnerText(sb, isDisplayScriptingText);
            }
            else
            {
                base.InternalInnerText(sb, isDisplayScriptingText);
            }
        }

        /// <summary>Gets direct inner text.</summary>
        /// <returns>The direct inner text.</returns>
        public override string GetDirectInnerText()
        {
            if (HasChildNodes)
            {
                StringBuilder sb = new StringBuilder();
                AppendDirectInnerText(sb);
                return sb.ToString();
            }

            return base.GetDirectInnerText();
        }

        internal override void AppendDirectInnerText(StringBuilder sb)
        {
            base.AppendDirectInnerText(sb);

            if (HasChildNodes)
            {
                foreach (HtmlNodeBase node in ChildNodes)
                {
                    node.AppendDirectInnerText(sb);
                }
            }
        }

        internal override void AppendInnerText(StringBuilder sb, bool isShowHideInnerText)
        {
            base.AppendInnerText(sb, isShowHideInnerText);

            if (!HasChildNodes || (IsHideInnerText && !isShowHideInnerText)) return;

            foreach (HtmlNodeBase node in ChildNodes)
            {
                node.AppendInnerText(sb, isShowHideInnerText);
            }
        }

        internal void CloseNode(HtmlNode endnode, int level = 0)
        {
            if (level > HtmlDocument.MaxDepthLevel)
            {
                throw new ArgumentException(HtmlNodeBase.DepthLevelExceptionMessage);
            }

            if (!OwnerDocument.OptionAutoCloseOnEnd)
            {
                // close all children
                if (HasChildNodes)
                {
                    foreach (HtmlNodeBase child in ChildNodes)
                    {
                        HtmlNode normalChild = child as HtmlNode;
                        if (normalChild == null || normalChild.Closed)
                        {
                            continue;
                        }

                        // create a fake closer node
                        HtmlNode close = HtmlNodeFactory.Create(OwnerDocument, NodeType, -1) as HtmlNode;
                        if (close != null)
                        {
                            close.EndNode = close;
                            normalChild.CloseNode(close, level + 1);
                        }
                    }
                }
            }

            if (!Closed)
            {
                EndNode = endnode;

                if (OwnerDocument.Openednodes != null)
                    OwnerDocument.Openednodes.Remove(OuterStartIndex);

                HtmlNodeBase self = Utilities.GetDictionaryValueOrDefault(OwnerDocument.Lastnodes, Name);
                if (self == this)
                {
                    OwnerDocument.Lastnodes.Remove(Name);
                    OwnerDocument.UpdateLastParentNode();


                    if (StartTag && !String.IsNullOrEmpty(Name))
                    {
                        UpdateLastNode();
                    }
                }

                if (endnode == this)
                    return;

                // create an inner section
                InnerStartIndex = OuterStartIndex + OuterLength;
                InnerLength = endnode.OuterStartIndex - InnerStartIndex;

                // update full length
                OuterLength = (endnode.OuterStartIndex + endnode.OuterLength) - OuterStartIndex;
            }
        }

        private string GetId()
        {
            HtmlAttribute att = Attributes["id"];
            return att == null ? string.Empty : att.Value;
        }

        private void SetId(string id)
        {
            HtmlAttribute att = Attributes["id"] ?? OwnerDocument.CreateAttribute("id");
            att.Value = id;
            OwnerDocument.SetIdForNode(this, att.Value);
            Attributes["id"] = att;
            IsChanged = true;
        }

        /// <summary>Gets data attribute.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The data attribute.</returns>
        public HtmlAttribute GetDataAttribute(string key)
        {
            return Attributes.Hashitems.SingleOrDefault(x => x.Key.Equals("data-" + key, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>Gets the data attributes in this collection.</summary>
        /// <returns>
        /// An enumerator that allows foreach to be used to process the data attributes in this
        /// collection.
        /// </returns>
		public IEnumerable<HtmlAttribute> GetDataAttributes()
        {
            return Attributes.Hashitems.Where(x => x.Key.StartsWith("data-", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value).ToList();
        }

        /// <summary>Gets the attributes in this collection.</summary>
        /// <returns>
        /// An enumerator that allows foreach to be used to process the attributes in this collection.
        /// </returns>
		public IEnumerable<HtmlAttribute> GetAttributes()
        {
            return Attributes.items;
        }

        /// <summary>Gets the attributes in this collection.</summary>
        /// <param name="attributeNames">A variable-length parameters list containing attribute names.</param>
        /// <returns>
        /// An enumerator that allows foreach to be used to process the attributes in this collection.
        /// </returns>
		public IEnumerable<HtmlAttribute> GetAttributes(params string[] attributeNames)
        {
            List<HtmlAttribute> list = new List<HtmlAttribute>();

            foreach (var name in attributeNames)
            {
                list.Add(Attributes[name]);
            }

            return list;
        }

        /// <summary>
        /// Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
        /// </summary>
        /// <param name="name">The name of the attribute to get. May not be <c>null</c>.</param>
        /// <param name="def">The default value to return if not found.</param>
        /// <returns>The value of the attribute if found, the default value if not found.</returns>
        public string GetAttributeValue(string name, string def)
        {
#if METRO || NETSTANDARD1_3 || NETSTANDARD1_6
            if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			if (!HasAttributes)
			{
				return def;
			}

			HtmlAttribute att = Attributes[name];
			if (att == null)
			{
				return def;
			}

			return att.Value;
#else
            return GetAttributeValue<string>(name, def);
#endif
        }

        /// <summary>
        /// Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
        /// </summary>
        /// <param name="name">The name of the attribute to get. May not be <c>null</c>.</param>
        /// <param name="def">The default value to return if not found.</param>
        /// <returns>The value of the attribute if found, the default value if not found.</returns>
        public int GetAttributeValue(string name, int def)
        {
#if METRO || NETSTANDARD1_3 || NETSTANDARD1_6
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			if (!HasAttributes)
			{
				return def;
			}

			HtmlAttribute att = Attributes[name];
			if (att == null)
			{
				return def;
			}

			try
			{
				return Convert.ToInt32(att.Value);
			}
			catch
			{
				return def;
			}
#else
            return GetAttributeValue<int>(name, def);
#endif
        }

        /// <summary>
        /// Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
        /// </summary>
        /// <param name="name">The name of the attribute to get. May not be <c>null</c>.</param>
        /// <param name="def">The default value to return if not found.</param>
        /// <returns>The value of the attribute if found, the default value if not found.</returns>
        public bool GetAttributeValue(string name, bool def)
        {
#if METRO || NETSTANDARD1_3 || NETSTANDARD1_6
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			if (!HasAttributes)
			{
				return def;
			}

			HtmlAttribute att = Attributes[name];
			if (att == null)
			{
				return def;
			}

			try
			{
				return Convert.ToBoolean(att.Value);
			}
			catch
			{
				return def;
			}
#else
            return GetAttributeValue<bool>(name, def);
#endif
        }


#if !(METRO || NETSTANDARD1_3 || NETSTANDARD1_6)
        /// <summary>
        /// Helper method to get the value of an attribute of this node. If the attribute is not found,
        /// the default value will be returned.
        /// </summary>
        /// <param name="name">The name of the attribute to get. May not be <c>null</c>.</param>
        /// <param name="def">The default value to return if not found.</param>
        /// <returns>The value of the attribute if found, the default value if not found.</returns>
        public T GetAttributeValue<T>(string name, T def)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (!HasAttributes)
            {
                return def;
            }

            HtmlAttribute att = Attributes[name];
            if (att == null)
            {
                return def;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

            try
            {
                if (converter != null && converter.CanConvertTo(att.Value.GetType()))
                {
                    return (T)converter.ConvertTo(att.Value, typeof(T));
                }
                else
                {
                    return (T)(object)att.Value;
                }
            }
            catch
            {
                return def;
            }
        }
#endif

        /// <summary>
        /// Helper method to set the value of an attribute of this node. If the attribute is not found, it will be created automatically.
        /// </summary>
        /// <param name="name">The name of the attribute to set. May not be null.</param>
        /// <param name="value">The value for the attribute.</param>
        /// <returns>The corresponding attribute instance.</returns>
        public HtmlAttribute SetAttributeValue(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlAttribute att = Attributes[name];
            if (att == null)
            {
                return Attributes.Append(OwnerDocument.CreateAttribute(name, value));
            }

            att.Value = value;
            return att;
        }

        /// <summary>
        /// Adds the specified node to the end of the list of children of this node.
        /// </summary>
        /// <param name="newChild">The node to add. May not be null.</param>
        /// <returns>The node added.</returns>
        public HtmlNodeBase AppendChild(HtmlNodeBase newChild)
        {
            if (newChild == null)
            {
                throw new ArgumentNullException("newChild");
            }

            ChildNodes.Append(newChild);

            HtmlNode normalChild = newChild as HtmlNode;
            if (normalChild != null)
            {
                OwnerDocument.SetIdForNode(newChild, normalChild.GetId());
            }
            SetChildNodesId(newChild);

            IsChanged = true;
            return newChild;
        }

        /// <summary>
        /// Adds the specified node to the end of the list of children of this node.
        /// </summary>
        /// <param name="newChildren">The node list to add. May not be null.</param>
        public void AppendChildren(HtmlNodeCollection newChildren)
        {
            if (newChildren == null)
                throw new ArgumentNullException("newChildren");

            foreach (HtmlNodeBase newChild in newChildren)
            {
                AppendChild(newChild);
            }
        }

        /// <summary>Sets child nodes identifier.</summary>
        /// <param name="childNode">The chil node.</param>
        public void SetChildNodesId(HtmlNodeBase childNode)
        {
            IHtmlNodeContainer childContainer = childNode as IHtmlNodeContainer;
            if (childContainer == null)
            {
                return;
            }

            foreach (HtmlNodeBase child in childContainer.ChildNodes)
            {
                HtmlNode normalChild = child as HtmlNode;
                if (normalChild != null)
                {
                    OwnerDocument.SetIdForNode(child, normalChild.GetId());
                }

                SetChildNodesId(child);
            }
        }

        /// <summary>
        /// Gets all Attributes with name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlAttribute> ChildAttributes(string name)
        {
            return Attributes.AttributesWithName(name);
        }

        /// <summary>
        /// Inserts the specified node immediately after the specified reference node.
        /// </summary>
        /// <param name="newChild">The node to insert. May not be <c>null</c>.</param>
        /// <param name="refChild">The node that is the reference node. The newNode is placed after the refNode.</param>
        /// <returns>The node being inserted.</returns>
        public HtmlNodeBase InsertAfter(HtmlNodeBase newChild, HtmlNodeBase refChild)
        {
            if (newChild == null)
            {
                throw new ArgumentNullException("newChild");
            }

            if (refChild == null)
            {
                return PrependChild(newChild);
            }

            if (newChild == refChild)
            {
                return newChild;
            }

            int index = -1;

            if (HasChildNodes)
            {
                index = ChildNodes[refChild];
            }

            if (index == -1)
            {
                throw new ArgumentException(HtmlDocument.HtmlExceptionRefNotChild);
            }

            if (HasChildNodes) ChildNodes.Insert(index + 1, newChild);

            HtmlNode normalNewChild = newChild as HtmlNode;
            if (normalNewChild != null)
            {
                OwnerDocument.SetIdForNode(newChild, normalNewChild.GetId());
            }
            SetChildNodesId(newChild);

            IsChanged = true;
            return newChild;
        }

        /// <summary>
        /// Inserts the specified node immediately before the specified reference node.
        /// </summary>
        /// <param name="newChild">The node to insert. May not be <c>null</c>.</param>
        /// <param name="refChild">The node that is the reference node. The newChild is placed before this node.</param>
        /// <returns>The node being inserted.</returns>
        public HtmlNodeBase InsertBefore(HtmlNodeBase newChild, HtmlNodeBase refChild)
        {
            if (newChild == null)
            {
                throw new ArgumentNullException("newChild");
            }

            if (refChild == null)
            {
                return AppendChild(newChild);
            }

            if (newChild == refChild)
            {
                return newChild;
            }

            int index = -1;

            if (HasChildNodes)
            {
                index = ChildNodes[refChild];
            }

            if (index == -1)
            {
                throw new ArgumentException(HtmlDocument.HtmlExceptionRefNotChild);
            }

            if (HasChildNodes) ChildNodes.Insert(index, newChild);

            HtmlNode normalNewChild = newChild as HtmlNode;
            if (normalNewChild != null)
            {
                OwnerDocument.SetIdForNode(newChild, normalNewChild.GetId());
            }
            SetChildNodesId(newChild);

            IsChanged = true;
            return newChild;
        }

        /// <summary>
        /// Adds the specified node to the beginning of the list of children of this node.
        /// </summary>
        /// <param name="newChild">The node to add. May not be <c>null</c>.</param>
        /// <returns>The node added.</returns>
        public HtmlNodeBase PrependChild(HtmlNodeBase newChild)
        {
            if (newChild == null)
            {
                throw new ArgumentNullException("newChild");
            }

            ChildNodes.Prepend(newChild);

            HtmlNode normalNewChild = newChild as HtmlNode;
            if (normalNewChild != null)
            {
                OwnerDocument.SetIdForNode(newChild, normalNewChild.GetId());
            }
            SetChildNodesId(newChild);

            IsChanged = true;
            return newChild;
        }

        /// <summary>
        /// Adds the specified node list to the beginning of the list of children of this node.
        /// </summary>
        /// <param name="newChildren">The node list to add. May not be <c>null</c>.</param>
        public void PrependChildren(HtmlNodeCollection newChildren)
        {
            if (newChildren == null)
            {
                throw new ArgumentNullException("newChildren");
            }

            for (int i = newChildren.Count - 1; i >= 0; i--)
            {
                PrependChild(newChildren[i]);
            }
        }

        /// <summary>
        /// Removes all the children of the current node.
        /// </summary>
        public void RemoveAllChildren()
        {
            if (!HasChildNodes)
            {
                return;
            }

            if (OwnerDocument.OptionUseIdAttribute)
            {
                // remove nodes from id list
                foreach (HtmlNodeBase child in ChildNodes)
                {
                    HtmlNode normalChild = child as HtmlNode;
                    if (normalChild != null)
                    {
                        OwnerDocument.SetIdForNode(null, normalChild.GetId());
                        normalChild.RemoveAllID();
                    }
                }
            }

            ChildNodes.Clear();
            IsChanged = true;
        }

        /// <summary>
        /// Removes the specified child node.
        /// </summary>
        /// <param name="oldChild">The node being removed. May not be <c>null</c>.</param>
        /// <returns>The node removed.</returns>
        public HtmlNodeBase RemoveChild(HtmlNodeBase oldChild)
        {
            if (oldChild == null)
            {
                throw new ArgumentNullException("oldChild");
            }

            int index = -1;

            if (HasChildNodes)
            {
                index = ChildNodes[oldChild];
            }

            if (index == -1)
            {
                throw new ArgumentException(HtmlDocument.HtmlExceptionRefNotChild);
            }

            if (HasChildNodes)
                ChildNodes.Remove(index);

            HtmlNode normalOldChild = oldChild as HtmlNode;
            if (normalOldChild != null)
            {
                OwnerDocument.SetIdForNode(null, normalOldChild.GetId());
                normalOldChild.RemoveAllID();
            }

            IsChanged = true;
            return oldChild;
        }

        /// <summary>
        /// Removes the specified child node.
        /// </summary>
        /// <param name="oldChild">The node being removed. May not be <c>null</c>.</param>
        /// <param name="keepGrandChildren">true to keep grand children of the node, false otherwise.</param>
        /// <returns>The node removed.</returns>
        public HtmlNodeBase RemoveChild(HtmlNodeBase oldChild, bool keepGrandChildren)
        {
            if (oldChild == null)
            {
                throw new ArgumentNullException("oldChild");
            }

            HtmlNode normalOldChild = oldChild as HtmlNode;
            if ((normalOldChild != null && normalOldChild.HasChildNodes) && keepGrandChildren)
            {
                // get prev sibling
                HtmlNodeBase prev = oldChild.PreviousSibling;

                // reroute grand children to ourselves
                foreach (HtmlNodeBase grandchild in normalOldChild.ChildNodes)
                {
                    prev = InsertAfter(grandchild, prev);
                }
            }

            RemoveChild(oldChild);
            IsChanged = true;
            return oldChild;
        }

        /// <summary>
        /// Replaces the child node oldChild with newChild node.
        /// </summary>
        /// <param name="newChild">The new node to put in the child list.</param>
        /// <param name="oldChild">The node being replaced in the list.</param>
        /// <returns>The node replaced.</returns>
        public HtmlNodeBase ReplaceChild(HtmlNodeBase newChild, HtmlNodeBase oldChild)
        {
            if (newChild == null)
            {
                return RemoveChild(oldChild);
            }

            if (oldChild == null)
            {
                return AppendChild(newChild);
            }

            int index = -1;

            if (HasChildNodes)
            {
                index = ChildNodes[oldChild];
            }

            if (index == -1)
            {
                throw new ArgumentException(HtmlDocument.HtmlExceptionRefNotChild);
            }

            if (HasChildNodes) ChildNodes.Replace(index, newChild);

            HtmlNode normalOldChild = oldChild as HtmlNode;
            if (normalOldChild != null)
            {
                OwnerDocument.SetIdForNode(null, normalOldChild.GetId());
                normalOldChild.RemoveAllID();
            }

            HtmlNode normalNewChild = newChild as HtmlNode;
            if (normalNewChild != null)
            {
                OwnerDocument.SetIdForNode(newChild, normalNewChild.GetId());
            }
            SetChildNodesId(newChild);

            IsChanged = true;
            return newChild;
        }

        /// <summary>Removes all id.</summary>
        public void RemoveAllID()
        {
            foreach (HtmlNodeBase child in ChildNodes)
            {
                HtmlNode normalChild = child as HtmlNode;
                if (normalChild != null)
                {
                    OwnerDocument.SetIdForNode(null, normalChild.GetId());
                    normalChild.RemoveAllID();
                }
            }
        }

        /// <summary>
        /// Removes all the children and/or attributes of the current node.
        /// </summary>
        public void RemoveAll()
        {
            RemoveAllChildren();

            if (HasAttributes)
            {
                Attributes.Clear();
            }

            if ((EndNode != null) && (EndNode != this))
            {
                if (EndNode.Attributes != null)
                {
                    EndNode.Attributes.Clear();
                }
            }

            IsChanged = true;
        }

        /// <summary>
        /// Gets all Descendant nodes in enumerated list
        /// </summary>
        /// <remarks>DO NOT REMOVE, the empty method is required for Fizzler third party library</remarks>
        public IEnumerable<HtmlNodeBase> Descendants()
        {
            return Descendants(0);
        }

        /// <summary>
        /// Gets all Descendant nodes in enumerated list
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Descendants(int level)
        {
            if (level > HtmlDocument.MaxDepthLevel)
            {
                throw new ArgumentException(HtmlNodeBase.DepthLevelExceptionMessage);
            }

            foreach (HtmlNodeBase child in ChildNodes)
            {
                yield return child;

                IHtmlNodeContainer childContainer = child as IHtmlNodeContainer;
                if (childContainer != null)
                {
                    foreach (HtmlNodeBase descendant in childContainer.Descendants(level + 1))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        /// <summary>
        /// Get all descendant nodes with matching name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Descendants(string name)
        {
            foreach (HtmlNodeBase node in Descendants())
                if (String.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return node;
        }

        /// <summary>
        /// Returns a collection of all descendant nodes of this element, in document order
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> DescendantsAndSelf()
        {
            yield return this;

            foreach (HtmlNodeBase n in Descendants())
            {
                HtmlNodeBase el = n;
                if (el != null)
                    yield return el;
            }
        }

        /// <summary>
        /// Gets all descendant nodes including this node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> DescendantsAndSelf(string name)
        {
            yield return this;

            foreach (HtmlNodeBase node in Descendants())
                if (node.Name == name)
                    yield return node;
        }

        /// <summary>
        /// Gets first generation child node matching name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HtmlNodeBase Element(string name)
        {
            foreach (HtmlNodeBase node in ChildNodes)
                if (node.Name == name)
                    return node;
            return null;
        }

        /// <summary>
        /// Gets matching first generation child nodes matching name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Elements(string name)
        {
            foreach (HtmlNodeBase node in ChildNodes)
                if (node.Name == name)
                    yield return node;
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="deep">true to recursively clone the subtree under the specified node; false to clone only the node itself.</param>
        /// <returns>The cloned node.</returns>
        public override HtmlNodeBase Clone(bool deep)
        {
            HtmlNode newNode = base.Clone(deep) as HtmlNode;
            if (newNode != null)
            {
                // attributes
                if (HasAttributes)
                {
                    foreach (HtmlAttribute att in Attributes)
                    {
                        HtmlAttribute newatt = att.Clone();
                        newNode.Attributes.Append(newatt);
                    }
                }

                // closing attributes
                if (HasClosingAttributes)
                {
                    newNode.EndNode = EndNode.Clone(false) as HtmlNode;
                    if (newNode.EndNode != null)
                    {
                        foreach (HtmlAttribute att in EndNode.Attributes)
                        {
                            HtmlAttribute newatt = att.Clone();
                            newNode.EndNode.Attributes.Append(newatt);
                        }
                    }
                }

                // child nodes
                if (deep && HasChildNodes)
                {
                    foreach (HtmlNodeBase child in ChildNodes)
                    {
                        HtmlNodeBase newchild = child.Clone(deep);
                        newNode.AppendChild(newchild);
                    }
                }
            }
            return newNode;
        }

        /// <summary>
        /// Creates a duplicate of the node.
        /// </summary>
        /// <param name="node">The node to duplicate. May not be <c>null</c>.</param>
        /// <param name="deep">true to recursively clone the subtree under the specified node, false to clone only the node itself.</param>
        public override void CopyFrom(HtmlNodeBase node, bool deep)
        {
            base.CopyFrom(node, deep);

            HtmlNode normalSrc = node as HtmlNode;
            if (normalSrc == null)
            {
                return;
            }

            Attributes.RemoveAll();
            if (normalSrc.HasAttributes)
            {
                foreach (HtmlAttribute att in normalSrc.Attributes)
                {
                    HtmlAttribute newatt = att.Clone();
                    Attributes.Append(newatt);
                }
            }

            if (deep)
            {
                RemoveAllChildren();
                if (normalSrc.HasChildNodes)
                {
                    foreach (HtmlNodeBase child in normalSrc.ChildNodes)
                    {
                        AppendChild(child.Clone(true));
                    }
                }
            }
        }

        internal void WriteAttribute(TextWriter outText, HtmlAttribute att)
        {
            if (att.Value == null)
            {
                // null value attribute are not written
                return;
            }

            string quote = att.QuoteType == AttributeValueQuote.DoubleQuote ? "\"" : "'";
            string name = OwnerDocument.OptionOutputUpperCase ? att.Name.ToUpperInvariant() : att.Name;
            if (OwnerDocument.OptionOutputOriginalCase)
            {
                name = att.OriginalName;
            }
            if (att.Name.Length >= 4)
            {
                if ((att.Name[0] == '<') && (att.Name[1] == '%') &&
                    (att.Name[att.Name.Length - 1] == '>') && (att.Name[att.Name.Length - 2] == '%'))
                {
                    outText.Write(" " + name);
                    return;
                }
            }

            var value = att.QuoteType == AttributeValueQuote.DoubleQuote ? !att.Value.StartsWith("@") ? att.Value.Replace("\"", "&quot;") : att.Value : att.Value.Replace("'", "&#39;");
            if (OwnerDocument.OptionOutputOptimizeAttributeValues)
                if (att.Value.IndexOfAny(new char[] { (char)10, (char)13, (char)9, ' ' }) < 0)
                {
                    outText.Write(" " + name + "=" + att.Value);
                }
                else
                {
                    outText.Write(" " + name + "=" + quote + value + quote);
                }
            else
            {
                outText.Write(" " + name + "=" + quote + value + quote);
            }
        }

        internal void WriteAttributes(TextWriter outText, bool closing)
        {
            if (!closing)
            {
                if (Attributes != null)
                    foreach (HtmlAttribute att in Attributes)
                        WriteAttribute(outText, att);

                if (!OwnerDocument.OptionAddDebuggingAttributes) return;

                WriteAttribute(outText, OwnerDocument.CreateAttribute("_closed", Closed.ToString()));
                WriteAttribute(outText, OwnerDocument.CreateAttribute("_children", ChildNodes.Count.ToString()));

                int i = 0;
                foreach (HtmlNodeBase n in ChildNodes)
                {
                    WriteAttribute(outText, OwnerDocument.CreateAttribute("_child_" + i,
                        n.Name));
                    i++;
                }
            }
            else
            {
                if (EndNode == null || !EndNode.HasAttributes || EndNode == this)
                    return;

                foreach (HtmlAttribute att in EndNode.Attributes)
                    WriteAttribute(outText, att);

                if (!OwnerDocument.OptionAddDebuggingAttributes) return;

                WriteAttribute(outText, OwnerDocument.CreateAttribute("_closed", Closed.ToString()));
                WriteAttribute(outText, OwnerDocument.CreateAttribute("_children", ChildNodes.Count.ToString()));
            }
        }

        /// <summary>
        /// Saves all the children of the node to the specified TextWriter.
        /// </summary>
        /// <param name="outText">The TextWriter to which you want to save.</param>
        /// <param name="level">Identifies the level we are in starting at root with 0</param>
        public override void WriteContentTo(TextWriter outText, int level = 0)
        {
            if (level > HtmlDocument.MaxDepthLevel)
            {
                throw new ArgumentException(DepthLevelExceptionMessage);
            }

            if (ChildNodes == null)
            {
                return;
            }

            foreach (HtmlNodeBase node in ChildNodes)
            {
                node.WriteTo(outText, level + 1);
            }
        }
    }
}