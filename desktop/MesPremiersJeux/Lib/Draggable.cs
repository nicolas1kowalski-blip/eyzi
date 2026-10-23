using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// Rend une tuile (Oui/Non, Parler) DÉPLAÇABLE à la souris ou au doigt sur
    /// un écran tactile : on la saisit, on la glisse, elle reste là (même après
    /// redémarrage, via Settings.TilePositions). Un simple clic/regard garde son
    /// effet normal : le déplacement ne commence qu'au-delà de quelques pixels,
    /// et un déplacement n'envoie pas de clic. Le tactile est promu en souris
    /// par WPF, donc un seul code pour les deux.
    /// </summary>
    public static class Draggable
    {
        private const double Threshold = 14; // px avant de considérer un glissement

        public static void Attach(FrameworkElement el, string id, Settings settings)
        {
            var tt = new TranslateTransform();
            el.RenderTransform = tt;
            var pos = Read(settings);
            if (pos.TryGetValue(id, out var p)) { tt.X = p.X; tt.Y = p.Y; }
            el.Loaded += (s, e) => Clamp(el, tt);
            el.SizeChanged += (s, e) => Clamp(el, tt);

            Point start = default; double x0 = 0, y0 = 0; bool down = false, dragging = false;

            el.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var parent = VisualTreeHelper.GetParent(el) as UIElement;
                if (parent == null) return;
                start = e.GetPosition(parent); x0 = tt.X; y0 = tt.Y; down = true; dragging = false;
            };
            el.PreviewMouseMove += (s, e) =>
            {
                if (!down || e.LeftButton != MouseButtonState.Pressed) return;
                var parent = VisualTreeHelper.GetParent(el) as UIElement;
                if (parent == null) return;
                var cur = e.GetPosition(parent);
                double dx = cur.X - start.X, dy = cur.Y - start.Y;
                if (!dragging)
                {
                    if (Math.Abs(dx) < Threshold && Math.Abs(dy) < Threshold) return;
                    dragging = true;
                    el.Opacity = 0.75;
                }
                tt.X = x0 + dx; tt.Y = y0 + dy;
                Clamp(el, tt);
                e.Handled = true;
            };
            el.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (!down) return;
                down = false;
                if (!dragging) return;          // simple clic : le bouton fait son travail
                dragging = false;
                el.Opacity = 1;
                e.Handled = true;               // un glissement n'est pas un clic
                Mouse.Capture(null);            // le bouton relâche son état « pressé »
                Save(settings, id, tt.X, tt.Y);
            };
            el.LostMouseCapture += (s, e) => { if (!dragging) down = false; };
        }

        /// <summary>Garde la tuile entièrement visible dans son conteneur.</summary>
        private static void Clamp(FrameworkElement el, TranslateTransform tt)
        {
            try
            {
                var parent = VisualTreeHelper.GetParent(el) as FrameworkElement;
                if (parent == null || el.ActualWidth <= 0 || parent.ActualWidth <= 0) return;
                var at = el.TranslatePoint(new Point(0, 0), parent); // inclut la translation courante
                double baseX = at.X - tt.X, baseY = at.Y - tt.Y;
                double maxX = parent.ActualWidth - el.ActualWidth, maxY = parent.ActualHeight - el.ActualHeight;
                tt.X = Math.Max(-baseX, Math.Min(maxX - baseX, tt.X));
                tt.Y = Math.Max(-baseY, Math.Min(maxY - baseY, tt.Y));
            }
            catch { }
        }

        // ---- persistance : "ouinon=12.5,-30;parler=0,0" ----
        private static Dictionary<string, Point> Read(Settings s)
        {
            var d = new Dictionary<string, Point>();
            foreach (var part in (s?.TilePositions ?? "").Split(';'))
            {
                var kv = part.Split('=');
                if (kv.Length != 2) continue;
                var xy = kv[1].Split(',');
                if (xy.Length == 2
                    && double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    && double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    d[kv[0]] = new Point(x, y);
            }
            return d;
        }

        private static void Save(Settings s, string id, double x, double y)
        {
            if (s == null) return;
            var d = Read(s);
            d[id] = new Point(Math.Round(x), Math.Round(y));
            s.TilePositions = string.Join(";", d.Select(kv =>
                kv.Key + "=" + kv.Value.X.ToString(CultureInfo.InvariantCulture) + "," + kv.Value.Y.ToString(CultureInfo.InvariantCulture)));
            s.Save();
        }

        /// <summary>Remet toutes les tuiles à leur place d'origine.</summary>
        public static void Reset(Settings s, params FrameworkElement[] els)
        {
            foreach (var el in els)
                if (el?.RenderTransform is TranslateTransform tt) { tt.X = 0; tt.Y = 0; }
            if (s == null) return;
            s.TilePositions = "";
            s.Save();
        }
    }
}
