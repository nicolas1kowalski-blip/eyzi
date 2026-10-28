using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// LES RÉPONSES PROPOSÉES : la phrase entendue part (texte seulement, jamais
    /// l'audio) vers eyzi.fr/api/parler, qui demande à Claude trois à six réponses
    /// courtes à la première personne de l'enfant. Sans réseau ou sans réglage :
    /// des réponses de secours (oui, non, encore, les deux côtés d'un « ou »).
    /// </summary>
    public static class Suggest
    {
        public sealed class Reponse { public string Texte, Emoji; }
        public sealed class Resultat { public string Simple; public List<Reponse> Reponses = new List<Reponse>(); public bool EnLigne; }

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        public static string Url = "https://eyzi.fr/api/parler";

        public static async Task<Resultat> Pour(Settings s, string question)
        {
            var res = Defaults(question);
            if (s == null || !s.ListenAnswers || string.IsNullOrWhiteSpace(s.LicenceKey)) return res;
            try
            {
                var body = "{\"key\":\"" + Esc(s.LicenceKey) + "\",\"machine\":\"" + Esc(Environment.MachineName) + "\",\"child\":\"" + Esc(s.ChildName ?? "") + "\",\"question\":\"" + Esc(question) + "\"}";
                var resp = await Http.PostAsync(Url, new StringContent(body, Encoding.UTF8, "application/json"));
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) { Log.Write("ecoute", "Réponses : HTTP " + (int)resp.StatusCode + " " + json); return res; }
                var list = new List<Reponse>();
                foreach (Match m in Regex.Matches(json, "\\{\\s*\"texte\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s*(?:,\\s*\"emoji\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\")?\\s*\\}"))
                {
                    var t = Regex.Unescape(m.Groups[1].Value).Trim();
                    if (t.Length == 0) continue;
                    list.Add(new Reponse { Texte = t, Emoji = m.Groups[2].Success ? Regex.Unescape(m.Groups[2].Value) : "" });
                }
                if (list.Count == 0) return res;
                var simple = Cloud.JsonGet(json, "simple");
                return new Resultat { Simple = string.IsNullOrWhiteSpace(simple) ? question : simple, Reponses = list, EnLigne = true };
            }
            catch (Exception ex) { Log.Write("ecoute", "Réponses : " + ex.Message); return res; }
        }

        /// <summary>Sans serveur : oui, non, les deux côtés d'un « ou », encore, autre chose.</summary>
        public static Resultat Defaults(string q)
        {
            var r = new Resultat { Simple = q };
            var m = Regex.Match(q ?? "", @"(?:veux|préfères|prends|choisis|aimes|voudrais)\s+(.+?)\s+ou\s+(.+?)\s*\??$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                r.Reponses.Add(new Reponse { Texte = Nettoie(m.Groups[1].Value), Emoji = "👉" });
                r.Reponses.Add(new Reponse { Texte = Nettoie(m.Groups[2].Value), Emoji = "👉" });
            }
            r.Reponses.Add(new Reponse { Texte = "Oui", Emoji = "✅" });
            r.Reponses.Add(new Reponse { Texte = "Non", Emoji = "❌" });
            if (!m.Success) r.Reponses.Add(new Reponse { Texte = "Encore", Emoji = "🔁" });
            r.Reponses.Add(new Reponse { Texte = "Autre chose", Emoji = "💭" });
            return r;
        }

        private static string Nettoie(string s)
        {
            s = Regex.Replace(s.Trim().TrimEnd('?', '.', '!', ' '), @"^(plutôt|bien)\s+", "", RegexOptions.IgnoreCase);
            return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : s;
        }

        private static string Esc(string v) => (v ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
