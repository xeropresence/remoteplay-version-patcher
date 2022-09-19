using Ressy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ressy.HighLevel.Versions;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace remoteplay_version_patcher
{
    internal class Program
    {
        internal class SonyResponse
        {
            public string checksum;
            public string uri;
            public Version version;
        }

        private static string FindRemotePlay()
        {
            var baseKey = Environment.Is64BitOperatingSystem ?
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" :
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";

            using (var keys = Registry.LocalMachine.OpenSubKey(baseKey))
            {
                var remotePlayKey = keys?.GetSubKeyNames()
                    .Select(name => keys.OpenSubKey(name))
                    .FirstOrDefault(key => key.GetValue("DisplayName", "").ToString().Contains("PS Remote Play") &&
                                           key.GetValue("Publisher", "").ToString().Contains("Sony"));
                var path = Path.Combine(remotePlayKey?.GetValue("InstallLocation", null)?.ToString() ?? string.Empty, "RemotePlay.exe");
                return File.Exists(path) ? path : null;
            }
        }


        private static HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            var file = "RemotePlay.exe";

            if (!File.Exists(file))
            {
                file = FindRemotePlay();
                if (!File.Exists(file))
                {
                    Console.WriteLine("Cannot find Remoteplay.exe via the registry");
                    Console.WriteLine("Place RemotePlay.exe inside the same folder as this application");
                    Console.ReadKey();
                    return;
                }
            }

            SonyResponse result;
            try
            {
                var response = await client.GetAsync($"https://remoteplay.dl.playstation.net/remoteplay/module/win/rp-version-win.json");

                var responseString = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<SonyResponse>(responseString);

            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't fetch data from sony server");
                Console.WriteLine(e);
                Console.ReadKey();
                return;
            }
       


            var portableExecutable = new PortableExecutable(file);


            var versionInfo = portableExecutable.GetVersionInfo();
            

            Console.WriteLine($"Patching file version from {versionInfo.FileVersion} to {result.version}");
            Console.WriteLine($"Patching product version from {versionInfo.ProductVersion} to {result.version}");


            portableExecutable.SetVersionInfo(v => v
                .SetFileVersion(result.version)
                .SetProductVersion(result.version)
            );

            Console.WriteLine("Patching complete, press any key to continue...");
            Console.ReadKey();
        }
    }
}
