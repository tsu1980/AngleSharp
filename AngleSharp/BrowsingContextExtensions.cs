﻿namespace AngleSharp
{
    using AngleSharp.Dom;
    using AngleSharp.Dom.Html;
    using AngleSharp.Dom.Svg;
    using AngleSharp.Dom.Xml;
    using AngleSharp.Extensions;
    using AngleSharp.Network;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A set of extensions for the browsing context.
    /// </summary>
    [DebuggerStepThrough]
    public static class BrowsingContextExtensions
    {
        #region Navigation

        /// <summary>
        /// Opens a new document without any content in the given context.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="url">The optional base URL of the document.</param>
        /// <returns>The new, yet empty, document.</returns>
        public static Task<IDocument> OpenNewAsync(this IBrowsingContext context, String url = null)
        {
            return context.OpenAsync(m => m.Address(url));
        }

        /// <summary>
        /// Opens a new document created from the response asynchronously in
        /// the given context.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="response">The response to examine.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>The task that creates the document.</returns>
        public static async Task<IDocument> OpenAsync(this IBrowsingContext context, IResponse response, CancellationToken cancel)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            if (context == null)
                context = BrowsingContext.New();

            var source = new TextSource(response.Content, context.Configuration.DefaultEncoding());

            // Set TextSource encoding from HTTP header. modify by tsu1980
            var contentType = response.GetContentType();
            if (!string.IsNullOrEmpty(contentType))
            {
                var m = Regex.Match(contentType, @"charset=(\w+)");
                if (m.Success)
                {
                    var charset = m.Groups[1].Value;
                    if (!String.IsNullOrEmpty(charset) && TextEncoding.IsSupported(charset))
                    {
                        source.CurrentEncoding = TextEncoding.Resolve(charset);
                    }
                }
            }

            var document = await context.LoadDocumentAsync(response, source, cancel).ConfigureAwait(false);
            context.NavigateTo(document);
            return document;
        }

        /// <summary>
        /// Opens a new document loaded from the specified request
        /// asynchronously in the given context.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="request">The request to issue.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>The task that creates the document.</returns>
        public static async Task<IDocument> OpenAsync(this IBrowsingContext context, DocumentRequest request, CancellationToken cancel)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            using (var response = await context.Loader.SendAsync(request, cancel).ConfigureAwait(false))
            {
                if (response != null)
                    return await context.OpenAsync(response, cancel).ConfigureAwait(false);
            }

            return await context.OpenNewAsync(request.Target.Href).ConfigureAwait(false);
        }

        /// <summary>
        /// Opens a new document loaded from the provided url asynchronously in
        /// the given context.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="url">The URL to load.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>The task that creates the document.</returns>
        public static Task<IDocument> OpenAsync(this IBrowsingContext context, Url url, CancellationToken cancel)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            
            var request = DocumentRequest.Get(url);

            if (context != null && context.Active != null)
                request.Referer = context.Active.DocumentUri;

            return context.OpenAsync(request, cancel);
        }

        /// <summary>
        /// Opens a new document loaded from a virtual response that can be 
        /// filled via the provided callback.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="request">Callback with the response to setup.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>The task that creates the document.</returns>
        public static async Task<IDocument> OpenAsync(this IBrowsingContext context, Action<VirtualResponse> request, CancellationToken cancel)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (context == null)
                context = BrowsingContext.New();

            using (var response = new VirtualResponse())
            {
                request(response);
                var source = response.CreateSourceFor(context.Configuration);
                var document = await context.LoadDocumentAsync(response, source, cancel).ConfigureAwait(false);
                context.NavigateTo(document);
                return document;
            }
        }

        /// <summary>
        /// Opens a new document loaded from a virtual response that can be 
        /// filled via the provided callback without any ability to cancel it.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="request">Callback with the response to setup.</param>
        /// <returns>The task that creates the document.</returns>
        public static Task<IDocument> OpenAsync(this IBrowsingContext context, Action<VirtualResponse> request)
        {
            return context.OpenAsync(request, CancellationToken.None);
        }

        /// <summary>
        /// Opens a new document loaded from the provided url asynchronously in
        /// the given context without the ability to cancel it.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="url">The URL to load.</param>
        /// <returns>The task that creates the document.</returns>
        public static Task<IDocument> OpenAsync(this IBrowsingContext context, Url url)
        {
            return context.OpenAsync(url, CancellationToken.None);
        }

        /// <summary>
        /// Opens a new document loaded from the provided address
        /// asynchronously in the given context.
        /// </summary>
        /// <param name="context">The browsing context to use.</param>
        /// <param name="address">The address to load.</param>
        /// <returns>The task that creates the document.</returns>
        public static Task<IDocument> OpenAsync(this IBrowsingContext context, String address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            return context.OpenAsync(Url.Create(address), CancellationToken.None);
        }

        #endregion

        #region Helpers

        static async Task<IDocument> LoadDocumentAsync(this IBrowsingContext context, IResponse response, TextSource source, CancellationToken cancel)
        {
            var contentType = response.Headers.GetOrDefault(HeaderNames.ContentType, MimeTypes.Html);

            if (contentType.IndexOf(';') > 0)
            {
                contentType = contentType.Substring(0, contentType.IndexOf(';')).Trim();
            }

            if (contentType.Equals(MimeTypes.Xml, StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals(MimeTypes.ApplicationXml, StringComparison.OrdinalIgnoreCase))
            {
                return await XmlDocument.LoadAsync(context, response, source, cancel).ConfigureAwait(false);
            }
            else if (contentType.Equals(MimeTypes.Svg, StringComparison.OrdinalIgnoreCase))
            {
                return await SvgDocument.LoadAsync(context, response, source, cancel);
            }

            return await HtmlDocument.LoadAsync(context, response, source, cancel).ConfigureAwait(false);
        }

        #endregion

        #region Virtual Network Response

        /// <summary>
        /// The virtual response class.
        /// </summary>
        public class VirtualResponse : IResponse
        {
            Url address;
            HttpStatusCode status;
            Dictionary<String, String> headers;
            TextSource source;
            Stream content;
            Boolean dispose;

            /// <summary>
            /// Creates a new virtual response.
            /// </summary>
            public VirtualResponse()
            {
                address = Url.Create("http://localhost/");
                status = HttpStatusCode.OK;
                headers = new Dictionary<String, String>();
                content = MemoryStream.Null;
                source = null;
                dispose = false;
            }

            /// <summary>
            /// Sets the location of the response to the given url.
            /// </summary>
            /// <param name="url">The imaginary url of the response.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Address(Url url)
            {
                address = url;
                return this;
            }

            /// <summary>
            /// Sets the location of the response to the provided address.
            /// </summary>
            /// <param name="address">The string to use as an url.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Address(String address)
            {
                return Address(Url.Create(address ?? String.Empty));
            }

            /// <summary>
            /// Sets the location of the response to the uri's value.
            /// </summary>
            /// <param name="url">The Uri instance to convert.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Address(Uri url)
            {
                return Address(Url.Convert(url));
            }

            /// <summary>
            /// Sets the status code.
            /// </summary>
            /// <param name="code">The status code to set.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Status(HttpStatusCode code)
            {
                status = code;
                return this;
            }

            /// <summary>
            /// Sets the status code by providing the integer value.
            /// </summary>
            /// <param name="code">The integer representing the code.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Status(Int32 code)
            {
                return Status((HttpStatusCode)code);
            }

            /// <summary>
            /// Sets the header with the given name and value.
            /// </summary>
            /// <param name="name">The header name to set.</param>
            /// <param name="value">The value for the key.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Header(String name, String value)
            {
                headers[name] = value;
                return this;
            }

            /// <summary>
            /// Sets the headers with the name of the properties and their 
            /// assigned values.
            /// </summary>
            /// <param name="obj">The object to decompose.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Headers(Object obj)
            {
                var headers = obj.ToDictionary();
                return Headers(headers);
            }

            /// <summary>
            /// Sets the headers with the name of the keys and their assigned
            /// values.
            /// </summary>
            /// <param name="headers">The dictionary to use.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Headers(IDictionary<String, String> headers)
            {
                foreach (var header in headers)
                {
                    Header(header.Key, header.Value);
                }

                return this;
            }

            /// <summary>
            /// Sets the response's content from the provided string.
            /// </summary>
            /// <param name="text">The text to use as content.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Content(String text)
            {
                Release();
                source = new TextSource(text);
                return this;
            }

            /// <summary>
            /// Sets the response's content from the provided stream.
            /// </summary>
            /// <param name="stream">The response's content stream.</param>
            /// <param name="shouldDispose">True to dispose afterwards.</param>
            /// <returns>The current instance.</returns>
            public VirtualResponse Content(Stream stream, Boolean shouldDispose = false)
            {
                Release();
                content = stream;
                dispose = shouldDispose;
                return this;
            }

            Url IResponse.Address
            {
                get { return address; }
            }

            Stream IResponse.Content
            {
                get { return content; }
            }

            IDictionary<String, String> IResponse.Headers
            {
                get { return headers; }
            }

            HttpStatusCode IResponse.StatusCode
            {
                get { return status; }
            }

            void Release()
            {
                if (content != null && dispose)
                    content.Dispose();
                else if (source != null)
                    source.Dispose();

                dispose = false;
                source = null;
                content = null;
            }

            void IDisposable.Dispose()
            {
                Release();
            }

            internal TextSource CreateSourceFor(IConfiguration configuration)
            {
                if (source != null)
                    return source;
                else if (content != null)
                    return new TextSource(content, configuration.DefaultEncoding());

                return new TextSource(String.Empty);
            }
        }

        #endregion
    }
}