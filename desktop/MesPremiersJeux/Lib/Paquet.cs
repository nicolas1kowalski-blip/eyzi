using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🏪 L'application vient-elle du MICROSOFT STORE ? La même application existe
    /// en deux emballages : l'installeur d'eyzi.fr (canal test, mises à jour faites
    /// par Eyzi) et le paquet du Store (prod : c'est le Store qui installe et met à
    /// jour). Dans le paquet, Windows range ce que l'application écrit dans AppData
    /// à part, dans un dossier propre au paquet : l'application ne voit pas la
    /// différence, mais l'Explorateur, lui, doit recevoir le vrai chemin.
    /// </summary>
    public static class Paquet
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFamilyName(ref int length, StringBuilder name);

        private static bool _lu;
        private static string _famille;

        /// <summary>Le nom de famille du paquet, ou null hors du Store.</summary>
        public static string Famille
        {
            get
            {
                if (_lu) return _famille;
                _lu = true;
                try
                {
                    int n = 0;
                    int r = GetCurrentPackageFamilyName(ref n, null);   // 122 : le tampon est trop petit (donc il y a un paquet) ; 15700 : pas de paquet
                    if (r == 122 && n > 0)
                    {
                        var sb = new StringBuilder(n);
                        if (GetCurrentPackageFamilyName(ref n, sb) == 0 && sb.Length > 0) _famille = sb.ToString();
                    }
                }
                catch { /* Windows 7 : la fonction n'existe pas, donc pas de paquet */ }
                return _famille;
            }
        }

        public static bool EstStore => Famille != null;

        /// <summary>
        /// Le vrai emplacement d'un fichier ou dossier d'AppData, pour un programme extérieur (l'Explorateur).
        /// Hors du Store, ou si rien n'a été écrit à part, le chemin est rendu tel quel.
        /// </summary>
        public static string CheminReel(string chemin)
        {
            try
            {
                if (!EstStore || string.IsNullOrEmpty(chemin)) return chemin;
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cache = Path.Combine(local, "Packages", Famille, "LocalCache");
                string a = null;
                if (chemin.StartsWith(local + "\\", StringComparison.OrdinalIgnoreCase)) a = Path.Combine(cache, "Local", chemin.Substring(local.Length + 1));
                else if (chemin.StartsWith(roaming + "\\", StringComparison.OrdinalIgnoreCase)) a = Path.Combine(cache, "Roaming", chemin.Substring(roaming.Length + 1));
                if (a != null && (Directory.Exists(a) || File.Exists(a))) return a;
            }
            catch { }
            return chemin;
        }
    }
}
