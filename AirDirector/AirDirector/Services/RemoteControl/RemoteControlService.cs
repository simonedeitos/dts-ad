using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AirDirector.Services.RemoteControl
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    public class ConnectedUser
    {
        public string Name { get; set; } = "";
        public bool MicActive { get; set; } = false;
    }

    public class RemoteControlService : IDisposable
    {
        private const string ServerUrl = "wss://client.airdirector.app/ws";
        private const int ReconnectDelayMs = 5000;
        private const int StateSendIntervalMs = 500;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private string _token = "";
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _disposed = false;

        // ── Events ─────────────────────────────────────────────────────────────
        public event EventHandler<ConnectionState> ConnectionStateChanged;
        public event EventHandler<List<ConnectedUser>> ConnectedUsersChanged;
        public event EventHandler<string> CommandReceived;
        public event EventHandler<JObject> MessageReceived;

        // ── Player State (set by MainForm) ─────────────────────────────────────
        public string PlayerStatus { get; set; } = "stopped";
        public string PlayerTrack { get; set; } = "";
        public int PlayerPosition { get; set; } = 0;
        public int PlayerDuration { get; set; } = 0;
        public int NextScheduleCountdown { get; set; } = 0;
        public int NextAdCountdown { get; set; } = 0;
        public string NextScheduleName { get; set; } = "";

        // ── Playlist queue (set by MainForm) ───────────────────────────────────
        private List<object> _playlistQueue = new List<object>();
        private List<object> _musicArchive = new List<object>();
        private List<object> _clipsArchive = new List<object>();

        // ── State ───────────────────────────────────────────────────────────────
        private bool _reconnecting = false;

        public ConnectionState State => _state;

        // ── Connect / Disconnect ────────────────────────────────────────────────

        public async Task ConnectAsync(string token)
        {
            if (_state != ConnectionState.Disconnected)
                return;

            _token = token;
            _cts = new CancellationTokenSource();
            await ConnectInternalAsync();
        }

        private async Task ConnectInternalAsync()
        {
            SetState(ConnectionState.Connecting);
            try
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(ServerUrl), _cts.Token);

                SetState(ConnectionState.Connected);
                await AuthenticateAsync();

                // Start background loops
                _ = Task.Run(ReceiveLoopAsync);
                _ = Task.Run(StateSendLoopAsync);
                _reconnecting = false;
            }
            catch (OperationCanceledException)
            {
                SetState(ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlService] ❌ Connection error: {ex.Message}");
                SetState(ConnectionState.Disconnected);
                if (!_reconnecting && _cts != null && !_cts.IsCancellationRequested)
                    _ = Task.Run(ReconnectLoopAsync);
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "User disconnected", CancellationToken.None).Wait(2000); } catch { }
            SetState(ConnectionState.Disconnected);
            _reconnecting = false;
        }

        // ── Authentication ──────────────────────────────────────────────────────

        private async Task AuthenticateAsync()
        {
            var msg = new { type = "auth", role = "airdirector", token = _token };
            await SendJsonAsync(msg);
        }

        // ── Receive loop ────────────────────────────────────────────────────────

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[65536];
            var sb = new StringBuilder();

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await HandleDisconnectAsync();
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    string json = sb.ToString();
                    try
                    {
                        var obj = JObject.Parse(json);
                        await ProcessMessageAsync(obj);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RemoteControlService] ⚠️ Parse error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlService] ❌ Receive error: {ex.Message}");
            }

            if (!_cts.IsCancellationRequested)
                await HandleDisconnectAsync();
        }

        private async Task ProcessMessageAsync(JObject msg)
        {
            string type = msg["type"]?.ToString() ?? "";

            switch (type)
            {
                case "command":
                    string cmd = msg["command"]?.ToString() ?? "";
                    CommandReceived?.Invoke(this, cmd);
                    break;

                case "users_update":
                    var users = new List<ConnectedUser>();
                    if (msg["users"] is JArray arr)
                    {
                        foreach (var u in arr)
                        {
                            users.Add(new ConnectedUser
                            {
                                Name = u["name"]?.ToString() ?? "",
                                MicActive = u["mic_active"]?.Value<bool>() ?? false
                            });
                        }
                    }
                    ConnectedUsersChanged?.Invoke(this, users);
                    break;

                case "request_archive":
                    await SendArchiveListAsync();
                    break;

                default:
                    MessageReceived?.Invoke(this, msg);
                    break;
            }
        }

        // ── State send loop ─────────────────────────────────────────────────────

        private async Task StateSendLoopAsync()
        {
            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    await SendPlayerStateAsync();
                    await Task.Delay(StateSendIntervalMs, _cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlService] ⚠️ State loop error: {ex.Message}");
            }
        }

        // ── Send helpers ────────────────────────────────────────────────────────

        public async Task SendPlayerStateAsync()
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            var msg = new
            {
                type = "player_state",
                data = new
                {
                    status = PlayerStatus,
                    track = PlayerTrack,
                    position = PlayerPosition,
                    duration = PlayerDuration
                }
            };
            await SendJsonAsync(msg);
        }

        public async Task SendPlaylistQueueAsync(List<object> items)
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            _playlistQueue = items;
            var msg = new { type = "playlist_queue", data = items };
            await SendJsonAsync(msg);
        }

        public async Task SendArchiveListAsync()
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            var msg = new
            {
                type = "archive_list",
                data = new { music = _musicArchive, clips = _clipsArchive }
            };
            await SendJsonAsync(msg);
        }

        public async Task SendCountdownAsync(int nextSchedule, int nextAd, string scheduleName)
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            NextScheduleCountdown = nextSchedule;
            NextAdCountdown = nextAd;
            NextScheduleName = scheduleName;
            var msg = new
            {
                type = "countdown",
                data = new
                {
                    next_schedule = nextSchedule,
                    next_ad = nextAd,
                    schedule_name = scheduleName
                }
            };
            await SendJsonAsync(msg);
        }

        public void SetArchiveData(List<object> music, List<object> clips)
        {
            _musicArchive = music;
            _clipsArchive = clips;
        }

        public async Task SendBinaryAsync(byte[] data)
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, _cts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlService] ⚠️ Binary send error: {ex.Message}");
            }
        }

        private async Task SendJsonAsync(object obj)
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlService] ⚠️ Send error: {ex.Message}");
            }
        }

        // ── Reconnect ───────────────────────────────────────────────────────────

        private async Task HandleDisconnectAsync()
        {
            SetState(ConnectionState.Disconnected);
            if (!_cts.IsCancellationRequested)
                await ReconnectLoopAsync();
        }

        private async Task ReconnectLoopAsync()
        {
            _reconnecting = true;
            while (!_cts.IsCancellationRequested && _state == ConnectionState.Disconnected)
            {
                Console.WriteLine("[RemoteControlService] 🔄 Reconnecting in 5s...");
                try { await Task.Delay(ReconnectDelayMs, _cts.Token); } catch (OperationCanceledException) { break; }

                if (!_cts.IsCancellationRequested)
                    await ConnectInternalAsync();
            }
            _reconnecting = false;
        }

        // ── State helper ────────────────────────────────────────────────────────

        private void SetState(ConnectionState state)
        {
            _state = state;
            ConnectionStateChanged?.Invoke(this, state);
        }

        // ── IDisposable ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _webSocket?.Dispose();
        }
    }
}
