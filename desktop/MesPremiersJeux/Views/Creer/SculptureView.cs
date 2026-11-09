using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🏺 SCULPTER AU REGARD, deux ateliers :
    ///  · Le tour de potier : une motte tourne ; là où l'enfant regarde, la terre se
    ///    creuse ou s'élargit, et le vase prend sa forme. On choisit la glaçure, on
    ///    cuit (💾), le vase va dans la galerie.
    ///  · La statue cachée : un bloc de pierre ; le regard le fait éclater petit à
    ///    petit, et la statue apparaît dessous. Quand tout est dégagé, bravo !
    /// Rien ne peut rater : la terre suit les yeux, la pierre s'en va où l'on regarde.
    /// </summary>
    public sealed class SculptureView : UserControl
    {
        private const int W = 900, H = 800;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private readonly Grid _menu, _potier, _statue;
        private readonly Canvas _fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private string _mode = "menu";
        private readonly Random _rng = new Random();

        // --- tour de potier
        private const int Tranches = 44;
        private const double Haut = 110, Bas = 690, Axe = W / 2.0;
        private readonly double[] _r = new double[Tranches];
        private readonly Canvas _roue = new Canvas { Width = W, Height = H, ClipToBounds = true };
        private readonly Polygon _vase = new Polygon();
        private readonly Polygon _reflet = new Polygon { Fill = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)), IsHitTestVisible = false };
        private readonly Ellipse _col = new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x20)), IsHitTestVisible = false };
        private readonly List<Line> _stries = new List<Line>();
        private Color _glacure = Color.FromRgb(0xC9, 0x6A, 0x3A);
        private bool _faconne = true;
        private Button _btnFaconne;
        private double _phase;
        private readonly DispatcherTimer _tour = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        private Point? _regardRoue;

        // --- statue cachée
        private const int SW = 900, SH = 800;
        private readonly WriteableBitmap _pierre = new WriteableBitmap(SW, SH, 96, 96, PixelFormats.Pbgra32, null);
        private readonly byte[] _ppx = new byte[SW * SH * 4];
        private readonly Image _pierreImg = new Image { Width = SW, Height = SH, Stretch = Stretch.Fill };
        private readonly Grid _figure = new Grid { Width = SW, Height = SH };
        private readonly Canvas _eclats = new Canvas { Width = SW, Height = SH, IsHitTestVisible = false, ClipToBounds = true };
        private readonly DispatcherTimer _taille = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        private Point? _regardPierre;
        private int _restant, _total;
        private bool _revele;
        private string _figNom;
        private DateTime _dernierCoup = DateTime.MinValue;
        private readonly TextBlock _progres;

        private static readonly (string Emoji, string Nom)[] Figures =
        {
            ("🦄", "une licorne"), ("🐘", "un éléphant"), ("🏰", "un château"), ("🐱", "un chat"), ("🦁", "un lion"), ("🌸", "une fleur"),
            ("⭐", "une étoile"), ("🐢", "une tortue"), ("🦋", "un papillon"), ("🐟", "un poisson"), ("🐻", "un ours"), ("🚀", "une fusée"),
        };

        public SculptureView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };

            // ----- le choix de l'atelier
            _menu = new Grid();
            var choix = new UniformGrid { Columns = 2, Margin = new Thickness(60, 40, 60, 40) };
            var t1 = MenuKit.ThemeTile("🏺", "Le tour de potier", Color.FromRgb(0xFF, 0xE3, 0xC9), "👀  🫙  🔥");
            t1.Click += (s, e) => OuvrirPotier();
            var t2 = MenuKit.ThemeTile("🗿", "La statue cachée", Color.FromRgb(0xE6, 0xE6, 0xEA), "🪨  👀  ✨");
            t2.Click += (s, e) => OuvrirStatue();
            choix.Children.Add(t1); choix.Children.Add(t2);
            var backM = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16, 10, 0, 0) };
            backM.Click += (s, e) => BackRequested?.Invoke();
            _menu.Children.Add(choix);
            _menu.Children.Add(backM);
            root.Children.Add(_menu);

            // ----- le tour de potier
            _potier = new Grid { Visibility = Visibility.Collapsed };
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 180 };
            var back1 = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Ateliers", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            back1.Click += (s, e) => Menu();
            gauche.Children.Add(back1);
            _btnFaconne = Outil("👀", "Je façonne / je regarde", () => { _faconne = !_faconne; _btnFaconne.Content = new Viewbox { Height = 40, Child = Art.ForEmoji(_faconne ? "👀" : "✋", 40) }; Speech.Say(_faconne ? "La terre suit tes yeux !" : "Stop. Le vase ne bouge plus."); });
            gauche.Children.Add(_btnFaconne);
            gauche.Children.Add(Outil("🎲", "Une nouvelle motte", () => { NouvelleMotte(); Speech.Say("Une nouvelle motte de terre !"); }));
            gauche.Children.Add(Outil("🔥", "Cuire et garder", Cuire));
            _potier.Children.Add(gauche);
            // La roue
            _roue.Children.Add(new Ellipse { Width = 620, Height = 90, Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x4E, 0x3A)), Margin = new Thickness(Axe - 310, Bas - 20, 0, 0) });
            _roue.Children.Add(new Rectangle { Width = 120, Height = 90, Fill = new SolidColorBrush(Color.FromRgb(0x4E, 0x38, 0x2A)), Margin = new Thickness(Axe - 60, Bas + 20, 0, 0) });
            _roue.Children.Add(_vase);
            for (int i = 0; i < 9; i++) { var l = new Line { Stroke = new SolidColorBrush(Color.FromArgb(0x38, 0, 0, 0)), StrokeThickness = 2, IsHitTestVisible = false }; _stries.Add(l); _roue.Children.Add(l); }
            _roue.Children.Add(_reflet);
            _roue.Children.Add(_col);
            var cadre1 = new Border { CornerRadius = new CornerRadius(14), Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0xF6, 0xEA), Color.FromRgb(0xEE, 0xDC, 0xC8), 90), Margin = new Thickness(2), Child = new Viewbox { Stretch = Stretch.Uniform, Child = _roue } };
            Grid.SetColumn(cadre1, 1);
            _potier.Children.Add(cadre1);
            // Les glaçures
            var droite = new StackPanel { Margin = new Thickness(6, 8, 10, 8), Width = 150 };
            droite.Children.Add(new TextBlock { Text = "Glaçure", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(4, 6, 0, 4) });
            var gl = new UniformGrid { Columns = 2 };
            foreach (var (hex, nom) in new[] { ("#C96A3A", "terre cuite"), ("#2F7FBF", "bleu"), ("#3C9D5B", "vert"), ("#E8B13A", "jaune"), ("#C73F6E", "rose"), ("#6A3FB5", "violet"), ("#EFEFEF", "blanc"), ("#2B2B2B", "noir") })
            {
                var c = (Color)ColorConverter.ConvertFromString(hex); var n = nom;
                var b = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = double.NaN, Height = double.NaN, MinHeight = 62, Margin = new Thickness(5), Background = new SolidColorBrush(c), ToolTip = nom };
                b.Click += (s, e) => { _glacure = c; Redessiner(); Speech.Say("Glaçure " + n); };
                gl.Children.Add(b);
            }
            droite.Children.Add(gl);
            Grid.SetColumn(droite, 2);
            _potier.Children.Add(droite);
            root.Children.Add(_potier);

            // ----- la statue cachée
            _statue = new Grid { Visibility = Visibility.Collapsed };
            _statue.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _statue.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var g2 = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 180 };
            var back2 = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Ateliers", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            back2.Click += (s, e) => Menu();
            g2.Children.Add(back2);
            g2.Children.Add(Outil("🎲", "Un autre bloc", () => { NouveauBloc(); Speech.Say("Un nouveau bloc de pierre ! Qui se cache dedans ?"); }));
            _progres = new TextBlock { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(4, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
            g2.Children.Add(_progres);
            _statue.Children.Add(g2);
            var pile = new Grid { Width = SW, Height = SH, Children = { _figure, _pierreImg, _eclats } };
            var cadre2 = new Border { CornerRadius = new CornerRadius(14), Background = new SolidColorBrush(Color.FromRgb(0xE9, 0xE4, 0xDC)), Margin = new Thickness(2), Child = new Viewbox { Stretch = Stretch.Uniform, Child = pile } };
            Grid.SetColumn(cadre2, 1);
            _statue.Children.Add(cadre2);
            root.Children.Add(_statue);

            root.Children.Add(_fx);
            Content = root;

            _pierreImg.Source = _pierre;
            _tour.Tick += (s, e) => TickRoue();
            _taille.Tick += (s, e) => TickPierre();
            Loaded += (s, e) => { GazeFeed.Sample += SurRegard; if (_mode == "potier") _tour.Start(); if (_mode == "statue") _taille.Start(); };
            Unloaded += (s, e) => { GazeFeed.Sample -= SurRegard; _tour.Stop(); _taille.Stop(); };
            _roue.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regardRoue = e.GetPosition(_roue); };
            _pierreImg.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regardPierre = e.GetPosition(_pierreImg); };
            NouvelleMotte();
        }

        private Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip, HorizontalAlignment = HorizontalAlignment.Left };
            b.Click += (s, e) => action();
            return b;
        }

        private void Menu()
        {
            _mode = "menu"; _tour.Stop(); _taille.Stop();
            _menu.Visibility = Visibility.Visible; _potier.Visibility = Visibility.Collapsed; _statue.Visibility = Visibility.Collapsed;
        }

        private void OuvrirPotier()
        {
            _mode = "potier";
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Visible; _statue.Visibility = Visibility.Collapsed;
            _tour.Start();
            Speech.Say("Le tour de potier ! Regarde la terre : elle suit tes yeux.");
        }

        private void OuvrirStatue()
        {
            _mode = "statue";
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Collapsed; _statue.Visibility = Visibility.Visible;
            NouveauBloc();
            _taille.Start();
            Speech.Say("Une statue se cache dans la pierre. Regarde le bloc pour le faire éclater !");
        }

        private void SurRegard(Point ecran, bool valide)
        {
            if (!IsVisible) return;
            if (!valide) { _regardRoue = null; _regardPierre = null; return; }
            try
            {
                if (_mode == "potier") { var p = _roue.PointFromScreen(ecran); _regardRoue = p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H ? p : (Point?)null; }
                else if (_mode == "statue") { var p = _pierreImg.PointFromScreen(ecran); _regardPierre = p.X >= 0 && p.Y >= 0 && p.X < SW && p.Y < SH ? p : (Point?)null; }
            }
            catch { }
        }

        // ------------------------------------------------------------------ le tour de potier
        private void NouvelleMotte()
        {
            for (int i = 0; i < Tranches; i++) _r[i] = 150 + 40 * Math.Sin(i / (double)Tranches * Math.PI);
            Redessiner();
        }

        private void TickRoue()
        {
            _phase += 0.08;
            if (_faconne && _regardRoue.HasValue)
            {
                var p = _regardRoue.Value;
                if (p.Y >= Haut - 30 && p.Y <= Bas + 10)
                {
                    double cible = Math.Max(45, Math.Min(400, Math.Abs(p.X - Axe)));
                    double yi = (p.Y - Haut) / (Bas - Haut) * (Tranches - 1);
                    for (int i = 0; i < Tranches; i++)
                    {
                        double d = Math.Abs(i - yi);
                        double k = Math.Exp(-d * d / 18.0) * 0.14; // la main appuie surtout là, un peu autour
                        _r[i] += (cible - _r[i]) * k;
                    }
                }
            }
            Redessiner();
        }

        private void Redessiner()
        {
            var pts = new PointCollection();
            double pas = (Bas - Haut) / (Tranches - 1);
            for (int i = 0; i < Tranches; i++) pts.Add(new Point(Axe - _r[i], Haut + i * pas));
            for (int i = Tranches - 1; i >= 0; i--) pts.Add(new Point(Axe + _r[i], Haut + i * pas));
            _vase.Points = pts;
            var sombre = Color.FromRgb((byte)(_glacure.R * 0.45), (byte)(_glacure.G * 0.45), (byte)(_glacure.B * 0.45));
            var clair = Color.FromRgb((byte)Math.Min(255, _glacure.R + 60), (byte)Math.Min(255, _glacure.G + 60), (byte)Math.Min(255, _glacure.B + 60));
            _vase.Fill = new LinearGradientBrush(new GradientStopCollection { new GradientStop(sombre, 0), new GradientStop(_glacure, 0.3), new GradientStop(clair, 0.42), new GradientStop(_glacure, 0.6), new GradientStop(sombre, 1) }, 0);
            // Le reflet qui tourne (une bande claire qui glisse) et le col sombre.
            double rx = 0.25 + 0.12 * Math.Sin(_phase);
            var rp = new PointCollection();
            for (int i = 0; i < Tranches; i++) rp.Add(new Point(Axe - _r[i] + _r[i] * 2 * rx - 8, Haut + i * pas));
            for (int i = Tranches - 1; i >= 0; i--) rp.Add(new Point(Axe - _r[i] + _r[i] * 2 * rx + 8, Haut + i * pas));
            _reflet.Points = rp;
            _col.Width = _r[0] * 2 - 6; _col.Height = Math.Max(10, _r[0] * 0.35);
            Canvas.SetLeft(_col, Axe - _r[0] + 3); Canvas.SetTop(_col, Haut - _col.Height / 2);
            // Les stries de tournage, qui défilent.
            for (int k = 0; k < _stries.Count; k++)
            {
                double y = Haut + ((k * (Bas - Haut) / _stries.Count) + _phase * 25) % (Bas - Haut);
                int i = (int)Math.Max(0, Math.Min(Tranches - 1, (y - Haut) / pas));
                var l = _stries[k];
                l.X1 = Axe - _r[i] + 6; l.X2 = Axe + _r[i] - 6; l.Y1 = l.Y2 = y;
            }
        }

        private void Cuire()
        {
            var path = Creations.Garder(_roue, W, H, "sculpture");
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder le vase."); return; }
            Speech.Say("Au four ! Quel beau vase ! Il est gardé dans ta galerie.");
            Celebrate?.Invoke(_fx);
        }

        // ------------------------------------------------------------------ la statue cachée
        private void NouveauBloc()
        {
            var f = Figures[_rng.Next(Figures.Length)];
            _figNom = f.Nom;
            _figure.Children.Clear();
            _figure.Children.Add(new Viewbox { Width = 560, Height = 560, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = Art.ForEmoji(f.Emoji, 560) });
            _eclats.Children.Clear();
            // La pierre : un gris granité, opaque partout.
            var rng = new Random(_rng.Next());
            for (int y = 0; y < SH; y++)
                for (int x = 0; x < SW; x++)
                {
                    int i = (y * SW + x) * 4;
                    byte g = (byte)(150 + rng.Next(-22, 23) + 18 * Math.Sin(x * 0.02) * Math.Cos(y * 0.015));
                    _ppx[i] = (byte)(g - 6); _ppx[i + 1] = g; _ppx[i + 2] = (byte)(g + 4); _ppx[i + 3] = 255;
                }
            _pierre.WritePixels(new Int32Rect(0, 0, SW, SH), _ppx, SW * 4, 0);
            _total = SW * SH; _restant = _total; _revele = false;
            _pierreImg.Opacity = 1;
            _progres.Text = "Qui se cache là ?";
        }

        private void TickPierre()
        {
            if (_revele || !_regardPierre.HasValue) return;
            var p = _regardPierre.Value;
            int r = 85, x0 = (int)Math.Max(0, p.X - r), x1 = (int)Math.Min(SW - 1, p.X + r), y0 = (int)Math.Max(0, p.Y - r), y1 = (int)Math.Min(SH - 1, p.Y + r);
            int enleves = 0;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    double d = Math.Sqrt((x - p.X) * (x - p.X) + (y - p.Y) * (y - p.Y)) / r;
                    if (d > 1) continue;
                    int i = (y * SW + x) * 4 + 3;
                    if (_ppx[i] == 0) continue;
                    int a = _ppx[i] - (int)(70 * (1 - d * d)) - _rng.Next(0, 20);
                    if (a <= 40) { a = 0; enleves++; }
                    _ppx[i] = (byte)a;
                }
            if (enleves == 0) return;
            _restant -= enleves;
            _pierre.WritePixels(new Int32Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), _ppx, SW * 4, x0, y0);
            Eclats(p);
            if ((DateTime.Now - _dernierCoup).TotalMilliseconds > 220) { _dernierCoup = DateTime.Now; SoundFx.Drum(); }
            int pct = (int)(100 - 100.0 * _restant / _total);
            _progres.Text = pct < 15 ? "Toc, toc, toc…" : pct < 50 ? "Ça apparaît…" : pct < 85 ? "Encore un peu !" : "Presque !";
            if (_restant < _total * 0.08) Reveler();
        }

        private void Eclats(Point p)
        {
            for (int k = 0; k < 3; k++)
            {
                var e = new Rectangle { Width = 8 + _rng.Next(12), Height = 6 + _rng.Next(10), Fill = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xA2)), RadiusX = 2, RadiusY = 2, RenderTransform = new RotateTransform(_rng.Next(360)) };
                Canvas.SetLeft(e, p.X + _rng.Next(-40, 41)); Canvas.SetTop(e, p.Y + _rng.Next(-40, 41));
                _eclats.Children.Add(e);
                var captured = e;
                var chute = new DoubleAnimation(Canvas.GetTop(e), SH + 40, TimeSpan.FromMilliseconds(600 + _rng.Next(500))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                chute.Completed += (s, ev) => _eclats.Children.Remove(captured);
                e.BeginAnimation(Canvas.TopProperty, chute);
                e.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(e), Canvas.GetLeft(e) + _rng.Next(-120, 121), TimeSpan.FromMilliseconds(900)));
            }
        }

        private void Reveler()
        {
            _revele = true;
            var fondu = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(600));
            _pierreImg.BeginAnimation(OpacityProperty, fondu);
            _progres.Text = "C'est " + _figNom + " !";
            Speech.Say("Bravo ! Tu as sculpté " + _figNom + " ! Elle est gardée dans ta galerie.");
            Celebrate?.Invoke(_fx);
            Dispatcher.BeginInvoke(new Action(() => Creations.Garder(_figure, SW, SH, "statue")), DispatcherPriority.Background);
        }
    }
}
