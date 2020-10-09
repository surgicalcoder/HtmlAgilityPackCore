using System;
using System.Collections;
using System.Collections.Generic;

namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents a combined list and collection of HTML nodes.
    /// </summary>
    public class HtmlNodeCollection : IList<HtmlNodeBase>
    {
        private readonly IHtmlNodeContainer parentnode;
        private readonly List<HtmlNodeBase> _items = new List<HtmlNodeBase>();

        /// <summary>
        /// Initialize the HtmlNodeCollection with the base parent node
        /// </summary>
        /// <param name="parentnode">The base node of the collection</param>
        public HtmlNodeCollection(IHtmlNodeContainer parentnode)
        {
            this.parentnode = parentnode; // may be null
        }

        /// <summary>
        /// Gets a given node from the list.
        /// </summary>
        public int this[HtmlNodeBase node]
        {
            get
            {
                int index = GetNodeIndex(node);
                if (index == -1)
                    throw new ArgumentOutOfRangeException("node",
                        "Node \"" + node.Clone(false).OuterHtml +
                        "\" was not found in the collection");
                return index;
            }
        }

        /// <summary>
        /// Get node with tag name
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public HtmlNodeBase this[string nodeName]
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                    if (string.Equals(_items[i].Name, nodeName, StringComparison.OrdinalIgnoreCase))
                        return _items[i];

                return null;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the list.
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// Is collection read only
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the node at the specified index.
        /// </summary>
        public HtmlNodeBase this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        /// <summary>
        /// Add node to the collection
        /// </summary>
        /// <param name="node"></param>
        public void Add(HtmlNodeBase node)
        {
            Add(node, true);
        }

        /// <summary>
        /// Add node to the collection
        /// </summary>
        /// <param name="node"></param>
        /// <param name="setParent"></param>
        public void Add(HtmlNodeBase node, bool setParent)
        {
            _items.Add(node);

            if (setParent)
            {
                node.ParentNode = parentnode;
            }
        }

        /// <summary>
        /// Clears out the collection of HtmlNodes. Removes each nodes reference to parentnode, nextnode and prevnode
        /// </summary>
        public void Clear()
        {
            foreach (HtmlNodeBase node in _items)
            {
                node.ParentNode = null;
                node.NextSibling = null;
                node.PreviousSibling = null;
            }

            _items.Clear();
        }

        /// <summary>
        /// Gets existence of node in collection
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(HtmlNodeBase item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        /// Copy collection to array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(HtmlNodeBase[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Get Enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator<HtmlNodeBase> IEnumerable<HtmlNodeBase>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Get Explicit Enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Get index of node
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(HtmlNodeBase item)
        {
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Insert node at index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="node"></param>
        public void Insert(int index, HtmlNodeBase node)
        {
            HtmlNodeBase next = null;
            HtmlNodeBase prev = null;

            if (index > 0)
                prev = _items[index - 1];

            if (index < _items.Count)
                next = _items[index];

            _items.Insert(index, node);

            if (prev != null)
            {
                if (node == prev)
                    throw new InvalidProgramException("Unexpected error.");

                prev.NextSibling = node;
            }

            if (next != null)
                next.PreviousSibling = node;

            node.PreviousSibling = prev;
            if (next == node)
                throw new InvalidProgramException("Unexpected error.");

            node.NextSibling = next;
            node.SetParent(parentnode);
        }

        /// <summary>
        /// Remove node
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(HtmlNodeBase item)
        {
            int i = _items.IndexOf(item);
            RemoveAt(i);
            return true;
        }

        /// <summary>
        /// Remove <see cref="HtmlNodeBase"/> at index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            HtmlNodeBase next = null;
            HtmlNodeBase prev = null;
            HtmlNodeBase oldnode = _items[index];

            // KEEP a reference since it will be set to null
            var parentNode = parentnode ?? oldnode.ParentNode;

            if (index > 0)
                prev = _items[index - 1];

            if (index < (_items.Count - 1))
                next = _items[index + 1];

            _items.RemoveAt(index);

            if (prev != null)
            {
                if (next == prev)
                    throw new InvalidProgramException("Unexpected error.");
                prev.NextSibling = next;
            }

            if (next != null)
                next.PreviousSibling = prev;

            oldnode.PreviousSibling = null;
            oldnode.NextSibling = null;
            oldnode.ParentNode = null;

            if (parentNode != null)
            {
                parentNode.IsChanged = true;
            }
        }

        /// <summary>
        /// Get first instance of node in supplied collection
        /// </summary>
        /// <param name="items"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static HtmlNodeBase FindFirst(HtmlNodeCollection items, string name)
        {
            foreach (HtmlNodeBase node in items)
            {
                if (node.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return node;

                HtmlNode normalNode = node as HtmlNode;
                if (normalNode == null || !normalNode.HasChildNodes)
                {
                    continue;
                }

                HtmlNodeBase returnNode = FindFirst(normalNode.ChildNodes, name);
                if (returnNode != null)
                    return returnNode;
            }

            return null;
        }

        /// <summary>
        /// Add node to the end of the collection
        /// </summary>
        /// <param name="node"></param>
        public void Append(HtmlNodeBase node)
        {
            HtmlNodeBase last = null;
            if (_items.Count > 0)
                last = _items[_items.Count - 1];

            _items.Add(node);
            node.PreviousSibling = last;
            node.NextSibling = null;
            node.SetParent(parentnode);
            if (last == null) return;
            if (last == node)
                throw new InvalidProgramException("Unexpected error.");

            last.NextSibling = node;
        }

        /// <summary>
        /// Get first instance of node with name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HtmlNodeBase FindFirst(string name)
        {
            return FindFirst(this, name);
        }

        /// <summary>
        /// Get index of node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int GetNodeIndex(HtmlNodeBase node)
        {
            // TODO: should we rewrite this? what would be the key of a node?
            for (int i = 0; i < _items.Count; i++)
                if (node == _items[i])
                    return i;
            return -1;
        }

        /// <summary>
        /// Add node to the beginning of the collection
        /// </summary>
        /// <param name="node"></param>
        public void Prepend(HtmlNodeBase node)
        {
            HtmlNodeBase first = null;
            if (_items.Count > 0)
                first = _items[0];

            _items.Insert(0, node);

            if (node == first)
                throw new InvalidProgramException("Unexpected error.");
            node.NextSibling = first;
            node.PreviousSibling = null;
            node.SetParent(parentnode);

            if (first != null)
                first.PreviousSibling = node;
        }

        /// <summary>
        /// Remove node at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Remove(int index)
        {
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Replace node at index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="node"></param>
        public void Replace(int index, HtmlNodeBase node)
        {
            HtmlNodeBase next = null;
            HtmlNodeBase prev = null;
            HtmlNodeBase oldnode = _items[index];

            if (index > 0)
                prev = _items[index - 1];

            if (index < (_items.Count - 1))
                next = _items[index + 1];

            _items[index] = node;

            if (prev != null)
            {
                if (node == prev)
                    throw new InvalidProgramException("Unexpected error.");
                prev.NextSibling = node;
            }

            if (next != null)
                next.PreviousSibling = node;

            node.PreviousSibling = prev;

            if (next == node)
                throw new InvalidProgramException("Unexpected error.");

            node.NextSibling = next;
            node.SetParent(parentnode);

            oldnode.PreviousSibling = null;
            oldnode.NextSibling = null;
            oldnode.ParentNode = null;
        }

        /// <summary>
        /// Get all node descended from this collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Descendants()
        {
            foreach (HtmlNodeBase item in _items)
            {
                HtmlNode normalNode = item as HtmlNode;
                if (normalNode != null)
                {
                    foreach (HtmlNodeBase n in normalNode.Descendants())
                    {
                        yield return n;
                    }
                }
            }
        }

        /// <summary>
        /// Get all node descended from this collection with matching name
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Descendants(string name)
        {
            foreach (HtmlNodeBase item in _items)
            {
                HtmlNode normalNode = item as HtmlNode;
                if (normalNode != null)
                {
                    foreach (HtmlNodeBase n in normalNode.Descendants(name))
                    {
                        yield return n;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all first generation elements in collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Elements()
        {
            foreach (HtmlNodeBase item in _items)
            {
                HtmlNode normalNode = item as HtmlNode;
                if (normalNode != null)
                {
                    foreach (HtmlNodeBase n in normalNode.ChildNodes)
                    {
                        yield return n;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all first generation elements matching name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Elements(string name)
        {
            foreach (HtmlNodeBase item in _items)
            {
                HtmlNode normalNode = item as HtmlNode;
                if (normalNode != null)
                {
                    foreach (HtmlNodeBase n in normalNode.Elements(name))
                    {
                        yield return n;
                    }
                }
            }
        }

        /// <summary>
        /// All first generation nodes in collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HtmlNodeBase> Nodes()
        {
            foreach (HtmlNodeBase item in _items)
            {
                HtmlNode normalNode = item as HtmlNode;
                if (normalNode != null)
                {
                    foreach (HtmlNodeBase n in normalNode.ChildNodes)
                    {
                        yield return n;
                    }
                }
            }
        }
    }
}