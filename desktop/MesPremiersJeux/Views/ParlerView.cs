using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 💬 Le monde « Parler » : le tableau de communication en pleine page, et
    /// au-dessus l'ÉCOUTE : 👂 écoute la personne qui parle à l'enfant, affiche la
    /// phrase entendue et propose des réponses en un regard (via eyzi.fr, sinon
    /// des réponses de secours). Il s'enrichira : messages des proches, photos
    /// de famille, tableaux par moment de la journée, prédiction.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        private readonly Border _panel;
        private readonly TextBlock _heard, _state;
        private readonly UniformGrid _answers;
        private readonly Button _ear;
        private readonly Ellipse _led;
        private Settings _settings;
        public event Action BackRequested;

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public ParlerView()
        {
            var root = new Grid();
            root.Background = new LinearGradientBrush(Color.FromRgb(0xF1, 0xE6, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- L'écoute : le bouton 👂, le voyant, la phrase entendue, les réponses ---
            var head = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 8) };
            _ear = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 190, Height = 110, Margin = new Thickness(0, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromRgb(0xE2, 0xD4, 0xFF)),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", "👂", 56), new TextBlock { Text = "J'écoute", FontSize = 28, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
                },
            };
            _ear.Click += async (s, e) => { if (Listening.Enabled) await Listening.ListenOnce(); else Speech.SayAlways("L'écoute n'est pas réglée. C'est dans les réglages, page Nuage."); };
            DockPanel.SetDock(_ear, Dock.Left);
            head.Children.Add(_ear);
            var close = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "✕", Width = 90, Height = 90, Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            close.Click += (s, e) => Masquer();
            DockPanel.SetDock(close, Dock.Right);
            head.Children.Add(close);
            _led = new Ellipse { Width = 22, Height = 22, Fill = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _state = new TextBlock { FontSize = 20, Foreground = Dark, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
            _heard = new TextBlock { FontSize = 40, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { _led, _state } });
            texts.Children.Add(_heard);
            head.Children.Add(texts);

            _answers = new UniformGrid { Rows = 1, HorizontalAlignment = HorizontalAlignment.Center };
            var panelBody = new StackPanel();
            panelBody.Children.Add(head);
            panelBody.Children.Add(_answers);
            _panel = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(28), Padding = new Thickness(18, 12, 18, 12), Margin = new Thickness(14, 12, 14, 0),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.18 },
                Child = panelBody, Visibility = Visibility.Collapsed,
            };
            root.Children.Add(_panel);

            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            Grid.SetRow(_board, 1);
            root.Children.Add(_board);
            Content = root;

            Listening.StatusChanged += st => Dispatcher.BeginInvoke(new Action(() => MontrerEtat(st)));
            Emoji.FixTree(head);
        }

        /// <summary>On entre dans le monde : la grille s'annonce.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); }
            // Le bandeau d'écoute n'est là que si l'écoute est réglée.
            _panel.Visibility = (_settings.ListenMode ?? "off") != "off" ? Visibility.Visible : Visibility.Collapsed;
            MontrerEtat(Listening.Status);
        }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }

        private void MontrerEtat(string st)
        {
            switch (st)
            {
                case "veille": _led.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0xB8, 0x8A)); _state.Text = "Je veille : dis « Dis Eyzi » puis ta question."; break;
                case "ecoute": _led.Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x3A, 0x3A)); _state.Text = "J'écoute…"; Pulse(); break;
                case "pret": _led.Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x9B, 0xFF)); _state.Text = "Appuie sur 👂 puis parle à " + (_settings?.ChildName ?? "l'enfant") + "."; break;
                case "erreur": _led.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)); _state.Text = "L'écoute ne marche pas : " + Listening.LastError; break;
                default: _led.Fill = Brushes.Gray; _state.Text = "Écoute éteinte (réglages → Nuage)."; break;
            }
            Emoji.Fix(_state);
        }

        private void Pulse()
        {
            var a = new DoubleAnimation(1, 0.3, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(6) };
            _led.BeginAnimation(OpacityProperty, a);
        }

        /// <summary>Une phrase entendue : on l'affiche et on propose des réponses.</summary>
        public async void ShowHeard(string text)
        {
            _settings = _settings ?? Settings.Load();
            _panel.Visibility = Visibility.Visible;
            _heard.Text = "« " + text + " »";
            _answers.Children.Clear();
            _answers.Columns = 1;
            _answers.Children.Add(new TextBlock { Text = "Je cherche des réponses…", FontSize = 22, Foreground = Dark, Opacity = 0.6, Margin = new Thickness(8) });
            Suggest.Resultat res;
            try { res = await Suggest.Pour(_settings, text); } catch { res = Suggest.Defaults(text); }
            if (res.Simple != null && res.Simple != text) _heard.Text = "« " + text + " »\n" + res.Simple;
            _answers.Children.Clear();
            _answers.Columns = Math.Max(1, Math.Min(6, res.Reponses.Count));
            foreach (var r in res.Reponses)
            {
                var cap = r;
                var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                if (!string.IsNullOrWhiteSpace(r.Emoji)) st.Children.Add(Art.Visual("", r.Emoji.Trim(), 70));
                st.Children.Add(new TextBlock { Text = r.Texte, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 220, Margin = new Thickness(0, 6, 0, 0) });
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Content = st, Width = 240, Height = 170, Margin = new Thickness(8) };
                b.Click += (s, e) =>
                {
                    Speech.SayAlways(cap.Texte);
                    GazeStats.RecordComm("ecoute");
                    var sc = new ScaleTransform(1, 1); b.RenderTransformOrigin = new Point(0.5, 0.5); b.RenderTransform = sc;
                    var pop = new DoubleAnimation(1, 1.08, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                    sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop); sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
                };
                _answers.Children.Add(b);
            }
            Emoji.FixTree(_answers);
            Speech.SayAlways(res.Simple ?? text);
        }

        private void Masquer()
        {
            _heard.Text = "";
            _answers.Children.Clear();
            if ((_settings?.ListenMode ?? "off") == "off") _panel.Visibility = Visibility.Collapsed;
        }
    }
}
