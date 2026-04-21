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
                string path = ctx.Request.Url?.AbsolutePath ?? "";
                if (!path.Equals("/p", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 404;
                    return;
                }

                string upstreamRaw = GetQueryParam(ctx.Request.Url?.Query, "u");
                if (string.IsNullOrWhiteSpace(upstreamRaw))
                {
                    ctx.Response.StatusCode = 400;
                    return;
                }

                if (!Uri.TryCreate(upstreamRaw, UriKind.Absolute, out var upstreamUri))
                {
                    ctx.Response.StatusCode = 400;
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
                await using var body = await upstreamResp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await body.CopyToAsync(ctx.Response.OutputStream).ConfigureAwait(false);
                Log($"GET {upstreamUri} → {(int)upstreamResp.StatusCode}");
            }
            catch (Exception ex)
            {
                try
                {
                    ctx.Response.StatusCode = 502;
                }
                catch { }
                Log("ERR request: " + ex.Message);
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

            var rewritten = RewriteManifest(content, upstreamUri, out int rewrittenItems);
            var bytes = Encoding.UTF8.GetBytes(rewritten);

            ctx.Response.StatusCode = (int)upstreamResp.StatusCode;
            CopyResponseHeaders(upstreamResp, ctx.Response, includeContentLength: false, includeContentType: false);
            ctx.Response.ContentType = "application/vnd.apple.mpegurl";
            ctx.Response.ContentLength64 = bytes.LongLength;
            ctx.Response.Headers["Cache-Control"] = "no-cache";
            ctx.Response.Headers["Pragma"] = "no-cache";
            ctx.Response.Headers["Expires"] = "0";
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            Log($"GET {upstreamUri} → {(int)upstreamResp.StatusCode} (HLS rewrite, {rewrittenItems} segments)");
        }

        private string RewriteManifest(string content, Uri originalUri, out int rewrittenItems)
        {
            int changedCount = 0;
            var sb = new StringBuilder(content.Length + 256);
            var baseUri = new Uri(originalUri, "./");
            string newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            using var reader = new StringReader(content);
            string? line;
            bool first = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (!first) sb.Append(newline);
                first = false;

                if (line.StartsWith("#EXT-X-KEY:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#EXT-X-MAP:", StringComparison.OrdinalIgnoreCase))
                {
                    string replaced = Regex.Replace(line, "URI=\"([^\"]+)\"", m =>
                    {
                        string uriValue = m.Groups[1].Value;
                        string rewritten = RewritePlaylistUri(uriValue, baseUri);
                        if (!string.Equals(uriValue, rewritten, StringComparison.Ordinal))
                            changedCount++;
                        return "URI=\"" + rewritten + "\"";
                    });
                    sb.Append(replaced);
                    continue;
                }

                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    string rewritten = RewritePlaylistUri(line.Trim(), baseUri);
                    if (!string.Equals(line.Trim(), rewritten, StringComparison.Ordinal))
                        changedCount++;
                    sb.Append(rewritten);
                    continue;
                }

                sb.Append(line);
            }
            rewrittenItems = changedCount;
            return sb.ToString();
        }

        private string RewritePlaylistUri(string value, Uri baseUri)
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

            return BuildProxyUrl(absolute.ToString());
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

        private string BuildProxyUrl(string originalUrl)
        {
            return $"http://127.0.0.1:{Port}/p?u={Uri.EscapeDataString(originalUrl)}";
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
