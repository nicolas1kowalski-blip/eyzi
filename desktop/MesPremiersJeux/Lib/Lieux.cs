using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 📍 OÙ EST L'ENFANT, d'après le Wi-Fi : le réseau de la maison, celui de
    /// l'école, le partage de connexion du téléphone dans la voiture. Le nom du
    /// réseau est lu sur la tablette (netsh) et associé une fois à un lieu par
    /// l'accompagnant ; ensuite le lieu se règle tout seul. Fichier lieux.ini
    /// (réseau=lieu). Rien de tout cela ne quitte la tablette : seul le mot
    /// « maison » ou « école » part avec une phrase à répondre.
    /// </summary>
    public static class Lieux
    {
        /// <summary>Le réseau courant est connu : (réseau, lieu). Levé sur un fil de fond.</summary>
        public static event Action<string, string> Changed;
        /// <summary>Un réseau jamais vu : (réseau). Levé sur un fil de fond, une fois par réseau.</summary>
        public static event Action<string> Inconnu;

        public const string Ignorer = "ignorer";
        public static readonly (string Cle, string Icone, string Nom)[] Tous = { ("maison", "🏠", "Maison"), ("ecole", "🏫", "École"), ("dehors", "🌳", "Dehors"), ("voiture", "🚗", "Voiture") };

        public static string ReseauActuel { get; private set; } = "";
        public static bool Actif => _timer != null;

        private static Dictionary<string, string> _map;
        private static Timer _timer;
        private static string _dernierReseau = "\0", _dernierInconnu = "";
        private static readonly object _lock = new object();
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "lieux.ini");

        public static string Nom(string cle) { var l = Tous.FirstOrDefault(x => x.Cle == cle); return l.Cle == null ? (cle == Ignorer ? "ne pas demander" : cle) : l.Icone + " " + l.Nom; }

        private static void Load()
        {
            lock (_lock)
            {
                if (_map != null) return;
                _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    if (!File.Exists(FilePath)) return;
                    foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                    {
                        int eq = line.LastIndexOf('=');
                        if (eq <= 0) continue;
                        _map[line.Substring(0, eq)] = line.Substring(eq + 1).Trim();
                    }
                }
                catch { }
            }
        }

        private static void Save()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllLines(FilePath, _map.Select(kv => kv.Key + "=" + kv.Value), Encoding.UTF8);
                }
                catch { }
            }
        }

        /// <summary>Les réseaux connus et leur lieu, pour l'administration.</summary>
        public static List<KeyValuePair<string, string>> Associations() { Load(); lock (_lock) return _map.OrderBy(kv => kv.Key).ToList(); }

        /// <summary>Le lieu d'un réseau, ou "" s'il est inconnu.</summary>
        public static string LieuDe(string reseau)
        {
            if (string.IsNullOrWhiteSpace(reseau)) return "";
            Load();
            lock (_lock) return _map.TryGetValue(reseau, out var l) ? l : "";
        }

        /// <summary>L'accompagnant a répondu « on est où ? » pour ce réseau (ou « ne plus demander »).</summary>
        public static void Associer(string reseau, string lieu)
        {
            if (string.IsNullOrWhiteSpace(reseau) || string.IsNullOrWhiteSpace(lieu)) return;
            Load();
            lock (_lock) _map[reseau.Trim()] = lieu.Trim();
            Save();
            Log.Write("lieu", "Réseau « " + reseau + " » : " + lieu);
            if (string.Equals(reseau.Trim(), ReseauActuel, StringComparison.OrdinalIgnoreCase) && lieu != Ignorer)
            {
                _dernierReseau = ReseauActuel;
                try { Changed?.Invoke(ReseauActuel, lieu); } catch { }
            }
        }

        public static void Oublier(string reseau)
        {
            Load();
            lock (_lock) _map.Remove(reseau);
            Save();
            if (string.Equals(reseau, _dernierReseau, StringComparison.OrdinalIgnoreCase)) _dernierReseau = "\0";
            if (string.Equals(reseau, _dernierInconnu, StringComparison.OrdinalIgnoreCase)) _dernierInconnu = "";
        }

        /// <summary>Surveille le réseau : tout de suite, puis toutes les 45 secondes.</summary>
        public static void Demarrer()
        {
            if (_timer != null) return;
            Load();
            _timer = new Timer(_ => { try { Verifier(); } catch (Exception ex) { Log.Write("lieu", "Vérification : " + ex.Message); } }, null, 1500, 45000);
        }

        public static void Arreter()
        {
            var t = _timer; _timer = null;
            try { t?.Dispose(); } catch { }
        }

        /// <summary>Lit le réseau et prévient si quelque chose a changé.</summary>
        public static void Verifier()
        {
            var reseau = LireReseau();
            ReseauActuel = reseau;
            if (reseau.Length == 0) return; // pas de Wi-Fi : on ne change rien, le bouton de lieu reste à la main
            if (string.Equals(reseau, _dernierReseau, StringComparison.OrdinalIgnoreCase)) return;
            var lieu = LieuDe(reseau);
            if (lieu.Length == 0)
            {
                if (string.Equals(reseau, _dernierInconnu, StringComparison.OrdinalIgnoreCase)) return;
                _dernierInconnu = reseau;
                try { Inconnu?.Invoke(reseau); } catch { }
                return;
            }
            _dernierReseau = reseau;
            if (lieu == Ignorer) return;
            try { Changed?.Invoke(reseau, lieu); } catch { }
        }

        /// <summary>Le nom (SSID) du Wi-Fi connecté, ou "" (netsh wlan show interfaces).</summary>
        public static string LireReseau()
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", "wlan show interfaces")
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                };
                try { psi.StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); } catch { }
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "";
                    var output = p.StandardOutput.ReadToEnd();
                    if (!p.WaitForExit(4000)) { try { p.Kill(); } catch { } return ""; }
                    foreach (var raw in output.Split('\n'))
                    {
                        var line = raw.Trim();
                        if (!line.StartsWith("SSID", StringComparison.OrdinalIgnoreCase)) continue; // pas BSSID
                        int c = line.IndexOf(':');
                        if (c < 0) continue;
                        var ssid = line.Substring(c + 1).Trim();
                        if (ssid.Length > 0) return ssid;
                    }
                }
            }
            catch (Exception ex) { Log.Write("lieu", "netsh : " + ex.Message); }
            return "";
        }
    }
}
