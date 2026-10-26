using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// ⏳ L'écran de démarrage : affiché dès la première seconde, avant que la
    /// fenêtre principale (lourde à construire) n'existe. Il dit ce qui se passe
    /// (« Préparation des activités… », « Démarrage du regard… ») avec une barre
    /// animée, pour que personne ne croie que rien ne se passe — surtout après
    /// une mise à jour, où l'application redémarre toute seule.
    /// </summary>
    public static class Splash
    {
        private static Window _win;
        private static TextBlock _status;

        public static void Show(bool afterUpdate)
        {
            try
            {
                var root = new Border
                {
                    Background = new LinearGradientBrush(Color.FromRgb(0xF6, 0xF0, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90),
                    CornerRadius = new CornerRadius(28),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xC8, 0xF5)),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(40, 30, 40, 28),
                };
                var col = new StackPanel();
                col.Children.Add(new TextBlock
                {
                    Text = "Eyzi",
                    FontSize = 64,
                    FontWeight = FontWeights.ExtraBold,
                    Foreground = new LinearGradientBrush(Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0xFF, 0x8A, 0x3D), 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                col.Children.Add(new TextBlock
                {
                    Text = afterUpdate ? "Mise à jour installée ✓   Eyzi redémarre…" : "Eyzi démarre…",
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 18),
                });
                // La barre : un segment qui va et vient (on ne connaît pas la durée exacte).
                var track = new Border
                {
                    Height = 14, Width = 420, CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(Color.FromRgb(0xE6, 0xDE, 0xF5)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                var seg = new Border
                {
                    Height = 14, Width = 140, CornerRadius = new CornerRadius(7),
                    Background = new LinearGradientBrush(Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0xFF, 0x8A, 0x3D), 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    RenderTransform = new TranslateTransform(),
                };
                track.Child = seg;
                col.Children.Add(track);
                seg.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, 280, TimeSpan.FromMilliseconds(1100)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
                _status = new TextBlock
                {
                    Text = "Chargement…",
                    FontSize = 17,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x5B, 0x8A)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 14, 0, 0),
                };
                col.Children.Add(_status);
                col.Children.Add(new TextBlock
                {
                    Text = "version " + Updater.Current,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x8A, 0xB8)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0),
                });
                root.Child = col;

                _win = new Window
                {
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 560, Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Content = root,
                };
                _win.Show();
                // On force un passage de rendu tout de suite : la fenêtre principale
                // va occuper le fil d'affichage pendant plusieurs secondes.
                _win.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.Render);
            }
            catch (Exception ex) { Log.Write("app", "Écran de démarrage : " + ex.Message); }
        }

        /// <summary>Met à jour la ligne d'état (et laisse l'écran se redessiner).</summary>
        public static void Status(string text)
        {
            try
            {
                if (_win == null || _status == null) return;
                _status.Text = text;
                _win.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.Render);
            }
            catch { }
        }

        public static void Close()
        {
            try { _win?.Close(); } catch { }
            _win = null;
            _status = null;
        }
    }
}
