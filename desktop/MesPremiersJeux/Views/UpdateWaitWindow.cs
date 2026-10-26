using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// ⏳ La fenêtre d'attente pendant une mise à jour. Entre la fermeture de
    /// l'ancienne version et le premier écran de la nouvelle, il s'écoule
    /// facilement une minute (dernières tâches, analyse de l'installeur par
    /// Windows, installation) sans rien à l'écran : cette fenêtre tient
    /// compagnie. Elle tourne dans une COPIE de l'exécutable (Eyzi-Attente.exe,
    /// dans le dossier des mises à jour) pour que l'installeur puisse remplacer
    /// Eyzi.exe, et se ferme toute seule quand la nouvelle version est à l'écran.
    /// </summary>
    public sealed class UpdateWaitWindow : Window
    {
        private readonly int _oldPid;
        private readonly DateTime _start = DateTime.Now;
        private readonly TextBlock _hint;

        public UpdateWaitWindow(int oldPid)
        {
            _oldPid = oldPid;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Width = 620; Height = 330;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;

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
                Text = "Eyzi se met à jour",
                FontSize = 44, FontWeight = FontWeights.ExtraBold,
                Foreground = new LinearGradientBrush(Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0xFF, 0x8A, 0x3D), 0),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            col.Children.Add(new TextBlock
            {
                Text = "L'installation est en cours. L'application va redémarrer toute seule : ne touchez à rien.",
                FontSize = 19, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                Margin = new Thickness(0, 8, 0, 18),
            });
            var track = new Border { Height = 14, Width = 460, CornerRadius = new CornerRadius(7), Background = new SolidColorBrush(Color.FromRgb(0xE6, 0xDE, 0xF5)), HorizontalAlignment = HorizontalAlignment.Center };
            var seg = new Border
            {
                Height = 14, Width = 150, CornerRadius = new CornerRadius(7),
                Background = new LinearGradientBrush(Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0xFF, 0x8A, 0x3D), 0),
                HorizontalAlignment = HorizontalAlignment.Left, RenderTransform = new TranslateTransform(),
            };
            track.Child = seg;
            col.Children.Add(track);
            seg.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, 310, TimeSpan.FromMilliseconds(1100)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            _hint = new TextBlock
            {
                Text = "Cela prend en général une à deux minutes.",
                FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x5B, 0x8A)),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0),
            };
            col.Children.Add(_hint);
            root.Child = col;
            Content = root;

            // Surveillance : la nouvelle version est à l'écran → on s'efface.
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            t.Tick += (s, e) =>
            {
                try
                {
                    int me = Process.GetCurrentProcess().Id;
                    foreach (var p in Process.GetProcessesByName("Eyzi"))
                    {
                        try
                        {
                            if (p.Id == me || p.Id == _oldPid) continue;
                            p.Refresh();
                            if (p.MainWindowHandle != IntPtr.Zero) { t.Stop(); Close(); return; }
                        }
                        catch { }
                    }
                    var age = DateTime.Now - _start;
                    if (age > TimeSpan.FromSeconds(90)) _hint.Text = "C'est un peu plus long que d'habitude… encore un instant.";
                    if (age > TimeSpan.FromMinutes(5)) { t.Stop(); Close(); } // on ne reste pas là éternellement
                }
                catch { }
            };
            t.Start();
        }
    }
}
