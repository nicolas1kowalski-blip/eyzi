using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 🔒 Le code d'accès à l'administration (4 chiffres), tapé à la souris ou au
    /// doigt. Le regard est en pause pendant ce temps : ni l'enfant ni le pilotage
    /// oculaire ne peuvent le composer. Trois erreurs : la fenêtre se ferme.
    /// </summary>
    public sealed class PinWindow : Window
    {
        private readonly string _expected;
        private readonly TextBlock _dots, _msg;
        private string _typed = "";
        private int _errors;

        public PinWindow(string expected)
        {
            _expected = expected;
            Title = "Eyzi — Administration";
            Width = 420; Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Background = new LinearGradientBrush(Color.FromRgb(0xF6, 0xF0, 0xFF), Color.FromRgb(0xFF, 0xF6, 0xEC), 90);

            var col = new StackPanel { Margin = new Thickness(28, 22, 28, 18) };
            col.Children.Add(new TextBlock { Text = "🔒 Code d'accès parent", FontSize = 24, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), HorizontalAlignment = HorizontalAlignment.Center });
            _dots = new TextBlock { FontSize = 44, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 6), Text = "– – – –" };
            col.Children.Add(_dots);
            _msg = new TextBlock { FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)), HorizontalAlignment = HorizontalAlignment.Center, MinHeight = 20 };
            col.Children.Add(_msg);
            var pad = new UniformGrid { Columns = 3, Margin = new Thickness(0, 8, 0, 0) };
            foreach (var k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "✕" })
            {
                var b = new Button { Content = k, FontSize = 28, FontWeight = FontWeights.Bold, Height = 76, Margin = new Thickness(5), Focusable = false };
                string key = k;
                b.Click += (s, e) => Press(key);
                pad.Children.Add(b);
            }
            col.Children.Add(pad);
            Content = col;
        }

        private void Press(string k)
        {
            if (k == "✕") { DialogResult = false; Close(); return; }
            if (k == "⌫") { if (_typed.Length > 0) _typed = _typed.Substring(0, _typed.Length - 1); Show(); return; }
            if (_typed.Length >= 4) return;
            _typed += k;
            Show();
            if (_typed.Length == 4)
            {
                if (_typed == _expected) { DialogResult = true; Close(); return; }
                _errors++;
                _msg.Text = _errors >= 3 ? "Trois erreurs : fermeture." : "Code incorrect.";
                _typed = "";
                if (_errors >= 3) { DialogResult = false; Close(); return; }
                Dispatcher.BeginInvoke(new Action(Show), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void Show()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 4; i++) { if (i > 0) sb.Append(' '); sb.Append(i < _typed.Length ? "●" : "–"); }
            _dots.Text = sb.ToString();
        }
    }
}
