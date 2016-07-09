namespace AngleSharp.Dom.Html
{
    using AngleSharp.Attributes;
    using System;

    /// <summary>
    /// Represents the frame HTML element.
    /// </summary>
    [DomName("HTMLFrameElement")]
    public interface IHtmlFrameElement : IHtmlElement
    {
        /// <summary>
        /// Gets or sets the frame source.
        /// </summary>
        [DomName("src")]
        String Source { get; set; }

        /// <summary>
        /// Gets the content of the page that the nested browsing context is to contain.
        /// </summary>
        [DomName("srcdoc")]
        String ContentHtml { get; set; }

        /// <summary>
        /// Gets or sets the name of the frame.
        /// </summary>
        [DomName("name")]
        String Name { get; set; }

        /// <summary>
        /// Gets the tokens of the sandbox attribute.
        /// </summary>
        [DomName("sandbox")]
        ISettableTokenList Sandbox { get; }

        /// <summary>
        /// Gets the browsing context.
        /// </summary>
        [DomName("context")]
        IBrowsingContext Context { get; }

        /// <summary>
        /// Gets the document this frame contains, if there is any.
        /// </summary>
        [DomName("contentDocument")]
        IDocument ContentDocument { get; }

        /// <summary>
        /// Gets the frame's parent's window context.
        /// </summary>
        [DomName("contentWindow")]
        IWindow ContentWindow { get; }
    }
}
