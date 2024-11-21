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
using Microsoft.VisualBasic;
using WatsonWebsocket;
using Thread = FFXIVClientStructs.FFXIV.Client.System.Threading.Thread;

namespace IQ.SonarEcho
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Echo - Sonar";

        private IDalamudPluginInterface PluginInterface { get; init; }
      [PluginService]  private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("IQ.SonarEcho");

        private ConfigWindow ConfigWindow { get; init; }
        public WatsonWsClient SocketClient { get; init; }
        [PluginService]     private IPluginLog Log { get; init; }
        [PluginService]      private IChatGui ChatGui { get; init; }

        public SonarClient Client { get; init; }
        [PluginService]   private IClientState ClientState { get; init; }

        public bool DisconnectedManually { get; set; } = false;
        
        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog logger,
            IClientState clientState,
            IChatGui chatGui)
        {
            this.PluginInterface = pluginInterface;

            PluginInterface.Inject(this);
            
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

            ClientState.Logout += (type, code) =>
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
