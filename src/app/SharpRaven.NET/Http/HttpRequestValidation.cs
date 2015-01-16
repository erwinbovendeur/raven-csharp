using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;

namespace SharpRaven.Http
{
    internal static class HttpRequestValidation
    {
        /// <summary>
        /// Returns unvalidated collections if build targets .NET Framework
        /// 4.0 or later and if caller is hosted at run-time (based on value
        /// of <see cref="HostingEnvironment.IsHosted"/>). In all other 
        /// cases, collections returned are validated ones from
        /// <see cref="HttpRequestBase.Form"/> and 
        /// <see cref="HttpRequestBase.QueryString"/> and therefore
        /// could raise <see cref="HttpRequestValidationException"/>.
        /// </summary>

        internal static T TryGetUnvalidatedCollections<T>(this HttpRequestBase request,
            Func<NameValueCollection, NameValueCollection, HttpCookieCollection, T> resultor)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (resultor == null) throw new ArgumentNullException("resultor");

            var form = request.Unvalidated.Form;
            var queryString = request.Unvalidated.QueryString;
            var cookies = request.Unvalidated.Cookies;

            return resultor(form ?? request.Form, queryString ?? request.QueryString, cookies ?? request.Cookies);
        }
    }
}
