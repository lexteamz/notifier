using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace Notifier.Classes
{
    public static class WarpInterface
    {
        public static async void InitWarp(bool enabled)
        {
            if (!enabled)
                return;

            if (!IsWarpInstalled())
            {
                Logger.Print(Logger.Error, "Cloudflare WARP was not detected in your system. Try downloading it from https://one.one.one.one and then reboot your machine.");
                return;
            }
            else
            {
                Logger.Print(Logger.Ok, "Cloudflare WARP is installed.");
                await ConnectWarpAsync();

                int ipApiAttempts = 0;
                dynamic? parsed = null;
                while (parsed == null && ipApiAttempts < 3)
                {
                    try
                    {
                        string response = await RBX.httpClient.GetStringAsync("https://api.ipquery.io/?format=json");
                        parsed = JsonConvert.DeserializeObject<dynamic>(response);

                        if (parsed != null)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Print(Logger.Warning, $"Attempt {ipApiAttempts + 1}: Failed to fetch IP details. Error: {ex.Message}");
                    }

                    ipApiAttempts++;
                    Thread.Sleep(3000);
                }

                if (parsed == null)
                    Logger.Print(Logger.Warning, "Couldn't fetch from ipquery, your IP could be not protected!");

                string formatted = JsonConvert.SerializeObject(parsed, Formatting.Indented);

                Logger.Print(Logger.Info, $"IP Data: https://api.ipquery.io/?format=json\n{string.Concat(Enumerable.Repeat("-", 50))}\n{formatted}\n{string.Concat(Enumerable.Repeat("-", 50))}");
            }
        }

        public static string ExecuteScript(string script, string args)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Script path cannot be null or empty.", nameof(script));

            var output = new StringBuilder();

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = script,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();

                    output.Append(process.StandardOutput.ReadToEnd());
                    process.WaitForExit();
                }

                Logger.Print(Logger.Verbose, $"Executed: {script} {args} : {output.ToString().Replace("\n", " | ")}");
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, $"Error executing script '{script}': {ex.Message}");
                throw;
            }

            return output.ToString();
        }

        public static bool CommandExistsSafely(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("The command cannot be empty or null.", nameof(command));

            string checker = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = checker,
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, $"Error checking command '{command}': {ex.Message}");
                return false;
            }
        }

        public static bool IsWarpConnected() =>
            ExecuteScript("warp-cli", "status").Contains("Status update: Connected");

        public static bool IsWarpInstalled() => CommandExistsSafely("warp-cli");

        public static async Task<bool> DisposeWarpAsync()
        {
            if (IsWarpConnected())
                ExecuteScript("warp-cli", "disconnect");

            await Task.Delay(5000);
            return true;
        }

        public static async Task<bool> ConnectWarpAsync()
        {
            if (!IsWarpConnected())
                ExecuteScript("warp-cli", "connect");

            await Task.Delay(1000);
            return true;
        }
    }
}
