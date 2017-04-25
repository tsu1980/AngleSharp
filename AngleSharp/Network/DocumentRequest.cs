namespace AngleSharp.Network
{
    using AngleSharp.Dom;
    using AngleSharp.Html;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Represents the arguments to load a document.
    /// </summary>
    public class DocumentRequest
    {
        /// <summary>
        /// Creates a new document request for the given url.
        /// </summary>
        /// <param name="target">The resource's url.</param>
        public DocumentRequest(Url target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            Target = target;
            Referer = null;
            Method = HttpMethod.Get;
            Body = MemoryStream.Null;
            MimeType = null;
            CustomHeaders = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a GET request for the given target from the optional source
        /// node and optional referer string.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="source">The optional source of the request.</param>
        /// <param name="referer">The optional referrer string.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest Get(Url target, INode source = null, String referer = null)
        {
            return new DocumentRequest(target)
            {
                Method = HttpMethod.Get,
                Referer = referer,
                Source = source
            };
        }

        /// <summary>
        /// Creates a POST request for the given target with the provided body
        /// and encoding type from the optional source node and optional
        /// referer string.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="type">The type of the request's body.</param>
        /// <param name="source">The optional source of the request.</param>
        /// <param name="referer">The optional referrer string.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest Post(Url target, Stream body, String type, INode source = null, String referer = null,
            Dictionary<string, string> customHeaders = null)
        {
            if (body == null)
                throw new ArgumentNullException("body");

            if (type == null)
                throw new ArgumentNullException("type");

            var req = new DocumentRequest(target)
            {
                Method = HttpMethod.Post,
                Body = body,
                MimeType = type,
                Referer = referer,
                Source = source
            };
            if (customHeaders != null)
            {
                req.CustomHeaders = customHeaders;
            }
            return req;
        }

        /// <summary>
        /// Creates a POST request for the given target with the fields being
        /// used to generate the body and encoding type plaintext.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="fields">The fields to send.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest PostAsPlaintext(Url target, IDictionary<String, String> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            var fds = new FormDataSet();

            foreach (var field in fields)
                fds.Append(field.Key, field.Value, InputTypeNames.Text);

            return Post(target, fds.AsPlaintext(), MimeTypes.Plain);
        }

        /// <summary>
        /// Creates a POST request for the given target with the fields being
        /// used to generate the body and encoding type url encoded.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="fields">The fields to send.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest PostAsUrlencoded(Url target, IDictionary<String, String> fields,
            Dictionary<string, string> customHeaders = null)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            var fds = new FormDataSet();

            foreach (var field in fields)
                fds.Append(field.Key, field.Value, InputTypeNames.Text);

            return Post(target, fds.AsUrlEncoded(), MimeTypes.UrlencodedForm,
                customHeaders: customHeaders);
        }

        /// <summary>
        /// Creates a POST request for the given target with the fields being
        /// used to generate the body and encoding type multipart.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="fields">The fields to send.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest PostAsMultipart(Url target, IDictionary<String, object> fields,
            Dictionary<string, string> customHeaders = null)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            var fds = new FormDataSet();

            foreach (var field in fields)
            {
                if (field.Value is AngleSharp.Dom.Io.IFile)
                    fds.Append(field.Key, (AngleSharp.Dom.Io.IFile)field.Value, InputTypeNames.File);
                else
                    fds.Append(field.Key, (String)field.Value, InputTypeNames.Text);
            }

            var enctype = String.Concat(MimeTypes.MultipartForm, "; boundary=", fds.Boundary);
            return Post(target, fds.AsMultipart(), enctype,
                customHeaders: customHeaders);
        }

        /// <summary>
        /// Creates a DELETE request for the given target with the provided body
        /// and encoding type from the optional source node and optional
        /// referer string.
        /// </summary>
        /// <param name="target">The target to use.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="type">The type of the request's body.</param>
        /// <param name="source">The optional source of the request.</param>
        /// <param name="referer">The optional referrer string.</param>
        /// <returns>The new document request.</returns>
        public static DocumentRequest Delete(Url target, INode source = null, String referer = null,
            Dictionary<string, string> customHeaders = null)
        {
            var req = new DocumentRequest(target)
            {
                Method = HttpMethod.Delete,
                Referer = referer,
                Source = source
            };
            if (customHeaders != null)
            {
                req.CustomHeaders = customHeaders;
            }
            return req;
        }

        /// <summary>
        /// Gets or sets the source of the request, if any.
        /// </summary>
        public INode Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the target of the request.
        /// </summary>
        public Url Target
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the referrer of the request, if any. The name is
        /// intentionally spelled wrong, to emphasize the relationship with the
        /// HTTP header.
        /// </summary>
        public String Referer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the method to use.
        /// </summary>
        public HttpMethod Method
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the stream of the request's body.
        /// </summary>
        public Stream Body
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the mime-type to use, if any.
        /// </summary>
        public String MimeType
        {
            get;
            set;
        }

        public Dictionary<String, String> CustomHeaders
        {
            get;
            set;
        }
    }
}
