using NLog;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using System.Windows.Controls;
using System.Collections.Concurrent;
using Sandbox.Engine.Multiplayer;

namespace DiscordPlugin
{
    public class Settings : ViewModel
    {
        private bool _enabled = true;
        private string _botToken = "";
        private ulong _adminChannelId;
        private bool _isBotRunning = false;

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        public string BotToken
        {
            get => _botToken;
            set { _botToken = value; OnPropertyChanged(); }
        }

        public ulong AdminChannelId
        {
            get => _adminChannelId;
            set { _adminChannelId = value; OnPropertyChanged(); }
        }

        public string BotStatus
        {
            get => _isBotRunning ? "Running" : "Stopped";
        }

        public bool IsBotRunning
        {
            get => _isBotRunning;
            set { _isBotRunning = value; OnPropertyChanged(); }
        }
    }

    public interface IBotControl
    {
        void Start();
        void Stop();
    }

    [Plugin("Discord", typeof(DiscordPlugin), "bf93c983-da1f-4401-a48e-f74d353e4705")]
    public sealed class DiscordPlugin : TorchPluginBase, IWpfPlugin, IBotControl
    {
        public Persistent<Settings> Settings { get; private set; }

        private static readonly Logger _log = LogManager.GetLogger("Discord");

        private bool _init = false;
        private Bot _bot;
        private UserControl _userControl;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Settings = Persistent<Settings>.Load(Path.Combine(StoragePath, "Discord.cfg"));
            // Settings.Data.PropertyChanged += PropertyChanged;
        }

        UserControl IWpfPlugin.GetControl()
        {
            if (_userControl == null)
            {
                _userControl = new DiscordControl(this) { DataContext = this };
            }
            return _userControl;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Update()
        {
            if (!Settings.Data.Enabled)
                return;

            if (!_init)
            {
                InitByUpdate();
            }
        }

        private void InitByUpdate()
        {
            _init = true;
        }

        async void IBotControl.Start()
        {
            _log.Info("Starting bot...");
            _bot = new Bot(Settings.Data.BotToken, Settings.Data.AdminChannelId, this.Torch);
            await _bot.StartAsync();
        }

        async void IBotControl.Stop()
        {
            _log.Info("Stopping bot");
            await _bot.StopAsync();
            _bot = null;
        }
    }
}
