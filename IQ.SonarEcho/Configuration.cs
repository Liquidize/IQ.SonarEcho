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
        public readonly string IP = "";
        public readonly int PORT = 9000;
        
        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
