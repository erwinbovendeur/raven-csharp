using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
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

            Application.Current.UnhandledException += OnUnhandledException;
            Task.Run(() => SendSaveExceptions());
        }

        private const string filePrefix = "_sr_";

        private async static void SendSaveExceptions()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) // No network, don't send
                return;

            var store = IsolatedStorageFile.GetUserStoreForApplication();

            string[] exceptions = store.GetFileNames(string.Format("{0}*.json", filePrefix));
            if (exceptions.Length == 0) return;

            RavenClient cl = new RavenClient(Dsn);

            foreach (var exception in exceptions)
            {
                // Send the exception now
                try
                {
                    // Deserialize? that seems like a waste...
                    using (var sr = new StreamReader(store.OpenFile(exception, FileMode.Open)))
                    {
                        var s = await sr.ReadToEndAsync();
                        await cl.Send(JsonConvert.DeserializeObject<SentryRequest>(s));
                    }
                    store.DeleteFile(exception);
                }
                catch
                {
                    // Gracefully handle the exception? We should keep track of the amount of times we tried to upload this file.
                    // If it fails more than twice, just delete it.
                    if (exception.EndsWith(".1.json")) // Already tried this file before
                    {
                        store.DeleteFile(exception);
                    }
                    else
                    {
                        string newname = exception.Replace(".json", ".1.json");
                        store.MoveFile(exception, newname);
                    }
                }
            }
        }

        private static void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                // Store the exception somewhere, and send it later
                RavenClient cl = new RavenClient(Dsn);
                var request = cl.SentryRequestFactory.Create(Dsn.ProjectID, args.ExceptionObject);

                string filename = string.Concat(filePrefix, Guid.NewGuid().ToString(), ".json");
                var store = IsolatedStorageFile.GetUserStoreForApplication();

                using (var s = new StreamWriter(store.CreateFile(filename)))
                {
                    var json = JsonConvert.SerializeObject(request);
                    s.Write(json);
                }
            }
            catch
            {
                // We don't want any exceptions here
            }
        }

        public static void CaptureException(Exception ex)
        {
            RavenClient cl = new RavenClient(Dsn);
            cl.CaptureException(ex);
        }
    }
}
