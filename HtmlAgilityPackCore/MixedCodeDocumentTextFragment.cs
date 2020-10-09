#if !METRO
namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents a fragment of text in a mixed code document.
    /// </summary>
    public class MixedCodeDocumentTextFragment : MixedCodeDocumentFragment
    {
        internal MixedCodeDocumentTextFragment(MixedCodeDocument doc)
            :
            base(doc, MixedCodeDocumentFragmentType.Text)
        {
        }

        /// <summary>
        /// Gets the fragment text.
        /// </summary>
        public string Text
        {
            get { return FragmentText; }
            set { FragmentText = value; }
        }
    }
}
#endif