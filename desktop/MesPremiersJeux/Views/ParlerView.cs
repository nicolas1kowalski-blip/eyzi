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
    /// 💬 Le monde « Parler » : le tableau de communication, sur toute la page.
    /// Rien au-dessus, pour que les cases soient les plus grandes possible :
    /// l'écoute s'ouvre avec l'oreille 👂 en haut à droite de la fenêtre, le lieu
    /// se règle dans l'administration, et « Je veux dire… » se trouve dans l'écran
    /// d'écoute. Seule exception : quand un Wi-Fi inconnu apparaît, la question
    /// « on est où ? » se pose en haut, pour l'accompagnant, le temps d'y répondre.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        private readonly Border _lieuPrompt;
        private readonly TextBlock _lieuQuestion;
        private string _reseauInconnu = "";
        private Settings _settings;
        public event Action BackRequested;

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public ParlerView()
        {
            var root = new Grid();
            root.Background = new LinearGradientBrush(Color.FromRgb(0xF1, 0xE6, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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
            root.Children.Add(_lieuPrompt);

            // --- Le tableau, sur toute la page ---
            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            _board.AvecRetour = false; // la barre du haut suffit pour changer de monde
            Grid.SetRow(_board, 1);
            root.Children.Add(_board);
            Content = root;
            Emoji.FixTree(_lieuPrompt);
        }

        /// <summary>Le Wi-Fi a dit où l'on est (le réglage est déjà enregistré par la fenêtre principale) : la question n'a plus lieu d'être.</summary>
        public void AfficherLieu(string lieu)
        {
            if (string.IsNullOrWhiteSpace(lieu)) return;
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

        /// <summary>On entre dans le monde.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); }
            _lieuPrompt.Visibility = _reseauInconnu.Length > 0 && _settings.LieuAuto ? Visibility.Visible : Visibility.Collapsed;
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings);
        }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }
    }
}
