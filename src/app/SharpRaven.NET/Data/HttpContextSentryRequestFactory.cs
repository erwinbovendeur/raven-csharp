using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

namespace SharpRaven.Data
{
    /// <summary>
    /// A <see cref="ISentryRequestFactory"/> which decorates another <see cref="ISentryRequestFactory"/> appending HttpContext information
    /// </summary>
    public class HttpContextSentryRequestFactory : ISentryRequestFactory
    {
        private readonly ISentryRequestFactory _sentryRequestFactory;

        /// <summary>
        /// Gets or sets the HTTP context.
        /// </summary>
        /// <value>
        /// The HTTP context.
        /// </value>
        internal static dynamic HttpContext { get; set; }

        /// <summary>
        /// Whether or not the HttpContext is valid
        /// </summary>
        private static bool HasHttpContext
        {
            get { return HttpContext != null; }
        }

        /// <summary>
        /// Construct the <see cref="HttpContextSentryRequestFactory"/>
        /// </summary>
        /// <param name="sentryRequestFactory">The super <see cref="ISentryRequestFactory"/> which this class decorates</param>
        public HttpContextSentryRequestFactory(ISentryRequestFactory sentryRequestFactory)
        {
            _sentryRequestFactory = sentryRequestFactory;
        }

        /// <summary>
        /// Creates a new instance of
        /// <see cref="SentryRequest" /> for the specified
        /// <paramref name="project" />.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="message">The message to capture.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="message" />. Default <see cref="ErrorLevel.Info" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="message" />.</param>
        /// <returns>
        /// A new instance of <see cref="SentryRequest" /> for the specified <paramref name="project" />.
        /// </returns>
        public SentryRequest Create(
            string project, 
            SentryMessage message, 
            ErrorLevel level = ErrorLevel.Info, 
            IDictionary<string, string> tags = null,
            object extra = null)
        {
            return AddHttpContext(_sentryRequestFactory.Create(project, message, level, tags, extra));
        }

        /// <summary>
        /// Creates a new instance of
        /// <see cref="SentryRequest" /> for the specified
        /// <paramref name="project" />, with the
        /// given
        /// <paramref name="exception" />.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="exception">The <see cref="Exception" /> to capture.</param>
        /// <param name="message">The optional messge to capture. Default: <see cref="Exception.Message" />.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="exception" />. Default: <see cref="ErrorLevel.Error" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="exception" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="exception" />.</param>
        /// <returns>
        /// A new instance of
        /// <see cref="SentryRequest" /> for the specified
        /// <paramref name="project" />, with the
        /// given
        /// <paramref name="exception" />.
        /// </returns>
        public SentryRequest Create(
            string project, 
            Exception exception, 
            SentryMessage message = null, 
            ErrorLevel level = ErrorLevel.Error,
            IDictionary<string, string> tags = null, 
            object extra = null)
        {
            return AddHttpContext(_sentryRequestFactory.Create(project, exception, message, level, tags, extra));
        }


        private static SentryRequest AddHttpContext(SentryRequest request)
        {
            // NOTE: We're using dynamic to not require a reference to System.Web.
            GetHttpContext();

            if (HasHttpContext)
            {
                request.Request = new SentryHttpRequest
                {
                    Url = HttpContext.Request.Url.ToString(),
                    Method = HttpContext.Request.HttpMethod,
                    Environment = Convert(x => x.Request.ServerVariables),
                    Headers = Convert(x => x.Request.Headers),
                    Cookies = Convert(x => x.Request.Cookies),
                    Data = Convert(x => x.Request.Form),
                    QueryString = HttpContext.Request.QueryString.ToString(),
                };

                request.User = new SentryUser(GetPrincipal())
                {
                    IpAddress = GetIpAddress()
                };
            }

            return request;
        }

        private static IDictionary<string, string> Convert(Func<dynamic, NameObjectCollectionBase> collectionGetter)
        {
            if (!HasHttpContext)
                return null;

            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            try
            {
                var collection = collectionGetter.Invoke(HttpContext);
                var keys = Enumerable.ToArray(collection.AllKeys);

                foreach (object key in keys)
                {
                    if (key == null)
                        continue;

                    string stringKey = key as string ?? key.ToString();

                    // NOTE: Ignore these keys as they just add duplicate information. [asbjornu]
                    if (stringKey.StartsWith("ALL_") || stringKey.StartsWith("HTTP_"))
                        continue;

                    var value = collection[stringKey];
                    string stringValue = value as string;

                    if (stringValue != null)
                    {
                        // Most dictionary values will be strings and go through this path.
                        dictionary.Add(stringKey, stringValue);
                    }
                    else
                    {
                        // HttpCookieCollection is an ugly, evil beast that needs to be treated with a sledgehammer.

                        try
                        {
                            // For whatever stupid reason, HttpCookie.ToString() doesn't return its Value, so we need to dive into the .Value property like this.
                            dictionary.Add(stringKey, value.Value);
                        }
                        catch (Exception exception)
                        {
                            dictionary.Add(stringKey, exception.ToString());
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return dictionary;
        }


        private static void GetHttpContext()
        {
            if (HasHttpContext)
                return;

            try
            {
                var systemWeb = AppDomain.CurrentDomain
                                         .GetAssemblies()
                                         .FirstOrDefault(assembly => assembly.FullName.StartsWith("System.Web,"));

                if (HasHttpContext || systemWeb == null)
                    return;

                var httpContextType = systemWeb.GetExportedTypes()
                                               .FirstOrDefault(type => type.Name == "HttpContext");

                if (HasHttpContext || httpContextType == null)
                    return;

                var currentHttpContextProperty = httpContextType.GetProperty("Current",
                                                                             BindingFlags.Static | BindingFlags.Public);

                if (HasHttpContext || currentHttpContextProperty == null)
                    return;

                HttpContext = currentHttpContextProperty.GetValue(null, null);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An error occurred while retrieving the HTTP context: {0}", exception);
            }
        }


        private static dynamic GetIpAddress()
        {
            try
            {
                return HttpContext.Request.UserHostAddress;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return null;
        }


        private static IPrincipal GetPrincipal()
        {
            try
            {
                return HttpContext.User as IPrincipal;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return null;
        }
    }
}
