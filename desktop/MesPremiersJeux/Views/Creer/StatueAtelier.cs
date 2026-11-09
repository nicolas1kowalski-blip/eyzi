using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🗿 LA STATUE : l'enfant choisit sa matière (pierre, marbre, bois, glace, or)
    /// et ce qu'il veut sculpter (ou une surprise). Un bloc est posé sur le socle ;
    /// le regard le fait éclater morceau par morceau, et la statue apparaît, taillée
    /// dans la matière. Ensuite on peut la PEINDRE (ses vraies couleurs) et la garder.
    /// </summary>
    public sealed class StatueAtelier : AtelierBase
    {
        private const int W = 900, H = 800, BW = 620, BH = 640, BX = 140, BY = 50, FS = 560;

        private static readonly (string Emoji, string Nom)[] Figures =
        {
            ("🦄", "une licorne"), ("🐘", "un éléphant"), ("🏰", "un château"), ("🐱", "un chat"), ("🦁", "un lion"), ("🌸", "une fleur"),
            ("⭐", "une étoile"), ("🐢", "une tortue"), ("🦋", "un papillon"), ("🐟", "un poisson"), ("🐻", "un ours"), ("🚀", "une fusée"),
            ("🐶", "un chien"), ("🐰", "un lapin"), ("🦉", "un hibou"), ("🐬", "un dauphin"), ("🦖", "un dinosaure"), ("👑", "une couronne"),
            ("🌳", "un arbre"), ("⛵", "un bateau"), ("🐴", "un cheval"), ("🦒", "une girafe"), ("❤️", "un cœur"), ("🎸", "une guitare"),
        };

        private readonly Canvas _scene = new Canvas { Width = W, Height = H, ClipToBounds = true };
        private readonly Image _relief = new Image { Width = FS, Height = FS, Stretch = Stretch.Uniform };
        private readonly ContentControl _couleur = new ContentControl { Width = FS, Height = FS, Visibility = Visibility.Collapsed };
        private readonly Image _blocImg = new Image { Width = BW, Height = BH, Stretch = Stretch.Fill };
        private readonly Canvas _eclats = new Canvas { Width = W, Height = H, IsHitTestVisible = false, ClipToBounds = true };
        private readonly Bloc _bloc = new Bloc(BW, BH);
        private readonly Grid _choix = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };
        private readonly StackPanel _gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 190 };
        private readonly StackPanel _apres = new StackPanel { Visibility = Visibility.Collapsed };
        private readonly TextBlock _progres;
        private readonly DispatcherTimer _taille = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        private Matiere.Sorte _matiere = Matiere.Pierre;
        private (string Emoji, string Nom) _figure;
        private Point? _regard;
        private bool _revele, _enCours, _peinte;
        private DateTime _dernierCoup = DateTime.MinValue;

        public StatueAtelier()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _gauche.Children.Add(BoutonRetour("⬅  Ateliers"));
            _gauche.Children.Add(Outil("🔨", "Une autre statue", ChoisirMatiere));
            _progres = new TextBlock { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(4, 6, 0, 10), TextWrapping = TextWrapping.Wrap };
            _gauche.Children.Add(_progres);
            _apres.Children.Add(Outil("🎨", "Peindre ma statue", Peindre));
            _apres.Children.Add(Outil("💾", "Garder dans ma galerie", () => Garder(true)));
            _gauche.Children.Add(_apres);
            root.Children.Add(_gauche);

            // La scène : le mur de l'atelier, le socle, la statue, le bloc, les éclats.
            _scene.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(Color.FromRgb(0xFB, 0xF3, 0xE6), Color.FromRgb(0xE6, 0xD6, 0xC0), 90) });
            var sol = new Rectangle { Width = W, Height = 110, Fill = new SolidColorBrush(Color.FromRgb(0xC9, 0xB3, 0x96)) };
            Canvas.SetTop(sol, H - 110); _scene.Children.Add(sol);
            var socle = new Rectangle { Width = 460, Height = 80, RadiusX = 10, RadiusY = 10, Fill = new LinearGradientBrush(Color.FromRgb(0xB8, 0xB8, 0xC0), Color.FromRgb(0x7E, 0x7E, 0x88), 90) };
            Canvas.SetLeft(socle, (W - 460) / 2.0); Canvas.SetTop(socle, BY + BH - 6); _scene.Children.Add(socle);
            Canvas.SetLeft(_relief, (W - FS) / 2.0); Canvas.SetTop(_relief, BY + BH - FS); _scene.Children.Add(_relief);
            Canvas.SetLeft(_couleur, (W - FS) / 2.0); Canvas.SetTop(_couleur, BY + BH - FS); _scene.Children.Add(_couleur);
            _blocImg.Source = _bloc.Image;
            Canvas.SetLeft(_blocImg, BX); Canvas.SetTop(_blocImg, BY); _scene.Children.Add(_blocImg);
            _scene.Children.Add(_eclats);
            var cadre = new Border { CornerRadius = new CornerRadius(14), Margin = new Thickness(2), ClipToBounds = true, Child = new Viewbox { Stretch = Stretch.Uniform, Child = _scene } };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);
            Grid.SetColumn(_choix, 1);
            root.Children.Add(_choix);
            Grid.SetColumnSpan(Fx, 2);
            root.Children.Add(Fx);
            Content = root;

            _taille.Tick += (s, e) => Tick();
            _scene.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regard = e.GetPosition(_scene); };
            _scene.MouseLeftButtonUp += (s, e) => _regard = null;
            ChoisirMatiere();
        }

        protected override void Entrer() { if (_enCours) _taille.Start(); }
        protected override void Sortir() { _taille.Stop(); }
        protected override void SurRegard(Point ecran, bool valide) { _regard = valide ? Dans(_scene, ecran, W, H) : null; }

        // ------------------------------------------------------------------ les choix
        private void MontrerChoix(string titre, IEnumerable<(FrameworkElement Visuel, string Label, Action Action)> tuiles, int colonnes)
        {
            _taille.Stop(); _enCours = false;
            _choix.Children.Clear();
            _choix.RowDefinitions.Clear();
            _choix.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _choix.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _choix.Children.Add(new TextBlock { Text = titre, FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 4) });
            var g = new UniformGrid { Columns = colonnes, Margin = new Thickness(20, 6, 20, 20) };
            foreach (var (visuel, label, action) in tuiles)
            {
                var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                st.Children.Add(new Viewbox { Height = 150, Child = visuel });
                st.Children.Add(new TextBlock { Text = label, FontSize = 28, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });
                var b = new Button { Style = (Style)Application.Current.Resources["MenuTile"], Width = double.NaN, Height = double.NaN, Margin = new Thickness(8), Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(12), Child = st } };
                var a = action;
                b.Click += (s, e) => a();
                g.Children.Add(b);
            }
            Grid.SetRow(g, 1);
            _choix.Children.Add(g);
            _choix.Visibility = Visibility.Visible;
            _apres.Visibility = Visibility.Collapsed;
            _progres.Text = "";
        }

        private static FrameworkElement Pastille(Matiere.Sorte m)
        {
            var img = new Image { Width = 150, Height = 150, Stretch = Stretch.Fill, Source = Matiere.Image(Matiere.Texture(m, 150, 150, 3), 150, 150) };
            return new Border { Width = 150, Height = 150, CornerRadius = new CornerRadius(24), ClipToBounds = true, Child = img, Clip = new RectangleGeometry(new Rect(0, 0, 150, 150), 24, 24) };
        }

        private void ChoisirMatiere()
        {
            var sortes = new[] { (Matiere.Pierre, "La pierre"), (Matiere.Marbre, "Le marbre"), (Matiere.Bois, "Le bois"), (Matiere.Glace, "La glace"), (Matiere.Or, "L'or") };
            MontrerChoix("Dans quoi tu veux sculpter ?", sortes.Select(s => (Pastille(s.Item1), s.Item2, (Action)(() => { _matiere = s.Item1; Speech.Say(s.Item2 + " !"); ChoisirFigure(); }))), 5);
            Speech.Say("Dans quoi tu veux sculpter ?");
        }

        private void ChoisirFigure()
        {
            var six = Figures.OrderBy(x => Rng.Next()).Take(7).ToList();
            var tuiles = new List<(FrameworkElement, string, Action)>
            {
                (Art.ForEmoji("🎁", 150), "Surprise !", () => Commencer(Figures[Rng.Next(Figures.Length)], true)),
            };
            foreach (var f in six) { var ff = f; tuiles.Add((Art.ForEmoji(f.Emoji, 150), Maj(f.Nom), () => Commencer(ff, false))); }
            MontrerChoix("Qu'est-ce que tu veux sculpter ?", tuiles, 4);
            Speech.Say("Qu'est-ce que tu veux sculpter ?");
        }

        private static string Maj(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ------------------------------------------------------------------ tailler
        private void Commencer((string Emoji, string Nom) f, bool surprise)
        {
            _figure = f; _revele = false; _peinte = false;
            _eclats.Children.Clear();
            var relief = Matiere.Relief(Art.ForEmoji(f.Emoji, FS), FS, _matiere);
            _relief.Source = relief;
            _relief.Visibility = relief != null ? Visibility.Visible : Visibility.Collapsed;
            _couleur.Content = new Viewbox { Child = Art.ForEmoji(f.Emoji, FS) };
            _couleur.Visibility = relief != null ? Visibility.Collapsed : Visibility.Visible; // sans relief : la figure en couleurs
            _bloc.Remplir(Matiere.Texture(_matiere, BW, BH, Rng.Next(1000)), 46);
            _blocImg.BeginAnimation(OpacityProperty, null);
            _blocImg.Opacity = 1;
            _choix.Visibility = Visibility.Collapsed;
            _apres.Visibility = Visibility.Collapsed;
            _progres.Text = surprise ? "Qui se cache là ?" : "Regarde le bloc !";
            _enCours = true;
            _taille.Start();
            Speech.Say(surprise ? "Une surprise se cache dans " + _matiere.Nom + ". Regarde le bloc pour le tailler !" : "On sculpte " + f.Nom + " dans " + _matiere.Nom + ". Regarde le bloc pour le tailler !");
        }

        private void Tick()
        {
            if (_revele || !_regard.HasValue || Games.GameControl.Paused) return;
            var p = _regard.Value;
            if (!_bloc.Tailler(p.X - BX, p.Y - BY, 85, Rng)) return;
            Eclats(_eclats, p, _matiere.Eclat, H);
            if ((DateTime.Now - _dernierCoup).TotalMilliseconds > 230) { _dernierCoup = DateTime.Now; Matiere.Toc(_matiere); }
            double fait = 1 - _bloc.Reste;
            _progres.Text = fait < 0.15 ? "Toc, toc, toc…" : fait < 0.5 ? "Ça apparaît…" : fait < 0.85 ? "Encore un peu !" : "Presque !";
            if (_bloc.Reste < 0.07) Reveler();
        }

        private void Reveler()
        {
            _revele = true; _enCours = false; _taille.Stop();
            var fondu = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(600));
            fondu.Completed += (s, e) => { _bloc.Vider(); };
            _blocImg.BeginAnimation(OpacityProperty, fondu);
            _progres.Text = "C'est " + _figure.Nom + " !";
            _apres.Visibility = Visibility.Visible;
            Speech.Say("Bravo ! Tu as sculpté " + _figure.Nom + " dans " + _matiere.Nom + " ! Tu peux la peindre, si tu veux.");
            Fete();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += (s, e) => { t.Stop(); Garder(false); };
            t.Start();
        }

        private void Peindre()
        {
            if (!_revele) return;
            _peinte = !_peinte;
            if (_relief.Source == null) _peinte = true;
            _couleur.Visibility = _peinte ? Visibility.Visible : Visibility.Collapsed;
            _relief.Visibility = _peinte ? Visibility.Collapsed : Visibility.Visible;
            SoundFx.PopSound();
            Speech.Say(_peinte ? "Et voilà les couleurs !" : "Comme avant, dans " + _matiere.Nom + ".");
        }

        private void Garder(bool dire)
        {
            if (!_revele) return;
            var path = Creations.Garder(_scene, W, H, "statue");
            if (!dire) return;
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la statue."); return; }
            Speech.Say("Ta statue est gardée dans ta galerie !");
            Fete();
        }
    }
}
