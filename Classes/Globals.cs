using Microsoft.VisualBasic;

namespace Notifier
{
    public static class Globals
    {
        private static string appRoot = Path.GetDirectoryName(Environment.ProcessPath)!;

        private static readonly Dictionary<string, dynamic> values = new()
        {
            { "AppName", $"This app was made by Excel@LEx Softworks!!\n${Globals.Get("DiscordServerURL")}" },
            { "AppRoot", appRoot },

            { "DiscordServerURL", "https://discord.com/invite/NQY28YSVAb" },
            { "GitHubUsername", "lexteamz" },
            { "GitHubRepoName", "Notifier" },
            { "GitHubURL", "https://github.com/lexteamz/Notifier" },


            // Remember to update those!!!
            { "HostUserId", 0000000000000000000 },

            { "GuildId", 0000000000000000000  },
            { "UpdatesChannelIdText", 0000000000000000000 },
            { "RobloxVersionIdVoice", 0000000000000000000 },
            { "StatusChannelIdVoice", 0000000000000000000 },

            // Must be in Markdown format!!!
            { "AppUpdatesRole", "<@&0000000000000000000>" },
            { "RobloxUpdatesRoleId", "<@&0000000000000000000>" },
            { "AppEmojiUrl", "https://cdn.discordapp.com/emojis/1165753412009328713.webp?size=96&quality=lossless"} // for the footer
        };

        public static dynamic? Get(string key) => values.TryGetValue(key, out var value) ? value : null;

        public static T GetAs<T>(string key)
        {
            if (!values.TryGetValue(key, out var value))
                return default!;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default!;
            }
        }

        public static void Add(string key, string value)
        {
            if (!values.ContainsKey(key))
                values[key] = value;
            else
                return;
        }

        public static void Update(string key, string value)
        {
            if (values.ContainsKey(key))
                values[key] = value;
            else
                return;
        }
    }
}