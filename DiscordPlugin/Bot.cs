using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Utils;
using VRage.GameServices;

namespace DiscordPlugin
{
    public class Bot
    {
        private DiscordSocketClient _client;
        private static readonly Logger Log = LogManager.GetLogger("DiscordBot");
        private string _token;
        private ulong _adminChannelId;
        private bool _started = false;
        private string _pluginDir = "";
        private static List<string> _dynamicDlls = new List<string>{"", ""};
        private ITorchBase _torch;

        public Bot(string token, ulong adminChannelId, ITorchBase torch)
        {
            Log.Info($"Bot constructor: token={token}, adminChannelId={adminChannelId}");
            _torch = torch;
            _token = token;
            _adminChannelId = adminChannelId;
            var manager = torch.Managers.GetManager(typeof(PluginManager)) as PluginManager;
            _pluginDir = manager.PluginDir;
            ApplyDllMagic();
        }

        private void ApplyDllMagic()
        {
            // var pluginItems = Directory.EnumerateFiles(_pluginDir, "*.zip").Union(Directory.EnumerateDirectories(_pluginDir));
            // TODO support zip format
            // var pluginItems = Directory.EnumerateDirectories(_pluginDir);
            var requiredDlls = new List<string> {
                Path.Combine(_pluginDir, "Discord", "Discord.Net.WebSocket.dll"),
                Path.Combine(_pluginDir, "Discord", "Discord.Net.Providers.WS4Net.dll"),
                Path.Combine(_pluginDir, "Discord", "WebSocket4Net.dll")
            };

            var assemblies = new List<Assembly>();
            foreach (var file in requiredDlls)
            {
                Log.Debug($"Loading DLL: {file}");
                using (var stream = File.OpenRead(file))
                {
                    var data = stream.ReadToEnd();
                    assemblies.Add(Assembly.Load(data));
                }
            }

            Assembly ResolveDependentAssembly(object sender, ResolveEventArgs args)
            {
                var requiredAssemblyName = new AssemblyName(args.Name);
                foreach (Assembly asm in assemblies)
                {
                    if (IsAssemblyCompatible(requiredAssemblyName, asm.GetName()))
                        return asm;
                }
                Log.Warn($"Could find dependent assembly! Requesting assembly: {args.RequestingAssembly}, dependent assembly: {requiredAssemblyName}");
                return null;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependentAssembly;
        }

        private static bool IsAssemblyCompatible(AssemblyName a, AssemblyName b)
        {
            return a.Name == b.Name && a.Version.Major == b.Version.Major && a.Version.Minor == b.Version.Minor;
        }

        public async Task StartAsync()
        {
            try
            {
                if (_started)
                    Log.Warn("Bot tried to be started, but it is already running! Should be stopped first!");
                var config = new DiscordSocketConfig()
                {
                    WebSocketProvider = WS4NetProvider.Instance
                };
                Log.Info($"Creating config with WS4NetProvider {config}");
                _client = new DiscordSocketClient(config);
                _client.Log += LogFromBot;
                _client.MessageReceived += OnMessageReceived;
                _client.Ready += OnReady;
                Log.Info($"Await login, token={_token}");
                await _client.LoginAsync(TokenType.Bot, _token);
                await _client.StartAsync();
                InitGameCallbacks();
                _started = true;
            }
            catch (Discord.Net.HttpException ex)
            {
                Log.Error(ex, "Could not start bot: ");
            }
        }

        private void InitGameCallbacks()
        {
            // TODO: https://github.com/TorchAPI/Torch/blob/staging/Torch.Server/Views/ChatControl.xaml.cs#L52-L83
            // public abstract class MultiplayerManagerBase : Manager, IMultiplayerManagerBase
            var multiManager = _torch.CurrentSession.Managers.GetManager<IMultiplayerManagerServer>();
            multiManager.PlayerJoined += PlayerJoinedCallback;
            multiManager.PlayerLeft += PlayerLeftCallback;

            // MyMultiplayer.Static.ClientJoined += ClientJoinedCallback;
            // MyMultiplayer.Static.ClientLeft += ClientLeftCallback;
        }

        private async void PlayerJoinedCallback(IPlayer player)
        {
            Log.Info($"Discord: Client left {player.Name} ({player.SteamId})");
            await Send($"Player joined: {player.Name} ({player.SteamId})");
        }

        private async void PlayerLeftCallback(IPlayer player)
        {
            Log.Info($"Discord: Client joined {player.Name} ({player.SteamId})");
            await Send($"Player left: {player.Name} ({player.SteamId})");
        }

        public async Task StopAsync()
        {
            await _client.StopAsync();
            _started = false;
        }

        private async Task OnReady()
        {
            Log.Info("Bot ready.");
        }

        public async Task Send(string message)
        {
            await FindAdminChannel().SendMessageAsync(message);
        }

        private async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel.Id != _adminChannelId)
                return;

            if (msg.Author.IsBot)
                return;

            if (msg.Content == "!ping")
            {
                await msg.Channel.SendMessageAsync($"!pong, {msg.Author.Mention}");
            }

            if (msg.Content == "!simspeed")
            {
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} simspeed is {Sync.ServerSimulationRatio}");
            }
        }

        private ISocketMessageChannel FindAdminChannel()
        {
            return _client.GetChannel(_adminChannelId) as ISocketMessageChannel;
        }

        private Task LogFromBot(LogMessage msg)
        {
            Log.Info(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
