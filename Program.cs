using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Notifier.Classes;
using Timer = System.Timers.Timer;

namespace Notifier
{
    internal class Program
    {

        private static DiscordSocketClient? _client;
        private static IServiceProvider? _services;
        private static Timer? _statustimer;

        
        static bool useWarp = false;
        public static long bot_started = long.MinValue;

        static async void ProcessExitHandler(object sender, dynamic e)
        {
            if (useWarp) {
                Logger.Print(Logger.Verbose, "Disposing Cloudflare WARP.");
                await WarpInterface.DisposeWarpAsync();
            }
        }

        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-w":
                    case "--warp":
                        useWarp = true;
                        break;
                    default:
                        break;
                }
            }

            WarpInterface.InitWarp(useWarp);


            AppDomain.CurrentDomain.ProcessExit += ProcessExitHandler!;
            Console.CancelKeyPress += ProcessExitHandler!;

            Logger.Print(Logger.Verbose, args != null && args.Length > 0 ? string.Join(", ", args) : "No arguments.");

            bot_started = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            });

            _client.Log += Log;

            string json = File.ReadAllText("auth.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            string? token = data?.token;

            if (token == null)
            {
                Logger.Print(Logger.Error, "Token not found or not set.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _client.SetStatusAsync(UserStatus.DoNotDisturb);

            _services = ConfigureServices();

            var interactions = _services.GetRequiredService<InteractionService>();
            await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteractionAsync;
            _client.Ready += async () =>
            {
                try
                {
                    await interactions.RegisterCommandsToGuildAsync(Globals.GetAs<ulong>("GuildId"));
                    // Logger.Print(Logger.Info, "Slash commands registered to guild.");
                }
                catch (Exception ex)
                {
                    Logger.Print(Logger.Error, ex.Message);
                }
            };

            await RBX.CheckDifferentAsync();
            RBX.InitializeTimer();

            await ChangeStatusAsync();
            InitializeStatusTimer();

            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_client!);
            serviceCollection.AddSingleton(new InteractionService(_client!));

            return serviceCollection.BuildServiceProvider();
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client!, interaction);
                var interactions = _services!.GetRequiredService<InteractionService>();
                await interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Logger.Print(Logger.Error, ex.Message);
            }
        }

        private Task Log(LogMessage msg)
        {
            var message = msg.ToString();

            var severityMap = new Dictionary<LogSeverity, Logger.LogType>
            {
                { LogSeverity.Critical, Logger.Critical },
                { LogSeverity.Error, Logger.Error },
                { LogSeverity.Warning, Logger.Warning },
                { LogSeverity.Info, Logger.Info },
                { LogSeverity.Verbose, Logger.Verbose },
                { LogSeverity.Debug, Logger.Debug }
            };

            var type = severityMap.TryGetValue(msg.Severity, out var logType) ? logType : Logger.Regular;

            Logger.Print(type, message);

            return Task.CompletedTask;
        }

        public static async Task FireUnpatch(string message, string version, string unp_p_atch)
        {
            if (_client == null) return;

            var embedBuilder = new EmbedBuilder();

            if (unp_p_atch == "unpatch")
            {
                embedBuilder.Color = new Color(81, 255, 164);
                embedBuilder.Title = $"`🟩` [ {Globals.Get("AppName")} | WindowsPlayer | LIVE ]";
                embedBuilder.Description = $"{Globals.Get("AppName")} has been updated for the latest Roblox Version";
                embedBuilder.AddField("Version", $"`{version}`");
                Logger.Print(Logger.Update, $"An update for {Globals.Get("AppName")} has been released.");
            }
            else if (unp_p_atch == "patch")
            {
                embedBuilder.Color = new Color(255, 100, 90);
                embedBuilder.Title = $"`🟥` [ {Globals.Get("AppName")} | WindowsPlayer | LIVE ]";
                embedBuilder.Description = $"{Globals.Get("AppName")} is currently patched";
                Logger.Print(Logger.Update, $"A patch for {Globals.Get("AppName")} was fired.");
            }

            if (message != "ignore")
                embedBuilder.AddField("Notes", message);

            embedBuilder.Footer = new EmbedFooterBuilder
            {
                Text = $"LEx Update Notifier",
                IconUrl = $"{Globals.Get("AppEmojiUrl")}"
            };

            var guild = await EnsureGuildConnection();
            if (guild == null) return;

            var channel = guild.GetTextChannel(Globals.GetAs<ulong>("UpdatesChannelIdText"));
            if (channel == null)
            {
                Logger.Print(Logger.Error, "Channel not found.");
                return;
            }

            string vcName = unp_p_atch == "unpatch" ? "\uD83D\uDFE9 Operational" : "\uD83D\uDD35 Not working";
            await channel.SendMessageAsync(
                unp_p_atch == "unpatch" ? $"{Globals.Get("AppName")} has been updated {Globals.Get("AppUpdatesRole")}" : $"{Globals.Get("AppName")} has been patched",
                false,
                embedBuilder.Build()
            );

            if (_client.GetChannel(Globals.GetAs<ulong>("StatusChannelIdVoice")) is SocketVoiceChannel versionChannel)
                await versionChannel.ModifyAsync(prop => prop.Name = vcName);
            else
                Logger.Print(Logger.Error, "Channel (VersionChannelVerbose) was null.");
        }

        private static async Task<SocketGuild> EnsureGuildConnection()
        {
            var guild = _client!.GetGuild(Globals.GetAs<ulong>("GuildId"));

            if (guild == null || !(guild?.IsConnected ?? false))
            {
                Logger.Print(Logger.Info, "Guild not found, attempting to connect.");

                int maxAttempts = 30;
                for (int attempts = 0; attempts < maxAttempts; attempts++)
                {
                    await Task.Delay(1000);
                    guild = _client.GetGuild(Globals.GetAs<ulong>("GuildId"));
                    if (guild?.IsConnected == true)
                    {
                        Logger.Print(Logger.Info, "Guild connected successfully.");
                        return guild;
                    }
                }

                Logger.Print(Logger.Error, "Maximum connection attempts reached. Connection failed.");
            }

            return guild!;
        }

        public static async Task Fire(RBX.Version version, RBX.Types typeUpdate)
        {
            if (_client == null) return;

            var embedBuilder = new EmbedBuilder
            {
                Footer = new EmbedFooterBuilder
                {
                    Text = "Roblox Update Notifier",
                    IconUrl = "https://cdn.discordapp.com/emojis/1143655222917480528.webp?size=96&quality=lossless" // If you want to change the road of blocks emoji do it then
                }
            };

            switch (typeUpdate)
            {
                case RBX.Types.Yes:
                    embedBuilder.Color = new Color(255, 100, 90);
                    embedBuilder.Title = "`🔴` [ Roblox | WindowsPlayer | LIVE ]";
                    embedBuilder.Description = "Roblox has pushed a new update";
                    break;
                case RBX.Types.Revert:
                    embedBuilder.Color = new Color(81, 255, 164);
                    embedBuilder.Title = "`🟢` [ Roblox | WindowsPlayer | LIVE ]";
                    embedBuilder.Description = "Roblox has reverted";
                    break;
                case RBX.Types.Error:
                default:
                    return;
            }

            embedBuilder.AddField("Version", $"`{version.ClientVersionUpload}`", true);

            var guild = await EnsureGuildConnection();
            if (guild == null) return;

            var channel = guild.GetTextChannel(Globals.GetAs<ulong>("UpdatesChannelIdText"));
            if (channel == null)
            {
                Logger.Print(Logger.Error, "Channel not found.");
                return;
            }

            await channel.SendMessageAsync($"An update has been detected {Globals.Get("RobloxUpdatesRoleId")}", false, embedBuilder.Build());
            Logger.Print(Logger.Update, "An update has been detected.");

            if (_client.GetChannel(Globals.GetAs<ulong>("RobloxVersionIdVoice")) is SocketVoiceChannel versionChannel)
                await versionChannel.ModifyAsync(prop => prop.Name = version.ClientVersionUpload);
            else
                Logger.Print(Logger.Error, "Channel (VersionChannelVerbose) was null.");
        }

        public static void InitializeStatusTimer()
        {
            _statustimer = new Timer(TimeSpan.FromMinutes(3).TotalMilliseconds);
            _statustimer.Elapsed += async (sender, e) => await ChangeStatusAsync();
            _statustimer.AutoReset = true;
            _statustimer.Enabled = true;
        }

        private static async Task ChangeStatusAsync()
        {
            if (_client == null)
                return;

            string[] statusList = ["for updates 👀", $"{Globals.Get("AppName")} grow 🐲", "for Roblox changes 🤖", "Excel mop the the floor"];
            await _client.SetGameAsync(statusList[new Random().Next(0, statusList.Length)], null, ActivityType.Watching);
        }
    }
}
