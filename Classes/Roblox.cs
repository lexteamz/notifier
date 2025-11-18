using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace Notifier
{
    public class RBX
    {
        public static string _savespath = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty), "saves");
        public static string _current = Path.Combine(_savespath, "current");
        public static string _logfile = Path.Combine(_savespath, "log.txt");
        public static string _previous = Path.Combine(_savespath, "previous");
        public static string _livejson = "https://clientsettings.roblox.com/v2/client-version/WindowsPlayer/channel/LIVE";
        public static string[]? _cached_history;

        public static HttpClient httpClient = new();
        private static Timer? _timer;

        public struct Version
        {
            [JsonProperty("version")]
            public string VersionNumber { get; set; }

            [JsonProperty("clientVersionUpload")]
            public string ClientVersionUpload { get; set; }

            [JsonProperty("bootstrapperVersion")]
            public string BootstrapperVersion { get; set; }
        }

        public enum Types
        {
            Yes = 1,
            No = 2,
            Error = 3,
            Revert = 4
        }

        public static async Task<Types> CheckDifferentAsync()
        {

            EnsureDirectoryExists();
            GetCurrent();
            GetPrevious();

            string _livecontent;
            try
            {
                _livecontent = await httpClient.GetStringAsync(_livejson);
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, $"Couldn't fetch live content: {ex.Message} at {ex.StackTrace}");
                return Types.Error;
            }

            if (string.IsNullOrEmpty(_livecontent))
                return Types.Error;

            Version jsonResponse;
            try
            {
                jsonResponse = JsonConvert.DeserializeObject<Version>(_livecontent);
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, $"JSON not deserialized: {ex.Message} at {ex.StackTrace}");
                return Types.Error;
            }

            string clientVersionUpload = jsonResponse.ClientVersionUpload;
            string currentVersion = GetCurrent();
            string previousVersion = GetPrevious();

            if (clientVersionUpload != currentVersion)
            {
                if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrWhiteSpace(currentVersion))
                {
                    File.WriteAllText(_current, clientVersionUpload);
                    Logger.Print(Logger.Info, $"Written latest ({clientVersionUpload}) to file ({_current}), no content was detected.)");
                }

                if (string.IsNullOrEmpty(previousVersion) || string.IsNullOrWhiteSpace(previousVersion))
                {
                    Logger.Print(Logger.Warning, "No previous version content was detected, returning default (Types.No).");
                    return Types.No;
                }

                if (clientVersionUpload == previousVersion)
                {
                    Logger.Print(Logger.Update, $"Version reverted to: {clientVersionUpload}");
                    await Program.Fire(jsonResponse, Types.Revert);
                }
                else
                {
                    Logger.Print(Logger.Update, $"Update detected: {clientVersionUpload}");
                    await Program.Fire(jsonResponse, Types.Yes);
                }

                File.WriteAllText(_previous, currentVersion);
                File.WriteAllText(_current, clientVersionUpload);

                _cached_history = await GetHistory();

                return Types.Yes;
            }

            return Types.No;
        }

        public static void InitializeTimer()
        {
            _timer = new Timer(TimeSpan.FromMinutes(1));
            _timer.Elapsed += async (sender, e) => await CheckDifferentAsync();
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public static string GetCurrent()
        {
            EnsureDirectoryExists();

            if (File.Exists(_current))
            {
                return File.ReadAllText(_current);
            }
            else
            {
                File.WriteAllText(_current, string.Empty);
                return string.Empty;
            }
        }

        public static string GetPrevious()
        {
            EnsureDirectoryExists();

            if (File.Exists(_previous))
            {
                return File.ReadAllText(_previous);
            }
            else
            {
                File.WriteAllText(_previous, string.Empty);
                return string.Empty;
            }
        }

        public static async Task<string[]> GetHistory()
        {
            string history = string.Empty;
            history = await httpClient.GetStringAsync("http://setup.roblox.com/DeployHistory.txt");

            var lines = history.Split(["\r\n", "\n"], StringSplitOptions.None);

            var filteredLines = lines
                .Where(line => line.Contains("New WindowsPlayer"))
                .ToList();

            var lastThreeLines = filteredLines
                .Reverse<string>()
                .Take(10)
                .ToArray();

            string[] versionsWithTimestamps = lastThreeLines
                .Select(line =>
                {
                    var match = Regex.Match(line, @"New WindowsPlayer (version-\S+) at ([\d/ :AMP]+)");
                    return match.Success ? $"{match.Groups[1].Value} - {match.Groups[2].Value}" : string.Empty;
                })
                .Where(result => !string.IsNullOrEmpty(result))
                .ToArray();

            return versionsWithTimestamps;
        }

        public static async Task FetchHistory() => _cached_history = await GetHistory();

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_savespath))
            {
                Directory.CreateDirectory(_savespath);
            }
        }
    }
}