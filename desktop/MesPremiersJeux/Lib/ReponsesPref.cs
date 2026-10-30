using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// CE QU'ELLE CHOISIT : chaque réponse regardée est comptée (fichier
    /// reponses.ini). Les plus fréquentes sont données à l'IA, qui les met en
    /// avant quand elles ont un sens pour la question posée.
    /// </summary>
    public static class ReponsesPref
    {
        private static Dictionary<string, int> _counts;
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "reponses.ini");

        private static void Load()
        {
            if (_counts != null) return;
            _counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.LastIndexOf('=');
                    if (eq <= 0) continue;
                    if (int.TryParse(line.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) _counts[line.Substring(0, eq)] = n;
                }
            }
            catch { }
        }

        public static void Choisie(string texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return;
            Load();
            var k = texte.Trim();
            _counts[k] = _counts.TryGetValue(k, out var n) ? n + 1 : 1;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, _counts.OrderByDescending(kv => kv.Value).Take(300).Select(kv => kv.Key + "=" + kv.Value));
            }
            catch { }
        }

        /// <summary>Les réponses les plus choisies (hors oui / non / autre chose), avec leur nombre.</summary>
        public static List<(string Texte, int N)> Preferees(int max = 15)
        {
            Load();
            var exclues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Oui", "Non", "Autre chose", "Je ne sais pas", "D'accord", "Attends", "Les deux" };
            return _counts.Where(kv => !exclues.Contains(kv.Key)).OrderByDescending(kv => kv.Value).Take(max).Select(kv => (kv.Key, kv.Value)).ToList();
        }
    }
}
