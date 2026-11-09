using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🖼️ MA GALERIE : tout ce que l'enfant crée (peinture, collage, sculpture,
    /// musique) est gardé dans le dossier de la famille (Contenu\Créations), daté,
    /// avec le genre de l'œuvre. Le dossier voyage avec le contenu de la famille
    /// (sauvegarde et nuage), pour que les parents gardent et montrent.
    /// </summary>
    public static class Creations
    {
        public sealed class Oeuvre
        {
            public string Path, Genre, Titre; // Genre : peinture | collage | sculpture | statue | musique
            public DateTime Quand;
            public bool EstMusique => Genre == "musique";
        }

        public static string Dir => System.IO.Path.Combine(UserContent.RootDir, "Créations");
        public static event Action Changed;

        private static readonly Dictionary<string, string> Noms = new Dictionary<string, string>
        {
            ["peinture"] = "Ma peinture", ["collage"] = "Mon collage", ["sculpture"] = "Mon vase", ["statue"] = "Ma statue", ["totem"] = "Mon totem", ["gravure"] = "Ma gravure", ["musique"] = "Ma musique",
        };
        public static string Nom(string genre) => Noms.TryGetValue(genre ?? "", out var n) ? n : "Mon œuvre";

        private static string Nouveau(string genre, string ext)
        {
            Directory.CreateDirectory(Dir);
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
            return System.IO.Path.Combine(Dir, stamp + "_" + genre + ext);
        }

        /// <summary>Garde une image (le rendu d'un élément de l'écran, à la taille voulue).</summary>
        public static string Garder(Visual visuel, double largeur, double hauteur, string genre)
        {
            try
            {
                int w = (int)Math.Max(1, largeur), h = (int)Math.Max(1, hauteur);
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visuel);
                return Ecrire(rtb, genre);
            }
            catch (Exception ex) { Log.Write("creations", "Garder : " + ex.Message); return null; }
        }

        /// <summary>Garde une image déjà prête (toile de peinture).</summary>
        public static string Garder(BitmapSource image, string genre) { try { return Ecrire(image, genre); } catch (Exception ex) { Log.Write("creations", "Garder : " + ex.Message); return null; } }

        private static string Ecrire(BitmapSource image, string genre)
        {
            var path = Nouveau(genre, ".png");
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(image));
            using (var fs = File.Create(path)) enc.Save(fs);
            Usage.Count("creer:" + genre);
            try { Changed?.Invoke(); } catch { }
            return path;
        }

        /// <summary>Garde une musique (la grille du séquenceur, en texte).</summary>
        public static string GarderMusique(string contenu)
        {
            try
            {
                var path = Nouveau("musique", ".musique.txt");
                File.WriteAllText(path, contenu);
                Usage.Count("creer:musique");
                try { Changed?.Invoke(); } catch { }
                return path;
            }
            catch (Exception ex) { Log.Write("creations", "Garder musique : " + ex.Message); return null; }
        }

        /// <summary>Toutes les œuvres, la plus récente d'abord.</summary>
        public static List<Oeuvre> Toutes()
        {
            var l = new List<Oeuvre>();
            try
            {
                if (!Directory.Exists(Dir)) return l;
                foreach (var f in Directory.GetFiles(Dir))
                {
                    var name = System.IO.Path.GetFileName(f);
                    var ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".png" && !name.EndsWith(".musique.txt", StringComparison.OrdinalIgnoreCase)) continue;
                    var parts = name.Split('_');
                    if (parts.Length < 3) continue;
                    DateTime.TryParseExact(parts[0] + "_" + parts[1], "yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var q);
                    var genre = parts[2].Split('.')[0];
                    l.Add(new Oeuvre { Path = f, Genre = genre, Titre = Nom(genre), Quand = q == DateTime.MinValue ? File.GetCreationTime(f) : q });
                }
            }
            catch (Exception ex) { Log.Write("creations", "Liste : " + ex.Message); }
            return l.OrderByDescending(o => o.Quand).ToList();
        }

        public static void Supprimer(Oeuvre o)
        {
            try { if (o != null && File.Exists(o.Path)) File.Delete(o.Path); } catch (Exception ex) { Log.Write("creations", "Supprimer : " + ex.Message); }
            try { Changed?.Invoke(); } catch { }
        }

        public static string Quand(DateTime d)
        {
            var j = (DateTime.Today - d.Date).Days;
            if (j == 0) return "aujourd'hui à " + d.ToString("H'h'mm", CultureInfo.GetCultureInfo("fr-FR"));
            if (j == 1) return "hier";
            if (j < 7) return d.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));
            return "le " + d.ToString("d MMMM", CultureInfo.GetCultureInfo("fr-FR"));
        }
    }
}
