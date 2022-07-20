using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public static class Updates
    {
        public static event EventHandler<UpdateFoundEventArgs> UpdateFound;

        public static void CheckForUpdate(VersionNumber currentVersion)
        {
            Task.Run(() =>
            {
                try
                {
                    var request = (HttpWebRequest) WebRequest.Create("https://api.github.com/repos/vileelf/chatterino/releases/latest");
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    request.UserAgent = "Chatterino";
                    using (var response = request.GetResponse()) {
                        using (var stream = response.GetResponseStream())
                        {
                            var parser = new JsonParser();

                            dynamic json = parser.Parse(stream);
                            string tagname = json["tag_name"];
                            dynamic assets = json["assets"];
                            
                            foreach (var asset in assets) {
                                if (asset["content_type"] == "application/x-zip-compressed") {
                                    VersionNumber onlineVersion = VersionNumber.Parse(tagname);
                                    
                                    string url = asset["browser_download_url"];

                                    if (onlineVersion.IsNewerThan(currentVersion))
                                    {
                                        UpdateFound?.Invoke(null, new UpdateFoundEventArgs(onlineVersion, url));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    log(exc.ToString());
                }
            });
        }
        
        private static object logLock = new object();
        private static void log(string text)
        {
            #if DEBUG
                bool debug = true;
            #else
                bool debug = false;
            #endif
            if (debug)
            {
                lock (logLock) {
                    string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    StreamWriter file = new StreamWriter(folder + @"\updatelog.txt", true);
                    file.WriteLine(text);
                    file.Close();
                }
            }
        }
    }

    public class UpdateFoundEventArgs : EventArgs
    {
        public VersionNumber Version { get; private set; }
        public string Url { get; private set; }

        public UpdateFoundEventArgs(VersionNumber version, string url)
        {
            Version = version;
            Url = url;
        }
    }
}
