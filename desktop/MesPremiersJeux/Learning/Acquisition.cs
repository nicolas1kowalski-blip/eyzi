using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MesPremiersJeux.Learning
{
    /// <summary>
    /// ACQUIS OU PAS : la mémoire des apprentissages (fichier apprentissages.ini).
    ///  • Par axe : les 5 derniers essais (jour + réussi ou non). Un axe est
    ///    validé avec 4 réussites sur 5, réparties sur au moins 2 jours.
    ///  • Par nœud (compétence + palier) : l'état après le défi (acquis, à
    ///    consolider), la date de la prochaine révision, le rang de révision
    ///    (3, 7, 21 jours puis 2 mois), et la décision manuelle de l'accompagnant.
    /// Une réussite = la bonne réponse au premier regard, sans aucune erreur.
    /// </summary>
    public static class Acquisition
    {
        public const int Fenetre = 5, SeuilAxe = 4, JoursMin = 2;
        public const int DefiExercices = 6, DefiSeuil = 5;
        public static readonly int[] Revisions = { 3, 7, 21, 60 };

        private sealed class Essai { public DateTime Jour; public bool Ok; }
        private sealed class Fiche
        {
            public string Etat = "";        // « acquis » / « consolider » / vide (calculé)
            public DateTime? Acquis;
            public int Rang;                // rang de la prochaine révision
            public DateTime? Revision;      // date de la prochaine révision
            public int Serie;               // révisions réussies d'affilée (consolidation)
            public string Manuel = "";      // « acquis » / « nonacquis » / vide
            public string Par = "";         // qui a décidé (« accompagnant »)
        }

        private static readonly Dictionary<string, List<Essai>> Essais = new Dictionary<string, List<Essai>>();
        private static readonly Dictionary<string, Fiche> Fiches = new Dictionary<string, Fiche>();
        private static bool _loaded;

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MesPremiersJeux", "apprentissages.ini");

        public static event Action Changed;

        // ------------------------------------------------------------------
        //  Lecture
        // ------------------------------------------------------------------
        private static List<Essai> EssaisDe(Noeud n, string axe)
        {
            Load();
            var k = n.Key + "|" + axe;
            if (!Essais.TryGetValue(k, out var l)) { l = new List<Essai>(); Essais[k] = l; }
            return l;
        }

        private static Fiche FicheDe(Noeud n)
        {
            Load();
            if (!Fiches.TryGetValue(n.Key, out var f)) { f = new Fiche(); Fiches[n.Key] = f; }
            return f;
        }

        /// <summary>(réussites, essais) sur la fenêtre des 5 derniers essais.</summary>
        public static (int Ok, int Total) Jauge(Noeud n, string axe)
        {
            var l = EssaisDe(n, axe);
            return (l.Count(e => e.Ok), l.Count);
        }

        public static bool AxeValide(Noeud n, string axe)
        {
            var l = EssaisDe(n, axe);
            var ok = l.Where(e => e.Ok).ToList();
            return ok.Count >= SeuilAxe && ok.Select(e => e.Jour.Date).Distinct().Count() >= JoursMin;
        }

        public static bool AxeVu(Noeud n, string axe) => EssaisDe(n, axe).Count > 0;

        public static Etat EtatDe(Noeud n)
        {
            var f = FicheDe(n);
            if (f.Manuel == "acquis") return Etat.Acquis;
            if (f.Etat == "acquis") return Etat.Acquis;
            if (f.Etat == "consolider") return Etat.AConsolider;
            var axes = n.Def.Axes;
            if (axes.All(a => AxeVu(n, a.Id))) return Etat.EnValidation;
            if (axes.Any(a => AxeVu(n, a.Id))) return Etat.EnCours;
            return Etat.NonCommence;
        }

        /// <summary>Acquis (ou à consolider, donc acquis un jour) : ouvre le palier suivant.</summary>
        public static bool EstAcquis(Noeud n) { var e = EtatDe(n); return e == Etat.Acquis || e == Etat.AConsolider; }

        /// <summary>Tous les axes validés : le défi peut être proposé.</summary>
        public static bool PretPourDefi(Noeud n)
            => EtatDe(n) == Etat.EnValidation && n.Def.Axes.All(a => AxeValide(n, a.Id));

        /// <summary>Le nœud est ouvert : premier palier, ou palier précédent acquis, ou ouvert à la main.</summary>
        public static bool Ouvert(Noeud n)
        {
            if (n.Palier <= 1) return true;
            if (FicheDe(n).Manuel == "ouvert") return true;
            var prev = new Noeud { Competence = n.Competence, Palier = n.Palier - 1 };
            return prev.Def == null || EstAcquis(prev);
        }

        public static DateTime? ProchaineRevision(Noeud n) => FicheDe(n).Revision;
        public static DateTime? DateAcquis(Noeud n) => FicheDe(n).Acquis;
        public static string DecisionManuelle(Noeud n) => FicheDe(n).Manuel;

        /// <summary>Révision due aujourd'hui (acquis à revoir, ou à consolider).</summary>
        public static bool RevisionDue(Noeud n)
        {
            var f = FicheDe(n);
            if (f.Manuel == "acquis") return false;
            return (f.Etat == "acquis" || f.Etat == "consolider") && f.Revision.HasValue && f.Revision.Value.Date <= DateTime.Today;
        }

        /// <summary>Progression 0..1 d'un nœud en validation (moyenne des jauges d'axes).</summary>
        public static double Progression(Noeud n)
        {
            var axes = n.Def.Axes;
            if (axes.Count == 0) return 0;
            double s = 0;
            foreach (var a in axes) s += Math.Min(1.0, Jauge(n, a.Id).Ok / (double)SeuilAxe);
            return s / axes.Count;
        }

        // ------------------------------------------------------------------
        //  Écriture
        // ------------------------------------------------------------------
        /// <summary>Un essai sur un axe du nœud en cours d'apprentissage.</summary>
        public static void Enregistrer(Noeud n, string axe, bool ok)
        {
            var l = EssaisDe(n, axe);
            l.Add(new Essai { Jour = DateTime.Today, Ok = ok });
            while (l.Count > Fenetre) l.RemoveAt(0);
            Save();
        }

        /// <summary>Le défi est réussi : acquis, première révision dans 3 jours.</summary>
        public static void DefiReussi(Noeud n)
        {
            var f = FicheDe(n);
            f.Etat = "acquis";
            f.Acquis = DateTime.Today;
            f.Rang = 0;
            f.Serie = 0;
            f.Revision = DateTime.Today.AddDays(Revisions[0]);
            Save();
        }

        /// <summary>Le défi est raté : les axes ratés repartent en validation (on retire leurs dernières réussites).</summary>
        public static void DefiRate(Noeud n, IEnumerable<string> axesRates)
        {
            foreach (var a in axesRates.Distinct())
            {
                var l = EssaisDe(n, a);
                l.Add(new Essai { Jour = DateTime.Today, Ok = false });
                while (l.Count > Fenetre) l.RemoveAt(0);
            }
            Save();
        }

        /// <summary>Le résultat d'une révision espacée.</summary>
        public static void Revision(Noeud n, bool ok)
        {
            var f = FicheDe(n);
            if (ok)
            {
                if (f.Etat == "consolider")
                {
                    f.Serie++;
                    if (f.Serie >= 2) { f.Etat = "acquis"; f.Serie = 0; f.Revision = DateTime.Today.AddDays(Revisions[Math.Min(f.Rang, Revisions.Length - 1)]); }
                    else f.Revision = DateTime.Today.AddDays(1);
                }
                else
                {
                    f.Rang = Math.Min(f.Rang + 1, Revisions.Length - 1);
                    f.Revision = DateTime.Today.AddDays(Revisions[f.Rang]);
                }
            }
            else
            {
                f.Etat = "consolider";
                f.Serie = 0;
                f.Revision = DateTime.Today.AddDays(1);
            }
            Save();
        }

        /// <summary>Décision de l'accompagnant : « acquis », « nonacquis » (remise à zéro), « ouvert », ou vide.</summary>
        public static void Forcer(Noeud n, string decision)
        {
            var f = FicheDe(n);
            if (decision == "nonacquis")
            {
                Fiches[n.Key] = new Fiche();
                foreach (var a in n.Def.Axes) Essais.Remove(n.Key + "|" + a.Id);
            }
            else
            {
                f.Manuel = decision ?? "";
                f.Par = string.IsNullOrEmpty(f.Manuel) ? "" : "accompagnant";
                if (decision == "acquis") { f.Etat = "acquis"; f.Acquis = DateTime.Today; f.Revision = null; }
            }
            Save();
        }

        // ------------------------------------------------------------------
        //  Fichier
        // ------------------------------------------------------------------
        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = line.Substring(0, eq).Trim();
                    var val = line.Substring(eq + 1).Trim();
                    if (key.StartsWith("essais:"))
                    {
                        var l = new List<Essai>();
                        foreach (var p in val.Split(',').Where(x => x.Length > 0))
                        {
                            var q = p.Split(':');
                            if (q.Length == 2 && DateTime.TryParseExact(q[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                                l.Add(new Essai { Jour = d, Ok = q[1] == "1" });
                        }
                        Essais[key.Substring(7)] = l;
                    }
                    else if (key.StartsWith("fiche:"))
                    {
                        var f = new Fiche();
                        foreach (var p in val.Split(';'))
                        {
                            var q = p.Split(new[] { ':' }, 2);
                            if (q.Length != 2) continue;
                            switch (q[0])
                            {
                                case "etat": f.Etat = q[1]; break;
                                case "acquis": f.Acquis = ParseDay(q[1]); break;
                                case "rang": int.TryParse(q[1], out f.Rang); break;
                                case "revision": f.Revision = ParseDay(q[1]); break;
                                case "serie": int.TryParse(q[1], out f.Serie); break;
                                case "manuel": f.Manuel = q[1]; break;
                                case "par": f.Par = q[1]; break;
                            }
                        }
                        Fiches[key.Substring(6)] = f;
                    }
                }
            }
            catch { }
        }

        private static DateTime? ParseDay(string s)
            => DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        private static string Day(DateTime? d) => d.HasValue ? d.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture) : "";

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                foreach (var kv in Essais)
                    lines.Add("essais:" + kv.Key + "=" + string.Join(",", kv.Value.Select(e => Day(e.Jour) + ":" + (e.Ok ? "1" : "0"))));
                foreach (var kv in Fiches)
                {
                    var f = kv.Value;
                    lines.Add("fiche:" + kv.Key + "=etat:" + f.Etat + ";acquis:" + Day(f.Acquis) + ";rang:" + f.Rang +
                              ";revision:" + Day(f.Revision) + ";serie:" + f.Serie + ";manuel:" + f.Manuel + ";par:" + f.Par);
                }
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
            try { Changed?.Invoke(); } catch { }
        }
    }
}
