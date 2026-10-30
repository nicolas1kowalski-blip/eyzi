using System;
using System.Collections.Generic;
using System.Linq;
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
    /// l'ÉCOUTE : 👂 écoute la personne qui parle à l'enfant, affiche la phrase
    /// entendue et propose des réponses en PLEIN ÉCRAN, en grandes cases avec
    /// pictos (banque Mulberry, photos de la famille, mots du tableau). L'IA
    /// (eyzi.fr) adapte la grille au type de question ; sans IA, grille devinée.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        private readonly Border _panel;
        private readonly Grid _answersHost;
        private readonly UniformGrid _answers;
        private readonly TextBlock _heard, _state, _wait;
        private readonly Button _ear, _back;
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

            // --- Le bandeau d'écoute : 👂, le voyant et l'état, la phrase entendue, retour au tableau ---
            var head = new DockPanel { LastChildFill = true };
            _ear = GrosBouton("👂", "J'écoute", Color.FromRgb(0xE2, 0xD4, 0xFF));
            _ear.Click += async (s, e) => { if (Listening.Enabled) await Listening.ListenOnce(); else Speech.SayAlways("L'écoute n'est pas réglée. C'est dans les réglages, page Nuage."); };
            DockPanel.SetDock(_ear, Dock.Left);
            head.Children.Add(_ear);
            _back = GrosBouton("🧩", "Tableau", Color.FromRgb(0xD9, 0xF5, 0xE0));
            _back.Click += (s, e) => MontrerTableau();
            _back.Visibility = Visibility.Collapsed;
            DockPanel.SetDock(_back, Dock.Right);
            head.Children.Add(_back);
            _led = new Ellipse { Width = 22, Height = 22, Fill = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _state = new TextBlock { FontSize = 18, Foreground = Dark, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            _heard = new TextBlock { FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 14, 0) };
            texts.Children.Add(_heard);
            texts.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { _led, _state }, Margin = new Thickness(0, 4, 0, 0) });
            head.Children.Add(texts);
            _panel = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(24), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(12, 10, 12, 0),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.18 },
                Child = head, Visibility = Visibility.Collapsed,
            };
            root.Children.Add(_panel);

            // --- Le tableau, ou la grille de réponses en plein écran ---
            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            Grid.SetRow(_board, 1);
            root.Children.Add(_board);

            _answers = new UniformGrid { Margin = new Thickness(10) };
            _wait = new TextBlock { FontSize = 34, Foreground = Dark, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
            _answersHost = new Grid { Visibility = Visibility.Collapsed, Children = { _answers, _wait } };
            Grid.SetRow(_answersHost, 1);
            root.Children.Add(_answersHost);
            Content = root;

            Listening.StatusChanged += st => Dispatcher.BeginInvoke(new Action(() => MontrerEtat(st)));
            Emoji.FixTree(head);
        }

        private Button GrosBouton(string icone, string label, Color fond)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 170, Height = 100, Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(fond),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", icone, 48), new TextBlock { Text = label, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
                },
            };
            return b;
        }

        /// <summary>On entre dans le monde : la grille s'annonce.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            MontrerTableau();
            try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); }
            // Le bandeau d'écoute n'est là que si l'écoute est réglée.
            _panel.Visibility = (_settings.ListenMode ?? "off") != "off" ? Visibility.Visible : Visibility.Collapsed;
            MontrerEtat(Listening.Status);
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings); // le site se réveille pendant qu'on parle
        }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }

        private void MontrerTableau()
        {
            _answersHost.Visibility = Visibility.Collapsed;
            _board.Visibility = Visibility.Visible;
            _back.Visibility = Visibility.Collapsed;
            _heard.Text = "";
        }

        private void MontrerReponses()
        {
            _board.Visibility = Visibility.Collapsed;
            _answersHost.Visibility = Visibility.Visible;
            _back.Visibility = Visibility.Visible;
            _panel.Visibility = Visibility.Visible;
        }

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

        /// <summary>Une phrase entendue : on attend l'IA (chrono visible), puis la grille en plein écran.</summary>
        public async void ShowHeard(string text)
        {
            _settings = _settings ?? Settings.Load();
            MontrerReponses();
            _heard.Text = "« " + text + " »";
            _enCours = new Suggest.Echange { Question = text };
            _historique.Add(_enCours);
            while (_historique.Count > 3) _historique.RemoveAt(0);
            var histo = _historique.Take(_historique.Count - 1).ToList();

            var secours = Suggest.Defaults(text);
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey);
            if (!enLigne)
            {
                Grille(secours);
                _state.Text = "Réponses devinées (sans IA).";
                Speech.SayAlways(text);
                return;
            }

            _answers.Children.Clear();
            _wait.Visibility = Visibility.Visible;
            _wait.Text = "L'IA cherche des réponses…";
            var t0 = DateTime.Now;
            var tick = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            tick.Tick += (s, e) => _wait.Text = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "L'IA cherche des réponses… {0:0.0} s", (DateTime.Now - t0).TotalSeconds);
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
            _wait.Visibility = Visibility.Collapsed;
            if (_enCours?.Question != text) return; // une autre phrase est arrivée entre-temps
            if (res.EnLigne)
            {
                Grille(res);
                if (res.Simple != null && res.Simple != text) _heard.Text = "« " + text + " »  ·  " + res.Simple;
                _state.Text = "Réponses de l'IA · " + (res.Chrono ?? "");
                if (res.Simple != null && res.Simple != text) Speech.SayAlways(res.Simple);
            }
            else
            {
                Grille(secours);
                _state.Text = "Réponses devinées (IA indisponible" + (string.IsNullOrEmpty(res.Erreur) ? "" : " : " + res.Erreur) + ")";
            }
        }

        /// <summary>Le visuel d'une réponse : la photo ou l'image du tableau, sinon le picto Mulberry, sinon l'emoji.</summary>
        private static FrameworkElement Visuel(Suggest.Reponse r, CommBoard.Mot mot)
        {
            try
            {
                if (mot != null)
                {
                    if (mot.Photo != null) return mot.Photo();
                    var src = Art.Find(mot.Img);
                    if (src != null) return new Image { Source = src, Stretch = Stretch.Uniform };
                }
                var p = Pictos.Picture(r.Picto, 200) ?? Pictos.Picture(r.Texte, 200);
                if (p != null) return p;
                if (!string.IsNullOrWhiteSpace(r.Emoji)) return Art.Visual("", r.Emoji.Trim(), 200);
            }
            catch { }
            return Art.Visual("", "💬", 200);
        }

        /// <summary>La grille en plein écran : 1 rangée jusqu'à 3 réponses, 2 rangées jusqu'à 8 ; les cases prennent toute la place.</summary>
        private void Grille(Suggest.Resultat res)
        {
            _answers.Children.Clear();
            var reps = res.Reponses.Take(8).ToList();
            int n = Math.Max(1, reps.Count);
            int rows = n <= 3 ? 1 : 2;
            _answers.Rows = rows;
            _answers.Columns = (int)Math.Ceiling(n / (double)rows);
            foreach (var r in reps)
            {
                var cap = r;
                var mot = CommBoard.Trouver(r.Cle);
                var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                var pic = Visuel(r, mot);
                st.Children.Add(new Viewbox { Child = pic, Height = rows == 1 ? 260 : 170, Stretch = Stretch.Uniform });
                st.Children.Add(new TextBlock { Text = r.Texte, FontSize = rows == 1 ? 40 : 30, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 380, Margin = new Thickness(0, 8, 0, 0) });
                var b = new Button
                {
                    Style = (Style)Application.Current.Resources["MenuTile"],
                    Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(10, 8, 10, 8), Child = st },
                    Width = double.NaN, Height = double.NaN, Margin = new Thickness(8),
                };
                b.Click += (s, e) =>
                {
                    var dit = mot != null && !string.IsNullOrEmpty(mot.Dit) ? mot.Dit : cap.Texte;
                    if (mot == null || !VoiceBank.Play("comm-" + mot.Cle)) Speech.SayAlways(dit);
                    if (_enCours != null) _enCours.Reponse = cap.Texte;
                    GazeStats.RecordComm("ecoute-" + res.Type);
                    var sc = new ScaleTransform(1, 1); b.RenderTransformOrigin = new Point(0.5, 0.5); b.RenderTransform = sc;
                    var pop = new DoubleAnimation(1, 1.06, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                    sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop); sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
                };
                _answers.Children.Add(b);
            }
            Emoji.FixTree(_answers);
        }
    }
}
