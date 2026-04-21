using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AirDirector.Services.Streaming
{
    public class HttpsPassthroughProxy : IDisposable
    {
        private const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        private static readonly object _sync = new object();
        private static HttpsPassthroughProxy? _instance;

        private readonly HttpListener _listener;
        private readonly HttpClient _httpClient;
        private readonly Action<string>? _logger;
        private volatile bool _running;
        private bool _loggedManifestDump;

        public int Port { get; }

        private HttpsPassthroughProxy(Action<string>? logger)
        {
            _logger = logger;
            Port = GetFreeLoopbackPort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = System.Threading.Timeout.InfiniteTimeSpan
            };

            _listener.Start();
            _running = true;
            _ = Task.Run(ListenLoopAsync);
        }

        public static HttpsPassthroughProxy Start(Action<string>? logger = null)
        {
            lock (_sync)
            {
                if (_instance == null)
                    _instance = new HttpsPassthroughProxy(logger);
                return _instance;
            }
        }

        public string Rewrite(string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
                return originalUrl;

            if (!originalUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return originalUrl;

            return BuildProxyUrl(originalUrl);
        }

        private async Task ListenLoopAsync()
        {
            while (_running)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) when (!_running) { break; }
                catch (HttpListenerException) when (!_running) { break; }
                catch (Exception ex)
                {
                    Log("ERR listen: " + ex.Message);
                    continue;
                }

                if (ctx != null)
                    _ = Task.Run(() => HandleRequestAsync(ctx));
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            try
            {
                if (!TryGetUpstreamUrl(ctx.Request, out string upstreamRaw, out string error))
                {
                    ctx.Response.StatusCode = 400;
                    ApplyProxyHeaders(ctx.Response);
                    await WriteTextResponseAsync(ctx.Response, error).ConfigureAwait(false);
                    Log($"[PROXY] ERR bad request: {ctx.Request.Url?.AbsolutePath}");
                    return;
                }

                if (!Uri.TryCreate(upstreamRaw, UriKind.Absolute, out var upstreamUri))
                {
                    ctx.Response.StatusCode = 400;
                    ApplyProxyHeaders(ctx.Response);
                    await WriteTextResponseAsync(ctx.Response, "Invalid upstream URL").ConfigureAwait(false);
                    Log($"[PROXY] ERR bad request: {ctx.Request.Url?.AbsolutePath}");
                    return;
                }

                if (upstreamUri.Scheme != Uri.UriSchemeHttp && upstreamUri.Scheme != Uri.UriSchemeHttps)
                {
                    ctx.Response.StatusCode = 400;
                    ApplyProxyHeaders(ctx.Response);
                    await WriteTextResponseAsync(ctx.Response, "Only http/https URLs are supported").ConfigureAwait(false);
                    Log($"[PROXY] ERR bad request: {ctx.Request.Url?.AbsolutePath}");
                    return;
                }

                using var req = new HttpRequestMessage(HttpMethod.Get, upstreamUri);
                ForwardRequestHeaders(ctx.Request, req);
                string? userAgent = ctx.Request.UserAgent;
                if (!string.IsNullOrWhiteSpace(userAgent))
                    req.Headers.UserAgent.TryParseAdd(userAgent);
                if (req.Headers.UserAgent.Count == 0)
                    req.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);

                using var upstreamResp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                bool isHls = IsHlsResponse(upstreamUri, upstreamResp);
                if (isHls)
                {
                    await HandleHlsManifestAsync(ctx, upstreamUri, upstreamResp).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = (int)upstreamResp.StatusCode;
                CopyResponseHeaders(upstreamResp, ctx.Response, includeContentLength: true, includeContentType: true);
                ApplyProxyHeaders(ctx.Response);
                await using var body = await upstreamResp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                long copiedBytes = await CopyToWithCountAsync(body, ctx.Response.OutputStream).ConfigureAwait(false);
                string contentType = upstreamResp.Content.Headers.ContentType?.ToString() ?? "(none)";
                Log($"[PROXY] GET {upstreamUri} → {(int)upstreamResp.StatusCode} ({copiedBytes} bytes, {contentType})");
            }
            catch (Exception ex)
            {
                try
                {
                    ctx.Response.StatusCode = 502;
                    ApplyProxyHeaders(ctx.Response);
                    await WriteTextResponseAsync(ctx.Response, "Proxy error").ConfigureAwait(false);
                }
                catch { }
                Log("[PROXY] ERR request: " + TruncateException(ex));
            }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
                try { ctx.Response.Close(); } catch { }
            }
        }

        private async Task HandleHlsManifestAsync(HttpListenerContext ctx, Uri upstreamUri, HttpResponseMessage upstreamResp)
        {
            string content;
            await using (var body = await upstreamResp.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var rewritten = RewriteManifest(content, upstreamUri, out int rewrittenItems, out var rewriteSamples);
            var bytes = Encoding.UTF8.GetBytes(rewritten);

            ctx.Response.StatusCode = (int)upstreamResp.StatusCode;
            CopyResponseHeaders(upstreamResp, ctx.Response, includeContentLength: false, includeContentType: false);
            ApplyProxyHeaders(ctx.Response);
            ctx.Response.ContentType = "application/vnd.apple.mpegurl";
            ctx.Response.ContentLength64 = bytes.LongLength;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            Log($"[PROXY] GET {upstreamUri} → {(int)upstreamResp.StatusCode} (HLS rewrite, {rewrittenItems} segments)");
            foreach (var sample in rewriteSamples)
                Log("[PROXY] HLS rewrite sample: " + sample);

            if (!_loggedManifestDump)
            {
                _loggedManifestDump = true;
                Log("[PROXY] HLS upstream head: " + TruncateSingleLine(content, 500));
                Log("[PROXY] HLS rewritten head: " + TruncateSingleLine(rewritten, 500));
            }
        }

        private string RewriteManifest(string content, Uri originalUri, out int rewrittenItems, out string[] rewriteSamples)
        {
            int changedCount = 0;
            var samples = new System.Collections.Generic.List<string>(2);
            var sb = new StringBuilder(content.Length + 256);
            var baseUri = new Uri(originalUri, "./");
            string newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            using var reader = new StringReader(content);
            string? line;
            bool first = true;
            bool nextLineIsVariantPlaylist = false;
            while ((line = reader.ReadLine()) != null)
            {
                if (!first) sb.Append(newline);
                first = false;

                if (line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                    nextLineIsVariantPlaylist = true;

                if (line.StartsWith("#EXT", StringComparison.OrdinalIgnoreCase) && line.IndexOf("URI=\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string replaced = Regex.Replace(line, "URI=\"([^\"]+)\"", m =>
                    {
                        string uriValue = m.Groups[1].Value;
                        bool isPlaylistUriTag =
                            line.StartsWith("#EXT-X-MEDIA:", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("#EXT-X-I-FRAME-STREAM-INF:", StringComparison.OrdinalIgnoreCase);
                        string rewritten = RewritePlaylistUri(uriValue, baseUri, isPlaylistUriTag);
                        if (!string.Equals(uriValue, rewritten, StringComparison.Ordinal))
                        {
                            changedCount++;
                            if (samples.Count < 2)
                                samples.Add($"{uriValue} → {rewritten}");
                        }
                        return "URI=\"" + rewritten + "\"";
                    });
                    sb.Append(replaced);
                    continue;
                }

                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    string original = line.Trim();
                    string rewritten = RewritePlaylistUri(original, baseUri, preferPlaylistDummy: nextLineIsVariantPlaylist);
                    if (!string.Equals(line.Trim(), rewritten, StringComparison.Ordinal))
                    {
                        changedCount++;
                        if (samples.Count < 2)
                            samples.Add($"{original} → {rewritten}");
                    }
                    sb.Append(rewritten);
                    nextLineIsVariantPlaylist = false;
                    continue;
                }

                sb.Append(line);
            }

            if (sb.Length == 0 || sb[sb.Length - 1] != '\n')
                sb.Append('\n');

            rewrittenItems = changedCount;
            rewriteSamples = samples.ToArray();
            return sb.ToString();
        }

        private string RewritePlaylistUri(string value, Uri baseUri, bool preferPlaylistDummy)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            {
                try { absolute = new Uri(baseUri, value); }
                catch { return value; }
            }

            if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
                return value;

            string dummyName = GetDummyName(absolute, preferPlaylistDummy);
            return BuildProxyUrl(absolute.ToString(), dummyName);
        }

        private void ForwardRequestHeaders(HttpListenerRequest source, HttpRequestMessage target)
        {
            foreach (string key in source.Headers.AllKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;

                var val = source.Headers[key];
                if (string.IsNullOrEmpty(val)) continue;
                target.Headers.TryAddWithoutValidation(key, val);
            }

            var range = source.Headers["Range"];
            if (!string.IsNullOrWhiteSpace(range))
                target.Headers.TryAddWithoutValidation("Range", range);
        }

        private static bool IsHlsResponse(Uri upstreamUri, HttpResponseMessage response)
        {
            string ct = response.Content.Headers.ContentType?.MediaType ?? "";
            if (ct.IndexOf("mpegurl", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (ct.IndexOf("m3u8", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return upstreamUri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyResponseHeaders(HttpResponseMessage source, HttpListenerResponse target, bool includeContentLength, bool includeContentType)
        {
            foreach (var h in source.Headers)
            {
                if (SkipResponseHeader(h.Key)) continue;
                TrySetHeader(target, h.Key, string.Join(",", h.Value));
            }

            foreach (var h in source.Content.Headers)
            {
                if (SkipResponseHeader(h.Key)) continue;
                if (!includeContentLength && h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                if (!includeContentType && h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                TrySetHeader(target, h.Key, string.Join(",", h.Value));
            }
        }

        private static bool SkipResponseHeader(string key)
        {
            if (key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void TrySetHeader(HttpListenerResponse response, string key, string value)
        {
            try { response.Headers[key] = value; } catch { }
        }

        private string BuildProxyUrl(string originalUrl, string? dummyName = null)
        {
            string token = EncodeBase64Url(originalUrl);
            string safeDummy = dummyName ?? "playlist.m3u8";
            if (string.IsNullOrWhiteSpace(dummyName) && Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri))
                safeDummy = GetDummyName(uri, preferPlaylistDummy: true);
            return $"http://127.0.0.1:{Port}/p/{token}/{Uri.EscapeDataString(safeDummy)}";
        }

        private static int GetFreeLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string GetQueryParam(string? query, string key)
        {
            if (string.IsNullOrEmpty(query)) return "";
            string q = query.StartsWith("?") ? query.Substring(1) : query;
            string[] parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                int idx = part.IndexOf('=');
                string k = idx >= 0 ? part.Substring(0, idx) : part;
                if (!k.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
                string v = idx >= 0 ? part.Substring(idx + 1) : "";
                return Uri.UnescapeDataString(v);
            }
            return "";
        }

        private static async Task WriteTextResponseAsync(HttpListenerResponse response, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? "");
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = bytes.LongLength;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        private static async Task<long> CopyToWithCountAsync(Stream source, Stream destination)
        {
            byte[] buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                total += read;
            }
            return total;
        }

        private static string GetDummyName(Uri uri, bool preferPlaylistDummy)
        {
            string last = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath) ?? "");
            if (!string.IsNullOrWhiteSpace(last) && !string.IsNullOrWhiteSpace(Path.GetExtension(last)))
                return last;

            return preferPlaylistDummy ? "playlist.m3u8" : "seg.ts";
        }

        private static string EncodeBase64Url(string text)
        {
            var raw = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static bool TryDecodeBase64Url(string token, out string value)
        {
            value = "";
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string base64 = token.Replace('-', '+').Replace('_', '/');
            int mod = base64.Length % 4;
            if (mod > 0)
                base64 = base64.PadRight(base64.Length + (4 - mod), '=');

            try
            {
                var bytes = Convert.FromBase64String(base64);
                value = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TruncateException(Exception ex)
        {
            var full = ex.ToString();
            const int max = 1200;
            return full.Length <= max ? full : full.Substring(0, max) + "...";
        }

        private static string TruncateSingleLine(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string oneLine = text.Replace('\r', ' ').Replace('\n', ' ');
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "...";
        }

        private static void ApplyProxyHeaders(HttpListenerResponse response)
        {
            TrySetHeader(response, "Cache-Control", "no-cache, no-store, must-revalidate");
            TrySetHeader(response, "Pragma", "no-cache");
            TrySetHeader(response, "Expires", "0");
            TrySetHeader(response, "Access-Control-Allow-Origin", "*");
        }

        private static bool TryGetUpstreamUrl(HttpListenerRequest request, out string upstreamRaw, out string error)
        {
            upstreamRaw = "";
            error = "Missing upstream URL";

            string path = request.Url?.AbsolutePath ?? "";
            if (path.StartsWith("/p/", StringComparison.OrdinalIgnoreCase))
            {
                string rest = path.Substring(3);
                if (string.IsNullOrWhiteSpace(rest))
                {
                    error = "Malformed proxy path";
                    return false;
                }

                int slash = rest.IndexOf('/');
                string token = slash >= 0 ? rest.Substring(0, slash) : rest;
                if (!TryDecodeBase64Url(token, out upstreamRaw))
                {
                    error = "Invalid base64url token";
                    return false;
                }
                return true;
            }

            if (!path.Equals("/p", StringComparison.OrdinalIgnoreCase))
            {
                error = "Unsupported proxy path";
                return false;
            }

            upstreamRaw = GetQueryParam(request.Url?.Query, "u");
            if (string.IsNullOrWhiteSpace(upstreamRaw))
            {
                error = "Missing query parameter u";
                return false;
            }
            return true;
        }

        private void Log(string message)
        {
            try { _logger?.Invoke(message); } catch { }
        }

        public void Dispose()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
            try { _httpClient.Dispose(); } catch { }
            lock (_sync)
            {
                if (ReferenceEquals(_instance, this))
                    _instance = null;
            }
        }
    }
}
