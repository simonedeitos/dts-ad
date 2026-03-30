using System;
using System.Collections.Generic;
using System.IO;
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

    public class RemoteControlLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "INFO";
        public string Message { get; set; } = "";
    }

    public class RemoteControlService : IDisposable
    {
        public string ServerUrl { get; set; } = "wss://store-uglh.onrender.com";
        private const int ReconnectDelayMs = 5000;
        private const int StateSendIntervalMs = 500;
        private const int MaxLogEntries = 500;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private string _token = "";
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _disposed = false;
        private bool _manualDisconnect = false;

        // ── Events ─────────────────────────────────────────────────────────────
        public event EventHandler<ConnectionState> ConnectionStateChanged;
        public event EventHandler<List<ConnectedUser>> ConnectedUsersChanged;
        public event EventHandler<string> CommandReceived;
        public event EventHandler<JObject> MessageReceived;
        public event EventHandler<byte[]> BinaryDataReceived;
        public event EventHandler<string> AudioDataReceived;
        public event EventHandler<RemoteControlLogEntry> LogAdded;

        // ── Log ────────────────────────────────────────────────────────────────
        private readonly List<RemoteControlLogEntry> _logEntries = new List<RemoteControlLogEntry>();
        private readonly object _logLock = new object();

        public IReadOnlyList<RemoteControlLogEntry> LogEntries
        {
            get { lock (_logLock) { return _logEntries.ToArray(); } }
        }

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
        private readonly object _reconnectLock = new object();
        private bool _reconnecting = false;
        private int _reconnectAttempt = 0;

        public ConnectionState State => _state;

        // ── Logging ────────────────────────────────────────────────────────────

        private void AddLog(string level, string message)
        {
            var entry = new RemoteControlLogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };
            lock (_logLock)
            {
                _logEntries.Add(entry);
                if (_logEntries.Count > MaxLogEntries)
                    _logEntries.RemoveAt(0);
            }
            LogAdded?.Invoke(this, entry);
        }

        public void Log(string message) => AddLog("INFO", message);
        public void LogWarning(string message) => AddLog("WARN", message);
        public void LogError(string message) => AddLog("ERROR", message);

        public void ClearLog()
        {
            lock (_logLock) { _logEntries.Clear(); }
        }

        // ── Connect / Disconnect ────────────────────────────────────────────────

        public async Task ConnectAsync(string token)
        {
            if (_state != ConnectionState.Disconnected)
            {
                LogWarning("ConnectAsync called but state is not Disconnected — ignored.");
                return;
            }

            _token = token;
            _manualDisconnect = false;
            _reconnectAttempt = 0;
            _cts = new CancellationTokenSource();

            Log($"Connecting with token: {MaskToken(token)}");
            await ConnectInternalAsync();
        }

        private async Task ConnectInternalAsync()
        {
            SetState(ConnectionState.Connecting);
            try
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                string wsUrl = $"{ServerUrl}?ad_token={Uri.EscapeDataString(_token)}";
                Log($"WebSocket connecting to {ServerUrl}...");

                await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
                Log("WebSocket connected successfully.");

                SetState(ConnectionState.Connected);
                _reconnectAttempt = 0;

                Log("Sending authentication message...");
                await AuthenticateAsync();
                Log("Authentication message sent.");

                // Start background loops
                _ = Task.Run(ReceiveLoopAsync);
                _ = Task.Run(StateSendLoopAsync);
            }
            catch (OperationCanceledException)
            {
                Log("Connection cancelled by user.");
                SetState(ConnectionState.Disconnected);
            }
            catch (WebSocketException wsEx)
            {
                LogError($"WebSocket error: {wsEx.Message} (Code: {wsEx.WebSocketErrorCode})");
                if (wsEx.InnerException != null)
                    LogError($"  Inner: {wsEx.InnerException.Message}");
                SetState(ConnectionState.Disconnected);
                ScheduleReconnect();
            }
            catch (Exception ex)
            {
                LogError($"Connection error: {ex.Message}");
                if (ex.InnerException != null)
                    LogError($"  Inner: {ex.InnerException.Message}");
                SetState(ConnectionState.Disconnected);
                ScheduleReconnect();
            }
        }

        public void Disconnect()
        {
            _manualDisconnect = true;
            _cts?.Cancel();
            Log("User requested disconnect.");

            try
            {
                if (_webSocket?.State == WebSocketState.Open || _webSocket?.State == WebSocketState.CloseReceived)
                    _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User disconnected", CancellationToken.None).Wait(2000);
            }
            catch (Exception ex)
            {
                LogWarning($"Disconnect close error: {ex.Message}");
            }

            SetState(ConnectionState.Disconnected);
            lock (_reconnectLock) { _reconnecting = false; }
            Log("Disconnected.");
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

            Log("Receive loop started.");

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    bool isBinary = false;
                    var binaryChunks = new System.IO.MemoryStream();

                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Log($"Server closed connection: {result.CloseStatusDescription ?? "no reason"}");
                            await HandleDisconnectAsync();
                            return;
                        }
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            isBinary = true;
                            binaryChunks.Write(buffer, 0, result.Count);
                        }
                        else
                        {
                            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        }
                    } while (!result.EndOfMessage);

                    if (isBinary)
                    {
                        using (binaryChunks)
                        {
                            BinaryDataReceived?.Invoke(this, binaryChunks.ToArray());
                        }
                        continue;
                    }

                    string json = sb.ToString();
                    try
                    {
                        var obj = JObject.Parse(json);
                        await ProcessMessageAsync(obj);
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Message parse error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log("Receive loop cancelled.");
            }
            catch (WebSocketException wsEx)
            {
                LogError($"Receive WebSocket error: {wsEx.Message} (Code: {wsEx.WebSocketErrorCode})");
            }
            catch (Exception ex)
            {
                LogError($"Receive loop error: {ex.Message}");
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
                    string cmd = msg["command"]?.ToString() ?? msg["action"]?.ToString() ?? "";
                    Log($"Command received: {cmd}");
                    CommandReceived?.Invoke(this, cmd);
                    // Also fire MessageReceived so handlers can access command payload (e.g. queue_remove, queue_reorder, queue_add)
                    MessageReceived?.Invoke(this, msg);
                    break;

                case "connected_users":
                case "users_update":
                    var users = new List<ConnectedUser>();
                    var usersArray = msg["data"] as JArray ?? msg["users"] as JArray;
                    if (usersArray is JArray arr)
                    {
                        foreach (var u in arr)
                        {
                            users.Add(new ConnectedUser
                            {
                                Name = u["name"]?.ToString() ?? u["userName"]?.ToString() ?? "",
                                MicActive = u["mic_active"]?.Value<bool>() ?? false
                            });
                        }
                    }
                    Log($"Users update: {users.Count} user(s) connected.");
                    ConnectedUsersChanged?.Invoke(this, users);
                    break;

                case "request_archive":
                    Log("Archive list requested by client.");
                    await SendArchiveListAsync();
                    break;

                case "auth_ok":
                    Log("Authentication accepted by server.");
                    break;

                case "auth_error":
                    string reason = msg["message"]?.ToString() ?? "Unknown reason";
                    LogError($"Authentication rejected: {reason}");
                    break;

                case "error":
                    string errMsg = msg["message"]?.ToString() ?? "Unknown error";
                    LogError($"Server error: {errMsg}");
                    break;

                default:
                    // Browser sends { command: "audio_data", data: "..." } without a type field
                    string defaultCmd = msg["command"]?.ToString();
                    if (defaultCmd == "audio_data")
                    {
                        string audioData = msg["data"]?.ToString();
                        if (!string.IsNullOrEmpty(audioData))
                            AudioDataReceived?.Invoke(this, audioData);
                    }
                    else
                    {
                        MessageReceived?.Invoke(this, msg);
                    }
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
                LogWarning($"State send loop error: {ex.Message}");
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
                LogWarning($"Binary send error: {ex.Message}");
            }
        }

        public async Task SendRawTextAsync(byte[] utf8Bytes)
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(utf8Bytes), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogWarning($"Raw text send error: {ex.Message}");
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
                LogWarning($"JSON send error: {ex.Message}");
            }
        }

        // ── Reconnect ───────────────────────────────────────────────────────────

        private async Task HandleDisconnectAsync()
        {
            SetState(ConnectionState.Disconnected);
            if (!_manualDisconnect && !_cts.IsCancellationRequested)
            {
                Log("Connection lost. Will attempt to reconnect...");
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            if (_manualDisconnect || _cts == null || _cts.IsCancellationRequested)
                return;

            lock (_reconnectLock)
            {
                if (_reconnecting) return;
                _reconnecting = true;
            }

            _ = Task.Run(ReconnectLoopAsync);
        }

        private async Task ReconnectLoopAsync()
        {
            try
            {
                while (!_manualDisconnect && !_cts.IsCancellationRequested && _state == ConnectionState.Disconnected)
                {
                    _reconnectAttempt++;
                    Log($"Reconnect attempt #{_reconnectAttempt} in {ReconnectDelayMs / 1000}s...");

                    try { await Task.Delay(ReconnectDelayMs, _cts.Token); }
                    catch (OperationCanceledException) { break; }

                    if (!_manualDisconnect && !_cts.IsCancellationRequested)
                        await ConnectInternalAsync();
                }
            }
            finally
            {
                lock (_reconnectLock) { _reconnecting = false; }
            }
        }

        // ── State helper ────────────────────────────────────────────────────────

        private void SetState(ConnectionState state)
        {
            if (_state == state)
            {
                AddLog("DEBUG", $"Redundant state change ignored: {state}");
                return;
            }
            var prev = _state;
            _state = state;
            Log($"State: {prev} → {state}");
            ConnectionStateChanged?.Invoke(this, state);
        }

        private static string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "(empty)";
            if (token.Length <= 8) return new string('*', token.Length);
            return token.Substring(0, 4) + "..." + token.Substring(token.Length - 4);
        }

        // ── IDisposable ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _manualDisconnect = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _webSocket?.Dispose();
        }
    }
}
