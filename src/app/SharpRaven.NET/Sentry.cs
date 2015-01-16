using System;
using System.Web;
using SharpRaven.Data;
using SharpRaven.Utilities;

namespace SharpRaven
{
    public static class Sentry
    {
        public static Dsn Dsn { get; set; }

        public static void Init()
        {
            SystemUtilFactory.Instance = new SystemUtil();
            GzipUtilFactory.Instance = new GzipUtil();
            SentryRequestFactoryFactory.Instance = new SentryRequestFactory();

            if (HttpContext.Current == null) // Only do this when there is no Web context, with a web context, use the SentryHttpModule
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
                CaptureException(ex);
        }

        public static void CaptureException(Exception ex)
        {
            RavenClient cl = new RavenClient(Dsn);
            cl.CaptureException(ex);
        }
    }
}
