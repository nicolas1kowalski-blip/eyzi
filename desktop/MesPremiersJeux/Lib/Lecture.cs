using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 📚 LA MÉMOIRE DE LECTURE : pour chaque livre, la page où l'on en est, le
    /// nombre de fois qu'il a été lu jusqu'au bout et la dernière fois. La
    /// bibliothèque propose « Continuer » l'histoire laissée en route et marque
    /// les livres déjà lus. Un fichier sur la tablette (lecture.ini).
    /// </summary>
    public static class Lecture
    {
        public sealed class Etat { public string Titre; public int Page, Total, Fois; public DateTime Derniere; }

        private static Dictionary<string, Etat> _etats;
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "lecture.ini");

        private static void Charger()
        {
            if (_etats != null) return;
            _etats = new Dictionary<string, Etat>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\t');
                    if (p.Length < 5) continue;
                    var e = new Etat { Titre = p[0] };
                    int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out e.Page);
                    int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out e.Total);
                    int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out e.Fois);
                    DateTime.TryParseExact(p[4], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out e.Derniere);
                    _etats[e.Titre] = e;
                }
            }
            catch (Exception ex) { Log.Write("histoires", "Mémoire de lecture : " + ex.Message); }
        }

        private static void Sauver()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, _etats.Values.Select(e => string.Join("\t", e.Titre.Replace('\t', ' '), e.Page, e.Total, e.Fois, e.Derniere.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))));
            }
            catch (Exception ex) { Log.Write("histoires", "Mémoire de lecture (écriture) : " + ex.Message); }
        }

        public static Etat Pour(string titre)
        {
            if (string.IsNullOrWhiteSpace(titre)) return null;
            Charger();
            return _etats.TryGetValue(titre, out var e) ? e : null;
        }

        /// <summary>On est sur cette page de ce livre.</summary>
        public static void Noter(string titre, int page, int total)
        {
            if (string.IsNullOrWhiteSpace(titre)) return;
            Charger();
            if (!_etats.TryGetValue(titre, out var e)) { e = new Etat { Titre = titre }; _etats[titre] = e; }
            e.Page = page; e.Total = total; e.Derniere = DateTime.Now;
            Sauver();
        }

        /// <summary>Le livre a été lu jusqu'au bout.</summary>
        public static void Fini(string titre)
        {
            if (string.IsNullOrWhiteSpace(titre)) return;
            Charger();
            if (!_etats.TryGetValue(titre, out var e)) { e = new Etat { Titre = titre }; _etats[titre] = e; }
            e.Fois++; e.Page = 0; e.Derniere = DateTime.Now;
            Sauver();
        }

        /// <summary>L'histoire laissée en route il y a moins d'une semaine (au moins une page lue, pas la fin), ou null.</summary>
        public static Etat EnCours()
        {
            Charger();
            return _etats.Values.Where(e => e.Page > 0 && e.Total > 1 && e.Page < e.Total - 1 && (DateTime.Now - e.Derniere).TotalDays < 7)
                .OrderByDescending(e => e.Derniere).FirstOrDefault();
        }
    }
}
