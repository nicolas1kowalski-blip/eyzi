using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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

        public sealed class Contexte { public string Lieu, Jour; public List<string> Activites = new List<string>(); public List<string> Preferees = new List<string>(); }

        /// <summary>Le contexte du moment : lieu choisi, jour, activités du jour sur la tablette, réponses préférées.</summary>
        public static Contexte ContexteActuel(Settings s)
        {
            var c = new Contexte();
            switch ((s?.ParlerLieu ?? "maison").ToLowerInvariant())
            {
                case "ecole": c.Lieu = "à l'école"; break;
                case "dehors": c.Lieu = "dehors (parc, promenade, courses)"; break;
                case "voiture": c.Lieu = "en voiture"; break;
                default: c.Lieu = "à la maison"; break;
            }
            var now = DateTime.Now;
            var jours = new[] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };
            c.Jour = jours[(int)now.DayOfWeek] + (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday ? " (week-end)" : " (jour d'école)");
            try
            {
                foreach (var g in Progress.Load().Where(r => r.Day.Date == now.Date).OrderByDescending(r => r.Count).Take(6))
                    c.Activites.Add(ActivityNames.Of(g.Game));
            }
            catch { }
            try { c.Preferees = ReponsesPref.Preferees().Select(x => x.Texte).ToList(); } catch { }
            return c;
        }

        public static Task<Resultat> Pour(Settings s, string question, IList<Mot> vocabulaire = null, IList<Echange> historique = null)
            => PourFlux(s, question, vocabulaire, historique, null, null, null);

        /// <summary>
        /// Demande les réponses EN FLUX : « surTete » reçoit le type et la phrase simple dès qu'ils sont
        /// connus, « surReponse » chaque réponse dès qu'elle est écrite. Renvoie le résultat complet.
        /// </summary>
        public static async Task<Resultat> PourFlux(Settings s, string question, IList<Mot> vocabulaire, IList<Echange> historique, Contexte ctx,
                                                    Action<Resultat> surTete, Action<Reponse> surReponse)
        {
            var res = Defaults(question);
            if (s == null || !s.ListenAnswers || string.IsNullOrWhiteSpace(s.LicenceKey)) return res;
            try
            {
                ctx = ctx ?? ContexteActuel(s);
                var sb = new StringBuilder();
                sb.Append("{\"key\":\"").Append(Esc(s.LicenceKey)).Append("\",\"machine\":\"").Append(Esc(Environment.MachineName))
                  .Append("\",\"child\":\"").Append(Esc(s.ChildName ?? "")).Append("\",\"question\":\"").Append(Esc(question)).Append("\"")
                  .Append(",\"age\":").Append(s.ChildAge).Append(",\"profil\":\"").Append(Esc(s.ChildProfile ?? "")).Append("\"")
                  .Append(",\"heure\":").Append(DateTime.Now.Hour).Append(",\"moment\":\"").Append(Moment(DateTime.Now)).Append("\"")
                  .Append(",\"lieu\":\"").Append(Esc(ctx.Lieu)).Append("\",\"jour\":\"").Append(Esc(ctx.Jour)).Append("\"");
                if (ctx.Activites.Count > 0) sb.Append(",\"activites\":[").Append(string.Join(",", ctx.Activites.Select(a => "\"" + Esc(a) + "\""))).Append("]");
                if (ctx.Preferees.Count > 0) sb.Append(",\"preferees\":[").Append(string.Join(",", ctx.Preferees.Select(a => "\"" + Esc(a) + "\""))).Append("]");
                if (vocabulaire != null && vocabulaire.Count > 0)
                    sb.Append(",\"vocab\":[").Append(string.Join(",", vocabulaire.Take(120).Select(m => "{\"cle\":\"" + Esc(m.Cle) + "\",\"mot\":\"" + Esc(m.Label) + "\"}"))).Append("]");
                if (historique != null && historique.Count > 0)
                    sb.Append(",\"historique\":[").Append(string.Join(",", historique.Skip(Math.Max(0, historique.Count - 3)).Select(h => "{\"q\":\"" + Esc(h.Question) + "\",\"r\":\"" + Esc(h.Reponse ?? "") + "\"}"))).Append("]");
                sb.Append("}");

                var chrono = System.Diagnostics.Stopwatch.StartNew();
                var req = new HttpRequestMessage(HttpMethod.Post, Url) { Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json") };
                var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    Log.Write("ecoute", "Réponses : HTTP " + (int)resp.StatusCode + " " + json);
                    var msg = Cloud.JsonGet(json, "message");
                    if (string.IsNullOrEmpty(msg)) msg = Regex.Replace(json ?? "", "<[^>]+>", " ").Trim();
                    if (msg.Length > 160) msg = msg.Substring(0, 160) + "…";
                    res.Erreur = "HTTP " + (int)resp.StatusCode + " " + msg;
                    return res;
                }
                var fin = new Resultat { Simple = question, Type = res.Type };
                double msIa = 0, msSrv = 0; bool tete = false; double premiere = 0;
                using (var stream = await resp.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0) continue;
                        if (line.Contains("\"erreur\"")) { fin.Erreur = Cloud.JsonGet(line, "erreur"); break; }
                        if (line.Contains("\"reponse\""))
                        {
                            var r = new Reponse { Texte = (Cloud.JsonGet(line, "texte") ?? "").Trim(), Emoji = Cloud.JsonGet(line, "emoji") ?? "", Cle = Cloud.JsonGet(line, "cle") ?? "", Picto = Cloud.JsonGet(line, "picto") ?? "" };
                            if (r.Texte.Length == 0) continue;
                            if (premiere == 0) premiere = chrono.ElapsedMilliseconds;
                            fin.Reponses.Add(r);
                            try { surReponse?.Invoke(r); } catch { }
                        }
                        else if (line.Contains("\"fin\""))
                        {
                            double.TryParse(Cloud.JsonGet(line, "ia") ?? "", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out msIa);
                            double.TryParse(Cloud.JsonGet(line, "total") ?? "", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out msSrv);
                        }
                        else if (line.Contains("\"type\""))
                        {
                            var type = (Cloud.JsonGet(line, "type") ?? "").Trim();
                            if (type.Length > 0) fin.Type = type;
                            var simple = Cloud.JsonGet(line, "simple");
                            if (!string.IsNullOrWhiteSpace(simple)) fin.Simple = simple;
                            if (!tete) { tete = true; try { surTete?.Invoke(fin); } catch { } }
                        }
                        else if (line.Contains("\"reponses\""))
                        {
                            // Réponse entière d'un coup (hébergeur sans flux) : même lecture qu'avant.
                            var type = (Cloud.JsonGet(line, "type") ?? "").Trim(); if (type.Length > 0) fin.Type = type;
                            var simple = Cloud.JsonGet(line, "simple"); if (!string.IsNullOrWhiteSpace(simple)) fin.Simple = simple;
                            if (!tete) { tete = true; try { surTete?.Invoke(fin); } catch { } }
                            int iRep = line.IndexOf("\"reponses\"", StringComparison.Ordinal);
                            foreach (Match m in Regex.Matches(line.Substring(iRep), @"\{[^{}]*\}"))
                            {
                                var r = new Reponse { Texte = (Cloud.JsonGet(m.Value, "texte") ?? "").Trim(), Emoji = Cloud.JsonGet(m.Value, "emoji") ?? "", Cle = Cloud.JsonGet(m.Value, "cle") ?? "", Picto = Cloud.JsonGet(m.Value, "picto") ?? "" };
                                if (r.Texte.Length == 0) continue;
                                fin.Reponses.Add(r);
                                try { surReponse?.Invoke(r); } catch { }
                            }
                        }
                    }
                }
                chrono.Stop();
                if (fin.Reponses.Count == 0) { res.Erreur = fin.Erreur ?? "aucune réponse"; return res; }
                fin.EnLigne = true;
                fin.Chrono = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "première case {0:0.0} s · IA {1:0.0} s · total {2:0.0} s", premiere / 1000.0, msIa / 1000.0, chrono.ElapsedMilliseconds / 1000.0);
                Log.Write("ecoute", "Réponses : " + fin.Chrono);
                return fin;
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
        private static readonly HashSet<string> Tetes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "au", "aux", "à", "de", "du", "des", "d'", "le", "la", "les", "l'", "un", "une", "en", "avec", "chez", "dans", "sur" };

        /// <summary>
        /// Les options d'une question à choix, sans le début de la phrase :
        /// « Laura, veux-tu un yaourt au chocolat, à la vanille ou aux fruits ? » → au chocolat · à la vanille · aux fruits.
        /// </summary>
        public static List<string> OptionsDuChoix(string q)
        {
            var res = new List<string>();
            var s = Regex.Replace(q ?? "", @"[?!.]+\s*$", "").Trim();
            if (s.Length == 0) return res;
            s = Regex.Replace(s, @"^[A-ZÀ-Ý][\wà-ÿ-]+\s*,\s*", ""); // « Laura, … »
            var m = Regex.Match(s, @"^(.+?)\s+ou\s+(?:bien\s+)?(.+)$", RegexOptions.IgnoreCase);
            if (!m.Success) return res;
            var gauche = m.Groups[1].Value; var droite = m.Groups[2].Value;
            var morceaux = gauche.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (morceaux.Count == 0) return res;
            // Le premier morceau porte le début de la question : on garde ce qui suit le verbe…
            var premier = morceaux[0];
            var mv = Regex.Match(premier, @"(?:veux|préfères|prends|choisis|aimes|voudrais|plutôt|veux-tu|tu veux|préfères-tu|tu préfères|voulez-vous|est-ce que)\s+(?:tu\s+|vous\s+)?(?:veux\s+|préfères\s+)?(.+)$", RegexOptions.IgnoreCase);
            if (mv.Success) premier = mv.Groups[1].Value.Trim();
            // … et, si la liste a des virgules (« au chocolat, à la vanille ou aux fruits »), on coupe au dernier petit mot de tête : « un yaourt au chocolat » → « au chocolat ».
            if (morceaux.Count >= 2)
            {
                var mots = premier.Split(' ');
                int coupe = -1;
                for (int i = mots.Length - 2; i >= 1; i--) if (Tetes.Contains(mots[i]) || mots[i].EndsWith("'") && Tetes.Contains(mots[i])) { coupe = i; break; }
                if (coupe > 0) premier = string.Join(" ", mots.Skip(coupe));
            }
            res.Add(premier);
            res.AddRange(morceaux.Skip(1));
            res.Add(droite.Trim());
            return res.Select(Nettoie).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
        }

        public static Resultat Defaults(string q)
        {
            var r = new Resultat { Simple = q, Type = TypeDe(q) };
            void Add(string t, string e) => r.Reponses.Add(new Reponse { Texte = t, Emoji = e, Cle = "", Picto = PictoDe(t) });
            switch (r.Type)
            {
                case "choix":
                {
                    var options = OptionsDuChoix(q);
                    if (options.Count >= 2) { foreach (var o in options) Add(o, "👉"); if (options.Count == 2) Add("Les deux", "🤲"); }
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
