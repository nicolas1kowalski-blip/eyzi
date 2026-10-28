using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MesPremiersJeux.Gaze
{
    /// <summary>
    /// Recense les « cibles » visibles à l'écran pour l'aimant du regard : tous les
    /// boutons actifs, plus tout élément marqué GazeTargets.IsTarget="true" (mots
    /// de lecture, zones des histoires…). Le moteur attire le regard vers la cible
    /// la plus proche et remplit le cercle SUR elle.
    /// </summary>
    public static class GazeTargets
    {
        public static readonly DependencyProperty IsTargetProperty =
            DependencyProperty.RegisterAttached("IsTarget", typeof(bool), typeof(GazeTargets),
                new PropertyMetadata(false));

        public static void SetIsTarget(DependencyObject el, bool value) => el.SetValue(IsTargetProperty, value);
        public static bool GetIsTarget(DependencyObject el) => (bool)el.GetValue(IsTargetProperty);

        /// <summary>Élément réservé à l'accompagnant (bouton ⚙) : jamais une
        /// cible du regard, et le dwell libre n'y clique pas non plus.</summary>
        public static readonly DependencyProperty ParentOnlyProperty =
            DependencyProperty.RegisterAttached("ParentOnly", typeof(bool), typeof(GazeTargets),
                new PropertyMetadata(false));

        public static void SetParentOnly(DependencyObject el, bool value) => el.SetValue(ParentOnlyProperty, value);
        public static bool GetParentOnly(DependencyObject el) => (bool)el.GetValue(ParentOnlyProperty);

        /// <summary>Zones écran interdites au regard (recensées à chaque Collect).</summary>
        private static readonly List<Rect> _forbidden = new List<Rect>();

        public static bool IsForbidden(Point screen)
        {
            lock (_forbidden)
                foreach (var r in _forbidden)
                    if (r.Contains(screen)) return true;
            return false;
        }

        /// <summary>
        /// Cible de PRÉCISION (jeux qui travaillent la visée) : l'aimant du regard
        /// ne l'attire pas de loin, il faut poser le regard DEDANS. Le reste de
        /// l'écran garde son aimant habituel.
        /// </summary>
        public static readonly DependencyProperty PreciseProperty =
            DependencyProperty.RegisterAttached("Precise", typeof(bool), typeof(GazeTargets),
                new PropertyMetadata(false));

        public static void SetPrecise(DependencyObject el, bool value) => el.SetValue(PreciseProperty, value);
        public static bool GetPrecise(DependencyObject el) => (bool)el.GetValue(PreciseProperty);

        /// <summary>
        /// FACTEUR DE FIXATION d'une cible : 1 = le temps réglé, 0.6 = 40 % plus
        /// rapide (jeux d'adresse où l'on vise vite), 1.5 = plus lent. Ne change
        /// que le temps nécessaire sur CETTE cible, rien d'autre.
        /// </summary>
        public static readonly DependencyProperty DwellFactorProperty =
            DependencyProperty.RegisterAttached("DwellFactor", typeof(double), typeof(GazeTargets),
                new PropertyMetadata(1.0));

        public static void SetDwellFactor(DependencyObject el, double value) => el.SetValue(DwellFactorProperty, value);
        public static double GetDwellFactor(DependencyObject el) => (double)el.GetValue(DwellFactorProperty);

        public sealed class Target
        {
            public FrameworkElement Element;
            public Rect ScreenRect; // pixels écran (physiques)
            public bool Precise;    // sans aimant : le regard doit être dedans
            public double DwellFactor = 1.0;
        }

        /// <summary>
        /// Surcouche MODALE (grille de communication) : quand elle est visible,
        /// seules SES cibles comptent — le regard ne peut pas viser un bouton du
        /// jeu caché derrière.
        /// </summary>
        public static FrameworkElement ModalRoot;

        /// <summary>Collecte les cibles visibles sous la racine donnée.</summary>
        public static List<Target> Collect(Visual root)
        {
            var list = new List<Target>();
            lock (_forbidden) _forbidden.Clear();
            if (ModalRoot != null && ModalRoot.IsVisible) root = ModalRoot;
            Walk(root, list);
            return list;
        }

        private static void Walk(DependencyObject d, List<Target> list)
        {
            if (d == null) return;

            if (d is FrameworkElement fe)
            {
                if (!fe.IsVisible) return; // sous-arbre invisible : inutile de descendre

                if (GetParentOnly(fe))
                {
                    // Réservé à l'accompagnant : pas une cible, et zone interdite au dwell libre.
                    try
                    {
                        var tl = fe.PointToScreen(new Point(0, 0));
                        var br = fe.PointToScreen(new Point(fe.ActualWidth, fe.ActualHeight));
                        var r = new Rect(tl, br); r.Inflate(12, 12);
                        lock (_forbidden) _forbidden.Add(r);
                    }
                    catch { }
                    return;
                }

                bool isButton = fe is ButtonBase b && b.IsEnabled;
                if ((isButton || GetIsTarget(fe)) && fe.IsHitTestVisible)
                {
                    try
                    {
                        var tl = fe.PointToScreen(new Point(0, 0));
                        var br = fe.PointToScreen(new Point(fe.ActualWidth, fe.ActualHeight));
                        var r = new Rect(tl, br);
                        if (r.Width >= 8 && r.Height >= 8)
                            list.Add(new Target { Element = fe, ScreenRect = r, Precise = GetPrecise(fe), DwellFactor = Math.Max(0.3, Math.Min(3.0, GetDwellFactor(fe))) });
                    }
                    catch { /* élément pas encore connecté à l'écran */ }
                    return; // le contenu d'une cible n'est pas une cible séparée
                }
            }

            int n = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < n; i++) Walk(VisualTreeHelper.GetChild(d, i), list);
        }
    }
}
