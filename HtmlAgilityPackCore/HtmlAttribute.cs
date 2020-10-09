// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzprojects/html-agility-pack
// License: https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright ©ZZZ Projects Inc. 2014 - 2017. All rights reserved.

using System;
using System.Diagnostics;
using HtmlAgilityPackCore.Nodes;

// ReSharper disable InconsistentNaming

namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents an HTML attribute.
    /// </summary>
    [DebuggerDisplay("Name: {OriginalName}, Value: {Value}")]
    public class HtmlAttribute : IComparable
    {
        internal int _namelength;
        internal int _namestartindex;
        internal string _value;

        internal HtmlAttribute(HtmlDocument ownerdocument)
        {
            OwnerDocument = ownerdocument;
        }

        /// <summary>
        /// Gets the line number of this attribute in the document.
        /// </summary>
        public int Line { get; internal set; }

        /// <summary>
        /// Gets the column number of this attribute in the document.
        /// </summary>
        public int LinePosition { get; internal set; }

        /// <summary>
        /// Gets the stream position of the value of this attribute in the document, relative to the start of the document.
        /// </summary>
        public int ValueStartIndex { get; internal set; }

        /// <summary>
        /// Gets the length of the value.
        /// </summary>
        public int ValueLength { get; internal set; }

        public bool UseOriginalName { get; set; } = false;

        /// <summary>
        /// Gets the qualified name of the attribute.
        /// </summary>
        public string Name
        {
            get
            {
                if (OriginalName == null)
                {
                    OriginalName = OwnerDocument.Text.Substring(_namestartindex, _namelength);
                }

                return UseOriginalName ? OriginalName : OriginalName.ToLowerInvariant();
            }
            set
            {
                OriginalName = value ?? throw new ArgumentNullException("value");

                if (OwnerNode != null)
                {
                    OwnerNode.IsChanged = true;
                }
            }
        }

        /// <summary>
        /// Name of attribute with original case
        /// </summary>
        public string OriginalName { get; internal set; }

        /// <summary>
        /// Gets the HTML document to which this attribute belongs.
        /// </summary>
        public HtmlDocument OwnerDocument { get; }

        /// <summary>
        /// Gets the HTML node to which this attribute belongs.
        /// </summary>
        public HtmlNode OwnerNode { get; internal set; }

        /// <summary>
        /// Specifies what type of quote the data should be wrapped in
        /// </summary>
        public AttributeValueQuote QuoteType { get; set; } = AttributeValueQuote.DoubleQuote;

        /// <summary>
        /// Gets the stream position of this attribute in the document, relative to the start of the document.
        /// </summary>
        public int StreamPosition { get; internal set; }

        /// <summary>
        /// Gets or sets the value of the attribute.
        /// </summary>
        public string Value // todo
        {
            get
            {
                // A null value has been provided, the attribute should be considered as "hidden"
                if (_value == null && OwnerDocument.Text == null && ValueStartIndex == 0 && ValueLength == 0)
                {
                    return null;
                }

                if (_value == null)
                {
                    _value = OwnerDocument.Text.Substring(ValueStartIndex, ValueLength);
                    _value = HtmlEntity.DeEntitize(_value);
                }

                return _value;
            }
            set
            {
                _value = value;

                if (OwnerNode != null)
                {
                    OwnerNode.IsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets the DeEntitized value of the attribute.
        /// </summary>
        public string DeEntitizeValue => HtmlEntity.DeEntitize(Value);

        /// <summary>
        /// Gets a valid XPath string that points to this Attribute
        /// </summary>
        public string XPath
        {
            get
            {
                string basePath = (OwnerNode == null) ? "/" : $"{OwnerNode.XPath}/";
                return basePath + GetRelativeXpath();
            }
        }

        /// <summary>
        /// Compares the current instance with another attribute. Comparison is based on attributes' name.
        /// </summary>
        /// <param name="obj">An attribute to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the names comparison.</returns>
        public int CompareTo(object obj)
        {
            var att = obj as HtmlAttribute;
            
            if (att == null)
            {
                throw new ArgumentException("obj");
            }

            return Name.CompareTo(att.Name);
        }

        /// <summary>
        /// Creates a duplicate of this attribute.
        /// </summary>
        /// <returns>The cloned attribute.</returns>
        public HtmlAttribute Clone()
        {
            HtmlAttribute att = new HtmlAttribute(OwnerDocument);
            att.Name = Name;
            att.Value = Value;
            att.QuoteType = QuoteType;
            return att;
        }

        /// <summary>
        /// Removes this attribute from it's parents collection
        /// </summary>
        public void Remove()
        {
            OwnerNode.Attributes.Remove(this);
        }

        private string GetRelativeXpath()
        {
            if (OwnerNode == null)
                return Name;

            int i = 1;
            foreach (HtmlAttribute node in OwnerNode.Attributes)
            {
                if (node.Name != Name) continue;

                if (node == this)
                    break;

                i++;
            }

            return "@" + Name + "[" + i + "]";
        }
    }

    /// <summary>
    /// An Enum representing different types of Quotes used for surrounding attribute values
    /// </summary>
    public enum AttributeValueQuote
    {
        /// <summary>
        /// A single quote mark '
        /// </summary>
        SingleQuote,

        /// <summary>
        /// A double quote mark "
        /// </summary>
        DoubleQuote
    }
}