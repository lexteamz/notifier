using Discord;
using Discord.Interactions;

namespace Notifier.Classes
{
    public class CommandsModule : InteractionModuleBase<SocketInteractionContext>
    {

        private async Task Reply(string content) => await RespondAsync(content);

        public static async Task<bool> CheckAdminAsync(SocketInteractionContext x)
        {
            if (!Permissions.IsBotAdminstrator(x.User))
            {
                await x.Interaction.RespondAsync($"You're not an Administrator of {Globals.Get("AppName")} Status. SCAM :100: :bangbang:");
                return false;
            }
            return true;
        }

        // Administrator Only Commands
        [SlashCommand("sendupdate", $"Post a \"AppName\" update/patch embed")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task LInjectorUpdate(string message, string version, string type)
        {
            if (!await CheckAdminAsync(Context)) return;
            string ver = version == "current" ? RBX.GetCurrent() : version;
            await Program.FireUnpatch(message, ver, type);
        }

        [SlashCommand("print", "Log a message to console")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task PrintAsync(string message)
        {
            if (!await CheckAdminAsync(Context)) return;
            Logger.Print(Logger.Regular, message);
            await RespondAsync("Printed.");
        }


        [SlashCommand("getlogs", "Send current logs to your DMs")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetLogsAsync()
        {
            if (!await CheckAdminAsync(Context)) return;
            var dmChannel = await Context.User.CreateDMChannelAsync();
            await dmChannel.SendFileAsync(RBX._logfile, "Current Log File.");
            await RespondAsync("Sent to DMs!");
        }

        [SlashCommand("manualset", "Write to current/previous saves file")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ManualSetAsync(string type, string content)
        {
            if (!await CheckAdminAsync(Context)) return;
            try
            {
                switch (type.ToLower())
                {
                    case "current":
                        await File.WriteAllTextAsync(RBX._current, content);
                        break;
                    case "previous":
                        await File.WriteAllTextAsync(RBX._previous, content);
                        break;
                    default:
                        await RespondAsync("Invalid type. Use `current` or `previous`.");
                        return;
                }
                await RespondAsync($"Successfully written `{content}` to `saves/{type}` file.");
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, ex.Message);
            }
        }

        [SlashCommand("fetchhistory", "Fetch Roblox version history")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task FetchHistoryAsync()
        {
            if (!await CheckAdminAsync(Context)) return;
            await RBX.FetchHistory();
            await RespondAsync("History fetched.");
        }

        [SlashCommand("refetch", "Refresh the timer for the next version check")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RefetchAsync()
        {
            if (!await CheckAdminAsync(Context)) return;
            await RBX.CheckDifferentAsync();
            await RespondAsync("Timer refetched.");
        }


        // Application Public Commands
        [SlashCommand("uptime", "Show bot uptime")]
        public async Task UptimeAsync() => await Reply($"The Application is running since <t:{Program.bot_started}>");

        [SlashCommand("version", "Show current Roblox version")]
        public async Task VersionAsync() => await ReplyVersionAsync();

        private async Task ReplyVersionAsync()
        {
            var version = RBX.GetCurrent();
            await Reply($"The current Roblox Version for (WindowsPlayer, LIVE) is `{version}`");
        }

        [SlashCommand("history", "Show latest 10 Roblox versions")]
        public async Task HistoryAsync()
        {
            try
            {
                if (RBX._cached_history == null || RBX._cached_history.Length == 0)
                    await RBX.FetchHistory();

                var history = RBX._cached_history;
                if (history == null || history.Length == 0)
                {
                    await Reply("No history data available.");
                    return;
                }

                var response = $"The latest 10 Roblox Versions for WindowsPlayer:\n```{string.Join("\n", history)}```";
                await Reply(response);
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, ex.Message);
            }
        }


        [SlashCommand("previous", "Show previous Roblox version")]
        public async Task PreviousAsync()
        {
            var previousVersion = RBX.GetPrevious();
            if (string.IsNullOrEmpty(previousVersion))
                await Reply($"`previous` file content is null. However, I'm going to ping <@{Globals.GetAs<ulong>("HostUserId")}> to fix this.\n");
            else
                await Reply($"The previous Roblox Version for (WindowsPlayer, LIVE) is: `{previousVersion}`");
        }

        [SlashCommand("help", "Show available commands")]
        public async Task HelpAsync()
        {
            string message =
            @"```
/help     - Displays this message.
/previous - Replies with the previous Roblox Version.
/version  - Replies the current Roblox Version.
/history  - Replies with the latest ten Roblox Versions.
/uptime   - Replies with the bot's uptime in Unix Time.```
";
            await Reply(message);
        }
    }
}