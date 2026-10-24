using System;
using System.Windows;
using System.Windows.Media;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// « ON ÉCOUTE D'ABORD » : le jeu en cours déclare quand il n'accepte pas de
    /// sélection (consigne en train d'être dite, tour verrouillé). Le moteur du
    /// regard ne remplit alors PAS le cercle sur les boutons du jeu — rien ne
    /// fait semblant de cliquer — tandis que les tuiles hors du jeu (Parler,
    /// retour) restent accessibles.
    /// </summary>
    public static class InputGate
    {
        private static FrameworkElement _owner;
        private static Func<bool> _check;

        public static void Set(FrameworkElement owner, Func<bool> check) { _owner = owner; _check = check; }
        public static void Clear(FrameworkElement owner) { if (ReferenceEquals(_owner, owner)) { _owner = null; _check = null; } }

        private static bool Active()
        {
            try { return _owner != null && _owner.IsVisible && _check != null && _check(); }
            catch { return false; }
        }

        /// <summary>Le regard ne doit pas sélectionner cet élément pour l'instant.</summary>
        public static bool Blocks(DependencyObject target)
        {
            if (!Active() || target == null) return false;
            try { return _owner.IsAncestorOf(target); } catch { return false; }
        }

        /// <summary>Le regard ne doit pas « cliquer dans le vide » à ce point écran pour l'instant.</summary>
        public static bool BlocksPoint(Point screen)
        {
            if (!Active()) return false;
            try
            {
                var p = _owner.PointFromScreen(screen);
                return p.X >= 0 && p.Y >= 0 && p.X <= _owner.ActualWidth && p.Y <= _owner.ActualHeight;
            }
            catch { return false; }
        }
    }
}
