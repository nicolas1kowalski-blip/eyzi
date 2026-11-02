using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🔗 LES RÉGLAGES PARTAGÉS DE LA FAMILLE : la clé Azure, l'écoute, l'âge et
    /// le profil de l'enfant, la télé, les réseaux Wi-Fi appris. Écrits dans
    /// famille.ini, dans le dossier Contenu : ils voyagent avec le contenu, dans
    /// le nuage famille, chiffrés avec le mot de passe famille, jamais lisibles
    /// par l'éditeur. La tablette qui a changé un réglage le plus récemment gagne.
    /// </summary>
    public static class Partage
    {
        public static string FilePath => Path.Combine(UserContent.RootDir, "famille.ini");

        /// <summary>L'empreinte des valeurs partagées (pour savoir si elles ont changé sur cette tablette).</summary>
        public static string Empreinte(Settings s)
        {
            var sb = new StringBuilder();
            foreach (var kv in Valeurs(s)) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
            return sb.ToString().GetHashCode().ToString("X");
        }

        private static List<KeyValuePair<string, string>> Valeurs(Settings s)
        {
            var l = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("AzureKey", s.AzureKey ?? ""),
                new KeyValuePair<string, string>("AzureRegion", s.AzureRegion ?? ""),
                new KeyValuePair<string, string>("AzureVoice", s.AzureVoice ?? ""),
                new KeyValuePair<string, string>("ListenMode", s.ListenMode ?? "off"),
                new KeyValuePair<string, string>("ListenAnswers", s.ListenAnswers ? "1" : "0"),
                new KeyValuePair<string, string>("ChildAge", s.ChildAge.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("ChildProfile", (s.ChildProfile ?? "").Replace("\r", "").Replace("\n", "\\n")),
                new KeyValuePair<string, string>("TvIp", s.TvIp ?? ""),
                new KeyValuePair<string, string>("TvKey", s.TvKey ?? ""),
                new KeyValuePair<string, string>("TvMac", s.TvMac ?? ""),
            };
            foreach (var kv in Lieux.Associations()) l.Add(new KeyValuePair<string, string>("Lieu:" + kv.Key, kv.Value));
            return l;
        }

        /// <summary>Avant d'envoyer au nuage : écrit famille.ini si les valeurs ont changé sur cette tablette.</summary>
        public static void Ecrire(Settings s)
        {
            try
            {
                var emp = Empreinte(s);
                bool change = emp != s.PartageEmpreinte;
                if (change) { s.PartageEmpreinte = emp; s.PartageStamp = DateTime.UtcNow.ToString("o"); s.Save(); }
                if (!change && File.Exists(FilePath)) return;
                if (string.IsNullOrEmpty(s.PartageStamp)) return; // rien n'a jamais été réglé ici : on n'écrase pas ce que les autres ont envoyé
                Directory.CreateDirectory(UserContent.RootDir);
                // Ce que les autres tablettes ont déjà mis dans le fichier et que nous n'avons pas (une clé Azure,
                // une télé, un Wi-Fi) reste : une tablette sans clé n'efface jamais la clé des autres.
                var avant = Lire();
                var lines = new List<string> { "Stamp=" + s.PartageStamp, "Machine=" + Environment.MachineName };
                var vus = new HashSet<string>(StringComparer.Ordinal);
                foreach (var kv in Valeurs(s))
                {
                    vus.Add(kv.Key);
                    var v = kv.Value;
                    if (v.Length == 0 && avant.TryGetValue(kv.Key, out var ancien) && ancien.Length > 0 && Remplissable(kv.Key)) v = ancien;
                    lines.Add(kv.Key + "=" + v);
                }
                foreach (var kv in avant) if (kv.Key.StartsWith("Lieu:") && !vus.Contains(kv.Key)) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(FilePath, lines, Encoding.UTF8);
                Log.Write("nuage", "Réglages partagés écrits (" + s.PartageStamp + ")");
            }
            catch (Exception ex) { Log.Write("nuage", "Réglages partagés : " + ex.Message); }
        }

        // Les réglages qu'une tablette prend aux autres dès qu'elle ne les a pas, quel que soit l'âge du fichier.
        private static bool Remplissable(string k) => k == "AzureKey" || k == "AzureRegion" || k == "AzureVoice" || k == "TvIp" || k == "TvKey" || k == "TvMac" || k == "ChildProfile";

        private static Dictionary<string, string> Lire()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                if (!File.Exists(FilePath)) return map;
                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int eq = line.IndexOf('='); if (eq <= 0) continue;
                    map[line.Substring(0, eq)] = line.Substring(eq + 1);
                }
            }
            catch { }
            return map;
        }

        /// <summary>Après réception du nuage : applique famille.ini s'il est plus récent que nos propres changements ;
        /// et dans tous les cas, prend ce qui nous MANQUE (une clé Azure, une télé, un Wi-Fi appris) même d'un fichier plus ancien.
        /// Renvoie vrai si quelque chose a changé.</summary>
        public static bool Appliquer(Settings s)
        {
            try
            {
                if (!File.Exists(FilePath)) return false;
                var map = Lire();
                var stamp = map.TryGetValue("Stamp", out var st) ? st : "";
                if (stamp.Length == 0) return false;
                string V(string k) => map.TryGetValue(k, out var v) ? v : null;
                bool changed = false;
                if (string.CompareOrdinal(stamp, s.PartageStamp ?? "") <= 0)
                {
                    // Nos propres réglages sont plus récents : on garde les nôtres, mais on complète les trous.
                    void Prendre(ref string champ, string k) { var v = V(k); if (!string.IsNullOrEmpty(v) && string.IsNullOrEmpty(champ)) { champ = v; changed = true; } }
                    if (string.IsNullOrEmpty(s.AzureKey)) { Prendre(ref s.AzureKey, "AzureKey"); if (changed) { Prendre(ref s.AzureRegion, "AzureRegion"); Prendre(ref s.AzureVoice, "AzureVoice"); } }
                    if (string.IsNullOrEmpty(s.TvIp)) { Prendre(ref s.TvIp, "TvIp"); Prendre(ref s.TvKey, "TvKey"); Prendre(ref s.TvMac, "TvMac"); }
                    Prendre(ref s.ChildProfile, "ChildProfile"); if (s.ChildProfile != null) s.ChildProfile = s.ChildProfile.Replace("\\n", "\n");
                    foreach (var kv in map) if (kv.Key.StartsWith("Lieu:") && Lieux.LieuDe(kv.Key.Substring(5)).Length == 0) { Lieux.Associer(kv.Key.Substring(5), kv.Value); changed = true; }
                    if (changed) { s.PartageEmpreinte = Empreinte(s); s.Save(); Log.Write("nuage", "Réglages partagés (plus anciens) : ce qui manquait ici a été pris de " + (V("Machine") ?? "?")); }
                    return changed;
                }
                void Set(ref string champ, string k, bool garderLocalSiVide = true)
                {
                    var v = V(k); if (v == null) return;
                    if (v.Length == 0 && garderLocalSiVide && !string.IsNullOrEmpty(champ)) return; // une tablette sans clé n'efface pas la clé des autres
                    if (v != (champ ?? "")) { champ = v; changed = true; }
                }
                Set(ref s.AzureKey, "AzureKey"); Set(ref s.AzureRegion, "AzureRegion"); Set(ref s.AzureVoice, "AzureVoice");
                // L'écoute ne s'éteint jamais à distance : une tablette où elle est « off » ou sans réponses n'impose pas ça aux autres.
                var mode = V("ListenMode"); if (mode != null && mode != "off" && mode != (s.ListenMode ?? "off")) { s.ListenMode = mode; changed = true; }
                if (V("ListenAnswers") == "1" && !s.ListenAnswers) { s.ListenAnswers = true; changed = true; }
                if (int.TryParse(V("ChildAge") ?? "", out var age) && age > 0 && age < 30 && age != s.ChildAge) { s.ChildAge = age; changed = true; }
                var prof = V("ChildProfile"); if (prof != null) { prof = prof.Replace("\\n", "\n"); if (prof.Length > 0 || string.IsNullOrEmpty(s.ChildProfile)) if (prof != (s.ChildProfile ?? "")) { s.ChildProfile = prof; changed = true; } }
                Set(ref s.TvIp, "TvIp"); Set(ref s.TvKey, "TvKey"); Set(ref s.TvMac, "TvMac");
                foreach (var kv in map) if (kv.Key.StartsWith("Lieu:") && Lieux.LieuDe(kv.Key.Substring(5)).Length == 0) { Lieux.Associer(kv.Key.Substring(5), kv.Value); changed = true; }
                s.PartageStamp = stamp;
                s.PartageEmpreinte = Empreinte(s);
                s.Save();
                Log.Write("nuage", "Réglages partagés reçus de " + (V("Machine") ?? "?") + (changed ? " (appliqués)" : " (identiques)"));
                return changed;
            }
            catch (Exception ex) { Log.Write("nuage", "Réglages partagés reçus : " + ex.Message); return false; }
        }
    }
}
