using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// CE QU'ELLE A DÉJÀ RÉPONDU À UNE QUESTION PROCHE : chaque réponse
    /// regardée est retenue avec la question (« Quel yaourt tu veux ? » → chocolat,
    /// trois fois). Quand une question qui parle des mêmes choses revient, ces
    /// réponses sont données à l'IA, qui les met en premier. Un fichier par
    /// tablette dans le dossier de la famille (habitudes-&lt;tablette&gt;.ini), lus
    /// tous ensemble : les tablettes de la famille partagent ce qu'elles apprennent.
    /// </summary>
    public static class Habitudes
    {
        public sealed class Entree
        {
            public string Question, Reponse; public int N; public DateTime Derniere;
            /// <summary>Le poids du choix : le nombre de fois, divisé par deux tous les 60 jours depuis la dernière fois (un goût récent compte plus qu'un ancien).</summary>
            public double Poids => N * Math.Pow(0.5, Math.Max(0, (DateTime.Today - (Derniere == DateTime.MinValue ? DateTime.Today : Derniere)).TotalDays) / 60.0);
        }

        private static readonly object _lock = new object();
        private static List<Entree> _toutes;            // tous les fichiers de la famille
        private static Dictionary<string, Entree> _mienne; // cette tablette (ce qu'on écrit)
        private static DateTime _lu = DateTime.MinValue;

        private static string Machine => Regex.Replace(Environment.MachineName ?? "tablette", @"[^A-Za-z0-9-]", "").ToLowerInvariant();
        private static string Dossier => UserContent.RootDir;
        private static string MonFichier => Path.Combine(Dossier, "habitudes-" + Machine + ".ini");

        private static readonly HashSet<string> Vides = new HashSet<string>
        {
            "le", "la", "les", "l", "un", "une", "des", "du", "de", "d", "au", "aux", "a", "et", "ou", "tu", "te", "toi", "veux", "veux-tu", "est", "ce", "que", "qu",
            "qui", "quoi", "quel", "quelle", "quels", "quelles", "on", "il", "elle", "en", "pour", "avec", "dans", "sur", "pas", "ne", "n", "est-ce", "y", "ton", "ta", "tes",
            "mon", "ma", "mes", "ca", "sa", "se", "lequel", "laquelle", "plutot", "bien", "alors", "maintenant", "aujourd", "hui", "vas", "va", "as", "ai", "aimes", "prefere", "preferes",
        };

        /// <summary>Les mots qui comptent dans une question (sans accents ni petits mots), triés.</summary>
        public static List<string> Mots(string question)
        {
            var n = Arasaac.Norm(question ?? "").Replace("'", " ").Replace("-", " ");
            return Regex.Split(n, @"\s+").Where(w => w.Length >= 3 && !Vides.Contains(w)).Distinct().OrderBy(w => w, StringComparer.Ordinal).ToList();
        }

        private static string Cle(string question) => string.Join(" ", Mots(question));

        private static void Charger(bool force = false)
        {
            lock (_lock)
            {
                if (!force && _toutes != null && (DateTime.Now - _lu).TotalSeconds < 60) return;
                _toutes = new List<Entree>();
                _mienne = _mienne ?? new Dictionary<string, Entree>(StringComparer.Ordinal);
                try
                {
                    if (Directory.Exists(Dossier))
                        foreach (var f in Directory.GetFiles(Dossier, "habitudes-*.ini"))
                        {
                            bool mien = string.Equals(Path.GetFileName(f), Path.GetFileName(MonFichier), StringComparison.OrdinalIgnoreCase);
                            if (mien && _mienne.Count > 0) { _toutes.AddRange(_mienne.Values); continue; }
                            foreach (var line in File.ReadAllLines(f))
                            {
                                // question|réponse=n;AAAA-MM-JJ
                                int eq = line.LastIndexOf('='); if (eq <= 0) continue;
                                int bar = line.IndexOf('|'); if (bar <= 0 || bar > eq) continue;
                                var reste = line.Substring(eq + 1).Split(';');
                                if (!int.TryParse(reste[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) continue;
                                DateTime d = DateTime.MinValue;
                                if (reste.Length > 1) DateTime.TryParseExact(reste[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                                var e = new Entree { Question = line.Substring(0, bar), Reponse = line.Substring(bar + 1, eq - bar - 1), N = n, Derniere = d };
                                _toutes.Add(e);
                                if (mien) _mienne[e.Question + "|" + e.Reponse] = e;
                            }
                        }
                }
                catch (Exception ex) { Log.Write("habitudes", "Lecture : " + ex.Message); }
                _lu = DateTime.Now;
            }
        }

        /// <summary>L'enfant a regardé <paramref name="reponse"/> pour <paramref name="question"/>.</summary>
        public static void Choisie(string question, string reponse)
        {
            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(reponse)) return;
            var r = reponse.Trim();
            if (Ignorees.Contains(r)) return;
            var q = Cle(question);
            if (q.Length == 0) return;
            Charger();
            lock (_lock)
            {
                var k = q + "|" + r;
                if (!_mienne.TryGetValue(k, out var e)) { e = new Entree { Question = q, Reponse = r }; _mienne[k] = e; _toutes.Add(e); }
                e.N++; e.Derniere = DateTime.Today;
                try
                {
                    Directory.CreateDirectory(Dossier);
                    File.WriteAllLines(MonFichier, _mienne.Values.OrderByDescending(x => x.N).Take(400)
                        .Select(x => x.Question + "|" + x.Reponse + "=" + x.N + ";" + x.Derniere.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                }
                catch (Exception ex) { Log.Write("habitudes", "Écriture : " + ex.Message); }
            }
        }

        private static readonly HashSet<string> Ignorees = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Autre chose", "Je ne sais pas", "Les deux", "Attends", "D'accord", "Plus tard", "Pas ça", "Je réfléchis", "Fini", "Encore", "Et toi ?", "Pourquoi ?" };

        /// <summary>Les réponses déjà choisies pour des questions proches de celle-ci (mots en commun), les plus fréquentes d'abord.</summary>
        public static List<Entree> Pour(string question, int max = 8)
        {
            var mots = Mots(question);
            if (mots.Count == 0) return new List<Entree>();
            Charger();
            lock (_lock)
            {
                var res = new Dictionary<string, Entree>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in _toutes)
                {
                    var em = e.Question.Split(' ');
                    int commun = em.Count(mots.Contains);
                    // Au moins un mot fort en commun, et la majorité des mots de la plus courte des deux questions.
                    int min = Math.Min(mots.Count, em.Length);
                    if (commun == 0 || commun * 2 < min) continue;
                    if (res.TryGetValue(e.Reponse, out var x)) { x.N += e.N; if (e.Derniere > x.Derniere) x.Derniere = e.Derniere; }
                    else res[e.Reponse] = new Entree { Question = e.Question, Reponse = e.Reponse, N = e.N, Derniere = e.Derniere };
                }
                return res.Values.OrderByDescending(x => x.Poids).ThenByDescending(x => x.Derniere).Take(max).ToList();
            }
        }

        /// <summary>Combien de choix retenus (toutes tablettes de la famille).</summary>
        public static int Total() { Charger(); lock (_lock) return _toutes.Sum(e => e.N); }

        /// <summary>Oublier ce que CETTE tablette a appris (les autres tablettes gardent leur fichier).</summary>
        public static void Oublier()
        {
            lock (_lock)
            {
                try { if (File.Exists(MonFichier)) File.Delete(MonFichier); } catch (Exception ex) { Log.Write("habitudes", "Oubli : " + ex.Message); }
                _mienne = new Dictionary<string, Entree>(StringComparer.Ordinal);
                _toutes = null;
            }
            Charger(force: true);
        }
    }
}
