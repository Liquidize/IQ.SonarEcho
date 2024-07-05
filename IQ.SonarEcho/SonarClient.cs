using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using WatsonWebsocket;

namespace IQ.SonarEcho;

public class SonarClient : IDisposable
{
    private WatsonWsClient? SocketClient { get; set; }
    private Configuration _config { get; set; }
    private readonly IPluginLog _logger;
    private readonly IChatGui _chatGui;
    private Thread? _thread;
    private bool _disposed = false;

    public bool IsConnected => SocketClient != null && SocketClient.Connected;

    public SonarClient(Configuration config, IChatGui gui, IPluginLog logger)
    {
        _config = config;
        _chatGui = gui;
        _logger = logger;
        _chatGui.ChatMessage += ChatGuiOnChatMessage;
    }


    private void ChatGuiOnChatMessage(
        XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (type.ToString() != _config.SonarChatChannel)
        {
            return;
        }

        if (SocketClient == null || !SocketClient.Connected)
        {
            return;
        }
        var str = message.ToString();
        str = str.Replace("\ue0bb", "#");
        str = str.Replace("\ue05d", "$");
        var regExp = new Regex(
            "Rank\\s*(?<HuntRank>[A-Za-z]+):\\s*(?<HuntName>[^#$]+)\\s*#\\s*(?<Location>.*?)\\s*\\(\\s*(?<XCord>\\d+(\\.\\d+)?)\\s*,\\s*(?<YCord>\\d+(\\.\\d+)?)\\s*\\)\\s*<.*?\\s*(?<World>\\w+)\\s*>",
            RegexOptions.IgnoreCase);
        var match = regExp.Match(str);
        if (match.Success)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)0);
                    writer.Write(match.Groups["HuntName"].Value);
                    writer.Write(Convert.ToDouble(match.Groups["XCord"].Value));
                    writer.Write(Convert.ToDouble(match.Groups["YCord"].Value));
                    writer.Write(match.Groups["World"].Value);
                    writer.Write((byte)(str.IndexOf("was just killed") > -1 ? 2 : 1));
                }

                SocketClient.SendAsync(stream.ToArray(), WebSocketMessageType.Binary).ConfigureAwait(false);
            }
        }
    }

    public void Start()
    {
        // If for w.e reason we call Start multiple times we'll just kill the old client and reconnect
        if (!_disposed)
        {
            SocketClient?.Stop();
            SocketClient?.Dispose();
            _disposed = true;
        }

        if (_thread == null || !_thread.IsAlive)
        {
            _logger.Information("Starting new thread...");
            _disposed = false;
            _thread = new Thread(SocketListenLoop);
            _thread.Name = $"Echo Socket Thread";
            _thread.IsBackground = true;
            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Start();
        }
    }

    public void Stop()
    {
        _disposed = true; // Set flag to stop retry loop
        SocketClient?.Stop();
        SocketClient = null;
    }

    public async void SocketListenLoop()
    {
        while (!_disposed)
        {
            if (SocketClient == null || !SocketClient.Connected)
            {
                if (_config.ConnectAutomatically)
                {
                    try
                    {
                        SocketClient = new WatsonWsClient(_config.IP, _config.PORT);
                        SocketClient.Logger += s => { _logger.Information(s); };

                        // Start the client asynchronously
                        SocketClient.StartAsync().WaitAsync(TimeSpan.FromSeconds(5)).ContinueWith((task =>
                                    {
                                        if (task.Exception != null)
                                        {
                                            _logger.Warning(
                                                task.Exception, "");
                                        }
                                    })).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "An error occurred when attempting to connect");
                    }

                    if (SocketClient?.Connected == false)
                    {
                        _logger.Information("Failed to connect. Retrying in 5 seconds.");
                        SocketClient?.Dispose();
                        await Task.Delay(5000); // Wait before retrying
                    }
                }
            }
            else
            {
                await Task.Delay(1000); // Wait for 1 second before checking again
            }
        }
    }


    public void Dispose()
    {
        _disposed = true; // Set flag to stop retry loop
        SocketClient?.Dispose();
        _chatGui.ChatMessage -= ChatGuiOnChatMessage;
    }
}
