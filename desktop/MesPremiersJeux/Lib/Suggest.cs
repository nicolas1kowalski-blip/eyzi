using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// LES RÉPONSES PROPOSÉES : la phrase entendue part (texte seulement, jamais
    /// l'audio) vers eyzi.fr/api/parler avec le vocabulaire de l'enfant et les
    /// derniers échanges ; Claude renvoie le TYPE de ce qui a été dit (fermée,
    /// choix, ouverte, consigne, autre), une reformulation simple et des réponses
    /// à la première personne, reliées quand c'est possible aux mots du tableau.
    /// Sans réseau ou sans réglage : le type est deviné ici et des réponses de
    /// secours sont proposées (oui, non, les deux côtés d'un « ou », encore…).
    /// </summary>
    public static class Suggest
    {
        public sealed class Reponse { public string Texte, Emoji, Cle; }
        public sealed class Resultat
        {
            public string Type = "autre";   // fermee | choix | ouverte | consigne | autre
            public string Simple;
            public List<Reponse> Reponses = new List<Reponse>();
            public bool EnLigne;
        }
        public sealed class Echange { public string Question, Reponse; }
        public sealed class Mot { public string Cle, Label; }

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        public static string Url = "https://eyzi.fr/api/parler";

        public static async Task<Resultat> Pour(Settings s, string question, IList<Mot> vocabulaire = null, IList<Echange> historique = null)
        {
            var res = Defaults(question);
            if (s == null || !s.ListenAnswers || string.IsNullOrWhiteSpace(s.LicenceKey)) return res;
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"key\":\"").Append(Esc(s.LicenceKey)).Append("\",\"machine\":\"").Append(Esc(Environment.MachineName))
                  .Append("\",\"child\":\"").Append(Esc(s.ChildName ?? "")).Append("\",\"question\":\"").Append(Esc(question)).Append("\"");
                if (vocabulaire != null && vocabulaire.Count > 0)
                    sb.Append(",\"vocab\":[").Append(string.Join(",", vocabulaire.Take(120).Select(m => "{\"cle\":\"" + Esc(m.Cle) + "\",\"mot\":\"" + Esc(m.Label) + "\"}"))).Append("]");
                if (historique != null && historique.Count > 0)
                    sb.Append(",\"historique\":[").Append(string.Join(",", historique.Skip(Math.Max(0, historique.Count - 3)).Select(h => "{\"q\":\"" + Esc(h.Question) + "\",\"r\":\"" + Esc(h.Reponse ?? "") + "\"}"))).Append("]");
                sb.Append("}");
                var resp = await Http.PostAsync(Url, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) { Log.Write("ecoute", "Réponses : HTTP " + (int)resp.StatusCode + " " + json); return res; }
                var list = new List<Reponse>();
                foreach (Match m in Regex.Matches(json, "\\{\\s*\"texte\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s*(?:,\\s*\"emoji\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\")?\\s*(?:,\\s*\"cle\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\")?\\s*\\}"))
                {
                    var t = Regex.Unescape(m.Groups[1].Value).Trim();
                    if (t.Length == 0) continue;
                    list.Add(new Reponse { Texte = t, Emoji = m.Groups[2].Success ? Regex.Unescape(m.Groups[2].Value) : "", Cle = m.Groups[3].Success ? Regex.Unescape(m.Groups[3].Value) : "" });
                }
                if (list.Count == 0) return res;
                var simple = Cloud.JsonGet(json, "simple");
                var type = (Cloud.JsonGet(json, "type") ?? "").Trim();
                return new Resultat
                {
                    Type = type.Length > 0 ? type : res.Type,
                    Simple = string.IsNullOrWhiteSpace(simple) ? question : simple,
                    Reponses = list, EnLigne = true,
                };
            }
            catch (Exception ex) { Log.Write("ecoute", "Réponses : " + ex.Message); return res; }
        }

        /// <summary>Le type de phrase deviné sans serveur.</summary>
        public static string TypeDe(string q)
        {
            var t = (q ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return "autre";
            if (Regex.IsMatch(t, @"\sou\s") && t.EndsWith("?")) return "choix";
            if (Regex.IsMatch(t, @"^(qu'est[- ]ce|quoi|qui|où|ou est|quand|combien|comment|quel|quelle|quels|quelles|lequel|laquelle|avec qui|pourquoi)")) return "ouverte";
            if (t.EndsWith("?") || Regex.IsMatch(t, @"^(est[- ]ce que|tu |t'|on |il y a|c'est|ça |veux|as-tu|es-tu)")) return "fermee";
            if (Regex.IsMatch(t, @"^(on va|viens|donne|regarde|allez|c'est l'heure|il faut|on doit|attends|arrête|mets|prends)")) return "consigne";
            return "autre";
        }

        /// <summary>Sans serveur : des réponses selon le type deviné.</summary>
        public static Resultat Defaults(string q)
        {
            var r = new Resultat { Simple = q, Type = TypeDe(q) };
            void Add(string t, string e) => r.Reponses.Add(new Reponse { Texte = t, Emoji = e, Cle = "" });
            switch (r.Type)
            {
                case "choix":
                {
                    var m = Regex.Match(q ?? "", @"(?:veux|préfères|prends|choisis|aimes|voudrais|plutôt)\s+(.+?)\s+ou\s+(.+?)\s*\??$", RegexOptions.IgnoreCase);
                    if (!m.Success) m = Regex.Match(q ?? "", @"^(.+?)\s+ou\s+(.+?)\s*\??$", RegexOptions.IgnoreCase);
                    if (m.Success) { Add(Nettoie(m.Groups[1].Value), "👉"); Add(Nettoie(m.Groups[2].Value), "👉"); Add("Les deux", "🤲"); }
                    else { Add("Oui", "✅"); Add("Non", "❌"); }
                    Add("Autre chose", "💭");
                    break;
                }
                case "ouverte": Add("Manger", "🍽️"); Add("Boire", "🥤"); Add("Jouer", "🎮"); Add("Un câlin", "🤗"); Add("Dormir", "😴"); Add("Autre chose", "💭"); break;
                case "consigne": Add("D'accord", "👍"); Add("Non", "❌"); Add("Attends", "✋"); break;
                case "fermee": Add("Oui", "✅"); Add("Non", "❌"); Add("Je ne sais pas", "🤷"); break;
                default: Add("D'accord", "👍"); Add("Encore", "🔁"); Add("Je t'aime", "❤️"); break;
            }
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
