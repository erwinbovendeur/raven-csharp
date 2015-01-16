using System;
using System.Web;
using SharpRaven.Data;

namespace SharpRaven.Http
{
    public class SentryHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.Error += OnError;
        }

        private RavenClient _client;

        private async void OnError(object sender, EventArgs e)
        {
            var app = (HttpApplication) sender;
            var ex = app.Server.GetLastError();

            if (_client == null || _client.CurrentDsn != Sentry.Dsn)
                _client = new RavenClient(Sentry.Dsn);

            var error = new Error(ex, new HttpContextWrapper(app.Context));

            var request = SentryRequestFactoryFactory.Instance.Create(error.ApplicationName, error.Exception);
            request.User = new SentryUser(error.User);
            request.Culprit = error.Source;
            request.Logger = "SentryHttpModule";
            request.Request = new SentryHttpRequest()
            {
                Cookies = error.Cookies.ToDictionary(),
                Environment = error.ServerVariables.ToDictionary(),
                Method = app.Context.Request.HttpMethod,
                Data = error.Form,
                Url = app.Context.Request.RawUrl,
                QueryString = error.RawQueryString,
                Headers = app.Request.Headers.ToDictionary()
            };

            await _client.Send(request);
        }

        public void Dispose()
        {
        }
    }
}