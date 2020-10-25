using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPackCore.Nodes;

namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents a complete HTML document.
    /// </summary>
    public partial class HtmlDocument
    {
        internal static bool _disableBehaviorTagP = true;

        /// <summary>True to disable, false to enable the behavior tag p.</summary>
        public static bool DisableBehaviorTagP
        {
            get => _disableBehaviorTagP;
            set
            {
                if (value)
                {
                    if (HtmlNodeBase.ElementsFlags.ContainsKey("p"))
                    {
                        HtmlNodeBase.ElementsFlags.Remove("p");
                    }
                }
                else
                {
                    if (!HtmlNodeBase.ElementsFlags.ContainsKey("p"))
                    {
                        HtmlNodeBase.ElementsFlags.Add("p", HtmlElementFlag.Empty | HtmlElementFlag.Closed);
                    }
                }

                _disableBehaviorTagP = value;
            }
        }

        /// <summary>Default builder to use in the HtmlDocument constructor</summary>
        public static Action<HtmlDocument> DefaultBuilder { get; set; }

        /// <summary>Action to execute before the Parse is executed</summary>
        public Action<HtmlDocument> ParseExecuting { get; set; }

        /// <summary>
        /// Defines the max level we would go deep into the html document
        /// </summary>
        private static int _maxDepthLevel = int.MaxValue;

        private int _c;
        private Crc32 _crc32;
        private HtmlAttribute _currentattribute;
        private HtmlNodeBase _currentnode;
        private Encoding _declaredencoding;
        private HtmlDocumentNode _documentnode;
        private bool _fullcomment;
        private int _index;
        internal Dictionary<ReadOnlyMemory<char>, HtmlNodeBase> Lastnodes = new Dictionary<ReadOnlyMemory<char>, HtmlNodeBase>();
        private HtmlNode _lastparentnode;
        private int _line;
        private int LinePosition, _maxlineposition;
        internal Dictionary<ReadOnlyMemory<char>, HtmlNodeBase> Nodesid;
        private ParseState _oldstate;
        private bool _onlyDetectEncoding;
        internal Dictionary<int, HtmlNodeBase> Openednodes;
        private List<HtmlParseError> _parseerrors = new List<HtmlParseError>();
        private ReadOnlyMemory<char> _remainder;
        private int _remainderOffset;
        private ParseState _state;
        private Encoding _streamencoding;
        private bool _useHtmlEncodingForStream;

        /// <summary>The HtmlDocument Text. Careful if you modify it.</summary>
        public ReadOnlyMemory<char> Text;

        /// <summary>
        /// Adds Debugging attributes to node. Default is false.
        /// </summary>
        public bool OptionAddDebuggingAttributes;

        /// <summary>
        /// Defines if closing for non closed nodes must be done at the end or directly in the document.
        /// Setting this to true can actually change how browsers render the page. Default is false.
        /// </summary>
        public bool OptionAutoCloseOnEnd; // close errors at the end

        /// <summary>
        /// Defines if non closed nodes will be checked at the end of parsing. Default is true.
        /// </summary>
        public bool OptionCheckSyntax = true;

        /// <summary>
        /// Defines if a checksum must be computed for the document while parsing. Default is false.
        /// </summary>
        public bool OptionComputeChecksum;

        /// <summary>
        /// Defines if SelectNodes method will return null or empty collection when no node matched the XPath expression.
        /// Setting this to true will return empty collection and false will return null. Default is false.
        /// </summary>
        public bool OptionEmptyCollection = false;

        /// <summary>True to disable, false to enable the server side code.</summary>
        public bool DisableServerSideCode = false;


        /// <summary>
        /// Defines the default stream encoding to use. Default is System.Text.Encoding.Default.
        /// </summary>
        public Encoding OptionDefaultStreamEncoding;

        /// <summary>
        /// Defines if source text must be extracted while parsing errors.
        /// If the document has a lot of errors, or cascading errors, parsing performance can be dramatically affected if set to true.
        /// Default is false.
        /// </summary>
        public bool OptionExtractErrorSourceText;

        // turning this on can dramatically slow performance if a lot of errors are detected

        /// <summary>
        /// Defines the maximum length of source text or parse errors. Default is 100.
        /// </summary>
        public int OptionExtractErrorSourceTextMaxLength = 100;

        /// <summary>
        /// Defines if LI, TR, TH, TD tags must be partially fixed when nesting errors are detected. Default is false.
        /// </summary>
        public bool OptionFixNestedTags; // fix li, tr, th, td tags

        /// <summary>
        /// Defines if attribute value output must be optimized (not bound with double quotes if it is possible). Default is false.
        /// </summary>
        public bool OptionOutputOptimizeAttributeValues;

        /// <summary>
        /// Defines if name must be output with it's original case. Useful for asp.net tags and attributes. Default is false.
        /// </summary>
        public bool OptionOutputOriginalCase;

        /// <summary>
        /// Defines if name must be output in uppercase. Default is false.
        /// </summary>
        public bool OptionOutputUpperCase;

        /// <summary>
        /// Defines if declared encoding must be read from the document.
        /// Declared encoding is determined using the meta http-equiv="content-type" content="text/html;charset=XXXXX" html node.
        /// Default is true.
        /// </summary>
        public bool OptionReadEncoding = true;

        /// <summary>
        /// Defines the name of a node that will throw the StopperNodeException when found as an end node. Default is null.
        /// </summary>
        public string OptionStopperNodeName;

        /// <summary>
        /// Defines if the 'id' attribute must be specifically used. Default is true.
        /// </summary>
        public bool OptionUseIdAttribute = true;

        /// <summary>
        /// Defines if empty nodes must be written as closed during output. Default is false.
        /// </summary>
        public bool OptionWriteEmptyNodes;

        /// <summary>
        /// The max number of nested child nodes. 
        /// Added to prevent stackoverflow problem when a page has tens of thousands of opening html tags with no closing tags 
        /// </summary>
        public int OptionMaxNestedChildNodes = 0;


        internal static readonly string HtmlExceptionRefNotChild = "Reference node must be a child of this node";

        internal static readonly string HtmlExceptionUseIdAttributeFalse = "You need to set UseIdAttribute property to true to enable this feature";

        internal static readonly string HtmlExceptionClassDoesNotExist = "Class name doesn't exist";

        internal static readonly string HtmlExceptionClassExists = "Class name already exists";

        internal static readonly Dictionary<string, string[]> HtmlResetters = new Dictionary<string, string[]>()
        {
            {"li", new[] {"ul", "ol"}},
            {"tr", new[] {"table"}},
            {"th", new[] {"tr", "table"}},
            {"td", new[] {"tr", "table"}},
        };

        /// <summary>
        /// Creates an instance of an HTML document.
        /// </summary>
        public HtmlDocument()
        {
            if (DefaultBuilder != null)
            {
                DefaultBuilder(this);
            }

            _documentnode = new HtmlDocumentNode(this, 0);
#if SILVERLIGHT || METRO || NETSTANDARD1_3 || NETSTANDARD1_6
            OptionDefaultStreamEncoding = Encoding.UTF8;
#else
            OptionDefaultStreamEncoding = Encoding.Default;
#endif
        }

        /// <summary>Gets the parsed text.</summary>
        /// <value>The parsed text.</value>
        public ReadOnlyMemory<char> ParsedText
        {
            get { return Text; }
        }

        /// <summary>
        /// Defines the max level we would go deep into the html document. If this depth level is exceeded, and exception is
        /// thrown.
        /// </summary>
        public static int MaxDepthLevel
        {
            get { return _maxDepthLevel; }
            set { _maxDepthLevel = value; }
        }

        /// <summary>
        /// Gets the document CRC32 checksum if OptionComputeChecksum was set to true before parsing, 0 otherwise.
        /// </summary>
        public int CheckSum
        {
            get { return _crc32 == null ? 0 : (int)_crc32.CheckSum; }
        }

        /// <summary>
        /// Gets the document's declared encoding.
        /// Declared encoding is determined using the meta http-equiv="content-type" content="text/html;charset=XXXXX" html node (pre-HTML5) or the meta charset="XXXXX" html node (HTML5).
        /// </summary>
        public Encoding DeclaredEncoding
        {
            get { return _declaredencoding; }
        }

        /// <summary>
        /// Gets the root node of the document.
        /// </summary>
        public HtmlDocumentNode DocumentNode
        {
            get { return _documentnode; }
        }

        /// <summary>
        /// Gets the document's output encoding.
        /// </summary>
        public Encoding Encoding
        {
            get { return GetOutEncoding(); }
        }

        /// <summary>
        /// Gets a list of parse errors found in the document.
        /// </summary>
        public IEnumerable<HtmlParseError> ParseErrors
        {
            get { return _parseerrors; }
        }

        /// <summary>
        /// Gets the remaining text.
        /// Will always be null if OptionStopperNodeName is null.
        /// </summary>
        public ReadOnlyMemory<char> Remainder
        {
            get { return _remainder; }
        }

        /// <summary>
        /// Gets the offset of Remainder in the original Html text.
        /// If OptionStopperNodeName is null, this will return the length of the original Html text.
        /// </summary>
        public int RemainderOffset
        {
            get { return _remainderOffset; }
        }

        /// <summary>
        /// Gets the document's stream encoding.
        /// </summary>
        public Encoding StreamEncoding
        {
            get { return _streamencoding; }
        }

#if !METRO
        public void UseAttributeOriginalName(string tagName)
        {
            foreach (HtmlNodeBase node in DocumentNode.SelectNodes("//" + tagName))
            {
                HtmlNode normalNode = node as HtmlNode;
                if (normalNode == null)
                {
                    continue;
                }

                foreach (HtmlAttribute attr in normalNode.Attributes)
                {
                    attr.UseOriginalName = true;
                }
            }
        }
#endif

        /// <summary>
        /// Applies HTML encoding to a specified string.
        /// </summary>
        /// <param name="html">The input string to encode. May not be null.</param>
        /// <returns>The encoded string.</returns>
        public static string HtmlEncode(string html)
        {
            return HtmlEncodeWithCompatibility(html, true);
        }

        internal static string HtmlEncodeWithCompatibility(string html, bool backwardCompatibility = true)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            // replace & by &amp; but only once!

            Regex rx = backwardCompatibility ? new Regex("&(?!(amp;)|(lt;)|(gt;)|(quot;))", RegexOptions.IgnoreCase) : new Regex("&(?!(amp;)|(lt;)|(gt;)|(quot;)|(nbsp;)|(reg;))", RegexOptions.IgnoreCase);
            return rx.Replace(html, "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        /// <summary>
        /// Determines if the specified character is considered as a whitespace character.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>true if if the specified character is considered as a whitespace character.</returns>
        public static bool IsWhiteSpace(int c)
        {
            if ((c == 10) || (c == 13) || (c == 32) || (c == 9))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an HTML attribute with the specified name.
        /// </summary>
        /// <param name="name">The name of the attribute. May not be null.</param>
        /// <returns>The new HTML attribute.</returns>
        public HtmlAttribute CreateAttribute(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            HtmlAttribute att = CreateAttribute();
            att.Name = name;
            return att;
        }

        /// <summary>
        /// Creates an HTML attribute with the specified name.
        /// </summary>
        /// <param name="name">The name of the attribute. May not be null.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>The new HTML attribute.</returns>
        public HtmlAttribute CreateAttribute(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlAttribute att = CreateAttribute(name);
            att.Value = value;
            return att;
        }

        /// <summary>
        /// Creates an HTML comment node.
        /// </summary>
        /// <returns>The new HTML comment node.</returns>
        public HtmlComment CreateComment()
        {
            return (HtmlComment)HtmlNodeFactory.Create(this, HtmlNodeType.Comment);
        }

        /// <summary>
        /// Creates an HTML comment node with the specified comment text.
        /// </summary>
        /// <param name="comment">The comment text. May not be null.</param>
        /// <returns>The new HTML comment node.</returns>
        public HtmlComment CreateComment(ReadOnlyMemory<char> comment)
        {
            if (comment.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(comment));
            }

            var c = CreateComment();
            c.Comment = comment;
            return c;
        }

        /// <summary>
        /// Creates an HTML element node with the specified name.
        /// </summary>
        /// <param name="name">The qualified name of the element. May not be null.</param>
        /// <returns>The new HTML node.</returns>
        public HtmlElement CreateElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            HtmlElement node = new HtmlElement(this);
            node.Name = name;
            return node;
        }

        /// <summary>
        /// Creates an HTML text node.
        /// </summary>
        /// <returns>The new HTML text node.</returns>
        public HtmlText CreateTextNode()
        {
            return (HtmlText)HtmlNodeFactory.Create(this, HtmlNodeType.Text);
        }

        /// <summary>
        /// Creates an HTML text node with the specified text.
        /// </summary>
        /// <param name="text">The text of the node. May not be null.</param>
        /// <returns>The new HTML text node.</returns>
        public HtmlText CreateTextNode(ReadOnlyMemory<char> text)
        {
            if (text.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("text");
            }

            HtmlText t = CreateTextNode();
            t.Text = text;
            return t;
        }

        /// <summary>
        /// Detects the encoding of an HTML stream.
        /// </summary>
        /// <param name="stream">The input stream. May not be null.</param>
        /// <returns>The detected encoding.</returns>
        public Encoding DetectEncoding(Stream stream)
        {
            return DetectEncoding(stream, false);
        }

        /// <summary>
        /// Detects the encoding of an HTML stream.
        /// </summary>
        /// <param name="stream">The input stream. May not be null.</param>
        /// <param name="checkHtml">The html is checked.</param>
        /// <returns>The detected encoding.</returns>
        public Encoding DetectEncoding(Stream stream, bool checkHtml)
        {
            _useHtmlEncodingForStream = checkHtml;

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            return DetectEncoding(new StreamReader(stream));
        }


        /// <summary>
        /// Detects the encoding of an HTML text provided on a TextReader.
        /// </summary>
        /// <param name="reader">The TextReader used to feed the HTML. May not be null.</param>
        /// <returns>The detected encoding.</returns>
        public Encoding DetectEncoding(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            _onlyDetectEncoding = true;
            if (OptionCheckSyntax)
            {
                Openednodes = new Dictionary<int, HtmlNodeBase>();
            }
            else
            {
                Openednodes = null;
            }

            if (OptionUseIdAttribute)
            {
                Nodesid = new Dictionary<ReadOnlyMemory<char>, HtmlNodeBase>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Nodesid = null;
            }

            if (reader is StreamReader sr && !_useHtmlEncodingForStream)
            {
                Text = sr.ReadToEnd();
                _streamencoding = sr.CurrentEncoding;
                return _streamencoding;
            }

            _streamencoding = null;
            _declaredencoding = null;

            Text = reader.ReadToEnd();
            _documentnode = new HtmlDocumentNode(this, 0);

            // this is almost a hack, but it allows us not to muck with the original parsing code
            try
            {
                Parse();
            }
            catch (EncodingFoundException ex)
            {
                return ex.Encoding;
            }

            return _streamencoding;
        }


        /// <summary>
        /// Detects the encoding of an HTML text.
        /// </summary>
        /// <param name="html">The input html text. May not be null.</param>
        /// <returns>The detected encoding.</returns>
        public Encoding DetectEncodingHtml(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            using (StringReader sr = new StringReader(html))
            {
                Encoding encoding = DetectEncoding(sr);
                return encoding;
            }
        }

        /// <summary>
        /// Gets the HTML node with the specified 'id' attribute value.
        /// </summary>
        /// <param name="id">The attribute id to match. May not be null.</param>
        /// <returns>The HTML node with the matching id or null if not found.</returns>
        public HtmlNodeBase GetElementbyId(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (Nodesid == null)
            {
                throw new Exception(HtmlExceptionUseIdAttributeFalse);
            }

            return Nodesid.ContainsKey(id) ? Nodesid[id] : null;
        }

        /// <summary>
        /// Loads an HTML document from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        public void Load(Stream stream)
        {
            Load(new StreamReader(stream, OptionDefaultStreamEncoding));
        }

        /// <summary>
        /// Loads an HTML document from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="detectEncodingFromByteOrderMarks">Indicates whether to look for byte order marks at the beginning of the stream.</param>
        public void Load(Stream stream, bool detectEncodingFromByteOrderMarks)
        {
            Load(new StreamReader(stream, detectEncodingFromByteOrderMarks));
        }

        /// <summary>
        /// Loads an HTML document from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        public void Load(Stream stream, Encoding encoding)
        {
            Load(new StreamReader(stream, encoding));
        }

        /// <summary>
        /// Loads an HTML document from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="detectEncodingFromByteOrderMarks">Indicates whether to look for byte order marks at the beginning of the stream.</param>
        public void Load(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        {
            Load(new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks));
        }

        /// <summary>
        /// Loads an HTML document from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="detectEncodingFromByteOrderMarks">Indicates whether to look for byte order marks at the beginning of the stream.</param>
        /// <param name="buffersize">The minimum buffer size.</param>
        public void Load(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int buffersize)
        {
            Load(new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks, buffersize));
        }


        /// <summary>
        /// Loads the HTML document from the specified TextReader.
        /// </summary>
        /// <param name="reader">The TextReader used to feed the HTML data into the document. May not be null.</param>
        public void Load(TextReader reader)
        {
            // all Load methods pass down to this one
            if (reader == null)
                throw new ArgumentNullException("reader");

            _onlyDetectEncoding = false;

            if (OptionCheckSyntax)
                Openednodes = new Dictionary<int, HtmlNodeBase>();
            else
                Openednodes = null;

            if (OptionUseIdAttribute)
            {
                Nodesid = new Dictionary<ReadOnlyMemory<char>, HtmlNodeBase>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Nodesid = null;
            }

            StreamReader sr = reader as StreamReader;
            if (sr != null)
            {
                try
                {
                    // trigger bom read if needed
                    sr.Peek();
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
                // ReSharper restore EmptyGeneralCatchClause
                {
                    // void on purpose
                }

                _streamencoding = sr.CurrentEncoding;
            }
            else
            {
                _streamencoding = null;
            }

            _declaredencoding = null;

            Text = reader.ReadToEnd();
            _documentnode = new HtmlDocumentNode(this, 0);
            Parse();

            if (!OptionCheckSyntax || Openednodes == null) return;
            foreach (HtmlNodeBase node in Openednodes.Values)
            {
                if (!node.StartTag) // already reported
                {
                    continue;
                }

                string html;
                if (OptionExtractErrorSourceText)
                {
                    html = node.OuterHtml;
                    if (html.Length > OptionExtractErrorSourceTextMaxLength)
                    {
                        html = html.Substring(0, OptionExtractErrorSourceTextMaxLength);
                    }
                }
                else
                {
                    html = string.Empty;
                }

                AddError(
                    HtmlParseErrorCode.TagNotClosed,
                    node.Line, node.LinePosition,
                    node.StreamPosition, html,
                    "End tag </" + node.Name + "> was not found");
            }

            // we don't need this anymore
            Openednodes.Clear();
        }

        /// <summary>
        /// Loads the HTML document from the specified string.
        /// </summary>
        /// <param name="html">String containing the HTML document to load. May not be null.</param>
        public void LoadHtml(string html) // todo
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            using (StringReader sr = new StringReader(html))
            {
                Load(sr);
            }
        }

        /// <summary>
        /// Saves the HTML document to the specified stream.
        /// </summary>
        /// <param name="outStream">The stream to which you want to save.</param>
        public void Save(Stream outStream)
        {
            StreamWriter sw = new StreamWriter(outStream, GetOutEncoding());
            Save(sw);
        }

        /// <summary>
        /// Saves the HTML document to the specified stream.
        /// </summary>
        /// <param name="outStream">The stream to which you want to save. May not be null.</param>
        /// <param name="encoding">The character encoding to use. May not be null.</param>
        public void Save(Stream outStream, Encoding encoding)
        {
            if (outStream == null)
            {
                throw new ArgumentNullException("outStream");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            StreamWriter sw = new StreamWriter(outStream, encoding);
            Save(sw);
        }


        /// <summary>
        /// Saves the HTML document to the specified StreamWriter.
        /// </summary>
        /// <param name="writer">The StreamWriter to which you want to save.</param>
        public void Save(StreamWriter writer)
        {
            Save((TextWriter)writer);
        }

        /// <summary>
        /// Saves the HTML document to the specified TextWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to which you want to save. May not be null.</param>
        public void Save(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            DocumentNode.WriteTo(writer);
            writer.Flush();
        }

        internal HtmlAttribute CreateAttribute()
        {
            return new HtmlAttribute(this);
        }

        internal Encoding GetOutEncoding()
        {
            // when unspecified, use the stream encoding first
            return _declaredencoding ?? (_streamencoding ?? OptionDefaultStreamEncoding);
        }

        internal HtmlNodeBase GetXmlDeclaration()
        {
            if (!_documentnode.HasChildNodes)
                return null;

            foreach (HtmlNodeBase node in _documentnode.ChildNodes)
                if (node.Name == "?xml") // it's ok, names are case sensitive
                    return node;

            return null;
        }

        internal void SetIdForNode(HtmlNodeBase node, ReadOnlyMemory<char> id)
        {
            if (!OptionUseIdAttribute)
                return;

            if ((Nodesid == null) || (id.IsEmpty))
                return;

            if (node == null)
                Nodesid.Remove(id);
            else
                Nodesid[id] = node;
        }

        internal void UpdateLastParentNode()
        {
            do
            {
                if (_lastparentnode.Closed)
                    _lastparentnode = _lastparentnode.ParentNode as HtmlNode;
            } while ((_lastparentnode != null) && (_lastparentnode.Closed));

            if (_lastparentnode == null)
                _lastparentnode = _documentnode;
        }

        private void AddError(HtmlParseErrorCode code, int line, int linePosition, int streamPosition, ReadOnlyMemory<char> sourceText, string reason)
        {
            var err = new HtmlParseError(code, line, linePosition, streamPosition, sourceText, reason);
            _parseerrors.Add(err);
        }

        private void CloseCurrentNode()
        {
            var currentNode = _currentnode as HtmlNode;
            
            if (currentNode == null)
            {
                return;
            }

            if (currentNode.Closed) // text or document are by def closed
                return;

            bool error = false;
            
            var prev = Utilities.GetDictionaryValueOrDefault(Lastnodes, currentNode.Name);

            // find last node of this kind
            if (prev == null)
            {
                if (HtmlNodeBase.IsClosedElement(currentNode.Name))
                {
                    // </br> will be seen as <br>
                    currentNode.CloseNode(currentNode);

                    // add to parent node
                    if (_lastparentnode != null)
                    {
                        HtmlNode foundNode = null;
                        Stack<HtmlNodeBase> futureChild = new Stack<HtmlNodeBase>();
                        for (HtmlNodeBase node = _lastparentnode.LastChild; node != null; node = node.PreviousSibling)
                        {
                            HtmlNode normalNode = node as HtmlNode;
                            if (node.Name.Equals(currentNode.Name) && (normalNode == null || !normalNode.HasChildNodes))
                            {
                                foundNode = node as HtmlNode;
                                break;
                            }

                            futureChild.Push(node);
                        }

                        if (foundNode != null)
                        {
                            while (futureChild.Count != 0)
                            {
                                HtmlNodeBase node = futureChild.Pop();
                                _lastparentnode.RemoveChild(node);
                                foundNode.AppendChild(node);
                            }
                        }
                        else
                        {
                            _lastparentnode.AppendChild(currentNode);
                        }
                    }
                }
                else
                {
                    // node has no parent
                    // node is not a closed node

                    if (HtmlNodeBase.CanOverlapElement(currentNode.Name))
                    {
                        // this is a hack: add it as a text node
                        HtmlNodeBase closenode = HtmlNodeFactory.Create(this, HtmlNodeType.Text, currentNode.OuterStartIndex);
                        closenode.OuterLength = currentNode.OuterLength;
                        ((HtmlText)closenode).Text = ((HtmlText)closenode).Text.ToLowerInvariant();
                        if (_lastparentnode != null)
                        {
                            _lastparentnode.AppendChild(closenode);
                        }
                    }
                    else
                    {
                        if (HtmlNodeBase.IsEmptyElement(currentNode.Name))
                        {
                            AddError(
                                HtmlParseErrorCode.EndTagNotRequired,
                                currentNode.Line, currentNode.LinePosition,
                                currentNode.StreamPosition, currentNode.OuterHtml,
                                "End tag </" + currentNode.Name + "> is not required");
                        }
                        else
                        {
                            // node cannot overlap, node is not empty
                            AddError(
                                HtmlParseErrorCode.TagNotOpened,
                                currentNode.Line, currentNode.LinePosition,
                                currentNode.StreamPosition, currentNode.OuterHtml,
                                "Start tag <" + currentNode.Name + "> was not found");
                            error = true;
                        }
                    }
                }
            }
            else
            {
                if (OptionFixNestedTags)
                {
                    if (FindResetterNodes(prev, GetResetters(currentNode.Name)))
                    {
                        AddError(
                            HtmlParseErrorCode.EndTagInvalidHere,
                            currentNode.Line, currentNode.LinePosition,
                            currentNode.StreamPosition, currentNode.OuterHtml,
                            "End tag </" + currentNode.Name + "> invalid here");
                        error = true;
                    }
                }

                if (!error)
                {
                    Lastnodes[currentNode.Name] = prev.PrevWithSameName;

                    HtmlNode normalPrevNode = prev as HtmlNode;
                    if (normalPrevNode != null)
                    {
                        normalPrevNode.CloseNode(currentNode);
                    }
                }
            }


            // we close this node, get grandparent
            if (!error)
            {
                if ((_lastparentnode != null) &&
                    ((!HtmlNodeBase.IsClosedElement(currentNode.Name)) ||
                     (currentNode.StartTag)))
                {
                    UpdateLastParentNode();
                }
            }
        }

        private string CurrentNodeName()
        {
            return Text.Substring(_currentnode.NameStartIndex, _currentnode.Namelength);
        }


        private void DecrementPosition()
        {
            _index--;
            if (LinePosition == 0)
            {
                LinePosition = _maxlineposition;
                _line--;
            }
            else
            {
                LinePosition--;
            }
        }

        private HtmlNodeBase FindResetterNode(HtmlNodeBase node, string name)
        {
            HtmlNodeBase resetter = Utilities.GetDictionaryValueOrDefault(Lastnodes, name);
            if (resetter == null)
                return null;

            HtmlNode normalResetter = resetter as HtmlNode;
            if (normalResetter != null && normalResetter.Closed)
                return null;

            if (resetter.StreamPosition < node.StreamPosition)
            {
                return null;
            }

            return resetter;
        }

        private bool FindResetterNodes(HtmlNodeBase node, string[] names)
        {
            if (names == null)
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (FindResetterNode(node, names[i]) != null)
                    return true;
            }

            return false;
        }

        private void FixNestedTag(string name, string[] resetters)
        {
            if (resetters == null)
                return;

            HtmlNode prev = Utilities.GetDictionaryValueOrDefault(Lastnodes, _currentnode.Name) as HtmlNode;
            // if we find a previous unclosed same name node, without a resetter node between, we must close it
            HtmlNode last = Lastnodes[name] as HtmlNode;
            if (prev == null || last == null || last.Closed)
            {
                return;
            }
            // try to find a resetter node, if found, we do nothing
            if (FindResetterNodes(prev, resetters))
            {
                return;
            }

            // ok we need to close the prev now
            // create a fake closer node
            HtmlNode close = HtmlNodeFactory.Create(this, prev.NodeType, -1) as HtmlNode;
            close.EndNode = close;
            prev.CloseNode(close);
        }

        private void FixNestedTags()
        {
            // we are only interested by start tags, not closing tags
            if (!_currentnode.StartTag)
                return;

            string name = CurrentNodeName();
            FixNestedTag(name, GetResetters(name));
        }

        private string[] GetResetters(string name)
        {
            string[] resetters;

            if (!HtmlResetters.TryGetValue(name, out resetters))
            {
                return null;
            }

            return resetters;
        }

        private void IncrementPosition()
        {
            if (_crc32 != null)
            {
                // REVIEW: should we add some checksum code in DecrementPosition too?
                _crc32.AddToCRC32(_c);
            }

            _index++;
            _maxlineposition = LinePosition;
            if (_c == 10)
            {
                LinePosition = 0;
                _line++;
            }
            else
            {
                LinePosition++;
            }
        }

        private bool IsValidTag()
        {
            bool isValidTag = _c == '<' && _index < Text.Length && (Char.IsLetter(Text.Span[_index]) || Text.Span[_index] == '/' || Text.Span[_index] == '?' || Text.Span[_index] == '!' || Text.Span[_index] == '%');
            return isValidTag;
        }

        private bool NewCheck()
        {
            if (_c != '<' || !IsValidTag())
            {
                return false;
            }

            if (_index < Text.Length)
            {
                if (Text[_index] == '%')
                {
                    if (DisableServerSideCode)
                    {
                        return false;
                    }

                    switch (_state)
                    {
                        case ParseState.AttributeAfterEquals:
                            PushAttributeValueStart(_index - 1);
                            break;

                        case ParseState.BetweenAttributes:
                            PushAttributeNameStart(_index - 1, LinePosition - 1);
                            break;

                        case ParseState.WhichTag:
                            PushNodeNameStart(true, _index - 1);
                            _state = ParseState.Tag;
                            break;
                    }

                    _oldstate = _state;
                    _state = ParseState.ServerSideCode;
                    return true;
                }
            }

            if (!PushNodeEnd(_index - 1, true))
            {
                // stop parsing
                _index = Text.Length;
                return true;
            }

            _state = ParseState.WhichTag;
            if ((_index - 1) <= (Text.Length - 2))
            {
                if (Text[_index] == '!' || Text[_index] == '?')
                {
                    PushNodeStart(HtmlNodeType.Comment, _index - 1, LinePosition - 1);
                    PushNodeNameStart(true, _index);
                    PushNodeNameEnd(_index + 1);
                    _state = ParseState.Comment;
                    if (_index < (Text.Length - 2))
                    {
                        if ((Text[_index + 1] == '-') &&
                            (Text[_index + 2] == '-'))
                        {
                            _fullcomment = true;
                        }
                        else
                        {
                            _fullcomment = false;
                        }
                    }

                    return true;
                }
            }

            PushNodeStart(HtmlNodeType.Element, _index - 1, LinePosition - 1);
            return true;
        }

        private void Parse()
        {
            if (ParseExecuting != null)
            {
                ParseExecuting(this);
            }

            int lastquote = 0;
            if (OptionComputeChecksum)
            {
                _crc32 = new Crc32();
            }

            Lastnodes = new Dictionary<string, HtmlNodeBase>();
            _c = 0;
            _fullcomment = false;
            _parseerrors = new List<HtmlParseError>();
            _line = 1;
            LinePosition = 0;
            _maxlineposition = 0;

            _state = ParseState.Text;
            _oldstate = _state;
            _documentnode.InnerLength = Text.Length;
            _documentnode.OuterLength = Text.Length;
            _remainderOffset = Text.Length;

            _lastparentnode = _documentnode;
            _currentnode = HtmlNodeFactory.Create(this, HtmlNodeType.Text, 0);
            _currentattribute = null;

            _index = 0;
            PushNodeStart(HtmlNodeType.Text, 0, LinePosition);
            while (_index < Text.Length)
            {
                _c = Text[_index];
                IncrementPosition();

                switch (_state)
                {
                    case ParseState.Text:
                        if (NewCheck())
                            continue;
                        break;

                    case ParseState.WhichTag:
                        if (NewCheck())
                            continue;
                        if (_c == '/')
                        {
                            PushNodeNameStart(false, _index);
                        }
                        else
                        {
                            PushNodeNameStart(true, _index - 1);
                            DecrementPosition();
                        }

                        _state = ParseState.Tag;
                        break;

                    case ParseState.Tag:
                        if (NewCheck())
                            continue;
                        if (IsWhiteSpace(_c))
                        {
                            CloseParentImplicitExplicitNode();

                            PushNodeNameEnd(_index - 1);
                            if (_state != ParseState.Tag)
                                continue;
                            _state = ParseState.BetweenAttributes;
                            continue;
                        }

                        if (_c == '/')
                        {
                            CloseParentImplicitExplicitNode();

                            PushNodeNameEnd(_index - 1);
                            if (_state != ParseState.Tag)
                                continue;
                            _state = ParseState.EmptyTag;
                            continue;
                        }

                        if (_c == '>')
                        {
                            CloseParentImplicitExplicitNode();

                            //// CHECK if parent is compatible with end tag
                            //if (IsParentIncompatibleEndTag())
                            //{
                            //    _state = ParseState.Text;
                            //    PushNodeStart(HtmlNodeType.Text, _index);
                            //    break;
                            //}

                            PushNodeNameEnd(_index - 1);
                            if (_state != ParseState.Tag)
                                continue;
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.Tag)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                        }

                        break;

                    case ParseState.BetweenAttributes:
                        if (NewCheck())
                            continue;

                        if (IsWhiteSpace(_c))
                            continue;

                        if ((_c == '/') || (_c == '?'))
                        {
                            _state = ParseState.EmptyTag;
                            continue;
                        }

                        if (_c == '>')
                        {
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.BetweenAttributes)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        PushAttributeNameStart(_index - 1, LinePosition - 1);
                        _state = ParseState.AttributeName;
                        break;

                    case ParseState.EmptyTag:
                        if (NewCheck())
                            continue;

                        if (_c == '>')
                        {
                            if (!PushNodeEnd(_index, true))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.EmptyTag)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        // we may end up in this state if attributes are incorrectly seperated
                        // by a /-character. If so, start parsing attribute-name immediately.
                        if (!IsWhiteSpace(_c))
                        {
                            // Just do nothing and push to next one!
                            DecrementPosition();
                            _state = ParseState.BetweenAttributes;
                            continue;
                        }
                        else
                        {
                            _state = ParseState.BetweenAttributes;
                        }

                        break;

                    case ParseState.AttributeName:
                        if (NewCheck())
                            continue;

                        if (IsWhiteSpace(_c))
                        {
                            PushAttributeNameEnd(_index - 1);
                            _state = ParseState.AttributeBeforeEquals;
                            continue;
                        }

                        if (_c == '=')
                        {
                            PushAttributeNameEnd(_index - 1);
                            _state = ParseState.AttributeAfterEquals;
                            continue;
                        }

                        if (_c == '>')
                        {
                            PushAttributeNameEnd(_index - 1);
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.AttributeName)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        break;

                    case ParseState.AttributeBeforeEquals:
                        if (NewCheck())
                            continue;

                        if (IsWhiteSpace(_c))
                            continue;
                        if (_c == '>')
                        {
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.AttributeBeforeEquals)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        if (_c == '=')
                        {
                            _state = ParseState.AttributeAfterEquals;
                            continue;
                        }

                        // no equals, no whitespace, it's a new attrribute starting
                        _state = ParseState.BetweenAttributes;
                        DecrementPosition();
                        break;

                    case ParseState.AttributeAfterEquals:
                        if (NewCheck())
                            continue;

                        if (IsWhiteSpace(_c))
                            continue;

                        if ((_c == '\'') || (_c == '"'))
                        {
                            _state = ParseState.QuotedAttributeValue;
                            PushAttributeValueStart(_index, _c);
                            lastquote = _c;
                            continue;
                        }

                        if (_c == '>')
                        {
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.AttributeAfterEquals)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        PushAttributeValueStart(_index - 1);
                        _state = ParseState.AttributeValue;
                        break;

                    case ParseState.AttributeValue:
                        if (NewCheck())
                            continue;

                        if (IsWhiteSpace(_c))
                        {
                            PushAttributeValueEnd(_index - 1);
                            _state = ParseState.BetweenAttributes;
                            continue;
                        }

                        if (_c == '>')
                        {
                            PushAttributeValueEnd(_index - 1);
                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            if (_state != ParseState.AttributeValue)
                                continue;
                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        break;

                    case ParseState.QuotedAttributeValue:
                        if (_c == lastquote)
                        {
                            PushAttributeValueEnd(_index - 1);
                            _state = ParseState.BetweenAttributes;
                            continue;
                        }

                        if (_c == '<')
                        {
                            if (_index < Text.Length)
                            {
                                if (Text.Span[_index] == '%')
                                {
                                    _oldstate = _state;
                                    _state = ParseState.ServerSideCode;
                                    continue;
                                }
                            }
                        }

                        break;

                    case ParseState.Comment:
                        if (_c == '>')
                        {
                            if (_fullcomment)
                            {
                                if (((Text.Span[_index - 2] != '-') || (Text.Span[_index - 3] != '-'))
                                    &&
                                    ((Text.Span[_index - 2] != '!') || (Text.Span[_index - 3] != '-') ||
                                     (Text.Span[_index - 4] != '-')))
                                {
                                    continue;
                                }
                            }

                            if (!PushNodeEnd(_index, false))
                            {
                                // stop parsing
                                _index = Text.Length;
                                break;
                            }

                            _state = ParseState.Text;
                            PushNodeStart(HtmlNodeType.Text, _index, LinePosition);
                            continue;
                        }

                        break;

                    case ParseState.ServerSideCode:
                        if (_c == '%')
                        {
                            if (_index < Text.Length)
                            {
                                if (Text.span[_index] == '>')
                                {
                                    switch (_oldstate)
                                    {
                                        case ParseState.AttributeAfterEquals:
                                            _state = ParseState.AttributeValue;
                                            break;

                                        case ParseState.BetweenAttributes:
                                            PushAttributeNameEnd(_index + 1);
                                            _state = ParseState.BetweenAttributes;
                                            break;

                                        default:
                                            _state = _oldstate;
                                            break;
                                    }

                                    IncrementPosition();
                                }
                            }
                        }
                        else if (_oldstate == ParseState.QuotedAttributeValue
                                 && _c == lastquote)
                        {
                            _state = _oldstate;
                            DecrementPosition();
                        }

                        break;

                    case ParseState.PcData:
                        // look for </tag + 1 char

                        // check buffer end
                        if ((_currentnode.Namelength + 3) <= (Text.Length - (_index - 1)))
                        {
                            
                            
                            if (MemoryExtensions.Equals(Text.Slice(_index - 1, _currentnode.Namelength + 2).Span, $"</{_currentnode.Name}".AsSpan(), StringComparison.OrdinalIgnoreCase))
                            {
                                int c = Text.Span[_index - 1 + 2 + _currentnode.Name.Length];

                                if ((c == '>') || (IsWhiteSpace(c)))
                                {
                                    // add the script as a text node
                                    var script = HtmlNodeFactory.Create(this, HtmlNodeType.Text, _currentnode.OuterStartIndex + _currentnode.OuterLength);
                                    script.OuterLength = _index - 1 - script.OuterStartIndex;
                                    script.StreamPosition = script.OuterStartIndex;
                                    script.Line = _currentnode.Line;
                                    script.LinePosition = _currentnode.LinePosition + _currentnode.Namelength + 2;

                                    var normalCurrentNode = _currentnode as HtmlNode;
                                    normalCurrentNode?.AppendChild(script);

                                    // https://www.w3schools.com/jsref/prop_node_innertext.asp
                                    // textContent returns the text content of all elements, while innerText returns the content of all elements, except for <script> and <style> elements.
                                    // innerText will not return the text of elements that are hidden with CSS (textContent will). ==> The parser do not support that.
                                    if (_currentnode.Name.ToLowerInvariant().Equals("script") || _currentnode.Name.ToLowerInvariant().Equals("style"))
                                    {
                                        _currentnode.IsHideInnerText = true;
                                    }

                                    PushNodeStart(HtmlNodeType.Element, _index - 1, LinePosition - 1);
                                    PushNodeNameStart(false, _index - 1 + 2);
                                    _state = ParseState.Tag;
                                    IncrementPosition();
                                }
                            }
                        }

                        break;
                }
            }

            // TODO: Add implicit end here?


            // finish the current work
            if (_currentnode.NameStartIndex > 0)
            {
                PushNodeNameEnd(_index);
            }

            PushNodeEnd(_index, false);

            // we don't need this anymore
            Lastnodes.Clear();
        }

        // In this moment, we don't have value. 
        // Potential: "\"", "'", "[", "]", "<", ">", "-", "|", "/", "\\"
        private static List<string> BlockAttributes = new List<string>() { "\"", "'" };

        private void PushAttributeNameEnd(int index)
        {
            _currentattribute._namelength = index - _currentattribute._namestartindex;

            HtmlNode normalCurrentNode = _currentnode as HtmlNode;
            if (normalCurrentNode != null &&
                _currentattribute.Name != null && !BlockAttributes.Contains(_currentattribute.Name))
            {
                normalCurrentNode.Attributes.Append(_currentattribute);
            }
        }

        private void PushAttributeNameStart(int index, int lineposition)
        {
            _currentattribute = CreateAttribute();
            _currentattribute._namestartindex = index;
            _currentattribute.Line = _line;
            _currentattribute.LinePosition = lineposition;
            _currentattribute.StreamPosition = index;
        }

        private void PushAttributeValueEnd(int index)
        {
            _currentattribute.ValueLength = index - _currentattribute.ValueStartIndex;
        }

        private void PushAttributeValueStart(int index)
        {
            PushAttributeValueStart(index, 0);
        }

        private void CloseParentImplicitExplicitNode()
        {
            bool hasNodeToClose = true;

            while (hasNodeToClose && !_lastparentnode.Closed)
            {
                hasNodeToClose = false;

                bool forceExplicitEnd = false;

                // CHECK if parent must be implicitely closed
                if (IsParentImplicitEnd())
                {
                    CloseParentImplicitEnd();
                    hasNodeToClose = true;
                }

                // CHECK if parent must be explicitely closed
                if (forceExplicitEnd || IsParentExplicitEnd())
                {
                    CloseParentExplicitEnd();
                    hasNodeToClose = true;
                }
            }
        }
        private bool IsParentImplicitEnd()
        {
            // MUST be a start tag
            if (!_currentnode.StartTag) return false;

            bool isImplicitEnd = false;

            var parent = _lastparentnode.Name;

            Span<char> toLower = stackalloc char[_index - _currentnode.NameStartIndex - 1];
            Text.Slice(_currentnode.NameStartIndex, _index - _currentnode.NameStartIndex - 1).Span.ToLowerInvariant(toLower);
            
            var nodeName = toLower; //Text.Substring(_currentnode.NameStartIndex, _index - _currentnode.NameStartIndex - 1).ToLowerInvariant();

            switch (parent)
            {
                case "a":
                    isImplicitEnd = nodeName == "a".AsSpan();
                    break;
                case "dd":
                    isImplicitEnd = nodeName == "dt".AsSpan() || nodeName == "dd".AsSpan();
                    break;
                case "dt":
                    isImplicitEnd = nodeName == "dt".AsSpan() || nodeName == "dd".AsSpan();
                    break;
                case "li":
                    isImplicitEnd = nodeName == "li".AsSpan();
                    break;
                case "p":
                    if (DisableBehaviorTagP)
                    {
                        isImplicitEnd = nodeName == "address".AsSpan()
                                        || nodeName == "article".AsSpan()
                                        || nodeName == "aside".AsSpan()
                                        || nodeName == "blockquote".AsSpan()
                                        || nodeName == "dir".AsSpan()
                                        || nodeName == "div".AsSpan()
                                        || nodeName == "dl".AsSpan()
                                        || nodeName == "fieldset".AsSpan()
                                        || nodeName == "footer".AsSpan()
                                        || nodeName == "form".AsSpan()
                                        || nodeName == "h1".AsSpan()
                                        || nodeName == "h2".AsSpan()
                                        || nodeName == "h3".AsSpan()
                                        || nodeName == "h4".AsSpan()
                                        || nodeName == "h5".AsSpan()
                                        || nodeName == "h6".AsSpan()
                                        || nodeName == "header".AsSpan()
                                        || nodeName == "hr".AsSpan()
                                        || nodeName == "menu".AsSpan()
                                        || nodeName == "nav".AsSpan()
                                        || nodeName == "ol".AsSpan()
                                        || nodeName == "p".AsSpan()
                                        || nodeName == "pre".AsSpan()
                                        || nodeName == "section".AsSpan()
                                        || nodeName == "table".AsSpan()
                                        || nodeName == "ul".AsSpan();
                    }
                    else
                    {
                        isImplicitEnd = nodeName == "p".AsSpan();
                    }

                    break;
                case "option":
                    isImplicitEnd = nodeName == "option".AsSpan();
                    break;
            }

            return isImplicitEnd;
        }

        private bool IsParentExplicitEnd()
        {
            // MUST be a start tag
            if (!_currentnode.StartTag) return false;

            bool isExplicitEnd = false;

            var parent = _lastparentnode.Name;
            Span<char> toLower = stackalloc char[_index - _currentnode.NameStartIndex - 1];
            Text.Slice(_currentnode.NameStartIndex, _index - _currentnode.NameStartIndex - 1).Span.ToLowerInvariant(toLower);

            var nodeName = toLower; //Text.Substring(_currentnode.NameStartIndex, _index - _currentnode.NameStartIndex - 1).ToLowerInvariant();

            switch (parent)
            {
                case "title":
                    isExplicitEnd = nodeName == "title".AsSpan();
                    break;
                case "p":
                    isExplicitEnd = nodeName == "div".AsSpan();
                    break;
                case "table":
                    isExplicitEnd = nodeName == "table".AsSpan();
                    break;
                case "tr":
                    isExplicitEnd = nodeName == "tr".AsSpan();
                    break;
                case "td":
                    isExplicitEnd = nodeName == "td".AsSpan() || nodeName == "th".AsSpan() || nodeName == "tr".AsSpan();
                    break;
                case "th":
                    isExplicitEnd = nodeName == "td".AsSpan() || nodeName == "th".AsSpan() || nodeName == "tr".AsSpan();
                    break;
                case "h1":
                    isExplicitEnd = nodeName == "h2".AsSpan() || nodeName == "h3".AsSpan() || nodeName == "h4".AsSpan() || nodeName == "h5".AsSpan();
                    break;
                case "h2":
                    isExplicitEnd = nodeName == "h1".AsSpan() || nodeName == "h3".AsSpan() || nodeName == "h4".AsSpan() || nodeName == "h5".AsSpan();
                    break;
                case "h3":
                    isExplicitEnd = nodeName == "h1".AsSpan() || nodeName == "h2".AsSpan() || nodeName == "h4".AsSpan() || nodeName == "h5".AsSpan();
                    break;
                case "h4":
                    isExplicitEnd = nodeName == "h1".AsSpan() || nodeName == "h2".AsSpan() || nodeName == "h3".AsSpan() || nodeName == "h5".AsSpan();
                    break;
                case "h5":
                    isExplicitEnd = nodeName == "h1".AsSpan() || nodeName == "h2".AsSpan() || nodeName == "h3".AsSpan() || nodeName == "h4".AsSpan();
                    break;
            }

            return isExplicitEnd;
        }

        //private bool IsParentIncompatibleEndTag()
        //{
        //    // MUST be a end tag
        //    if (_currentnode._starttag) return false;

        //    bool isIncompatible = false;

        //    var parent = _lastparentnode.Name;
        //    var nodeName = Text.Substring(_currentnode._namestartindex, _index - _currentnode._namestartindex - 1);

        //    switch (parent)
        //    {
        //        case "h1":
        //            isIncompatible = nodeName == "h2" || nodeName == "h3" || nodeName == "h4" || nodeName == "h5";
        //            break;
        //        case "h2":
        //            isIncompatible = nodeName == "h1" || nodeName == "h3" || nodeName == "h4" || nodeName == "h5";
        //            break;
        //        case "h3":
        //            isIncompatible = nodeName == "h1" || nodeName == "h2" || nodeName == "h4" || nodeName == "h5";
        //            break;
        //        case "h4":
        //            isIncompatible = nodeName == "h1" || nodeName == "h2" || nodeName == "h3" || nodeName == "h5";
        //            break;
        //        case "h5":
        //            isIncompatible = nodeName == "h1" || nodeName == "h2" || nodeName == "h3" || nodeName == "h4";
        //            break;
        //    }

        //    return isIncompatible;
        //}

        private void CloseParentImplicitEnd()
        {
            HtmlNode close = HtmlNodeFactory.Create(this, _lastparentnode.NodeType, -1) as HtmlNode;
            close.EndNode = close;
            close.IsImplicitEnd = true;
            _lastparentnode.IsImplicitEnd = true;
            _lastparentnode.CloseNode(close);
        }

        private void CloseParentExplicitEnd()
        {
            HtmlNode close = HtmlNodeFactory.Create(this, _lastparentnode.NodeType, -1) as HtmlNode;
            if (close == null)
            {
                return;
            }

            close.EndNode = close;
            _lastparentnode.CloseNode(close);
        }

        private void PushAttributeValueStart(int index, int quote)
        {
            _currentattribute.ValueStartIndex = index;
            if (quote == '\'')
                _currentattribute.QuoteType = AttributeValueQuote.SingleQuote;
        }

        private bool PushNodeEnd(int index, bool close)
        {
            _currentnode.OuterLength = index - _currentnode.OuterStartIndex;

            if ((_currentnode.NodeType == HtmlNodeType.Text) ||
                (_currentnode.NodeType == HtmlNodeType.Comment))
            {
                // forget about void nodes
                if (_currentnode.OuterLength > 0)
                {
                    _currentnode.InnerLength = _currentnode.OuterLength;
                    _currentnode.InnerStartIndex = _currentnode.OuterStartIndex;
                    if (_lastparentnode != null)
                    {
                        _lastparentnode.AppendChild(_currentnode);
                    }
                }
            }
            else
            {
                if ((_currentnode.StartTag) && (_lastparentnode != _currentnode))
                {
                    // add to parent node
                    if (_lastparentnode != null)
                    {
                        _lastparentnode.AppendChild(_currentnode);
                    }

                    HtmlNode normalCurrentNode = _currentnode as HtmlNode;
                    if (normalCurrentNode != null)
                    {
                        ReadDocumentEncoding(normalCurrentNode);
                    }

                    // remember last node of this kind
                    HtmlNodeBase prev = Utilities.GetDictionaryValueOrDefault(Lastnodes, _currentnode.Name);

                    _currentnode.PrevWithSameName = prev;
                    Lastnodes[_currentnode.Name] = _currentnode;

                    // change parent?
                    if ((_currentnode.NodeType == HtmlNodeType.Document) ||
                        (_currentnode.NodeType == HtmlNodeType.Element))
                    {
                        _lastparentnode = normalCurrentNode;
                    }

                    if (HtmlNodeBase.IsCDataElement(CurrentNodeName()))
                    {
                        _state = ParseState.PcData;
                        return true;
                    }

                    if ((HtmlNodeBase.IsClosedElement(_currentnode.Name)) ||
                        (HtmlNodeBase.IsEmptyElement(_currentnode.Name)))
                    {
                        close = true;
                    }
                }
            }

            if ((close) || (!_currentnode.StartTag))
            {
                if ((OptionStopperNodeName != null) && (_remainder.IsEmpty) &&
                    (string.Compare(_currentnode.Name, OptionStopperNodeName, StringComparison.OrdinalIgnoreCase) == 0)) // todo
                {
                    _remainderOffset = index;
                    _remainder = Text.Slice(_remainderOffset);
                    CloseCurrentNode();
                    return false; // stop parsing
                }

                CloseCurrentNode();
            }

            return true;
        }

        private void PushNodeNameEnd(int index)
        {
            _currentnode.Namelength = index - _currentnode.NameStartIndex;
            if (OptionFixNestedTags)
            {
                FixNestedTags();
            }
        }

        private void PushNodeNameStart(bool starttag, int index)
        {
            _currentnode.StartTag = starttag;
            _currentnode.NameStartIndex = index;
        }

        private void PushNodeStart(HtmlNodeType type, int index, int lineposition)
        {
            _currentnode = HtmlNodeFactory.Create(this, type, index);
            _currentnode.Line = _line;
            _currentnode.LinePosition = lineposition;
            _currentnode.StreamPosition = index;
        }

        private void ReadDocumentEncoding(HtmlNode node)
        {
            if (!OptionReadEncoding)
                return;
            // format is 
            // <meta http-equiv="content-type" content="text/html;charset=iso-8859-1" />

            // when we append a child, we are in node end, so attributes are already populated
            if (node.Namelength != 4) // quick check, avoids string alloc
                return;
            if (node.Name != "meta") // all nodes names are lowercase
                return;

            string charset = null;
            HtmlAttribute att = node.Attributes["http-equiv"];
            if (att != null)
            {
                if (string.Compare(att.Value, "content-type", StringComparison.OrdinalIgnoreCase) != 0)
                    return;
                HtmlAttribute content = node.Attributes["content"];
                if (content != null)
                    charset = NameValuePairList.GetNameValuePairsValue(content.Value, "charset");
            }
            else
            {
                att = node.Attributes["charset"];
                if (att != null)
                    charset = att.Value;
            }

            if (!string.IsNullOrEmpty(charset))
            {
                // The following check fixes the the bug described at: http://htmlagilitypack.codeplex.com/WorkItem/View.aspx?WorkItemId=25273
                if (string.Equals(charset, "utf8", StringComparison.OrdinalIgnoreCase))
                    charset = "utf-8";
                try
                {
                    _declaredencoding = Encoding.GetEncoding(charset);
                }
                catch (ArgumentException)
                {
                    _declaredencoding = null;
                }

                if (_onlyDetectEncoding)
                {
                    throw new EncodingFoundException(_declaredencoding);
                }

                if (_streamencoding != null)
                {
#if SILVERLIGHT || PocketPC || METRO || NETSTANDARD1_3 || NETSTANDARD1_6
                    if (_declaredencoding.WebName != _streamencoding.WebName)
#else
                    if (_declaredencoding != null)
                        if (_declaredencoding.CodePage != _streamencoding.CodePage)
#endif
                        {
                            AddError(
                                HtmlParseErrorCode.CharsetMismatch,
                                _line, LinePosition,
                                _index, node.OuterHtml,
                                "Encoding mismatch between StreamEncoding: " +
                                _streamencoding.WebName + " and DeclaredEncoding: " +
                                _declaredencoding.WebName);
                        }
                }
            }
        }

        private enum ParseState
        {
            Text,
            WhichTag,
            Tag,
            BetweenAttributes,
            EmptyTag,
            AttributeName,
            AttributeBeforeEquals,
            AttributeAfterEquals,
            AttributeValue,
            Comment,
            QuotedAttributeValue,
            ServerSideCode,
            PcData
        }
    }
}