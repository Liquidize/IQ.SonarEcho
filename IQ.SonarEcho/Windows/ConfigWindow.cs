using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WatsonWebsocket;

namespace IQ.SonarEcho.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin _plugin;
    private SonarClient _client;
    
    public ConfigWindow(Plugin plugin) : base(
        "Echo - Sonar Configuration",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(250, 125);
        this.SizeCondition = ImGuiCond.Always;
        _plugin = plugin;
        this.Configuration = plugin.Configuration;
        _client = plugin.Client;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var connectAutomatically = this.Configuration.ConnectAutomatically;
        ImGui.Text("Status: ");
        ImGui.SameLine();
        if (_client.IsConnected)
        {
            ImGui.TextColored(new Vector4(0,255,0, 255), "Connected");
        } else if (!_client.IsConnected)
        {
            ImGui.TextColored(new Vector4(255f,0,0, 255), "Not Connected");
        }
        else
        {
            ImGui.Text("Unknown");
        }

        if (_client != null && _client.IsConnected)
        {
            ImGui.SameLine();
            if (ImGui.Button("Disconnect"))
            {
               _client.Stop();
            }
        } else if (_client != null && !_client.IsConnected)
        {
            ImGui.SameLine();
            if (ImGui.Button("Connect"))
            {
                _client.Start();
            }
        }
        
        if (ImGui.Checkbox("Connect Automatically", ref connectAutomatically))
        {
            Configuration.ConnectAutomatically = connectAutomatically;
            if (!connectAutomatically && _client is not null && !_client.IsConnected)
            {
                // if we aren't connected we want to kill the thread
                _client.Stop();
            } else if (connectAutomatically && _client is not null && !_client.IsConnected)
            {
                // Alternatively we do want to start if we aren't!
                _client.Start();
            }
            Configuration.Save();
        }

        var sonarChatChannel = Configuration.SonarChatChannel;
        ImGui.Text("Sonar Channel: ");
        ImGui.SameLine();

// Draw combo box for selecting sonar chat channel
        ImGui.PushItemWidth(120); // Adjust width as needed
        var chatTypeNames = Enum.GetNames(typeof(XivChatType));
        var selectedIndex = Array.IndexOf(chatTypeNames, sonarChatChannel.ToString());
        if (ImGui.Combo("##sonarChatChannelCombo", ref selectedIndex, chatTypeNames, chatTypeNames.Length))
        {
            sonarChatChannel = chatTypeNames[selectedIndex];
            Configuration.SonarChatChannel = sonarChatChannel;
            Configuration.Save();
        }
        ImGui.PopItemWidth();

        
    }
}
