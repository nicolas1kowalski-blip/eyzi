using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Linq;
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
        private readonly List<Suggest.Echange> _historique = new List<Suggest.Echange>();
        private Suggest.Echange _enCours;
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
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings); // le site se réveille pendant qu'on parle
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

        /// <summary>Une phrase entendue : la grille de secours tout de suite, puis la grille adaptée au type de question.</summary>
        public async void ShowHeard(string text)
        {
            _settings = _settings ?? Settings.Load();
            _panel.Visibility = Visibility.Visible;
            _heard.Text = "« " + text + " »";
            // L'échange en cours entre dans l'historique (la réponse sera notée au choix de l'enfant).
            _enCours = new Suggest.Echange { Question = text };
            _historique.Add(_enCours);
            while (_historique.Count > 3) _historique.RemoveAt(0);
            var histo = _historique.Take(_historique.Count - 1).ToList();

            var secours = Suggest.Defaults(text);
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey);
            if (!enLigne)
            {
                // Sans réponses en ligne : la grille devinée, tout de suite.
                Grille(secours);
                _state.Text = "Réponses devinées (sans IA).";
                Speech.SayAlways(text);
                return;
            }

            // En ligne : on ATTEND l'IA, avec un chrono visible, pour savoir d'où vient le temps.
            _answers.Children.Clear(); _answers.Columns = 1;
            var attente = new TextBlock { Text = "L'IA cherche des réponses…", FontSize = 26, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(8) };
            _answers.Children.Add(attente);
            var t0 = DateTime.Now;
            var tick = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            tick.Tick += (s, e) => attente.Text = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "L'IA cherche des réponses… {0:0.0} s", (DateTime.Now - t0).TotalSeconds);
            tick.Start();
            Speech.SayAlways(text);

            Suggest.Resultat res;
            try
            {
                var vocab = CommBoard.Vocabulaire().Select(m => new Suggest.Mot { Cle = m.Cle, Label = m.Label }).ToList();
                res = await Suggest.Pour(_settings, text, vocab, histo);
            }
            catch (Exception ex) { res = secours; res.Erreur = ex.Message; }
            tick.Stop();
            if (_enCours?.Question != text) return; // une autre phrase est arrivée entre-temps
            if (res.EnLigne)
            {
                Grille(res);
                if (res.Simple != null && res.Simple != text) _heard.Text = "« " + text + " »\n" + res.Simple;
                _state.Text = "Réponses de l'IA · " + (res.Chrono ?? "");
                if (res.Simple != null && res.Simple != text) Speech.SayAlways(res.Simple);
            }
            else
            {
                Grille(secours);
                _state.Text = "Réponses devinées (IA indisponible" + (string.IsNullOrEmpty(res.Erreur) ? "" : " : " + res.Erreur) + ")";
            }
            Speech.SayAlways(res.Simple ?? text);
        }

        /// <summary>La grille de réponses, adaptée au type : 3 grandes cases pour oui / non, les options d'un choix, jusqu'à 6 cases avec les pictos du tableau.</summary>
        private void Grille(Suggest.Resultat res)
        {
            _answers.Children.Clear();
            int n = Math.Max(1, Math.Min(6, res.Reponses.Count));
            bool grand = res.Type == "fermee" || res.Type == "consigne" || n <= 3;
            _answers.Columns = n;
            double w = grand ? 330 : n <= 4 ? 260 : 215, h = grand ? 210 : 170;
            foreach (var r in res.Reponses.Take(6))
            {
                var cap = r;
                var mot = CommBoard.Trouver(r.Cle);
                var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                FrameworkElement pic = null;
                if (mot != null)
                {
                    try { pic = mot.Photo != null ? mot.Photo() : Art.Visual(mot.Img, mot.Emoji, grand ? 96 : 70); } catch { pic = null; }
                    if (pic != null) { pic.Width = grand ? 110 : 80; pic.Height = grand ? 110 : 80; }
                }
                if (pic == null && !string.IsNullOrWhiteSpace(r.Emoji)) pic = Art.Visual("", r.Emoji.Trim(), grand ? 96 : 70);
                if (pic != null) st.Children.Add(pic);
                st.Children.Add(new TextBlock { Text = r.Texte, FontSize = grand ? 32 : 24, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = w - 30, Margin = new Thickness(0, 6, 0, 0) });
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Content = st, Width = w, Height = h, Margin = new Thickness(8) };
                b.Click += (s, e) =>
                {
                    var dit = mot != null && !string.IsNullOrEmpty(mot.Dit) ? mot.Dit : cap.Texte;
                    if (mot == null || !VoiceBank.Play("comm-" + mot.Cle)) Speech.SayAlways(dit);
                    if (_enCours != null) _enCours.Reponse = cap.Texte;
                    GazeStats.RecordComm("ecoute-" + res.Type);
                    var sc = new ScaleTransform(1, 1); b.RenderTransformOrigin = new Point(0.5, 0.5); b.RenderTransform = sc;
                    var pop = new DoubleAnimation(1, 1.08, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                    sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop); sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
                };
                _answers.Children.Add(b);
            }
            Emoji.FixTree(_answers);
        }

        private void Masquer()
        {
            _heard.Text = "";
            _answers.Children.Clear();
            if ((_settings?.ListenMode ?? "off") == "off") _panel.Visibility = Visibility.Collapsed;
        }
    }
}
