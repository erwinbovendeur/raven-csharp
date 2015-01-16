using System;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using Newtonsoft.Json;
using SharpRaven.Data;
using SharpRaven.Utilities;
using UIKit;

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

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Task.Run(() => SendSaveExceptions());
        }

        private const string folderName = "Sentry";

        private async static void SendSaveExceptions()
        {
            if (Reachability.InternetConnectionStatus() == NetworkStatus.NotReachable)
                return; // No network, don't send

            var store = UIDevice.CurrentDevice.CheckSystemVersion(8, 0)
                ? NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
                    [0].ToString()
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string folder = Path.Combine(store, folderName);
            if (!Directory.Exists(folder)) return;

            string[] exceptions = Directory.GetFiles(string.Format("*.json"));
            if (exceptions.Length == 0) return;

            RavenClient cl = new RavenClient(Dsn);

            foreach (var exception in exceptions)
            {
                // Send the exception now
                try
                {
                    // Deserialize? that seems like a waste...
                    var s = File.ReadAllText(exception);
                    await cl.Send(JsonConvert.DeserializeObject<SentryRequest>(s));

                    File.Delete(exception);
                }
                catch
                {
                    // Gracefully handle the exception? We should keep track of the amount of times we tried to upload this file.
                    // If it fails more than twice, just delete it.
                    if (exception.EndsWith(".1.json")) // Already tried this file before
                    {
                        File.Delete(exception);
                    }
                    else
                    {
                        string newname = exception.Replace(".json", ".1.json");
                        File.Move(exception, newname);
                    }
                }
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                // Store the exception somewhere, and send it later
                RavenClient cl = new RavenClient(Dsn);
                var request = cl.SentryRequestFactory.Create(Dsn.ProjectID, (Exception) args.ExceptionObject);

                // Store to local file
                var store = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string folder = Path.Combine(store, folderName);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filename = Path.Combine(folder, string.Concat(Guid.NewGuid().ToString(), ".json"));

                using (var s = new StreamWriter(filename))
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
