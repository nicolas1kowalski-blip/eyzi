using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🔄 MISES À JOUR AUTOMATIQUES, comme un jeu sur Windows : l'application
    /// regarde les versions publiées sur GitHub (compilées automatiquement),
    /// télécharge l'installeur de la plus récente de SON CANAL, vérifie son
    /// empreinte SHA-256, l'installe en silence et se relance.
    ///   · canal test : toutes les versions, y compris les pré-versions ;
    ///   · canal prod : seulement les versions promues (non pré-versions).
    /// Les installeurs téléchargés sont gardés : « Revenir à la version
    /// précédente » relance le précédent.
    /// </summary>
    public static class Updater
    {
        private const string Repo = "nicolas1kowalski-blip/eyzi";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        static Updater()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("MesPremiersJeux/" + Current);
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public static Version Current => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 1);

        /// <summary>Compilée à la main (Visual Studio) : pas de mise à jour automatique.</summary>
        public static bool IsDevBuild => Current.Major == 0;

        public static string UpdatesDir
        {
            get
            {
                var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MesPremiersJeux", "MisesAJour");
                try { Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }

        public sealed class Info
        {
            public Version Version;
            public string Tag, Title, Notes, SetupName, SetupUrl, ShaUrl;
            public string LightName, LightUrl, LightShaUrl; // l'installeur léger (sans la voix hors ligne), s'il existe
            public string VoicePackId;                      // la voix hors ligne qui accompagne cette version (nom de l'asset voix-*.txt)
            public bool Prerelease;
            public bool IsNewer => Version > Current;
        }

        /// <summary>La version la plus récente du canal (ou null si aucune / hors ligne).
        /// On regarde TOUTES les versions publiées et on garde le numéro le plus haut :
        /// l'ordre renvoyé par GitHub n'est pas fiable (versions du même jour).</summary>
        public static async Task<Info> CheckAsync(bool testChannel)
        {
            var json = await Http.GetStringAsync("https://api.github.com/repos/" + Repo + "/releases?per_page=50");
            // Découpage grossier des releases : chaque objet commence par "url":"…/releases/<id>".
            var releases = Regex.Split(json, "(?=\\{\\s*\"url\"\\s*:\\s*\"https://api\\.github\\.com/repos/[^\"]+/releases/\\d+\")");
            Info best = null;
            foreach (var r in releases)
            {
                if (string.IsNullOrWhiteSpace(r) || !r.Contains("\"tag_name\"")) continue;
                bool draft = Regex.IsMatch(r, "\"draft\"\\s*:\\s*true");
                bool pre = Regex.IsMatch(r, "\"prerelease\"\\s*:\\s*true");
                if (draft || (pre && !testChannel)) continue;
                var tag = Cloud.JsonGet(r, "tag_name") ?? "";
                if (!Version.TryParse(tag.TrimStart('v', 'V'), out var v)) continue;
                var setup = Regex.Match(r, "\"browser_download_url\"\\s*:\\s*\"([^\"]*Eyzi-Setup-[^\"]*\\.exe)\"");
                if (!setup.Success) continue;
                var sha = Regex.Match(r, "\"browser_download_url\"\\s*:\\s*\"([^\"]*Eyzi-Setup-[^\"]*\\.exe\\.sha256)\"");
                var light = Regex.Match(r, "\"browser_download_url\"\\s*:\\s*\"([^\"]*Eyzi-Leger-[^\"]*\\.exe)\"");
                var lightSha = Regex.Match(r, "\"browser_download_url\"\\s*:\\s*\"([^\"]*Eyzi-Leger-[^\"]*\\.exe\\.sha256)\"");
                var pack = Regex.Match(r, "\"name\"\\s*:\\s*\"voix-([^\"]+)\\.txt\"");
                if (best != null && v <= best.Version) continue;
                best = new Info
                {
                    Version = v, Tag = tag, Prerelease = pre,
                    Title = Cloud.JsonGet(r, "name") ?? tag,
                    Notes = Cloud.JsonGet(r, "body") ?? "",
                    SetupUrl = setup.Groups[1].Value, SetupName = Path.GetFileName(setup.Groups[1].Value),
                    ShaUrl = sha.Success ? sha.Groups[1].Value : null,
                    LightUrl = light.Success ? light.Groups[1].Value : null,
                    LightName = light.Success ? Path.GetFileName(light.Groups[1].Value) : null,
                    LightShaUrl = lightSha.Success ? lightSha.Groups[1].Value : null,
                    VoicePackId = pack.Success ? pack.Groups[1].Value : null,
                };
            }
            return best;
        }

        /// <summary>Télécharge l'installeur, vérifie l'empreinte, l'installe en silence et relance l'application.
        /// Renvoie null si l'installation a démarré, sinon le message d'erreur.</summary>
        /// <param name="status">(texte, avancement 0..1, ou -1 si inconnu)</param>
        /// <summary>La voix hors ligne installée sur cette tablette (Voix\piper\version.txt), ou null.</summary>
        public static string LocalVoicePackId()
        {
            try
            {
                var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Voix", "piper", "version.txt");
                return File.Exists(p) ? File.ReadAllText(p).Trim() : null;
            }
            catch { return null; }
        }

        public static async Task<string> InstallAsync(Info info, Action<string, double> status)
        {
            try
            {
                // LÉGER OU COMPLET ? La voix hors ligne (~200 Mo) ne change presque jamais :
                // si celle de la tablette est celle publiée avec la version, l'installeur
                // léger suffit (dix fois plus petit). Sinon, ou en cas de doute, le complet.
                string name = info.SetupName, url = info.SetupUrl, shaUrl = info.ShaUrl;
                var local = LocalVoicePackId();
                bool light = info.LightUrl != null && !string.IsNullOrEmpty(info.VoicePackId)
                             && string.Equals(local, info.VoicePackId, StringComparison.OrdinalIgnoreCase);
                if (light) { name = info.LightName; url = info.LightUrl; shaUrl = info.LightShaUrl; }
                Log.Write("maj", light
                    ? "Mise à jour légère : la voix hors ligne « " + info.VoicePackId + " » est déjà installée"
                    : "Mise à jour complète (voix installée : « " + (local ?? "aucune") + " », voix publiée : « " + (info.VoicePackId ?? "?") + " »)");

                var path = Path.Combine(UpdatesDir, name);
                status?.Invoke("Téléchargement de la version " + info.Version + (light ? " (mise à jour légère)" : "") + "…", 0);
                using (var res = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    res.EnsureSuccessStatusCode();
                    long total = res.Content.Headers.ContentLength ?? -1;
                    using (var src = await res.Content.ReadAsStreamAsync())
                    using (var fs = File.Create(path))
                    {
                        var buf = new byte[256 * 1024];
                        long done = 0; int lastPct = -1; int n;
                        while ((n = await src.ReadAsync(buf, 0, buf.Length)) > 0)
                        {
                            await fs.WriteAsync(buf, 0, n);
                            done += n;
                            if (total > 0)
                            {
                                int pct = (int)(done * 100 / total);
                                if (pct != lastPct)
                                {
                                    lastPct = pct;
                                    status?.Invoke("Téléchargement de la version " + info.Version + " : " + pct + " % (" + (done / 1048576) + " / " + (total / 1048576) + " Mo)", pct / 100.0);
                                }
                            }
                            else status?.Invoke("Téléchargement de la version " + info.Version + " : " + (done / 1048576) + " Mo", -1);
                        }
                    }
                }
                if (shaUrl != null)
                {
                    status?.Invoke("Vérification du fichier…", -1);
                    var expected = (await Http.GetStringAsync(shaUrl)).Trim().Split(' ')[0].ToLowerInvariant();
                    var actual = Sha256(path);
                    if (expected != actual)
                    {
                        try { File.Delete(path); } catch { }
                        Log.Write("maj", "Empreinte différente : attendu " + expected + ", obtenu " + actual);
                        return "Le fichier téléchargé est abîmé (empreinte différente). Réessaie plus tard.";
                    }
                }
                Log.Write("maj", "Installation de " + info.Version + " depuis " + path);
                status?.Invoke("Installation : l'application se ferme et se relance toute seule…", 1);
                Settings.Load().Save(); // rien à changer, mais on s'assure que tout est écrit avant de quitter
                return Launch(path);
            }
            catch (Exception ex)
            {
                Log.Write("maj", "Échec : " + ex);
                return "Mise à jour impossible : " + ex.Message;
            }
        }

        /// <summary>L'installeur de la version précédente, s'il est encore là.</summary>
        public static string PreviousSetup()
        {
            try
            {
                return SetupFiles()
                    .Select(f => (Path: f, Ver: ParseVersion(f)))
                    .Where(t => t.Ver != null && t.Ver < Current)
                    .OrderByDescending(t => t.Ver)
                    .Select(t => t.Path)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        public static string Rollback()
        {
            var prev = PreviousSetup();
            if (prev == null) return "Aucune version précédente n'est disponible sur cette tablette.";
            Log.Write("maj", "Retour à " + prev);
            return Launch(prev);
        }

        /// <summary>L'installeur à lancer APRÈS la fermeture complète (lu par App.OnExit).</summary>
        public static string PendingSetup { get; private set; }

        private static string Launch(string setupPath)
        {
            if (!File.Exists(setupPath)) return "Installeur introuvable : " + setupPath;
            // ORDRE IMPORTANT : on se ferme D'ABORD, l'installeur part ENSUITE
            // (App.OnExit). La fermeture prend quelques secondes (statistiques,
            // envoi au nuage) ; lancé avant, l'installeur trouvait l'application
            // encore ouverte (fichiers en cours d'usage → installation abandonnée
            // en silence) et la relance se heurtait au verrou « instance unique ».
            PendingSetup = setupPath;
            Log.Write("maj", "Fermeture de l'application ; l'installeur partira à la sortie : " + setupPath);
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => System.Windows.Application.Current.Shutdown()));
            return null;
        }

        /// <summary>Appelé par App.OnExit, une fois tout fermé et le verrou relâché.</summary>
        public static void RunPendingSetup()
        {
            var path = PendingSetup;
            PendingSetup = null;
            if (path == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(path, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /LAUNCH=1") { UseShellExecute = true });
                Log.Write("maj", "Installeur lancé : " + path);
            }
            catch (Exception ex) { Log.Write("maj", "Impossible de lancer l'installeur : " + ex.Message); }
        }

        // Les installeurs gardés sur la tablette (complets et légers).
        private static IEnumerable<string> SetupFiles() =>
            Directory.GetFiles(UpdatesDir, "Eyzi-Setup-*.exe").Concat(Directory.GetFiles(UpdatesDir, "Eyzi-Leger-*.exe"));

        private static Version ParseVersion(string path)
        {
            var m = Regex.Match(Path.GetFileName(path), "(?:Setup|Leger)-(\\d+\\.\\d+\\.\\d+\\.\\d+)");
            return m.Success && Version.TryParse(m.Groups[1].Value, out var v) ? v : null;
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>Ménage : on garde les trois derniers installeurs.</summary>
        public static void Cleanup()
        {
            try
            {
                foreach (var f in SetupFiles()
                             .Select(f => (Path: f, Ver: ParseVersion(f))).OrderByDescending(t => t.Ver).Skip(3))
                    File.Delete(f.Path);
            }
            catch { }
        }
    }
}
