using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using Dalamud.Game.Text;

namespace IQ.SonarEcho
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool ConnectAutomatically { get; set; } = true;
        public string SonarChatChannel { get; set; } = XivChatType.Echo.ToString();

        // Hard code it cause fuck it, who cares?
        public readonly string IP = "echo.kaliya.io";
        #if DEBUG
        public readonly int PORT = 9001;
        #else
        public readonly int PORT = 9000;
        #endif
        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
