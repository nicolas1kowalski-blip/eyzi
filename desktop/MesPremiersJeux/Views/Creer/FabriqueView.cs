using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🎼 LA FABRIQUE DE MUSIQUE : une grille de cases qui tourne en boucle. Chaque
    /// ligne est un instrument (grosse caisse, caisse claire, cymbale, piano,
    /// guitare, xylophone), chaque colonne un temps. L'enfant allume des cases d'un
    /// regard, et ça joue. Les notes des instruments mélodiques suivent une gamme
    /// qui sonne toujours juste : impossible de faire faux. 💾 garde la musique.
    /// </summary>
    public sealed class FabriqueView : UserControl
    {
        private const int Pas = 8;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private sealed class Ligne { public string Emoji, Nom; public Color Teinte; public Func<int, Func<float[]>> Part; }

        private static readonly int[] Penta = { 0, 2, 4, 7, 9, 7, 4, 2 };
        private static readonly int[] Basse = { -7, -7, -3, -3, -2, -2, -3, -5 };
        private static readonly int[] Aigu = { 7, 9, 11, 14, 11, 9, 7, 4 };

        private readonly Ligne[] _lignes =
        {
            new Ligne { Emoji = "🥁", Nom = "la grosse caisse", Teinte = Color.FromRgb(0xFF, 0xB3, 0x8A), Part = s => SoundFx.KitPart(0) },
            new Ligne { Emoji = "🪘", Nom = "la caisse claire", Teinte = Color.FromRgb(0xFF, 0xD6, 0x8A), Part = s => SoundFx.KitPart(1) },
            new Ligne { Emoji = "🔔", Nom = "la cymbale", Teinte = Color.FromRgb(0xFF, 0xF0, 0xA8), Part = s => SoundFx.KitPart(3) },
            new Ligne { Emoji = "🎹", Nom = "le piano", Teinte = Color.FromRgb(0xC9, 0xE4, 0xFF), Part = s => SoundFx.NotePart("piano", Penta[s], 0.4) },
            new Ligne { Emoji = "🎸", Nom = "la guitare", Teinte = Color.FromRgb(0xD9, 0xF2, 0xC9), Part = s => SoundFx.NotePart("guitare", Basse[s], 0.5) },
            new Ligne { Emoji = "🎵", Nom = "le xylophone", Teinte = Color.FromRgb(0xEA, 0xD6, 0xFF), Part = s => SoundFx.NotePart("xylophone", Aigu[s], 0.35) },
        };

        private readonly bool[,] _on;
        private readonly Button[,] _cases;
        private readonly Border[] _colonnes = new Border[Pas];
        private readonly Canvas _fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        private int _pas = -1;
        private Button _play;
        private readonly Random _rng = new Random();
        private readonly TextBlock _tempoText;

        public FabriqueView()
        {
            _on = new bool[_lignes.Length, Pas];
            _cases = new Button[_lignes.Length, Pas];
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xEC, 0xFF)) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var haut = new Grid { Margin = new Thickness(16, 8, 16, 0) };
            var back = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left };
            back.Click += (s, e) => { Stop(); BackRequested?.Invoke(); };
            haut.Children.Add(back);
            haut.Children.Add(new TextBlock { Text = "🎼  La fabrique de musique", FontSize = 34, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            root.Children.Add(haut);

            // La grille : une colonne d'instruments puis les temps.
            var grille = new Grid { Margin = new Thickness(16, 8, 16, 8) };
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            for (int s = 0; s < Pas; s++) grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int l = 0; l < _lignes.Length; l++) grille.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            for (int s = 0; s < Pas; s++)
            {
                var col = new Border { Background = new SolidColorBrush(Color.FromArgb(0x00, 0x7E, 0x3F, 0xF2)), CornerRadius = new CornerRadius(12), Margin = new Thickness(2, 0, 2, 0), IsHitTestVisible = false };
                Grid.SetColumn(col, s + 1); Grid.SetRowSpan(col, _lignes.Length);
                grille.Children.Add(col);
                _colonnes[s] = col;
            }
            for (int l = 0; l < _lignes.Length; l++)
            {
                var lg = _lignes[l];
                var etiquette = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = double.NaN, Height = double.NaN, Margin = new Thickness(4), Background = new SolidColorBrush(lg.Teinte), Content = new Viewbox { Margin = new Thickness(8), Child = Art.ForEmoji(lg.Emoji, 64) }, ToolTip = lg.Nom };
                int li = l;
                etiquette.Click += (s, e) => { Speech.Say(lg.Nom); SoundFx.PlayMix("solo-" + li, new List<Func<float[]>> { lg.Part(0) }); };
                Grid.SetRow(etiquette, l); Grid.SetColumn(etiquette, 0);
                grille.Children.Add(etiquette);
                for (int s = 0; s < Pas; s++)
                {
                    int ss = s;
                    var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = double.NaN, Height = double.NaN, Margin = new Thickness(5), Background = Brushes.White, Opacity = 0.9 };
                    b.Click += (sender, e) => Basculer(li, ss);
                    Grid.SetRow(b, l); Grid.SetColumn(b, s + 1);
                    grille.Children.Add(b);
                    _cases[l, s] = b;
                }
            }
            Grid.SetRow(grille, 1);
            root.Children.Add(grille);

            // Les commandes
            var bas = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            _play = Cmd("▶️", "Jouer / arrêter", () => { if (_timer.IsEnabled) Stop(); else Jouer(); });
            bas.Children.Add(_play);
            bas.Children.Add(Cmd("🐢", "Moins vite", () => Tempo(+60)));
            _tempoText = new TextBlock { Text = "", FontSize = 20, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Width = 90, TextAlignment = TextAlignment.Center };
            bas.Children.Add(_tempoText);
            bas.Children.Add(Cmd("🐇", "Plus vite", () => Tempo(-60)));
            bas.Children.Add(Cmd("🎲", "Une surprise", Surprise));
            bas.Children.Add(Cmd("🧹", "Tout effacer", () => { for (int l = 0; l < _lignes.Length; l++) for (int s = 0; s < Pas; s++) Mettre(l, s, false); Speech.Say("La grille est vide. À toi !"); }));
            bas.Children.Add(Cmd("💾", "Garder ma musique", Garder));
            Grid.SetRow(bas, 2);
            root.Children.Add(bas);
            Grid.SetRowSpan(_fx, 3);
            root.Children.Add(_fx);
            Content = root;

            _timer.Tick += (s, e) => Tick();
            Unloaded += (s, e) => Stop();
            Tempo(0);
        }

        private Button Cmd(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 44, Child = Art.ForEmoji(emoji, 44) }, MinWidth = 110, Height = 84, Margin = new Thickness(6, 0, 6, 0), ToolTip = tip };
            b.Click += (s, e) => action();
            return b;
        }

        private void Basculer(int l, int s)
        {
            Mettre(l, s, !_on[l, s]);
            if (_on[l, s]) SoundFx.PlayMix("solo-" + l + "-" + s, new List<Func<float[]>> { _lignes[l].Part(s) });
            if (!_timer.IsEnabled && Compte() == 1) Jouer(); // la première case allumée lance la boucle
        }

        private void Mettre(int l, int s, bool on)
        {
            _on[l, s] = on;
            var b = _cases[l, s];
            b.Background = on ? new SolidColorBrush(_lignes[l].Teinte) : Brushes.White;
            b.Content = on ? new Viewbox { Margin = new Thickness(10), Child = Art.ForEmoji(_lignes[l].Emoji, 48) } : null;
            b.Opacity = on ? 1 : 0.9;
        }

        private int Compte() { int n = 0; for (int l = 0; l < _lignes.Length; l++) for (int s = 0; s < Pas; s++) if (_on[l, s]) n++; return n; }

        private void Jouer() { _pas = -1; _timer.Start(); _play.Content = new Viewbox { Height = 44, Child = Art.ForEmoji("⏸️", 44) }; }
        private void Stop() { _timer.Stop(); _play.Content = new Viewbox { Height = 44, Child = Art.ForEmoji("▶️", 44) }; foreach (var c in _colonnes) c.Background = new SolidColorBrush(Color.FromArgb(0x00, 0x7E, 0x3F, 0xF2)); }

        private void Tempo(int delta)
        {
            var ms = Math.Max(180, Math.Min(540, (int)_timer.Interval.TotalMilliseconds + delta));
            _timer.Interval = TimeSpan.FromMilliseconds(ms);
            _tempoText.Text = (60000 / ms / 2) + " bpm";
            if (delta != 0) Speech.Say(delta > 0 ? "Moins vite." : "Plus vite !");
        }

        private void Tick()
        {
            if (Games.GameControl.Paused) return;
            int prev = _pas;
            _pas = (_pas + 1) % Pas;
            if (prev >= 0) _colonnes[prev].Background = new SolidColorBrush(Color.FromArgb(0x00, 0x7E, 0x3F, 0xF2));
            _colonnes[_pas].Background = new SolidColorBrush(Color.FromArgb(0x30, 0x7E, 0x3F, 0xF2));
            var parts = new List<Func<float[]>>(); var key = new StringBuilder();
            for (int l = 0; l < _lignes.Length; l++) if (_on[l, _pas]) { parts.Add(_lignes[l].Part(_pas)); key.Append(l); }
            if (parts.Count > 0) SoundFx.PlayMix(key + "-" + _pas, parts);
        }

        private void Surprise()
        {
            for (int l = 0; l < _lignes.Length; l++) for (int s = 0; s < Pas; s++) Mettre(l, s, false);
            for (int s = 0; s < Pas; s += 2) Mettre(0, s, true);                       // grosse caisse sur les temps
            for (int s = 2; s < Pas; s += 4) Mettre(1, s, true);                       // caisse claire
            Mettre(2, 0, _rng.Next(2) == 0); Mettre(2, 6, true);                       // cymbale
            for (int s = 0; s < Pas; s++) if (_rng.NextDouble() < 0.5) Mettre(3, s, true);
            for (int s = 0; s < Pas; s += 2) if (_rng.NextDouble() < 0.7) Mettre(4, s, true);
            for (int s = 1; s < Pas; s += 2) if (_rng.NextDouble() < 0.4) Mettre(5, s, true);
            Speech.Say("Surprise ! Écoute ça !");
            if (!_timer.IsEnabled) Jouer();
        }

        /// <summary>La grille en texte : « tempo=300 » puis une ligne de 0 et 1 par instrument.</summary>
        public string Texte()
        {
            var sb = new StringBuilder();
            sb.Append("tempo=").Append((int)_timer.Interval.TotalMilliseconds).Append('\n');
            for (int l = 0; l < _lignes.Length; l++) { for (int s = 0; s < Pas; s++) sb.Append(_on[l, s] ? '1' : '0'); sb.Append('\n'); }
            return sb.ToString();
        }

        /// <summary>Recharge une musique gardée (depuis la galerie) et la joue.</summary>
        public void Charger(string texte)
        {
            Stop();
            for (int l = 0; l < _lignes.Length; l++) for (int s = 0; s < Pas; s++) Mettre(l, s, false);
            var lignes = (texte ?? "").Replace("\r", "").Split('\n');
            int li = 0;
            foreach (var ln in lignes)
            {
                if (ln.StartsWith("tempo=")) { if (int.TryParse(ln.Substring(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms)) { _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(180, Math.Min(540, ms))); Tempo(0); } continue; }
                if (ln.Length < Pas || li >= _lignes.Length) continue;
                for (int s = 0; s < Pas; s++) Mettre(li, s, ln[s] == '1');
                li++;
            }
            if (Compte() > 0) Jouer();
        }

        private void Garder()
        {
            if (Compte() == 0) { Speech.Say("Allume d'abord des cases pour faire ta musique !"); return; }
            var path = Creations.GarderMusique(Texte());
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la musique."); return; }
            Speech.Say("Ta musique est gardée dans ta galerie ! Bravo, compositrice !");
            Celebrate?.Invoke(_fx);
        }
    }
}
