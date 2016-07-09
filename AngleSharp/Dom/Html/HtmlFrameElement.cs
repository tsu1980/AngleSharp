namespace AngleSharp.Dom.Html
{
    using System;
    using AngleSharp.Extensions;
    using AngleSharp.Html;
    using Network;
    using Collections;

    /// <summary>
    /// Represents the HTML frame element.
    /// </summary>
    sealed class HtmlFrameElement : HtmlFrameElementBase, IHtmlFrameElement
    {
        #region Fields

        readonly IBrowsingContext _context;
        SettableTokenList _sandbox;

        #endregion

        #region ctor

        public HtmlFrameElement(Document owner, String prefix = null)
            : base(owner, Tags.Frame, prefix, NodeFlags.SelfClosing)
        {
            _context = owner.NewChildContext(Sandboxes.None);
            RegisterAttributeObserver(AttributeNames.Src, UpdateSource);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the content of the page that the nested browsing context is to contain.
        /// </summary>
        public String ContentHtml
        {
            get { return GetOwnAttribute(AttributeNames.SrcDoc); }
            set { SetOwnAttribute(AttributeNames.SrcDoc, value); }
        }

        public ISettableTokenList Sandbox
        {
            get
            {
                if (_sandbox == null)
                {
                    _sandbox = new SettableTokenList(GetOwnAttribute(AttributeNames.Sandbox));
                    CreateBindings(_sandbox, AttributeNames.Sandbox);
                }

                return _sandbox;
            }
        }

        /// <summary>
        /// Gets the browsing context.
        /// </summary>
        public IBrowsingContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// Gets the frame's parent's window context.
        /// </summary>
        public IWindow ContentWindow
        {
            get { return _context.Current; }
        }

        /// <summary>
        /// Gets or sets if the frame cannot be resized.
        /// </summary>
        public Boolean NoResize
        {
            get { return GetOwnAttribute(AttributeNames.NoResize).ToBoolean(false); }
            set { SetOwnAttribute(AttributeNames.NoResize, value.ToString()); }
        }

        #endregion

        #region Methods

        void UpdateSource(String src)
        {
            this.CancelTasks();

            if (!String.IsNullOrEmpty(src))
            {
                var url = this.HyperReference(src);
                var request = DocumentRequest.Get(url, source: this, referer: Owner.DocumentUri);
                this.CreateTask(cancel => _context.OpenAsync(request, cancel))
                    .ContinueWith(m => this.FireLoadOrErrorEvent(m));
            }
        }

        #endregion
    }
}
