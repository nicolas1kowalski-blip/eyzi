using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 💬 Le monde « Parler » : le tableau de communication en pleine page, avec
    /// au-dessus une seule rangée : « Parle-moi » (ouvre l'écoute en surcouche),
    /// le lieu, et, quand un Wi-Fi inconnu apparaît, la question « on est où ? »
    /// pour l'accompagnant. L'écoute elle-même vit dans EcouteView, en surcouche,
    /// par-dessus n'importe quel écran.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        private readonly Border _panel, _lieuPrompt;
        private readonly TextBlock _lieuQuestion;
        private readonly Button _ear, _lieu;
        private string _reseauInconnu = "";
        private Settings _settings;
        public event Action BackRequested;
        /// <summary>L'accompagnant ou l'enfant a changé le lieu à la main.</summary>
        public event Action<string> LieuChoisi;
        /// <summary>« Parle-moi » : ouvrir l'écoute (en surcouche) et écouter une phrase.</summary>
        public event Action EcouteRequested;
        /// <summary>« Je veux dire… » : ce que l'enfant pourrait vouloir dire maintenant, sans question.</summary>
        public event Action SpontaneRequested;

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public ParlerView()
        {
            var root = new Grid();
            root.Background = new LinearGradientBrush(Color.FromRgb(0xF1, 0xE6, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- La rangée du haut : Parle-moi, le lieu ---
            var head = new DockPanel { LastChildFill = true };
            _ear = GrosBouton("👂", "Parle-moi", Color.FromRgb(0xE2, 0xD4, 0xFF));
            _ear.Click += (s, e) => EcouteRequested?.Invoke();
            DockPanel.SetDock(_ear, Dock.Left);
            head.Children.Add(_ear);
            var dire = GrosBouton("💬", "Je veux dire…", Color.FromRgb(0xFF, 0xE4, 0xF0));
            dire.Width = 250;
            dire.Click += (s, e) => SpontaneRequested?.Invoke();
            DockPanel.SetDock(dire, Dock.Left);
            head.Children.Add(dire);
            _lieu = GrosBouton("🏠", "Maison", Color.FromRgb(0xFF, 0xF0, 0xC8));
            Gaze.GazeTargets.SetParentOnly(_lieu, false);
            _lieu.Click += (s, e) => CyclerLieu();
            DockPanel.SetDock(_lieu, Dock.Right);
            head.Children.Add(_lieu);
            head.Children.Add(new TextBlock { Text = "Mes mots", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) });
            _panel = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(24), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(12, 10, 12, 0),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.18 },
                Child = head,
            };

            // 📍 Un Wi-Fi inconnu : « on est où ? », pour l'accompagnant (souris ou doigt, pas le regard).
            _lieuQuestion = new TextBlock { FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), TextWrapping = TextWrapping.Wrap };
            var choix = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var l in Lib.Lieux.Tous)
            {
                var cle = l.Cle;
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = 130, Height = 64, Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF0, 0xC8)), Content = new TextBlock { Text = l.Icone + " " + l.Nom, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Dark } };
                Gaze.GazeTargets.SetParentOnly(b, true);
                b.Click += (s, e) => RepondreLieu(cle);
                choix.Children.Add(b);
            }
            var jamais = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = 150, Height = 64, Background = Brushes.WhiteSmoke, Content = new TextBlock { Text = "Ne plus demander", FontSize = 16, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center } };
            Gaze.GazeTargets.SetParentOnly(jamais, true);
            jamais.Click += (s, e) => RepondreLieu(Lib.Lieux.Ignorer);
            choix.Children.Add(jamais);
            var lieuRow = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(choix, Dock.Right);
            lieuRow.Children.Add(choix);
            lieuRow.Children.Add(_lieuQuestion);
            _lieuPrompt = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(18), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(12, 8, 12, 0),
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 1, Opacity = 0.15 },
                Child = lieuRow, Visibility = Visibility.Collapsed,
            };
            Gaze.GazeTargets.SetParentOnly(_lieuPrompt, true);
            var top = new StackPanel();
            top.Children.Add(_panel);
            top.Children.Add(_lieuPrompt);
            root.Children.Add(top);

            // --- Le tableau ---
            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            Grid.SetRow(_board, 1);
            root.Children.Add(_board);
            Content = root;
            Emoji.FixTree(top);
        }

        private static (string Cle, string Icone, string Nom)[] Lieux => Lib.Lieux.Tous;

        private void MontrerLieu()
        {
            var cle = _settings?.ParlerLieu ?? "maison";
            var l = Lieux.FirstOrDefault(x => x.Cle == cle); if (l.Cle == null) l = Lieux[0];
            _lieu.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Children = { Art.Visual("", l.Icone, 44), new TextBlock { Text = l.Nom, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
            };
        }

        private void CyclerLieu()
        {
            if (_settings == null) return;
            int i = Array.FindIndex(Lieux, x => x.Cle == (_settings.ParlerLieu ?? "maison"));
            var next = Lieux[(i + 1 + Lieux.Length) % Lieux.Length];
            _settings.ParlerLieu = next.Cle;
            _settings.Save();
            MontrerLieu();
            Speech.SayAlways(next.Nom + " !");
            try { LieuChoisi?.Invoke(next.Cle); } catch { }
        }

        /// <summary>Le Wi-Fi a dit où l'on est : on l'affiche (le réglage est déjà enregistré par la fenêtre principale).</summary>
        public void AfficherLieu(string lieu)
        {
            if (_settings == null || string.IsNullOrWhiteSpace(lieu)) return;
            _settings.ParlerLieu = lieu;
            MontrerLieu();
            if (_reseauInconnu.Length > 0 && Lib.Lieux.LieuDe(_reseauInconnu).Length > 0) { _reseauInconnu = ""; _lieuPrompt.Visibility = Visibility.Collapsed; }
        }

        /// <summary>Un réseau jamais vu : on demande une fois à l'accompagnant.</summary>
        public void DemanderLieu(string reseau)
        {
            if (string.IsNullOrWhiteSpace(reseau)) return;
            _reseauInconnu = reseau;
            _lieuQuestion.Text = "📍 Nouveau Wi-Fi « " + reseau + " » : on est où ?";
            Emoji.FixTree(_lieuQuestion);
            if (_settings != null && _settings.LieuAuto) _lieuPrompt.Visibility = Visibility.Visible;
        }

        private void RepondreLieu(string cle)
        {
            var r = _reseauInconnu; _reseauInconnu = "";
            _lieuPrompt.Visibility = Visibility.Collapsed;
            if (r.Length > 0) Lib.Lieux.Associer(r, cle); // → Lieux.Changed → la fenêtre principale enregistre et nous rappelle
        }

        private Button GrosBouton(string icone, string label, Color fond)
        {
            return new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 190, Height = 92, Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(fond),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", icone, 44), new TextBlock { Text = label, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
                },
            };
        }

        /// <summary>On entre dans le monde.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); }
            _ear.Visibility = (_settings.ListenMode ?? "off") != "off" ? Visibility.Visible : Visibility.Collapsed;
            MontrerLieu();
            _lieuPrompt.Visibility = _reseauInconnu.Length > 0 && _settings.LieuAuto ? Visibility.Visible : Visibility.Collapsed;
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings);
        }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }
    }
}
