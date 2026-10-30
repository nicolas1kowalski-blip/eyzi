using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// LA BANQUE DE PICTOS (Mulberry Symbols, CC BY-SA 4.0) : 3 400 pictogrammes
    /// livrés dans Images\pictos avec leur table (fichier, nom anglais, nom
    /// français, mots-clés). On cherche d'abord par le nom anglais (celui des
    /// auteurs, le plus fiable), puis par le nom français, puis dans les mots-clés.
    /// </summary>
    public static class Pictos
    {
        private sealed class Row { public string File, En, Fr, Keys; }
        private static List<Row> _rows;
        private static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>();
        private static readonly object Lock = new object();

        public static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "pictos");

        private static void Load()
        {
            if (_rows != null) return;
            lock (Lock)
            {
                if (_rows != null) return;
                var rows = new List<Row>();
                try
                {
                    var csv = Path.Combine(Dir, "pictos.csv");
                    if (File.Exists(csv))
                        foreach (var line in File.ReadLines(csv, Encoding.UTF8).Skip(1))
                        {
                            var f = Split(line);
                            if (f.Count < 3) continue;
                            rows.Add(new Row { File = f[0], Fr = Norm(f[1]), En = Norm(f[2]), Keys = f.Count > 6 ? Norm(f[6]) : "" });
                        }
                }
                catch (Exception ex) { Log.Write("pictos", "Table : " + ex.Message); }
                _rows = rows;
            }
        }

        // Un découpage CSV minimal (guillemets, virgules).
        private static List<string> Split(string line)
        {
            var res = new List<string>(); var sb = new StringBuilder(); bool q = false;
            foreach (var c in line)
            {
                if (c == '"') { q = !q; continue; }
                if (c == ',' && !q) { res.Add(sb.ToString()); sb.Clear(); continue; }
                sb.Append(c);
            }
            res.Add(sb.ToString());
            return res;
        }

        /// <summary>Minuscules, sans accents, sans ponctuation, sans article.</summary>
        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in d)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }
            var t = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
            t = System.Text.RegularExpressions.Regex.Replace(t, @"^(the|a|an|to|some|du|de la|de l|des|de|le|la|les|l|un|une|mon|ma|mes|au|aux) ", "");
            return t;
        }

        /// <summary>Le fichier du picto le plus proche, ou null.</summary>
        public static string Find(string word)
        {
            Load();
            var w = Norm(word);
            if (w.Length < 2 || _rows.Count == 0) return null;
            Row best = _rows.FirstOrDefault(r => r.En == w) ?? _rows.FirstOrDefault(r => r.Fr == w);
            if (best == null && w.Length >= 4)
            {
                // « hug » → « hug , to » ; « pancake » → « pancakes » : le nom le plus court qui commence par le mot
                // (jamais pour les mots très courts : « no » donnerait « noisy », « yes » « yesterday »).
                best = _rows.Where(r => r.En.StartsWith(w + " ") || r.En.StartsWith(w)).OrderBy(r => r.En.Length).FirstOrDefault()
                    ?? _rows.Where(r => r.Fr.StartsWith(w + " ") || r.Fr.StartsWith(w)).OrderBy(r => r.Fr.Length).FirstOrDefault();
            }
            if (best == null && w.Contains(" "))
            {
                // Plusieurs mots : on tente le dernier (« hot chocolate » → « chocolate ») puis le premier.
                var parts = w.Split(' ');
                best = _rows.FirstOrDefault(r => r.En == parts[parts.Length - 1]) ?? _rows.FirstOrDefault(r => r.En == parts[0]);
            }
            if (best == null && w.Length >= 4) best = _rows.FirstOrDefault(r => (" " + r.Keys + " ").Contains(" " + w + " "));
            return best?.File;
        }

        public static ImageSource Source(string word)
        {
            var file = Find(word);
            if (file == null) return null;
            lock (Lock)
            {
                if (Cache.TryGetValue(file, out var hit)) return hit;
                try
                {
                    var path = Path.Combine(Dir, file);
                    if (!File.Exists(path)) return null;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bmp.StreamSource = new MemoryStream(File.ReadAllBytes(path));
                    bmp.EndInit();
                    bmp.Freeze();
                    Cache[file] = bmp;
                    return bmp;
                }
                catch (Exception ex) { Log.Write("pictos", file + " : " + ex.Message); return null; }
            }
        }

        /// <summary>L'image du picto, ou null si aucun ne correspond.</summary>
        public static Image Picture(string word, double size)
        {
            var src = Source(word);
            if (src == null) return null;
            return new Image { Source = src, Width = size, Height = size, Stretch = Stretch.Uniform };
        }
    }
}
