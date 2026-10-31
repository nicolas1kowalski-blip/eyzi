using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 📺 LA TÉLÉ LG (webOS) : la même API locale que les applications
    /// télécommande. Une WebSocket vers la télé (port 3000 ou 3001), une
    /// association acceptée une fois sur l'écran de la télé (la clé est gardée
    /// dans les réglages), puis des commandes « ssap:// » : volume, chaînes,
    /// lecture, applications, extinction. Les touches de navigation passent par
    /// la prise « pointer » de la télé. L'allumage se fait par Wake-on-LAN (la
    /// télé doit avoir « Allumage via le réseau / Quick Start+ » activé).
    /// Tout reste sur le réseau de la maison : rien ne sort.
    /// </summary>
    public static class LgTv
    {
        public static event Action<string> StatusChanged;
        public static string Status { get; private set; } = "off";
        public static string LastError { get; private set; } = "";
        public static bool Connected => _ws != null && _ws.State == WebSocketState.Open && _registered;

        private static ClientWebSocket _ws, _pointer;
        private static bool _registered;
        private static int _nextId = 1;
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static string _ip, _key;
        private static CancellationTokenSource _reader;

        private static void SetStatus(string s) { Status = s; try { StatusChanged?.Invoke(s); } catch { } }

        // ------------------------------------------------------------------
        //  Connexion et association
        // ------------------------------------------------------------------
        /// <summary>Se connecte à la télé et s'enregistre. Sans clé, la télé affiche une demande d'association : la clé obtenue est renvoyée.</summary>
        public static async Task<string> ConnectAsync(string ip, string key, int timeoutMs = 8000)
        {
            await _lock.WaitAsync();
            try
            {
                if (Connected && _ip == ip) return _key;
                Disconnect();
                _ip = ip; _key = key ?? "";
                SetStatus("connexion");
                var ws = await OpenSocketAsync(ip, timeoutMs);
                if (ws == null) { SetStatus("erreur"); return null; }
                _ws = ws;
                _reader = new CancellationTokenSource();
                _ = Task.Run(() => ReadLoop(ws, _reader.Token));

                string register = "{\"type\":\"register\",\"id\":\"register_0\",\"payload\":{\"forcePairing\":false,\"pairingType\":\"PROMPT\"," +
                    (string.IsNullOrEmpty(_key) ? "" : "\"client-key\":\"" + _key + "\",") +
                    "\"manifest\":{\"manifestVersion\":1,\"appVersion\":\"1.1\",\"signed\":{\"created\":\"20140509\",\"appId\":\"com.lge.test\",\"vendorId\":\"com.lge\",\"localizedAppNames\":{\"\":\"Eyzi\"},\"localizedVendorNames\":{\"\":\"Eyzi\"},\"permissions\":[\"TEST_SECURE\",\"CONTROL_INPUT_TEXT\",\"CONTROL_MOUSE_AND_KEYBOARD\",\"READ_INSTALLED_APPS\",\"READ_LGE_SDX\",\"READ_NOTIFICATIONS\",\"SEARCH\",\"WRITE_SETTINGS\",\"WRITE_NOTIFICATION_ALERT\",\"CONTROL_POWER\",\"READ_CURRENT_CHANNEL\",\"READ_RUNNING_APPS\",\"READ_UPDATE_INFO\",\"UPDATE_FROM_REMOTE_APP\",\"READ_LGE_TV_INPUT_EVENTS\",\"READ_TV_CURRENT_TIME\"],\"serial\":\"2f930e2d2cfe083771f68e4fe7bb07\"}," +
                    "\"permissions\":[\"LAUNCH\",\"LAUNCH_WEBAPP\",\"APP_TO_APP\",\"CLOSE\",\"TEST_OPEN\",\"TEST_PROTECTED\",\"CONTROL_AUDIO\",\"CONTROL_DISPLAY\",\"CONTROL_INPUT_JOYSTICK\",\"CONTROL_INPUT_MEDIA_RECORDING\",\"CONTROL_INPUT_MEDIA_PLAYBACK\",\"CONTROL_INPUT_TV\",\"CONTROL_POWER\",\"READ_APP_STATUS\",\"READ_CURRENT_CHANNEL\",\"READ_INPUT_DEVICE_LIST\",\"READ_NETWORK_STATE\",\"READ_RUNNING_APPS\",\"READ_TV_CHANNEL_LIST\",\"WRITE_NOTIFICATION_TOAST\",\"READ_POWER_STATE\",\"READ_COUNTRY_INFO\",\"READ_SETTINGS\",\"CONTROL_TV_SCREEN\",\"CONTROL_TV_STANBY\",\"CONTROL_FAVORITE_GROUP\",\"CONTROL_USER_INFO\",\"CHECK_BLUETOOTH_DEVICE\",\"CONTROL_BLUETOOTH\",\"CONTROL_TIMER_INFO\",\"STB_INTERNAL_CONNECTION\",\"CONTROL_RECORDING\",\"READ_RECORDING_STATE\",\"WRITE_RECORDING_LIST\",\"READ_RECORDING_LIST\",\"READ_RECORDING_SCHEDULE\",\"WRITE_RECORDING_SCHEDULE\",\"READ_STORAGE_DEVICE_LIST\",\"READ_TV_PROGRAM_INFO\",\"CONTROL_BOX_CHANNEL\",\"READ_TV_ACR_AUTH_TOKEN\",\"READ_TV_CONTENT_STATE\",\"READ_TV_CURRENT_TIME\",\"ADD_LAUNCHER_CHANNEL\",\"SET_CHANNEL_SKIP\",\"RELEASE_CHANNEL_SKIP\",\"CONTROL_CHANNEL_BLOCK\",\"DELETE_SELECT_CHANNEL\",\"CONTROL_CHANNEL_GROUP\",\"SCAN_TV_CHANNELS\",\"CONTROL_TV_POWER\",\"CONTROL_WOL\"]," +
                    "\"signatures\":[{\"signatureVersion\":1,\"signature\":\"eyJhbGdvcml0aG0iOiJSU0EtU0hBMjU2Iiwia2V5SWQiOiJ0ZXN0LXNpZ25pbmctY2VydCIsInNpZ25hdHVyZVZlcnNpb24iOjF9.hrVRgjCwXVvE2OOSpDZ58hR+59aFNwYDyjQgKk3auukd7pcegmE2CzPCa0bJ0ZsRAcKkCTJrWo5iDzNhMBWRyaMOv5zWSrthlf7G128qvIlpMT0YNY+n/FaOHE73uLrS/g7swl3/qH/BGFG2Hu4RlL48eb3lLKqTt2xKHdCs6Cd4RMfJPYnzgvI4BNrFUKsjkcu+WD4OO2A27Pq1n50cMchmcaXadJhGrOqH5YmHdOCj5NSHzJYrsW0HPlpuAx/ECMeIZYDh6RMqaFM2DXzdKX9NmmyqzJ3o/0lkk/N97gfVRLW5hA29yeAwaCViZNCP8iC9aO0q9fQojoa7NQnAtw==\"}]}}}";
                var tcs = new TaskCompletionSource<string>();
                _pending["register_0"] = tcs;
                await SendRaw(ws, register);
                // La première fois, la télé affiche « Autoriser Eyzi ? » : on attend jusqu'à une minute.
                var done = await Task.WhenAny(tcs.Task, Task.Delay(string.IsNullOrEmpty(_key) ? 60000 : timeoutMs));
                if (done != tcs.Task) { LastError = string.IsNullOrEmpty(_key) ? "La télé n'a pas répondu à la demande d'association." : "La télé ne répond pas."; Disconnect(); SetStatus("erreur"); return null; }
                var resp = tcs.Task.Result;
                var k = Cloud.JsonGet(resp, "client-key");
                if (string.IsNullOrEmpty(k)) { LastError = "Association refusée : " + (Cloud.JsonGet(resp, "error") ?? resp); Disconnect(); SetStatus("erreur"); return null; }
                _key = k; _registered = true;
                SetStatus("pret");
                Log.Write("tv", "Connectée à " + ip);
                return k;
            }
            catch (Exception ex) { LastError = ex.Message; Log.Write("tv", "Connexion : " + ex.Message); Disconnect(); SetStatus("erreur"); return null; }
            finally { _lock.Release(); }
        }

        private static async Task<ClientWebSocket> OpenSocketAsync(string ip, int timeoutMs)
        {
            // Les télés récentes n'ouvrent que le port 3001 (wss, certificat auto-signé) ; les anciennes le 3000 (ws).
            foreach (var url in new[] { "wss://" + ip + ":3001", "ws://" + ip + ":3000" })
            {
                var ws = new ClientWebSocket();
                try
                {
                    if (url.StartsWith("wss")) AcceptTvCertificate(ip);
                    using (var cts = new CancellationTokenSource(timeoutMs)) await ws.ConnectAsync(new Uri(url), cts.Token);
                    return ws;
                }
                catch (Exception ex) { Log.Write("tv", url + " : " + ex.GetBaseException().Message); try { ws.Dispose(); } catch { } }
            }
            LastError = "Impossible de joindre la télé à " + ip + ". Est-elle allumée, sur le même Wi-Fi ?";
            return null;
        }

        private static readonly HashSet<string> _trusted = new HashSet<string>();
        /// <summary>Le certificat de la télé est auto-signé : on l'accepte, pour cette adresse seulement.</summary>
        private static void AcceptTvCertificate(string ip)
        {
            lock (_trusted)
            {
                if (_trusted.Count == 0)
                {
                    var prev = ServicePointManager.ServerCertificateValidationCallback;
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) =>
                    {
                        if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                        var host = (sender as HttpWebRequest)?.RequestUri?.Host ?? (sender as WebRequest)?.RequestUri?.Host;
                        if (host != null) lock (_trusted) if (_trusted.Contains(host)) return true;
                        return prev != null && prev(sender, cert, chain, errors);
                    };
                }
                _trusted.Add(ip);
            }
        }

        public static void Disconnect()
        {
            _registered = false;
            try { _reader?.Cancel(); } catch { }
            var p = _pointer; _pointer = null; try { p?.Dispose(); } catch { }
            var w = _ws; _ws = null; try { w?.Dispose(); } catch { }
            foreach (var kv in _pending) kv.Value.TrySetCanceled();
            _pending.Clear();
            SetStatus("off");
        }

        private static async Task ReadLoop(ClientWebSocket ws, CancellationToken ct)
        {
            var buf = new byte[64 * 1024];
            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var sb = new StringBuilder();
                    WebSocketReceiveResult r;
                    do { r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct); sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count)); } while (!r.EndOfMessage);
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    var msg = sb.ToString();
                    var id = Cloud.JsonGet(msg, "id"); var type = Cloud.JsonGet(msg, "type");
                    if (id != null && _pending.TryGetValue(id, out var tcs))
                    {
                        // Pendant l'association, la télé répond d'abord « response » (PROMPT), puis « registered » : on garde le dernier.
                        if (id == "register_0" && type == "response") continue;
                        _pending.TryRemove(id, out _);
                        tcs.TrySetResult(msg);
                    }
                }
            }
            catch (Exception ex) { if (!ct.IsCancellationRequested) Log.Write("tv", "Lecture : " + ex.Message); }
            if (!ct.IsCancellationRequested) { _registered = false; SetStatus("off"); }
        }

        private static Task SendRaw(ClientWebSocket ws, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            return ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>Envoie une requête « ssap:// » et attend la réponse (JSON brut).</summary>
        public static async Task<string> RequestAsync(string uri, string payloadJson = null, int timeoutMs = 5000)
        {
            var ws = _ws;
            if (ws == null || ws.State != WebSocketState.Open || !_registered) { LastError = "Pas connectée à la télé."; return null; }
            var id = "r" + Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<string>(); _pending[id] = tcs;
            var json = "{\"type\":\"request\",\"id\":\"" + id + "\",\"uri\":\"" + uri + "\"" + (payloadJson != null ? ",\"payload\":" + payloadJson : "") + "}";
            try { await SendRaw(ws, json); } catch (Exception ex) { _pending.TryRemove(id, out _); LastError = ex.Message; return null; }
            var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            _pending.TryRemove(id, out _);
            if (done != tcs.Task) { LastError = "La télé n'a pas répondu."; return null; }
            var resp = tcs.Task.Result;
            if (Cloud.JsonGet(resp, "returnValue") == "false") { LastError = Cloud.JsonGet(resp, "errorText") ?? "refusé"; Log.Write("tv", uri + " : " + LastError); }
            return resp;
        }

        // ------------------------------------------------------------------
        //  Les commandes
        // ------------------------------------------------------------------
        public static Task<string> VolumeUp() => RequestAsync("ssap://audio/volumeUp");
        public static Task<string> VolumeDown() => RequestAsync("ssap://audio/volumeDown");
        public static async Task<string> ToggleMute()
        {
            var st = await RequestAsync("ssap://audio/getStatus");
            bool muted = Cloud.JsonGet(st ?? "", "mute") == "true";
            return await RequestAsync("ssap://audio/setMute", "{\"mute\":" + (muted ? "false" : "true") + "}");
        }
        public static Task<string> ChannelUp() => RequestAsync("ssap://tv/channelUp");
        public static Task<string> ChannelDown() => RequestAsync("ssap://tv/channelDown");
        public static Task<string> Play() => RequestAsync("ssap://media.controls/play");
        public static Task<string> Pause() => RequestAsync("ssap://media.controls/pause");
        public static Task<string> TurnOff() => RequestAsync("ssap://system/turnOff");
        public static Task<string> Launch(string appId) => RequestAsync("ssap://system.launcher/launch", "{\"id\":\"" + appId + "\"}");
        public static Task<string> Toast(string text) => RequestAsync("ssap://system.notifications/createToast", "{\"message\":\"" + text.Replace("\"", "'") + "\"}");

        /// <summary>L'adresse matérielle de la télé (pour l'allumer par le réseau).</summary>
        public static async Task<string> GetMacAsync()
        {
            var r = await RequestAsync("ssap://com.webos.service.connectionmanager/getinfo");
            if (r == null) return null;
            var m = Regex.Match(r, "\"macAddress\"\\s*:\\s*\"([0-9A-Fa-f:]{17})\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Une touche de la télécommande : HOME, BACK, ENTER, UP, DOWN, LEFT, RIGHT, EXIT, MENU…</summary>
        public static async Task<bool> ButtonAsync(string name)
        {
            try
            {
                if (_pointer == null || _pointer.State != WebSocketState.Open)
                {
                    var r = await RequestAsync("ssap://com.webos.service.networkinput/getPointerInputSocket");
                    var path = r == null ? null : Cloud.JsonGet(r, "socketPath");
                    if (string.IsNullOrEmpty(path)) return false;
                    var p = new ClientWebSocket();
                    if (path.StartsWith("wss")) AcceptTvCertificate(new Uri(path).Host);
                    using (var cts = new CancellationTokenSource(5000)) await p.ConnectAsync(new Uri(path), cts.Token);
                    _pointer = p;
                }
                await SendRaw(_pointer, "type:button\nname:" + name + "\n\n");
                return true;
            }
            catch (Exception ex) { LastError = ex.Message; Log.Write("tv", "Touche " + name + " : " + ex.Message); try { _pointer?.Dispose(); } catch { } _pointer = null; return false; }
        }

        // ------------------------------------------------------------------
        //  Allumer (Wake-on-LAN) et trouver la télé (SSDP)
        // ------------------------------------------------------------------
        public static void WakeOnLan(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac)) return;
            var hex = Regex.Replace(mac, "[^0-9A-Fa-f]", "");
            if (hex.Length != 12) return;
            var macBytes = new byte[6]; for (int i = 0; i < 6; i++) macBytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            var packet = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++) packet[i] = 0xFF;
            for (int i = 0; i < 16; i++) Array.Copy(macBytes, 0, packet, 6 + i * 6, 6);
            try
            {
                using (var udp = new UdpClient { EnableBroadcast = true })
                {
                    for (int i = 0; i < 3; i++) { udp.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9)); udp.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 7)); }
                }
                Log.Write("tv", "Wake-on-LAN envoyé à " + mac);
            }
            catch (Exception ex) { LastError = ex.Message; Log.Write("tv", "Wake-on-LAN : " + ex.Message); }
        }

        /// <summary>Cherche une télé LG sur le réseau (SSDP) : renvoie son adresse, ou null.</summary>
        public static async Task<string> DiscoverAsync(int timeoutMs = 3000)
        {
            try
            {
                using (var udp = new UdpClient())
                {
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udp.Client.ReceiveTimeout = timeoutMs;
                    var msg = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: urn:lge-com:service:webos-second-screen:1\r\n\r\n";
                    var bytes = Encoding.ASCII.GetBytes(msg);
                    var ep = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
                    for (int i = 0; i < 2; i++) await udp.SendAsync(bytes, bytes.Length, ep);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < timeoutMs)
                    {
                        var recv = udp.ReceiveAsync();
                        var done = await Task.WhenAny(recv, Task.Delay(Math.Max(1, (int)(timeoutMs - sw.ElapsedMilliseconds))));
                        if (done != recv) break;
                        var text = Encoding.ASCII.GetString(recv.Result.Buffer);
                        if (text.IndexOf("webos", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("LG", StringComparison.Ordinal) >= 0)
                            return recv.Result.RemoteEndPoint.Address.ToString();
                    }
                }
            }
            catch (Exception ex) { Log.Write("tv", "Recherche : " + ex.Message); }
            return null;
        }
    }
}
