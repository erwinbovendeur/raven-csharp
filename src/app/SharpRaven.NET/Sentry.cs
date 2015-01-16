using System;
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
        }

        public static void CaptureException(Exception ex)
        {
            RavenClient cl = new RavenClient(Dsn);
            cl.CaptureException(ex);
        }
    }
}
