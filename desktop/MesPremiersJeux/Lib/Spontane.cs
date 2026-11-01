using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 💬 « JE VEUX DIRE… » : ce que l'enfant pourrait vouloir dire MAINTENANT,
    /// sans qu'on lui ait posé de question. Les propositions dépendent du lieu,
    /// du moment de la journée, du jour, de la fiche de l'enfant et de ses
    /// habitudes. Elles sont demandées à l'IA au plus huit fois par jour et
    /// gardées trois heures pour un même contexte ; sans réseau, une liste
    /// toute faite selon le lieu et l'heure.
    /// </summary>
    public static class Spontane
    {
        private const int MaxParJour = 8;
        private static string _cleCache;
        private static DateTime _quand;
        private static List<Suggest.Reponse> _cache;
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "spontane.ini");

        private static string Moment(DateTime t) { int h = t.Hour; return h < 6 ? "nuit" : h < 10 ? "matin" : h < 12 ? "matinee" : h < 14 ? "midi" : h < 16 ? "apresmidi" : h < 18 ? "gouter" : h < 20 ? "soir" : "coucher"; }
        private static string Cle(Settings s) => (s?.ParlerLieu ?? "maison") + "|" + Moment(DateTime.Now) + "|" + (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? "we" : "sem");

        private static (string Jour, int N) Compteur()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var p = File.ReadAllText(FilePath).Trim().Split('=');
                    if (p.Length == 2 && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return (p[0], n);
                }
            }
            catch { }
            return ("", 0);
        }

        private static void Compter()
        {
            var jour = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var c = Compteur();
            int n = c.Jour == jour ? c.N + 1 : 1;
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, jour + "=" + n); } catch { }
        }

        /// <summary>Les propositions du moment : du cache, de l'IA (si le quota du jour le permet), sinon la liste toute faite.</summary>
        public static async Task<(List<Suggest.Reponse> Reponses, bool EnLigne, string Info)> Pour(Settings s, Action<Suggest.Reponse> surReponse = null)
        {
            var cle = Cle(s);
            if (_cache != null && _cleCache == cle && (DateTime.Now - _quand).TotalHours < 3)
            {
                foreach (var r in _cache) { try { surReponse?.Invoke(r); } catch { } }
                return (_cache, true, "propositions gardées");
            }
            var c = Compteur();
            bool quota = c.Jour == DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) && c.N >= MaxParJour;
            if (s != null && s.ListenAnswers && !string.IsNullOrWhiteSpace(s.LicenceKey) && !quota)
            {
                Compter();
                var res = await Suggest.PourFlux(s, "Je veux dire", Views.CommBoard.Vocabulaire().Select(m => new Suggest.Mot { Cle = m.Cle, Label = m.Label }).ToList(), null,
                    Suggest.ContexteActuel(s), null, surReponse, false, new Suggest.Options { Spontane = true });
                if (res.EnLigne && res.Reponses.Count > 0)
                {
                    _cache = res.Reponses; _cleCache = cle; _quand = DateTime.Now;
                    return (res.Reponses, true, "IA · " + (res.Chrono ?? ""));
                }
                var secours = Defaults(s);
                foreach (var r in secours) { try { surReponse?.Invoke(r); } catch { } }
                return (secours, false, "l'IA n'a pas répondu" + (string.IsNullOrEmpty(res.Erreur) ? "" : " (" + res.Erreur + ")"));
            }
            var def = Defaults(s);
            foreach (var r in def) { try { surReponse?.Invoke(r); } catch { } }
            return (def, false, quota ? "quota du jour atteint (" + MaxParJour + ") : liste toute faite" : "sans IA : liste toute faite");
        }

        public static void Invalider() { _cache = null; }

        /// <summary>Sans IA : des envies plausibles selon le lieu et l'heure.</summary>
        public static List<Suggest.Reponse> Defaults(Settings s)
        {
            var l = new List<Suggest.Reponse>();
            void Add(string t, string mot, string e) => l.Add(new Suggest.Reponse { Texte = t, Mot = mot, Emoji = e, Cle = "", Picto = "" });
            string lieu = s?.ParlerLieu ?? "maison"; string m = Moment(DateTime.Now);
            if (lieu == "ecole")
            {
                Add("Je veux aller aux toilettes", "toilettes", "🚽"); Add("J'ai mal", "douleur", "🤕"); Add("Je veux jouer avec un copain", "ami", "🧒"); Add("J'ai soif", "boire", "💧");
                Add("Je suis fatiguée", "fatigue", "😴"); Add("Je veux une histoire", "livre", "📖"); Add("Je veux ma maman", "maman", "👩"); Add("Autre chose", "", "💭");
            }
            else if (lieu == "voiture")
            {
                Add("On arrive quand ?", "voiture", "🚗"); Add("Je veux de la musique", "musique", "🎵"); Add("J'ai soif", "boire", "💧"); Add("J'ai mal au ventre", "ventre", "🤢");
                Add("Je veux mon doudou", "peluche", "🧸"); Add("Je veux regarder dehors", "fenêtre", "🪟"); Add("Autre chose", "", "💭");
            }
            else if (lieu == "dehors")
            {
                Add("Je veux aller au parc", "parc", "🛝"); Add("Je veux un câlin", "câlin", "🤗"); Add("J'ai froid", "froid", "🥶"); Add("J'ai soif", "boire", "💧");
                Add("Je veux rentrer", "maison", "🏠"); Add("Regarde !", "regarder", "👀"); Add("Autre chose", "", "💭");
            }
            else
            {
                if (m == "matin") { Add("J'ai faim", "manger", "🍞"); Add("Je veux un câlin", "câlin", "🤗"); Add("Je veux jouer", "jouer", "🎮"); Add("Je veux regarder un dessin animé", "télévision", "📺"); }
                else if (m == "gouter") { Add("Je veux mon goûter", "goûter", "🍪"); Add("J'ai soif", "boire", "💧"); Add("Je veux jouer dehors", "jardin", "🌳"); Add("Je veux une histoire", "livre", "📖"); }
                else if (m == "soir" || m == "coucher") { Add("Je suis fatiguée", "fatigue", "😴"); Add("Je veux mon doudou", "peluche", "🧸"); Add("Je veux une histoire", "livre", "📖"); Add("Je veux un câlin", "câlin", "🤗"); }
                else { Add("Je veux jouer", "jouer", "🎮"); Add("J'ai soif", "boire", "💧"); Add("Je veux un câlin", "câlin", "🤗"); Add("Je veux de la musique", "musique", "🎵"); }
                Add("J'ai mal", "douleur", "🤕"); Add("Je veux aller aux toilettes", "toilettes", "🚽"); Add("Je t'aime", "amour", "❤️"); Add("Autre chose", "", "💭");
            }
            return l.Take(8).ToList();
        }
    }
}
