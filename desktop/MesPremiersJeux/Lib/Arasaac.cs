using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🖼 LES PICTOGRAMMES ARASAAC : la banque que les orthophonistes et les
    /// écoles françaises connaissent (CC BY-NC-SA, Gouvernement d'Aragon).
    /// L'index complet en français est livré (Images\arasaac\index.csv) avec un
    /// noyau d'images ; les autres sont téléchargées à la demande depuis
    /// static.arasaac.org et gardées dans %LocalAppData%\MesPremiersJeux\arasaac.
    /// </summary>
    public static class Arasaac
    {
        public static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "arasaac");
        public static string CacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MesPremiersJeux", "arasaac");
        public static bool Available => File.Exists(Path.Combine(Dir, "index.csv"));

        private static Dictionary<string, List<int>> _parMot;      // mot normalisé → ids (du plus téléchargé au moins)
        private static Dictionary<string, int> _parTete;           // premier mot d'un libellé composé → meilleur id (« tartine » → « tartine de beurre »)
        private static readonly object _lock = new object();
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        private static readonly Dictionary<int, BitmapSource> _images = new Dictionary<int, BitmapSource>();

        private static void Load()
        {
            lock (_lock)
            {
                if (_parMot != null) return;
                var d = new Dictionary<string, List<int>>(StringComparer.Ordinal);
                var tetes = new Dictionary<string, int>(StringComparer.Ordinal);
                try
                {
                    var p = Path.Combine(Dir, "index.csv");
                    if (File.Exists(p))
                    {
                        bool first = true;
                        foreach (var line in File.ReadLines(p, Encoding.UTF8))
                        {
                            if (first) { first = false; continue; }
                            var parts = line.Split(';');
                            if (parts.Length < 3 || !int.TryParse(parts[0], out var id)) continue;
                            foreach (var w in parts[2].Split('|'))
                            {
                                var k = Norm(w); if (k.Length == 0) continue;
                                if (!d.TryGetValue(k, out var l)) d[k] = l = new List<int>();
                                if (!l.Contains(id)) l.Add(id); // l'index est trié par téléchargements : le premier est le meilleur
                                int sp = k.IndexOf(' ');
                                if (sp > 0) { var tete = k.Substring(0, sp); if (tete.Length >= 3 && !tetes.ContainsKey(tete)) tetes[tete] = id; }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log.Write("pictos", "Index ARASAAC : " + ex.Message); }
                _parTete = tetes;
                _parMot = d;
                Log.Write("pictos", "ARASAAC : " + d.Count + " mots");
            }
        }

        /// <summary>Minuscules, sans accents, sans ponctuation.</summary>
        public static string Norm(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = s.Normalize(NormalizationForm.FormD).ToLowerInvariant().Replace('’', '\'');
            var sb = new StringBuilder();
            foreach (var c in t) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return Regex.Replace(Regex.Replace(sb.ToString(), @"[!?.,;:«»""]+", " "), @"\s+", " ").Trim();
        }

        private static readonly Regex Article = new Regex(@"^(le|la|les|l'|un|une|des|du|de la|de l'|de|d'|au|aux|a la|a l'|a|mon|ma|mes|ton|ta|tes|son|sa|ses|notre|votre|nos|vos|en|avec|chez|dans|sur|je veux|je voudrais|j'ai|c'est|il y a)\s+", RegexOptions.IgnoreCase);

        /// <summary>L'id du picto pour un texte français (« du jus de pomme » → jus de pomme → jus), ou 0.</summary>
        public static int Find(string texte)
        {
            Load();
            var t = Norm(texte);
            if (t.Length == 0 || _parMot.Count == 0) return 0;
            for (int i = 0; i < 3; i++) { var t2 = Article.Replace(t, ""); if (t2 == t) break; t = t2.Trim(); }
            if (t.Length == 0) return 0;
            int id;
            if ((id = Exact(t)) != 0) return id;
            // les groupes de mots, du plus long au plus court, puis chaque mot
            var mots = t.Split(' ').Where(m => m.Length > 0).ToArray();
            for (int n = Math.Min(3, mots.Length); n >= 1; n--)
                for (int s = 0; s + n <= mots.Length; s++)
                {
                    var g = string.Join(" ", mots, s, n);
                    if (n == 1 && (g.Length < 3 || Article.IsMatch(g + " "))) continue;
                    if ((id = Exact(g)) != 0) return id;
                }
            return 0;
        }

        private static int Exact(string k)
        {
            if (_parMot.TryGetValue(k, out var l) && l.Count > 0) return l[0];
            if (k.EndsWith("s") && _parMot.TryGetValue(k.Substring(0, k.Length - 1), out l) && l.Count > 0) return l[0];
            if (k.EndsWith("x") && _parMot.TryGetValue(k.Substring(0, k.Length - 1), out l) && l.Count > 0) return l[0];
            if (k.EndsWith("e") && _parMot.TryGetValue(k.Substring(0, k.Length - 1), out l) && l.Count > 0) return l[0]; // fatiguée → fatigue
            if (k.IndexOf(' ') < 0 && _parTete != null && _parTete.TryGetValue(k, out var t)) return t;                  // tartine → tartine de beurre
            return 0;
        }

        private static BitmapSource Charger(string path)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit(); bi.CacheOption = BitmapCacheOption.OnLoad; bi.DecodePixelWidth = 300; bi.UriSource = new Uri(path); bi.EndInit(); bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        /// <summary>L'image d'un picto : locale, en cache, ou téléchargée (l'image se remplit quand elle arrive).</summary>
        private static BitmapSource Source(int id, Action<BitmapSource> quandArrive)
        {
            lock (_images) if (_images.TryGetValue(id, out var cached)) return cached;
            var local = Path.Combine(Dir, id + ".png");
            var cache = Path.Combine(CacheDir, id + ".png");
            var path = File.Exists(local) ? local : File.Exists(cache) ? cache : null;
            if (path != null)
            {
                var src = Charger(path);
                if (src != null) lock (_images) _images[id] = src;
                return src;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    var bytes = await Http.GetByteArrayAsync("https://static.arasaac.org/pictograms/" + id + "/" + id + "_300.png");
                    if (bytes == null || bytes.Length < 500) return;
                    Directory.CreateDirectory(CacheDir);
                    File.WriteAllBytes(cache, bytes);
                    var src = Charger(cache);
                    if (src == null) return;
                    lock (_images) _images[id] = src;
                    quandArrive?.Invoke(src);
                }
                catch (Exception ex) { Log.Write("pictos", "ARASAAC " + id + " : " + ex.Message); }
            });
            return null;
        }

        /// <summary>
        /// Le picto d'un texte français, dans une case de « size » : tout de suite s'il est sur la tablette,
        /// sinon « secours » s'affiche et le picto le remplace quand il est téléchargé. Null si le mot est inconnu.
        /// </summary>
        public static FrameworkElement Picture(string texte, double size, FrameworkElement secours = null)
        {
            if (!Available) return null;
            int id;
            try { id = Find(texte); } catch { return null; }
            if (id == 0) return null;
            var img = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
            var host = new Grid { Width = size, Height = size };
            if (secours != null) host.Children.Add(secours);
            host.Children.Add(img);
            var src = Source(id, s => img.Dispatcher.BeginInvoke(new Action(() => { img.Source = s; if (secours != null) secours.Visibility = Visibility.Collapsed; })));
            if (src != null) { img.Source = src; if (secours != null) secours.Visibility = Visibility.Collapsed; }
            return host;
        }
    }
}
