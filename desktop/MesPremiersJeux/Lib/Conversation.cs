using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// LE FIL DE LA JOURNÉE : tout ce qui s'est dit entre l'adulte et l'enfant
    /// par l'écoute (la question entendue, les cases proposées, ce que l'enfant
    /// a regardé, et ce que l'enfant a dit de lui-même), gardé sur la tablette
    /// (conversation.ini dans le dossier de l'application) et rendu à l'IA avec
    /// chaque nouvelle phrase : au moins les dix derniers échanges. Le fil
    /// repart à zéro chaque jour. Rien ne part ailleurs que vers eyzi.fr, en
    /// texte, avec la question du moment.
    /// </summary>
    public static class Conversation
    {
        public const int Garder = 10;
        private static readonly object _lock = new object();
        private static List<Suggest.Echange> _fil;
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "conversation.ini");

        private static void Charger()
        {
            if (_fil != null) return;
            _fil = new List<Suggest.Echange>();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    // at \t qui \t type \t ecart \t question \t reponse \t proposition␟proposition…
                    var p = line.Split('\t');
                    if (p.Length < 6) continue;
                    if (!DateTime.TryParseExact(p[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)) continue;
                    if (at.Date != DateTime.Today) continue; // le fil est celui de la journée
                    var e = new Suggest.Echange { At = at, Qui = D(p[1]), Type = D(p[2]), Ecart = D(p[3]), Question = D(p[4]), Reponse = D(p[5]) };
                    if (string.IsNullOrEmpty(e.Reponse)) e.Reponse = null;
                    if (p.Length > 6) e.Propositions = p[6].Split(Sep).Select(D).Where(x => x.Length > 0).ToList();
                    _fil.Add(e);
                }
            }
            catch (Exception ex) { Log.Write("ecoute", "Fil : " + ex.Message); }
        }

        private const char Sep = '\u001F'; // entre les propositions
        private static string E(string s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace(Sep, ' ');
        private static string D(string s) => s ?? "";

        /// <summary>Un nouvel échange commence (la question entendue, ou ce que l'enfant veut dire).</summary>
        public static void Ajouter(Suggest.Echange e)
        {
            if (e == null) return;
            lock (_lock)
            {
                Charger();
                _fil.RemoveAll(x => x.At.Date != DateTime.Today);
                if (!_fil.Contains(e)) _fil.Add(e);
                while (_fil.Count > 60) _fil.RemoveAt(0);
            }
            Sauver();
        }

        /// <summary>Une spéculation qui n'a pas abouti (la phrase entendue en entier était autre) : on la retire.</summary>
        public static void Retirer(Suggest.Echange e)
        {
            if (e == null) return;
            lock (_lock) { Charger(); _fil.Remove(e); }
            Sauver();
        }

        /// <summary>L'échange a changé (réponse regardée, cases proposées) : on réécrit le fichier.</summary>
        public static void Sauver()
        {
            lock (_lock)
            {
                Charger();
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllLines(FilePath, _fil.Select(e => string.Join("\t",
                        e.At.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), E(e.Qui ?? "adulte"), E(e.Type ?? ""), E(e.Ecart ?? ""),
                        E(e.Question ?? ""), E(e.Reponse ?? ""), string.Join(Sep.ToString(), e.Propositions.Take(12).Select(E)))));
                }
                catch (Exception ex) { Log.Write("ecoute", "Fil (écriture) : " + ex.Message); }
            }
        }

        /// <summary>Les derniers échanges du jour (au moins dix), sans celui en cours.</summary>
        public static List<Suggest.Echange> Recents(Suggest.Echange sauf = null, int n = Garder)
        {
            lock (_lock)
            {
                Charger();
                var l = _fil.Where(x => x != sauf && x.At.Date == DateTime.Today).ToList();
                return l.Skip(Math.Max(0, l.Count - n)).ToList();
            }
        }

        /// <summary>Le dernier échange, s'il date de moins de <paramref name="minutes"/> : la conversation continue.</summary>
        public static Suggest.Echange Dernier(Suggest.Echange sauf = null, int minutes = 30)
        {
            var r = Recents(sauf, 1).LastOrDefault();
            return r != null && (DateTime.Now - r.At).TotalMinutes <= minutes ? r : null;
        }

        /// <summary>Combien d'échanges aujourd'hui (pour la page d'administration).</summary>
        public static int Aujourdhui() { lock (_lock) { Charger(); return _fil.Count(x => x.At.Date == DateTime.Today); } }

        public static void Oublier()
        {
            lock (_lock) { _fil = new List<Suggest.Echange>(); try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { } }
        }
    }
}
