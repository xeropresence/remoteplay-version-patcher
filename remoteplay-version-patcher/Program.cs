using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using Ressy;
using Ressy.Versions;

namespace remoteplay_version_patcher
{
    internal class SonyResponse
    {
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("version")]
        public Version? Version { get; set; }
    }

    // Source-generated serialization: reflection-based JSON is not available under NativeAOT.
    [JsonSerializable(typeof(SonyResponse))]
    internal partial class SonyJsonContext : JsonSerializerContext;

    internal class Program
    {
        private const string VersionUrl =
            "https://remoteplay.dl.playstation.net/remoteplay/module/win/rp-version-win.json";

        private static readonly HttpClient Client = new();

        private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        // Ask for the registry view explicitly instead of spelling out WOW6432Node: WOW64
        // already redirects a 32-bit process into that node, so naming it by hand resolves
        // to WOW6432Node\WOW6432Node and finds nothing. Both views are searched so this
        // works regardless of the bitness of either the patcher or the Remote Play install.
        private static string? FindRemotePlay()
        {
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstall = hklm.OpenSubKey(UninstallKey);
                if (uninstall is null)
                    continue;

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var key = uninstall.OpenSubKey(name);
                    if (key is null)
                        continue;

                    var displayName = key.GetValue("DisplayName") as string ?? "";
                    var publisher = key.GetValue("Publisher") as string ?? "";

                    if (!displayName.Contains("PS Remote Play") || !publisher.Contains("Sony"))
                        continue;

                    if (key.GetValue("InstallLocation") is not string installLocation)
                        continue;

                    var path = Path.Combine(installLocation, "RemotePlay.exe");
                    if (File.Exists(path))
                        return path;
                }
            }

            return null;
        }

        private static async Task Main()
        {
            var file = "RemotePlay.exe";

            if (!File.Exists(file))
            {
                file = FindRemotePlay();
                if (file is null)
                {
                    Console.WriteLine("Cannot find Remoteplay.exe via the registry");
                    Console.WriteLine("Place RemotePlay.exe inside the same folder as this application");
                    Console.ReadKey();
                    return;
                }
            }

            SonyResponse? result;
            try
            {
                using var response = await Client.GetAsync(VersionUrl);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync();
                result = await JsonSerializer.DeserializeAsync(responseStream, SonyJsonContext.Default.SonyResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't fetch data from sony server");
                Console.WriteLine(e);
                Console.ReadKey();
                return;
            }

            if (result?.Version is null)
            {
                Console.WriteLine("Sony server response did not include a valid version");
                Console.ReadKey();
                return;
            }

            using var portableExecutable = PortableExecutable.OpenWrite(file);

            var versionInfo = portableExecutable.TryGetVersionInfo();

            Console.WriteLine($"Patching file version from {versionInfo?.FileVersion} to {result.Version}");
            Console.WriteLine($"Patching product version from {versionInfo?.ProductVersion} to {result.Version}");

            portableExecutable.SetVersionInfo(v => v
                .SetFileVersion(result.Version)
                .SetProductVersion(result.Version)
            );

            Console.WriteLine("Patching complete, press any key to continue...");
            Console.ReadKey();
        }
    }
}
