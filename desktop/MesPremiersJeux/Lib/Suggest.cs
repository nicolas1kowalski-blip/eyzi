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
        public sealed class Reponse { public string Texte, Emoji, Cle, Picto; }
        public sealed class Resultat
        {
            public string Type = "autre";   // fermee | choix | ouverte | consigne | autre
            public string Simple;
            public List<Reponse> Reponses = new List<Reponse>();
            public bool EnLigne;
            public string Erreur;           // pourquoi le serveur n'a pas répondu (affiché dans le bandeau)
            public string Chrono;           // « IA 1,2 s · serveur 1,9 s · total 3,4 s » : pour savoir d'où vient la lenteur
        }
        public sealed class Echange { public string Question, Reponse; }
        public sealed class Mot { public string Cle, Label; }

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        public static string Url = "https://eyzi.fr/api/parler";

        /// <summary>Réveille la fonction du site (démarrage à froid) pour que la première phrase ne traîne pas.</summary>
        public static void Reveiller(Settings s)
        {
            if (s == null || !s.ListenAnswers || string.IsNullOrWhiteSpace(s.LicenceKey)) return;
            _ = Task.Run(async () => { try { await Http.PostAsync(Url, new StringContent("{\"ping\":1}", Encoding.UTF8, "application/json")); } catch { } });
        }

        public static async Task<Resultat> Pour(Settings s, string question, IList<Mot> vocabulaire = null, IList<Echange> historique = null)
        {
            var res = Defaults(question);
            if (s == null || !s.ListenAnswers || string.IsNullOrWhiteSpace(s.LicenceKey)) return res;
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"key\":\"").Append(Esc(s.LicenceKey)).Append("\",\"machine\":\"").Append(Esc(Environment.MachineName))
                  .Append("\",\"child\":\"").Append(Esc(s.ChildName ?? "")).Append("\",\"question\":\"").Append(Esc(question)).Append("\"")
                  .Append(",\"age\":").Append(s.ChildAge).Append(",\"profil\":\"").Append(Esc(s.ChildProfile ?? "")).Append("\"")
                  .Append(",\"heure\":").Append(DateTime.Now.Hour).Append(",\"moment\":\"").Append(Moment(DateTime.Now)).Append("\"");
                if (vocabulaire != null && vocabulaire.Count > 0)
                    sb.Append(",\"vocab\":[").Append(string.Join(",", vocabulaire.Take(120).Select(m => "{\"cle\":\"" + Esc(m.Cle) + "\",\"mot\":\"" + Esc(m.Label) + "\"}"))).Append("]");
                if (historique != null && historique.Count > 0)
                    sb.Append(",\"historique\":[").Append(string.Join(",", historique.Skip(Math.Max(0, historique.Count - 3)).Select(h => "{\"q\":\"" + Esc(h.Question) + "\",\"r\":\"" + Esc(h.Reponse ?? "") + "\"}"))).Append("]");
                sb.Append("}");
                var chrono = System.Diagnostics.Stopwatch.StartNew();
                var resp = await Http.PostAsync(Url, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                var json = await resp.Content.ReadAsStringAsync();
                chrono.Stop();
                if (!resp.IsSuccessStatusCode)
                {
                    Log.Write("ecoute", "Réponses : HTTP " + (int)resp.StatusCode + " " + json);
                    // Le message du site, sinon le début de la réponse brute (page d'erreur de l'hébergeur : code en clair).
                    var msg = Cloud.JsonGet(json, "message");
                    if (string.IsNullOrEmpty(msg)) msg = Regex.Replace(json ?? "", "<[^>]+>", " ").Trim();
                    if (msg.Length > 160) msg = msg.Substring(0, 160) + "…";
                    res.Erreur = "HTTP " + (int)resp.StatusCode + " " + msg;
                    return res;
                }
                var list = new List<Reponse>();
                int iRep = json.IndexOf("\"reponses\"", StringComparison.Ordinal);
                foreach (Match m in Regex.Matches(iRep >= 0 ? json.Substring(iRep) : json, @"\{[^{}]*\}"))
                {
                    var obj = m.Value;
                    var t = (Cloud.JsonGet(obj, "texte") ?? "").Trim();
                    if (t.Length == 0) continue;
                    list.Add(new Reponse { Texte = t, Emoji = Cloud.JsonGet(obj, "emoji") ?? "", Cle = Cloud.JsonGet(obj, "cle") ?? "", Picto = Cloud.JsonGet(obj, "picto") ?? "" });
                }
                if (list.Count == 0) return res;
                var simple = Cloud.JsonGet(json, "simple");
                var type = (Cloud.JsonGet(json, "type") ?? "").Trim();
                double.TryParse(Cloud.JsonGet(json, "ia") ?? "", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var msIa);
                double.TryParse(Cloud.JsonGet(json, "total") ?? "", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var msSrv);
                var ch = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "IA {0:0.0} s · serveur {1:0.0} s · aller-retour {2:0.0} s", msIa / 1000.0, msSrv / 1000.0, chrono.ElapsedMilliseconds / 1000.0);
                Log.Write("ecoute", "Réponses : " + ch);
                return new Resultat
                {
                    Type = type.Length > 0 ? type : res.Type,
                    Simple = string.IsNullOrWhiteSpace(simple) ? question : simple,
                    Reponses = list, EnLigne = true, Chrono = ch,
                };
            }
            catch (Exception ex) { Log.Write("ecoute", "Réponses : " + ex.Message); res.Erreur = ex.Message; return res; }
        }

        /// <summary>Le moment de la journée, pour que les réponses collent à l'heure (petit-déjeuner, goûter…).</summary>
        public static string Moment(DateTime t)
        {
            int h = t.Hour;
            if (h < 6) return "la nuit";
            if (h < 10) return "le petit-déjeuner, le matin";
            if (h < 12) return "la matinée";
            if (h < 14) return "le déjeuner";
            if (h < 16) return "le début d'après-midi";
            if (h < 18) return "le goûter";
            if (h < 20) return "le dîner";
            return "le soir, avant le coucher";
        }

        /// <summary>Le type de phrase deviné sans serveur.</summary>
        public static string TypeDe(string q)
        {
            var t = (q ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return "autre";
            if (Regex.IsMatch(t, @"\sou\s") && (t.EndsWith("?") || Regex.IsMatch(t, @"^(tu|t'|veux|préf|qu)"))) return "choix";
            // Une question ouverte : un mot interrogatif au début (« que veux-tu », « qu'est-ce que »)
            // ou à la fin (« tu veux quoi ? », « tu veux manger quoi »).
            if (Regex.IsMatch(t, @"^(que\b|qu'|quoi|qui\b|où\b|ou est|quand|combien|comment|quel|quelle|quels|quelles|lequel|laquelle|avec qui|pourquoi|dis[- ]moi)")) return "ouverte";
            if (Regex.IsMatch(t, @"\b(quoi|qui|où|quand|combien|comment|lequel|laquelle)\s*\??$")) return "ouverte";
            if (t.EndsWith("?") || Regex.IsMatch(t, @"^(est[- ]ce que|tu |t'|on |il y a|c'est|ça |veux|as-tu|es-tu)")) return "fermee";
            if (Regex.IsMatch(t, @"^(on va|viens|donne|regarde|allez|c'est l'heure|il faut|on doit|attends|arrête|mets|prends)")) return "consigne";
            return "autre";
        }

        /// <summary>Sans serveur : des réponses selon le type deviné.</summary>
        public static Resultat Defaults(string q)
        {
            var r = new Resultat { Simple = q, Type = TypeDe(q) };
            void Add(string t, string e) => r.Reponses.Add(new Reponse { Texte = t, Emoji = e, Cle = "", Picto = PictoDe(t) });
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
                case "ouverte":
                {
                    var t = (q ?? "").ToLowerInvariant();
                    if (Regex.IsMatch(t, @"mang|goûter|gouter|repas|dîner|diner|déjeun|dejeun|petit[- ]déj"))
                    {
                        int h = DateTime.Now.Hour;
                        bool matin = Regex.IsMatch(t, @"petit[- ]déj|matin") || (h < 10 && !Regex.IsMatch(t, @"dîner|diner|goûter|gouter|déjeuner"));
                        bool gouter = Regex.IsMatch(t, @"goûter|gouter") || (h >= 15 && h < 18 && !Regex.IsMatch(t, @"dîner|diner|déjeuner|petit"));
                        if (matin) { Add("Une tartine", "🍞"); Add("Des céréales", "🥣"); Add("Un chocolat chaud", "☕"); Add("Une banane", "🍌"); Add("Un yaourt", "🥛"); Add("Autre chose", "💭"); }
                        else if (gouter) { Add("Une compote", "🍎"); Add("Un gâteau", "🍪"); Add("Un pain au chocolat", "🥐"); Add("Un yaourt", "🥣"); Add("Un fruit", "🍓"); Add("Autre chose", "💭"); }
                        else { Add("Des pâtes", "🍝"); Add("De la purée", "🥔"); Add("Du poulet", "🍗"); Add("Un yaourt", "🥣"); Add("Une compote", "🍎"); Add("Autre chose", "💭"); }
                    }
                    else if (Regex.IsMatch(t, @"boire|soif|bois")) { Add("De l'eau", "💧"); Add("Du jus", "🧃"); Add("Du lait", "🥛"); Add("Un chocolat", "☕"); Add("Autre chose", "💭"); }
                    else if (Regex.IsMatch(t, @"\bmal\b|bobo|douleur")) { Add("La tête", "🤕"); Add("Le ventre", "🤢"); Add("La jambe", "🦵"); Add("Le bras", "💪"); Add("Les dents", "🦷"); Add("Nulle part", "🙂"); }
                    else if (Regex.IsMatch(t, @"\bqui\b|avec qui")) { Add("Papa", "👨"); Add("Maman", "👩"); Add("Mamie", "👵"); Add("Papi", "👴"); Add("Personne", "🙅"); }
                    else if (Regex.IsMatch(t, @"\boù\b|aller")) { Add("Dehors", "🌳"); Add("Dans ma chambre", "🛏️"); Add("Au parc", "🛝"); Add("Chez mamie", "🏡"); Add("Nulle part", "🏠"); }
                    else if (Regex.IsMatch(t, @"comment ça va|comment tu vas|ça va")) { Add("Bien", "😊"); Add("Pas bien", "😢"); Add("Fatiguée", "😴"); Add("J'ai mal", "🤕"); Add("Contente", "🥳"); }
                    else { Add("Jouer", "🎮"); Add("Une histoire", "📖"); Add("De la musique", "🎵"); Add("Dehors", "🌳"); Add("Un câlin", "🤗"); Add("Autre chose", "💭"); }
                    break;
                }
                case "consigne": Add("D'accord", "👍"); Add("Non", "❌"); Add("Attends", "✋"); break;
                case "fermee": Add("Oui", "✅"); Add("Non", "❌"); Add("Je ne sais pas", "🤷"); break;
                default: Add("D'accord", "👍"); Add("Encore", "🔁"); Add("Je t'aime", "❤️"); break;
            }
            return r;
        }

        // Les mots anglais des réponses de secours, pour la banque de pictos.
        private static readonly Dictionary<string, string> PictoFr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Oui"] = "yes", ["Non"] = "no", ["Je ne sais pas"] = "don't know", ["Les deux"] = "both", ["Autre chose"] = "other", ["D'accord"] = "ok", ["Attends"] = "wait", ["Encore"] = "again", ["Je t'aime"] = "love",
            ["Du pain"] = "bread", ["Une compote"] = "apple", ["Un yaourt"] = "yogurt", ["Des pâtes"] = "pasta", ["Un gâteau"] = "cake", ["Une tartine"] = "toast", ["Des céréales"] = "cereal", ["Un chocolat chaud"] = "hot chocolate",
            ["Une banane"] = "banana", ["Un pain au chocolat"] = "croissant", ["Un fruit"] = "fruit", ["De la purée"] = "mashed potato", ["Du poulet"] = "chicken", ["De l'eau"] = "water", ["Du jus"] = "juice", ["Du lait"] = "milk", ["Un chocolat"] = "hot chocolate",
            ["La tête"] = "head", ["Le ventre"] = "tummy", ["La jambe"] = "leg", ["Le bras"] = "arm", ["Les dents"] = "teeth", ["Nulle part"] = "nowhere",
            ["Papa"] = "dad", ["Maman"] = "mum", ["Mamie"] = "grandma", ["Papi"] = "grandad", ["Personne"] = "nobody",
            ["Dehors"] = "outside", ["Dans ma chambre"] = "bedroom", ["Au parc"] = "park", ["Chez mamie"] = "house", ["Bien"] = "happy", ["Pas bien"] = "sad", ["Fatiguée"] = "tired", ["J'ai mal"] = "pain", ["Contente"] = "happy",
            ["Jouer"] = "play", ["Une histoire"] = "book", ["De la musique"] = "music", ["Un câlin"] = "hug", ["Dormir"] = "sleep", ["Manger"] = "eat", ["Boire"] = "drink",
        };
        private static string PictoDe(string t) => PictoFr.TryGetValue(t ?? "", out var p) ? p : "";

        private static string Nettoie(string s)
        {
            s = Regex.Replace(s.Trim().TrimEnd('?', '.', '!', ' '), @"^(plutôt|bien)\s+", "", RegexOptions.IgnoreCase);
            return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : s;
        }

        private static string Esc(string v) => (v ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
