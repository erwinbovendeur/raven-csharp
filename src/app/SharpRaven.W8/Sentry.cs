using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
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

        private const string folderName = "Sentry";

        private async static void SendSaveExceptions()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) // No network, don't send
                return;

            var store = ApplicationData.Current.LocalFolder;

            var folder = await store.GetFolderAsync(folderName);
            if (folder == null) return;

            var exceptions = await folder.GetFilesAsync();
            if (!exceptions.Any()) return;

            RavenClient cl = new RavenClient(Dsn);

            foreach (var exception in exceptions)
            {
                bool failure = false;
                // Send the exception now
                try
                {
                    using (var sr = new StreamReader(await exception.OpenStreamForReadAsync()))
                    {
                        var s = await sr.ReadToEndAsync();
                        await cl.Send(JsonConvert.DeserializeObject<SentryRequest>(s));
                    }
                    await exception.DeleteAsync();
                }
                catch
                {
                    failure = true;
                }

                if (!failure) continue;

                // Gracefully handle the exception? We should keep track of the amount of times we tried to upload this file.
                // If it fails more than twice, just delete it.
                if (exception.Name.EndsWith(".1.json")) // Already tried this file before
                {
                    await exception.DeleteAsync();
                }
                else
                {
                    string newname = exception.Name.Replace(".json", ".1.json");
                    exception.MoveAsync(folder, newname, NameCollisionOption.ReplaceExisting);
                }
            }
        }

        private async static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                // Store the exception somewhere, and send it later
                RavenClient cl = new RavenClient(Dsn);
                var request = cl.SentryRequestFactory.Create(Dsn.ProjectID, args.Exception);

                var store = ApplicationData.Current.LocalFolder;
                var folder = await store.GetFolderAsync(folderName) ?? await store.CreateFolderAsync(folderName);

                string filename = string.Concat(Guid.NewGuid().ToString(), ".json");

                var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                using (var s = new StreamWriter(await file.OpenStreamForWriteAsync()))
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
