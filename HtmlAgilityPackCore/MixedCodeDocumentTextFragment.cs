#if !METRO
namespace HtmlAgilityPackCore
{
    /// <summary>
    /// Represents a fragment of text in a mixed code document.
    /// </summary>
    public class MixedCodeDocumentTextFragment : MixedCodeDocumentFragment
    {
        #region Constructors

        internal MixedCodeDocumentTextFragment(MixedCodeDocument doc)
            :
            base(doc, MixedCodeDocumentFragmentType.Text)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the fragment text.
        /// </summary>
        public string Text
        {
            get { return FragmentText; }
            set { FragmentText = value; }
        }

        #endregion
    }
}
#endif