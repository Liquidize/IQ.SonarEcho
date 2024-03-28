using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using IQ.SonarEcho.Windows;
using Lumina;
using Lumina.Excel.GeneratedSheets;
using Microsoft.VisualBasic;
using WatsonWebsocket;
using Thread = FFXIVClientStructs.FFXIV.Client.System.Threading.Thread;

namespace IQ.SonarEcho
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Echo - Sonar";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("IQ.SonarEcho");

        private ConfigWindow ConfigWindow { get; init; }
        public WatsonWsClient SocketClient { get; init; }
        private IPluginLog Log { get; init; }
        private IChatGui ChatGui { get; init; }

        public SonarClient Client { get; init; }
        private IClientState ClientState { get; init; }

        public bool DisconnectedManually { get; set; } = false;
        
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IPluginLog logger,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IChatGui chatGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.Log = logger;
            ClientState = clientState;
            ChatGui = chatGui;
            
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);


            Client = new SonarClient(Configuration, ChatGui, Log);       
            
            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            
            ClientState.Login += () =>
            {
                if (Configuration.ConnectAutomatically)
                    Client.Start();
            };
            ClientState.Logout += () =>
            {
              Client.Stop();
            };

            if (ClientState.IsLoggedIn)
            {
                if (Configuration.ConnectAutomatically)
                    Client.Start();
            }
            
        }
        

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            Client.Dispose();
        }
        
        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
