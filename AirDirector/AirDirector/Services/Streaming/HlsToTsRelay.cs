using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AirDirector.Services.Streaming
{
    public sealed class HlsToTsRelay : IDisposable
    {
        private static readonly Regex BandwidthRegex = new Regex(@"(?:^|,)BANDWIDTH=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly HttpClient Http = CreateHttpClient();

        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, RelayRegistration> _registrations = new ConcurrentDictionary<string, RelayRegistration>(StringComparer.OrdinalIgnoreCase);
        private readonly Task _listenLoopTask;
        private volatile bool _disposed;

        public int Port { get; }
        public Action<string>? Logger { get; set; }

        public HlsToTsRelay()
        {
            Port = GetFreeLoopbackPort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _listenLoopTask = Task.Run(ListenLoopAsync);
            Log($"[RELAY] started on 127.0.0.1:{Port}");
        }

        public string Register(string upstreamUrl)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(upstreamUrl)) throw new ArgumentException("Upstream URL is required", nameof(upstreamUrl));
            if (!Uri.TryCreate(upstreamUrl, UriKind.Absolute, out var upstreamUri)) throw new ArgumentException("Invalid upstream URL", nameof(upstreamUrl));
            if (upstreamUri.Scheme != Uri.UriSchemeHttp && upstreamUri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Only HTTP/HTTPS upstream URLs are supported", nameof(upstreamUrl));

            while (true)
            {
                string id = Guid.NewGuid().ToString("N").Substring(0, 12);
                string localUrl = $"http://127.0.0.1:{Port}/s/{id}.ts";
                var registration = new RelayRegistration(id, localUrl, upstreamUri);
                if (_registrations.TryAdd(id, registration))
                {
                    Log($"[RELAY] register {id} → {upstreamUrl}");
                    return localUrl;
                }
            }
        }

        public void Unregister(string localUrl)
        {
            if (string.IsNullOrWhiteSpace(localUrl)) return;
            string? id = TryParseRelayId(localUrl);
            if (string.IsNullOrEmpty(id)) return;

            if (_registrations.TryRemove(id, out var registration))
            {
                registration.Dispose();
                Log($"[RELAY] unregister {id}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _disposeCts.Cancel(); } catch { }

            foreach (var kv in _registrations)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            _registrations.Clear();

            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
            try { _listenLoopTask.Wait(TimeSpan.FromSeconds(2)); } catch { }

            _disposeCts.Dispose();
        }

        private async Task ListenLoopAsync()
        {
            while (!_disposeCts.IsCancellationRequested)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) when (_disposeCts.IsCancellationRequested) { break; }
                catch (HttpListenerException) when (_disposeCts.IsCancellationRequested) { break; }
                catch (HttpListenerException) when (_disposed) { break; }
                catch (Exception)
                {
                    if (_disposeCts.IsCancellationRequested) break;
                    continue;
                }

                if (context != null)
                    _ = Task.Run(() => HandleContextAsync(context));
            }
        }

        private async Task HandleContextAsync(HttpListenerContext context)
        {
            string? id = TryParseRelayId(context.Request.Url?.ToString());
            if (string.IsNullOrEmpty(id) || !_registrations.TryGetValue(id, out var registration))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, registration.Cancellation.Token);

            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "video/mp2t";
            response.SendChunked = true;
            response.KeepAlive = false;
            response.Headers["Cache-Control"] = "no-cache, no-store";
            response.Headers["Connection"] = "close";

            Log($"[RELAY] {id} VLC connected, fetching upstream");

            try
            {
                await RelayToClientAsync(id, registration.UpstreamUri, response.OutputStream, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested) { }
            catch (IOException)
            {
                Log($"[RELAY] {id} VLC disconnected, stop fetch");
            }
            catch (SocketException)
            {
                Log($"[RELAY] {id} VLC disconnected, stop fetch");
            }
            catch (HttpListenerException)
            {
                Log($"[RELAY] {id} VLC disconnected, stop fetch");
            }
            catch (Exception ex)
            {
                Log($"[RELAY] {id} upstream err: {Short(ex.Message)}");
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
                try { response.Close(); } catch { }
            }
        }

        private async Task RelayToClientAsync(string id, Uri upstreamUri, Stream clientStream, CancellationToken cancellationToken)
        {
            Uri mediaPlaylistUri = upstreamUri;
            var seenSegments = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            while (!cancellationToken.IsCancellationRequested)
            {
                string playlist = await FetchTextWithRetryAsync(id, mediaPlaylistUri, cancellationToken).ConfigureAwait(false);

                if (LooksLikeMasterPlaylist(playlist))
                {
                    if (!TrySelectBestVariant(playlist, mediaPlaylistUri, out var variantUri, out int bandwidth))
                        throw new InvalidDataException("Master playlist without valid variant");

                    mediaPlaylistUri = variantUri;
                    Log($"[RELAY] {id} master → variant BW={bandwidth / 1000}k");
                    playlist = await FetchTextWithRetryAsync(id, mediaPlaylistUri, cancellationToken).ConfigureAwait(false);
                }

                ParseMediaPlaylist(playlist, mediaPlaylistUri, out var segments, out int targetDurationSeconds, out bool endList);

                int newSegments = 0;
                foreach (var segmentUri in segments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string key = NormalizeSegmentKey(segmentUri);
                    if (seenSegments.ContainsKey(key))
                    {
                        seenSegments[key] = DateTime.UtcNow;
                        continue;
                    }

                    long bytes = await PumpSegmentToClientAsync(id, segmentUri, clientStream, cancellationToken).ConfigureAwait(false);
                    seenSegments[key] = DateTime.UtcNow;
                    Log($"[RELAY] {id} segment {key} ({bytes}B) sent");
                    newSegments++;
                }

                Log($"[RELAY] {id} playlist poll: {newSegments} new segments");
                TrimSeenCache(seenSegments);

                if (endList)
                {
                    Log($"[RELAY] {id} ended (VOD #EXT-X-ENDLIST)");
                    return;
                }

                int delayMs = Math.Max(500, Math.Max(2, targetDurationSeconds) * 500);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void ParseMediaPlaylist(string playlist, Uri playlistUri, out List<Uri> segments, out int targetDurationSeconds, out bool endList)
        {
            segments = new List<Uri>();
            targetDurationSeconds = 4;
            endList = false;

            var lines = SplitLines(playlist);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#EXT-X-TARGETDURATION:", StringComparison.OrdinalIgnoreCase))
                {
                    string value = line.Substring("#EXT-X-TARGETDURATION:".Length).Trim();
                    if (int.TryParse(value, out int parsed) && parsed > 0)
                        targetDurationSeconds = parsed;
                    continue;
                }

                if (line.StartsWith("#EXT-X-ENDLIST", StringComparison.OrdinalIgnoreCase))
                {
                    endList = true;
                    continue;
                }

                if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (TryResolveHttpUri(playlistUri, line, out var segmentUri))
                    segments.Add(segmentUri);
            }
        }

        private static bool TrySelectBestVariant(string playlist, Uri playlistUri, out Uri variantUri, out int bandwidth)
        {
            variantUri = playlistUri;
            bandwidth = -1;

            var lines = SplitLines(playlist);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                    continue;

                int currentBandwidth = ParseBandwidth(line);
                string? candidate = null;

                for (int j = i + 1; j < lines.Count; j++)
                {
                    var next = lines[j].Trim();
                    if (next.Length == 0) continue;
                    if (next.StartsWith("#", StringComparison.Ordinal))
                    {
                        if (next.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                            break;
                        continue;
                    }

                    candidate = next;
                    i = j;
                    break;
                }

                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                if (!TryResolveHttpUri(playlistUri, candidate, out var resolved))
                    continue;

                if (currentBandwidth >= bandwidth)
                {
                    bandwidth = currentBandwidth;
                    variantUri = resolved;
                }
            }

            return bandwidth >= 0;
        }

        private static int ParseBandwidth(string streamInfLine)
        {
            var match = BandwidthRegex.Match(streamInfLine);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed))
                return parsed;
            return 0;
        }

        private async Task<string> FetchTextWithRetryAsync(string id, Uri uri, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetryAsync(id, cancellationToken, async ct =>
            {
                using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task<long> PumpSegmentToClientAsync(string id, Uri segmentUri, Stream clientStream, CancellationToken cancellationToken)
        {
            using var response = await ExecuteWithRetryAsync(id, cancellationToken, async ct =>
            {
                var res = await Http.GetAsync(segmentUri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                res.EnsureSuccessStatusCode();
                return res;
            }).ConfigureAwait(false);

            await using var upstreamStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
            long total = 0;
            try
            {
                while (true)
                {
                    int read = await upstreamStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    if (read <= 0) break;
                    await clientStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    await clientStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    total += read;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return total;
        }

        private async Task<T> ExecuteWithRetryAsync<T>(string id, CancellationToken cancellationToken, Func<CancellationToken, Task<T>> action)
        {
            Exception? lastError = null;
            int[] delaysMs = { 500, 1000, 2000, 4000, 4000 };

            for (int retry = 0; retry <= 5; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await action(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (retry >= 5) break;

                    Log($"[RELAY] {id} upstream err: {Short(ex.Message)} (retry {retry + 1}/5)");
                    await Task.Delay(delaysMs[Math.Min(retry, delaysMs.Length - 1)], cancellationToken).ConfigureAwait(false);
                }
            }

            throw lastError ?? new InvalidOperationException("Unknown upstream error");
        }

        private static bool LooksLikeMasterPlaylist(string playlist)
        {
            return playlist.IndexOf("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryResolveHttpUri(Uri baseUri, string value, out Uri resolved)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out resolved))
                return resolved.Scheme == Uri.UriSchemeHttp || resolved.Scheme == Uri.UriSchemeHttps;

            if (!Uri.TryCreate(baseUri, value, out resolved))
                return false;

            return resolved.Scheme == Uri.UriSchemeHttp || resolved.Scheme == Uri.UriSchemeHttps;
        }

        private static List<string> SplitLines(string content)
        {
            var lines = new List<string>();
            using var reader = new StringReader(content);
            while (true)
            {
                string? line = reader.ReadLine();
                if (line == null) break;
                lines.Add(line);
            }
            return lines;
        }

        private static string NormalizeSegmentKey(Uri segmentUri)
        {
            string left = segmentUri.GetLeftPart(UriPartial.Path);
            string fileName;
            try
            {
                fileName = Path.GetFileName(new Uri(left).AbsolutePath);
            }
            catch
            {
                fileName = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;

            int slash = left.LastIndexOf('/');
            return slash >= 0 && slash + 1 < left.Length ? left.Substring(slash + 1) : left;
        }

        private static void TrimSeenCache(Dictionary<string, DateTime> seen)
        {
            if (seen.Count <= 2048) return;
            DateTime cutoff = DateTime.UtcNow.AddMinutes(-10);
            var remove = new List<string>();
            foreach (var kv in seen)
            {
                if (kv.Value < cutoff)
                    remove.Add(kv.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                seen.Remove(remove[i]);
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                // Richiesto per stream HLS con certificati non trusted lato libVLC/Windows.
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
            };
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            return http;
        }

        private static int GetFreeLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string Short(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "unknown";
            message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return message.Length <= 240 ? message : message.Substring(0, 240);
        }

        private static string? TryParseRelayId(string? relayUrl)
        {
            if (string.IsNullOrWhiteSpace(relayUrl)) return null;
            if (!Uri.TryCreate(relayUrl, UriKind.Absolute, out var uri)) return null;

            string[] parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts.Length != 2) return null;
            if (!parts[0].Equals("s", StringComparison.OrdinalIgnoreCase)) return null;
            if (!parts[1].EndsWith(".ts", StringComparison.OrdinalIgnoreCase)) return null;
            return parts[1].Substring(0, parts[1].Length - 3);
        }

        private void Log(string message)
        {
            try { Logger?.Invoke(message); } catch { }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HlsToTsRelay));
        }

        private sealed class RelayRegistration : IDisposable
        {
            public RelayRegistration(string id, string localUrl, Uri upstreamUri)
            {
                Id = id;
                LocalUrl = localUrl;
                UpstreamUri = upstreamUri;
            }

            public string Id { get; }
            public string LocalUrl { get; }
            public Uri UpstreamUri { get; }
            public CancellationTokenSource Cancellation { get; } = new CancellationTokenSource();

            public void Dispose()
            {
                try { Cancellation.Cancel(); } catch { }
                Cancellation.Dispose();
            }
        }
    }
}
