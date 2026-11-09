using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
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
        // Le téléchargement de l'installeur : SANS délai global (358 Mo sur une connexion lente dépassent
        // n'importe quel délai fixe) ; seule l'inactivité compte (plus rien reçu pendant 90 s), et le
        // téléchargement reprend là où il s'est arrêté (Range) jusqu'à quatre essais.
        private static readonly HttpClient Dl = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private const int Inactivite = 90; // secondes sans rien recevoir avant de réessayer

        static Updater()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("MesPremiersJeux/" + Current);
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            Dl.DefaultRequestHeaders.UserAgent.ParseAdd("MesPremiersJeux/" + Current);
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
            public string PictoPackId;                      // le lot de pictogrammes ARASAAC qui accompagne cette version (asset pictos-*.txt)
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
                var pictos = Regex.Match(r, "\"name\"\\s*:\\s*\"pictos-([^\"]+)\\.txt\"");
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
                    PictoPackId = pictos.Success ? pictos.Groups[1].Value : null,
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

        /// <summary>Le lot de pictogrammes ARASAAC installé (Images\arasaac\version.txt), ou null.</summary>
        public static string LocalPictoPackId()
        {
            try
            {
                var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "arasaac", "version.txt");
                return File.Exists(p) ? File.ReadAllText(p).Trim() : null;
            }
            catch { return null; }
        }

        public static async Task<string> InstallAsync(Info info, Action<string, double> status)
        {
            // L'avancement peut venir d'un fil de fond (vérification du fichier) :
            // on le ramène toujours sur le fil d'affichage.
            var ui = status;
            status = (t, f) =>
            {
                var d = System.Windows.Application.Current?.Dispatcher;
                if (d != null && !d.CheckAccess()) d.BeginInvoke(new Action(() => ui?.Invoke(t, f)));
                else ui?.Invoke(t, f);
            };
            try
            {
                // LÉGER OU COMPLET ? La voix hors ligne (~200 Mo) ne change presque jamais :
                // si celle de la tablette est celle publiée avec la version, l'installeur
                // léger suffit (dix fois plus petit). Sinon, ou en cas de doute, le complet.
                string name = info.SetupName, url = info.SetupUrl, shaUrl = info.ShaUrl;
                var local = LocalVoicePackId();
                var localPictos = LocalPictoPackId();
                bool voixOk = !string.IsNullOrEmpty(info.VoicePackId) && string.Equals(local, info.VoicePackId, StringComparison.OrdinalIgnoreCase)
                              && PiperTts.Available; // l'étiquette ne suffit pas : les fichiers de la voix doivent être là
                bool pictosOk = string.IsNullOrEmpty(info.PictoPackId) // une version sans lot de pictos : rien à comparer
                                || (string.Equals(localPictos, info.PictoPackId, StringComparison.OrdinalIgnoreCase) && Arasaac.Available);
                bool light = info.LightUrl != null && voixOk && pictosOk;
                if (light) { name = info.LightName; url = info.LightUrl; shaUrl = info.LightShaUrl; }
                Log.Write("maj", light
                    ? "Mise à jour légère : la voix « " + info.VoicePackId + " » et les pictos « " + (info.PictoPackId ?? "-") + " » sont déjà installés"
                    : "Mise à jour complète (voix installée : « " + (local ?? "aucune") + " », publiée : « " + (info.VoicePackId ?? "?") + " » ; pictos installés : « " + (localPictos ?? "aucun") + " », publiés : « " + (info.PictoPackId ?? "?") + " »)");

                var path = Path.Combine(UpdatesDir, name);
                Directory.CreateDirectory(UpdatesDir);
                status?.Invoke("Téléchargement de la version " + info.Version + (light ? " (mise à jour légère)" : "") + "…", 0);
                string expected = null;
                if (shaUrl != null) { try { expected = (await Http.GetStringAsync(shaUrl)).Trim().Split(' ')[0].ToLowerInvariant(); } catch (Exception ex) { Log.Write("maj", "Empreinte attendue illisible : " + ex.Message); } }
                // Le fichier est peut-être déjà là, entier et bon (un essai précédent arrêté juste avant l'installation).
                if (File.Exists(path) && expected != null)
                {
                    var deja = await Task.Run(() => Sha256(path, pct => status?.Invoke("Vérification du fichier déjà téléchargé : " + pct + " %", pct / 100.0)));
                    if (deja == expected) { Log.Write("maj", "Installeur déjà téléchargé et vérifié : " + path); return Installer(info, path, status); }
                    try { File.Delete(path); } catch { }
                }
                await TelechargerAsync(url, path, info.Version.ToString(), status);
                if (expected != null)
                {
                    status?.Invoke("Vérification du fichier…", -1);
                    var actual = await Task.Run(() => Sha256(path, pct => status?.Invoke("Vérification du fichier : " + pct + " %", pct / 100.0)));
                    if (expected != actual)
                    {
                        try { File.Delete(path); } catch { }
                        Log.Write("maj", "Empreinte différente : attendu " + expected + ", obtenu " + actual);
                        return "Le fichier téléchargé est abîmé (empreinte différente). Réessaie plus tard.";
                    }
                }
                return Installer(info, path, status);
            }
            catch (Exception ex)
            {
                Log.Write("maj", "Échec : " + ex);
                var msg = ex is TaskCanceledException || ex is OperationCanceledException ? "la connexion est restée muette trop longtemps (" + Inactivite + " s)" : ex.Message;
                return "Mise à jour impossible : " + msg + "\nLe téléchargement reprendra là où il s'est arrêté au prochain essai (⚙ → Nuage → Vérifier les mises à jour).";
            }
        }

        private static string Installer(Info info, string path, Action<string, double> status)
        {
            Log.Write("maj", "Installation de " + info.Version + " depuis " + path);
            status?.Invoke("Installation : l'application se ferme et se relance toute seule…", 1);
            Settings.Load().Save(); // rien à changer, mais on s'assure que tout est écrit avant de quitter
            return Launch(path);
        }

        /// <summary>
        /// Télécharge « url » dans « path » (fichier .part pendant le transfert), en reprenant là où un essai
        /// précédent s'est arrêté (en-tête Range), jusqu'à quatre essais ; seul un silence de 90 s compte comme panne.
        /// </summary>
        private static async Task TelechargerAsync(string url, string path, string version, Action<string, double> status)
        {
            var part = path + ".part";
            Exception derniere = null;
            for (int essai = 1; essai <= 4; essai++)
            {
                long deja = 0;
                try { if (File.Exists(part)) deja = new FileInfo(part).Length; } catch { deja = 0; }
                var debut = DateTime.Now; long recu = 0;
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (deja > 0) req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(deja, null);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Inactivite)))
                    using (var res = await Dl.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                    {
                        bool reprise = res.StatusCode == System.Net.HttpStatusCode.PartialContent;
                        if (res.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                        {
                            // Le serveur ne veut pas de notre morceau (fichier changé ?) : on repart de zéro.
                            try { File.Delete(part); } catch { }
                            Log.Write("maj", "Reprise refusée (416) : téléchargement depuis le début");
                            continue;
                        }
                        res.EnsureSuccessStatusCode();
                        if (!reprise && deja > 0) { Log.Write("maj", "Pas de reprise possible : téléchargement depuis le début"); deja = 0; }
                        long total = res.Content.Headers.ContentLength.HasValue ? res.Content.Headers.ContentLength.Value + deja : -1;
                        if (reprise) Log.Write("maj", "Reprise du téléchargement à " + (deja / 1048576) + " Mo (essai " + essai + ")");
                        using (var src = await res.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(part, deja > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
                        {
                            var buf = new byte[256 * 1024];
                            long done = deja; int lastPct = -1; int n;
                            while (true)
                            {
                                cts.CancelAfter(TimeSpan.FromSeconds(Inactivite)); // le délai repart à chaque morceau reçu
                                n = await src.ReadAsync(buf, 0, buf.Length, cts.Token);
                                if (n <= 0) break;
                                await fs.WriteAsync(buf, 0, n);
                                done += n; recu += n;
                                if (total > 0)
                                {
                                    int pct = (int)(done * 100 / total);
                                    if (pct != lastPct)
                                    {
                                        lastPct = pct;
                                        var sec = (DateTime.Now - debut).TotalSeconds;
                                        var debit = sec > 2 ? recu / sec : 0;
                                        var reste = debit > 0 ? (total - done) / debit : 0;
                                        status?.Invoke("Téléchargement de la version " + version + " : " + pct + " % (" + (done / 1048576) + " / " + (total / 1048576) + " Mo"
                                            + (debit > 0 ? ", " + (debit / 1048576.0).ToString("0.0") + " Mo/s" + (reste > 0 ? ", encore " + (reste < 90 ? ((int)reste) + " s" : ((int)Math.Ceiling(reste / 60)) + " min") : "") : "") + ")", pct / 100.0);
                                    }
                                }
                                else status?.Invoke("Téléchargement de la version " + version + " : " + (done / 1048576) + " Mo", -1);
                            }
                            if (total > 0 && done < total) throw new IOException("fichier incomplet (" + done + " / " + total + " octets)");
                        }
                    }
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(part, path);
                    return;
                }
                catch (Exception ex)
                {
                    derniere = ex;
                    Log.Write("maj", "Téléchargement, essai " + essai + " : " + ex.Message + " (" + (recu / 1048576) + " Mo reçus cette fois)");
                    if (essai < 4)
                    {
                        status?.Invoke("La connexion a été coupée : nouvel essai dans quelques secondes (le téléchargement reprend où il s'est arrêté)…", -1);
                        await Task.Delay(TimeSpan.FromSeconds(3 * essai));
                    }
                }
            }
            throw derniere ?? new IOException("téléchargement impossible");
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
            StartWaiter();
            Log.Write("maj", "Fermeture de l'application ; l'installeur partira à la sortie : " + setupPath);
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => System.Windows.Application.Current.Shutdown()));
            return null;
        }

        /// <summary>
        /// La fenêtre d'attente : une COPIE de l'exécutable (pour que l'installeur
        /// puisse remplacer l'original), lancée avec --attente-maj. Elle reste à
        /// l'écran de la fermeture jusqu'au premier écran de la nouvelle version.
        /// </summary>
        private static void StartWaiter()
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var dir = UpdatesDir;
                Directory.CreateDirectory(dir);
                var copy = Path.Combine(dir, "Eyzi-Attente.exe");
                File.Copy(exe, copy, true);
                if (File.Exists(exe + ".config")) File.Copy(exe + ".config", copy + ".config", true);
                Process.Start(new ProcessStartInfo(copy, "--attente-maj " + Process.GetCurrentProcess().Id) { UseShellExecute = false, WorkingDirectory = dir });
                Log.Write("maj", "Fenêtre d'attente lancée");
            }
            catch (Exception ex) { Log.Write("maj", "Fenêtre d'attente impossible : " + ex.Message); }
        }

        /// <summary>Appelé par App.OnExit, une fois tout fermé et le verrou relâché.</summary>
        public static void RunPendingSetup()
        {
            var path = PendingSetup;
            PendingSetup = null;
            if (path == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(path, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /LAUNCH=1") { UseShellExecute = true });
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

        private static string Sha256(string path, Action<int> progress = null)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                var buf = new byte[1024 * 1024];
                long total = fs.Length, done = 0; int last = -1, n;
                while ((n = fs.Read(buf, 0, buf.Length)) > 0)
                {
                    sha.TransformBlock(buf, 0, n, null, 0);
                    done += n;
                    int pct = total > 0 ? (int)(done * 100 / total) : 0;
                    if (pct != last) { last = pct; progress?.Invoke(pct); }
                }
                sha.TransformFinalBlock(buf, 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
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
