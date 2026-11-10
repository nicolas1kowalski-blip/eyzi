using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views.Creer.Volume;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🗿 LA STATUE, EN VRAI VOLUME. L'enfant choisit sa matière (pierre, marbre,
    /// bois, glace, bronze) en voyant une boule de chacune, puis ce qu'il veut
    /// sculpter. Un bloc brut est posé sur le socle. Là où il regarde, le ciseau
    /// enlève de vrais éclats : des facettes planes, éclairées, qui s'enfoncent
    /// jusqu'à la forme. La statue sort du bloc peu à peu. À la fin elle est polie,
    /// la lumière suit le regard (on la voit tourner sur le volume), on peut la
    /// peindre (la couleur se pose sur le relief) et la garder.
    /// </summary>
    public sealed class StatueAtelier : AtelierBase
    {
        private const int W = 900, H = 800, VW = 620, VH = 640, VX = 140, VY = 50, FS = 560;

        private static readonly (string Emoji, string Nom)[] Figures =
        {
            ("🦄", "une licorne"), ("🐘", "un éléphant"), ("🏰", "un château"), ("🐱", "un chat"), ("🦁", "un lion"), ("🌸", "une fleur"),
            ("⭐", "une étoile"), ("🐢", "une tortue"), ("🦋", "un papillon"), ("🐟", "un poisson"), ("🐻", "un ours"), ("🚀", "une fusée"),
            ("🐶", "un chien"), ("🐰", "un lapin"), ("🦉", "un hibou"), ("🐬", "un dauphin"), ("👑", "une couronne"), ("🦊", "un renard"),
            ("🌳", "un arbre"), ("⛵", "un bateau"), ("🐴", "un cheval"), ("🦒", "une girafe"), ("❤️", "un cœur"), ("🎸", "une guitare"),
        };

        private readonly Canvas _scene = new Canvas { Width = W, Height = H, ClipToBounds = true };
        private readonly WriteableBitmap _bmp = new WriteableBitmap(VW, VH, 96, 96, PixelFormats.Pbgra32, null);
        private readonly Canvas _eclats = new Canvas { Width = W, Height = H, IsHitTestVisible = false, ClipToBounds = true };
        private readonly Canvas _ciseau = Ciseau();
        private readonly Grid _choix = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };
        private readonly StackPanel _apres = new StackPanel { Visibility = Visibility.Collapsed };
        private readonly TextBlock _progres;
        private readonly DispatcherTimer _taille = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        private Volume3D _vol;
        private Genre _genre = Genre.Pierre;
        private (string Emoji, string Nom) _figure;
        private Point? _regard;
        private bool _fini, _polit, _enCours;
        private DateTime _dernierCoup = DateTime.MinValue, _derniereLumiere = DateTime.MinValue;
        private double _lumX = -0.45, _lumY = -0.60;
        private int _jeton;

        public StatueAtelier()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 190 };
            gauche.Children.Add(BoutonRetour("⬅  Ateliers"));
            gauche.Children.Add(Outil("🔨", "Une autre statue", ChoisirMatiere));
            _progres = new TextBlock { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(4, 6, 0, 10), TextWrapping = TextWrapping.Wrap };
            gauche.Children.Add(_progres);
            _apres.Children.Add(Outil("🎨", "Peindre ma statue", Peindre));
            _apres.Children.Add(Outil("💾", "Garder dans ma galerie", () => Garder(true)));
            gauche.Children.Add(_apres);
            root.Children.Add(gauche);

            // La scène : le mur de l'atelier, le sol, le socle, le bloc (une image calculée), les éclats, le ciseau.
            _scene.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(Color.FromRgb(0xFB, 0xF3, 0xE6), Color.FromRgb(0xE6, 0xD6, 0xC0), 90) });
            var sol = new Rectangle { Width = W, Height = 96, Fill = new LinearGradientBrush(Color.FromRgb(0xCD, 0xB8, 0x9C), Color.FromRgb(0xB5, 0x9E, 0x80), 90) };
            Canvas.SetTop(sol, H - 96); _scene.Children.Add(sol);
            var ombreSocle = new Ellipse { Width = 560, Height = 46, Fill = new SolidColorBrush(Color.FromArgb(0x48, 0, 0, 0)) };
            Canvas.SetLeft(ombreSocle, (W - 560) / 2.0 + 14); Canvas.SetTop(ombreSocle, H - 58); _scene.Children.Add(ombreSocle);
            var socle = new Rectangle { Width = 500, Height = 76, RadiusX = 8, RadiusY = 8, Fill = new LinearGradientBrush(Color.FromRgb(0xC4, 0xC4, 0xCC), Color.FromRgb(0x74, 0x74, 0x80), 90), Stroke = new SolidColorBrush(Color.FromRgb(0x5E, 0x5E, 0x6A)), StrokeThickness = 2 };
            Canvas.SetLeft(socle, (W - 500) / 2.0); Canvas.SetTop(socle, VY + VH - 4); _scene.Children.Add(socle);
            var dessus = new Rectangle { Width = 520, Height = 16, RadiusX = 6, RadiusY = 6, Fill = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xE0)), Stroke = new SolidColorBrush(Color.FromRgb(0x5E, 0x5E, 0x6A)), StrokeThickness = 2 };
            Canvas.SetLeft(dessus, (W - 520) / 2.0); Canvas.SetTop(dessus, VY + VH - 10); _scene.Children.Add(dessus);
            var img = new Image { Width = VW, Height = VH, Stretch = Stretch.Fill, Source = _bmp };
            Canvas.SetLeft(img, VX); Canvas.SetTop(img, VY); _scene.Children.Add(img);
            _scene.Children.Add(_eclats);
            _scene.Children.Add(_ciseau);
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

        protected override void Entrer() { if (_enCours || _fini) _taille.Start(); }
        protected override void Sortir() { _taille.Stop(); }
        protected override void SurRegard(Point ecran, bool valide) { _regard = valide ? Dans(_scene, ecran, W, H) : null; }

        // ------------------------------------------------------------------ les choix
        private void MontrerChoix(string titre, IEnumerable<(FrameworkElement Visuel, string Label, Action Action)> tuiles, int colonnes)
        {
            _taille.Stop(); _enCours = false; _fini = false; _jeton++;
            _ciseau.Visibility = Visibility.Collapsed;
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

        private void ChoisirMatiere()
        {
            var sortes = new[] { (Genre.Pierre, "La pierre"), (Genre.Marbre, "Le marbre"), (Genre.Bois, "Le bois"), (Genre.Glace, "La glace"), (Genre.Bronze, "Le bronze") };
            MontrerChoix("Dans quoi tu veux sculpter ?", sortes.Select(s => ((FrameworkElement)new Image { Width = 150, Height = 150, Source = Matiere.Boule(s.Item1) }, s.Item2, (Action)(() => { _genre = s.Item1; Speech.Say(s.Item2 + " !"); ChoisirFigure(); }))), 5);
            Speech.Say("Dans quoi tu veux sculpter ?");
        }

        private void ChoisirFigure()
        {
            var sept = Figures.OrderBy(x => Rng.Next()).Take(7).ToList();
            var tuiles = new List<(FrameworkElement, string, Action)> { (Art.ForEmoji("🎁", 150), "Surprise !", () => Commencer(Figures[Rng.Next(Figures.Length)], true)) };
            foreach (var f in sept) { var ff = f; tuiles.Add((Art.ForEmoji(f.Emoji, 150), Maj(f.Nom), () => Commencer(ff, false))); }
            MontrerChoix("Qu'est-ce que tu veux sculpter ?", tuiles, 4);
            Speech.Say("Qu'est-ce que tu veux sculpter ?");
        }

        private static string Maj(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ------------------------------------------------------------------ le bloc, puis la taille
        private async void Commencer((string Emoji, string Nom) f, bool surprise)
        {
            int jeton = ++_jeton;
            _figure = f; _fini = false; _polit = false; _enCours = false;
            _taille.Stop();
            _eclats.Children.Clear();
            _choix.Visibility = Visibility.Collapsed;
            _apres.Visibility = Visibility.Collapsed;
            _progres.Text = "Je prépare le bloc…";
            // Le dessin est rendu ici (fil d'affichage) ; le volume se calcule à côté, sans figer l'écran.
            byte[] src = null;
            try { var rtb = Matiere.Rendre(Art.ForEmoji(f.Emoji, FS), FS); src = new byte[FS * FS * 4]; rtb.CopyPixels(src, FS * 4, 0); }
            catch (Exception ex) { Log.Write("creer", "Statue, rendu du dessin : " + ex.Message); }
            var genre = _genre; int graine = Rng.Next(1000);
            Volume3D vol = null;
            await Task.Run(() =>
            {
                try
                {
                    var fig = src == null ? null : Relief.DepuisImage(src, FS);
                    if (fig == null || fig.Vide) return;
                    var v = new Volume3D(VW, VH, graine) { Matiere = genre, Peinture = new byte[VW * VH * 3] };
                    int ox = (VW - FS) / 2, oy = VH - FS;
                    for (int y = 0; y < FS; y++)
                        for (int x = 0; x < FS; x++)
                        {
                            int p = (y + oy) * VW + x + ox, q = y * FS + x;
                            v.T[p] = fig.Hauteur[q];
                            v.Peinture[p * 3] = fig.Couleur[q * 3]; v.Peinture[p * 3 + 1] = fig.Couleur[q * 3 + 1]; v.Peinture[p * 3 + 2] = fig.Couleur[q * 3 + 2];
                        }
                    v.BlocBrut(new Random(graine), 26, 22);
                    v.Figer();
                    v.ToutOmbrer();
                    vol = v;
                }
                catch (Exception ex) { Log.Write("creer", "Statue, volume : " + ex); }
            });
            if (jeton != _jeton) return; // on a choisi autre chose entre-temps
            if (vol == null)
            {
                Speech.Say("Ce dessin-là ne se laisse pas sculpter. Choisis-en un autre !");
                ChoisirFigure();
                return;
            }
            _vol = vol;
            _lumX = -0.45; _lumY = -0.60;
            Ecrire(0, 0, VW - 1, VH - 1);
            _progres.Text = surprise ? "Qui se cache là ?" : "Regarde le bloc !";
            var m = Matiere.De(genre);
            _enCours = true;
            _taille.Start();
            Speech.Say(surprise ? "Une surprise se cache dans " + m.Nom + ". Regarde le bloc : ton ciseau le taille !" : "On sculpte " + f.Nom + " dans " + m.Nom + ". Regarde le bloc : ton ciseau le taille !");
        }

        private void Ecrire(int x0, int y0, int x1, int y1)
        {
            if (_vol == null || x1 < x0 || y1 < y0) return;
            _bmp.WritePixels(new Int32Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), _vol.Px, VW * 4, x0, y0);
        }

        private void Tick()
        {
            if (_vol == null || Games.GameControl.Paused) return;
            if (_polit) { Polir(); return; }
            if (!_regard.HasValue) { if (!_fini) _ciseau.Visibility = Visibility.Collapsed; return; }
            var p = _regard.Value;
            if (_fini) { LumiereSuit(p); return; }
            bool parti = _vol.Coup(p.X - VX, p.Y - VY, 85, 0.085, Rng, out int x0, out int y0, out int x1, out int y1);
            PoserCiseau(_ciseau, p, parti);
            if (!parti) return;
            Ecrire(x0, y0, x1, y1);
            var m = Matiere.De(_genre);
            Eclats(_eclats, p, m.Eclat, H, 2);
            if ((DateTime.Now - _dernierCoup).TotalMilliseconds > 230) { _dernierCoup = DateTime.Now; Matiere.Toc(m); }
            double fait = 1 - _vol.Reste;
            _progres.Text = fait < 0.15 ? "Toc, toc, toc…" : fait < 0.5 ? "La forme apparaît…" : fait < 0.85 ? "Encore un peu !" : "Presque !";
            if (_vol.Reste < 0.06) { _polit = true; _ciseau.Visibility = Visibility.Collapsed; _progres.Text = "On polit…"; }
        }

        /// <summary>Le polissage : les dernières aspérités fondent en quelques images, puis la statue est finie.</summary>
        private void Polir()
        {
            bool encore = _vol.Polir(0.55f);
            _vol.ToutOmbrer();
            Ecrire(0, 0, VW - 1, VH - 1);
            if (encore) return;
            _polit = false; _fini = true; _enCours = false;
            var m = Matiere.De(_genre);
            _progres.Text = "C'est " + _figure.Nom + " !";
            _apres.Visibility = Visibility.Visible;
            Speech.Say("Bravo ! Tu as sculpté " + _figure.Nom + " dans " + m.Nom + " ! Regarde autour : la lumière suit tes yeux. Tu peux aussi la peindre.");
            Fete();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += (s, e) => { t.Stop(); Garder(false); };
            t.Start();
        }

        /// <summary>La statue finie : la lumière vient de là où l'enfant regarde, et l'on voit le volume tourner sous elle.</summary>
        private void LumiereSuit(Point p)
        {
            if ((DateTime.Now - _derniereLumiere).TotalMilliseconds < 160) return;
            double lx = Math.Max(-0.85, Math.Min(0.85, (p.X - (VX + VW / 2.0)) / 380.0)), ly = Math.Max(-0.85, Math.Min(0.85, (p.Y - (VY + VH * 0.55)) / 380.0));
            if (Math.Abs(lx - _lumX) < 0.06 && Math.Abs(ly - _lumY) < 0.06) return;
            _derniereLumiere = DateTime.Now;
            _lumX = lx; _lumY = ly;
            _vol.Lumiere(lx, ly, 0.62);
            _vol.ToutOmbrer();
            Ecrire(0, 0, VW - 1, VH - 1);
        }

        private void Peindre()
        {
            if (!_fini || _vol == null) return;
            _vol.Peint = !_vol.Peint;
            _vol.ToutOmbrer();
            Ecrire(0, 0, VW - 1, VH - 1);
            SoundFx.PopSound();
            Speech.Say(_vol.Peint ? "Et voilà les couleurs, posées sur ta sculpture !" : "Comme avant, dans " + Matiere.De(_genre).Nom + ".");
        }

        private void Garder(bool dire)
        {
            if (!_fini) return;
            var path = Creations.Garder(_scene, W, H, "statue");
            if (!dire) return;
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la statue."); return; }
            Speech.Say("Ta statue est gardée dans ta galerie !");
            Fete();
        }
    }
}
