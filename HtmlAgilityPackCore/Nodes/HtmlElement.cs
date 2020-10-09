using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HtmlAgilityPackCore.Nodes
{
    /// <summary>
    /// Represents an HTML element.
    /// </summary>
    public class HtmlElement : HtmlNode
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownerDoc">The owner document of this node</param>
        public HtmlElement(HtmlDocument ownerDoc) : base(HtmlNodeType.Element, ownerDoc, -1) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownerDoc">The owner document of this node</param>
        /// <param name="index"></param>
        internal HtmlElement(HtmlDocument ownerDoc, int index) : base(HtmlNodeType.Element, ownerDoc, index) { }

        /// <summary>
        /// Initialize the Name property
        /// </summary>
        protected override void InitName() { }

        /// <summary>
        /// Initialize the end node field
        /// </summary>
        protected override void InitEndNode() { }

        //[Obsolete("Why can it be invalid?")]
        //internal bool IsValid()
        //{
        //    return !OuterHtml.Equals("\r\n");
        //}

        /// <summary>
        /// Adds one or more classes to this node.
        /// </summary>
        /// <param name="name">The node list to add. May not be null.</param>
        public void AddClass(string name)
        {
            AddClass(name, false);
        }

        /// <summary>
        /// Adds one or more classes to this node.
        /// </summary>
        /// <param name="name">The node list to add. May not be null.</param>
        /// <param name="throwError">true to throw Error if class name exists, false otherwise.</param>
        public void AddClass(string name, bool throwError)
        {
            var classAttributes = Attributes.AttributesWithName("class");

            if (!IsEmpty(classAttributes))
            {
                foreach (HtmlAttribute att in classAttributes)
                {
                    // Check class solo, check class in First with other class, check Class no first.
                    if (att.Value != null && att.Value.Split(' ').ToList().Any(x => x.Equals(name)))
                    {
                        if (throwError)
                        {
                            throw new Exception(HtmlDocument.HtmlExceptionClassExists); // TODO
                        }
                    }
                    else
                    {
                        SetAttributeValue(att.Name, att.Value + " " + name);
                    }
                }
            }
            else
            {
                HtmlAttribute attribute = OwnerDocument.CreateAttribute("class", name);
                Attributes.Append(attribute);
            }
        }

        /// <summary>
        /// Removes the class attribute from the node.
        /// </summary>
        public void RemoveClass()
        {
            RemoveClass(false);
        }

        /// <summary>
        /// Removes the class attribute from the node.
        /// </summary>
        /// <param name="throwError">true to throw Error if class name doesn't exist, false otherwise.</param>
        public void RemoveClass(bool throwError)
        {
            IEnumerable<HtmlAttribute> classAttributes = Attributes.AttributesWithName("class");
            if (IsEmpty(classAttributes) && throwError)
            {
                throw new Exception(HtmlDocument.HtmlExceptionClassDoesNotExist); // todo
            }

            foreach (var att in classAttributes)
            {
                Attributes.Remove(att);
            }
        }

        /// <summary>
        /// Removes the specified class from the node.
        /// </summary>
        /// <param name="name">The class being removed. May not be <c>null</c>.</param>
        public void RemoveClass(string name)
        {
            RemoveClass(name, false);
        }

        /// <summary>
        /// Removes the specified class from the node.
        /// </summary>
        /// <param name="name">The class being removed. May not be <c>null</c>.</param>
        /// <param name="throwError">true to throw Error if class name doesn't exist, false otherwise.</param>
        public void RemoveClass(string name, bool throwError)
        {
            IEnumerable<HtmlAttribute> classAttributes = Attributes.AttributesWithName("class");
            if (IsEmpty(classAttributes) && throwError)
            {
                throw new Exception(HtmlDocument.HtmlExceptionClassDoesNotExist);
            }

            else
            {
                foreach (var att in classAttributes)
                {
                    if (att.Value == null)
                    {
                        continue;
                    }

                    if (att.Value.Equals(name))
                    {
                        Attributes.Remove(att);
                    }
                    else if (att.Value != null && att.Value.Split(' ').ToList().Any(x => x.Equals(name)))
                    {
                        string[] classNames = att.Value.Split(' '); //todo

                        string newClassNames = "";

                        foreach (string item in classNames)
                        {
                            if (!item.Equals(name))
                                newClassNames += item + " ";
                        }

                        newClassNames = newClassNames.Trim();
                        SetAttributeValue(att.Name, newClassNames);
                    }
                    else
                    {
                        if (throwError)
                        {
                            throw new Exception(HtmlDocument.HtmlExceptionClassDoesNotExist);
                        }
                    }

                    if (string.IsNullOrEmpty(att.Value))
                    {
                        Attributes.Remove(att);
                    }
                }
            }
        }

        /// <summary>
        /// Replaces the class name oldClass with newClass name.
        /// </summary>
        /// <param name="newClass">The new class name.</param>
        /// <param name="oldClass">The class being replaced.</param>
        public void ReplaceClass(string newClass, string oldClass)
        {
            ReplaceClass(newClass, oldClass, false);
        }

        /// <summary>
        /// Replaces the class name oldClass with newClass name.
        /// </summary>
        /// <param name="newClass">The new class name.</param>
        /// <param name="oldClass">The class being replaced.</param>
        /// <param name="throwError">true to throw Error if class name doesn't exist, false otherwise.</param>
        public void ReplaceClass(string newClass, string oldClass, bool throwError)
        {
            if (string.IsNullOrEmpty(newClass))
            {
                RemoveClass(oldClass);
            }

            if (string.IsNullOrEmpty(oldClass))
            {
                AddClass(newClass);
            }

            var classAttributes = Attributes.AttributesWithName("class");

            if (IsEmpty(classAttributes) && throwError)
            {
                throw new Exception(HtmlDocument.HtmlExceptionClassDoesNotExist);
            }

            foreach (var att in classAttributes)
            {
                if (att.Value == null)
                {
                    continue;
                }

                if (att.Value.Equals(oldClass) || att.Value.Contains(oldClass))
                {
                    string newClassNames = att.Value.Replace(oldClass, newClass);
                    SetAttributeValue(att.Name, newClassNames);
                }
                else if (throwError)
                {
                    throw new Exception(HtmlDocument.HtmlExceptionClassDoesNotExist);
                }
            }
        }

        /// <summary>Gets the CSS Class from the node.</summary>
        /// <returns>
        ///     The CSS Class from the node
        /// </returns>
        public IEnumerable<string> GetClasses()
        {
            var classAttributes = Attributes.AttributesWithName("class");

            foreach (var att in classAttributes)
            {
                var classNames = att.Value.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

                foreach (var className in classNames)
                {
                    yield return className;
                }
            }
        }

        /// <summary>Check if the node class has the parameter class.</summary>
        /// <param name="class">The class.</param>
        /// <returns>True if node class has the parameter class, false if not.</returns>
        public bool HasClass(string className)
        {
            var classes = GetClasses();

            foreach (var @class in classes)
            {
                var classNames = @class.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                foreach (var theClassName in classNames)
                {
                    if (theClassName == className)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsEmpty<T>(IEnumerable<T> en)
        {
            foreach (T c in en)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the current node to the specified TextWriter.
        /// </summary>
        /// <param name="outText">The TextWriter to which you want to save.</param>
        /// <param name="level">identifies the level we are in starting at root with 0</param>
        public override void WriteTo(TextWriter outText, int level = 0)
        {
            string name = OwnerDocument.OptionOutputUpperCase ? Name.ToUpperInvariant() : Name;
            if (OwnerDocument.OptionOutputOriginalCase)
            {
                name = OriginalName;
            }

            outText.Write("<" + name);
            WriteAttributes(outText, false);

            if (HasChildNodes)
            {
                outText.Write(">");

                bool cdata = false;
                if (IsCDataElement(Name))
                {
                    // this code and the following tries to output things as nicely as possible for old browsers.
                    cdata = true;
                    outText.Write("\r\n//<![CDATA[\r\n");
                }

                if (cdata)
                {
                    if (HasChildNodes)
                        // child must be a text
                        ChildNodes[0].WriteTo(outText, level);

                    outText.Write("\r\n//]]>//\r\n");
                }
                else
                {
                    WriteContentTo(outText, level);
                }

                if (!IsImplicitEnd)
                {
                    outText.Write("</" + name);
                    WriteAttributes(outText, true);
                    outText.Write(">");
                }
            }
            else
            {
                if (IsEmptyElement(Name))
                {
                    if (OwnerDocument.OptionWriteEmptyNodes)
                    {
                        outText.Write(" />");
                    }
                    else
                    {
                        if (Name.Length > 0 && Name[0] == '?')
                        {
                            outText.Write("?");
                        }

                        outText.Write(">");
                    }
                }
                else
                {
                    if (!IsImplicitEnd)
                    {
                        outText.Write("></" + name + ">");
                    }
                    else
                    {
                        outText.Write(">");
                    }
                }
            }
        }
    }
}