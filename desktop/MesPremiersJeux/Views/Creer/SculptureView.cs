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
    /// 🏺 SCULPTER AU REGARD, quatre ateliers :
    ///  · Le tour de potier : une motte tourne ; là où l'enfant regarde, la terre se
    ///    creuse ou s'élargit, et le vase prend sa forme. Glaçure, cuisson, galerie.
    ///  · La statue : pierre, marbre, bois, glace ou or ; on choisit ce qu'on sculpte,
    ///    le regard fait éclater le bloc et la statue apparaît dans la matière.
    ///  · Le totem : un tronc, trois animaux taillés dans le bois, l'un sur l'autre.
    ///  · La gravure : le regard creuse un sillon dans le bois, la pierre ou le sable.
    /// Rien ne peut rater : la matière suit les yeux.
    /// </summary>
    public sealed class SculptureView : UserControl
    {
        private const int W = 900, H = 800;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private readonly Grid _menu, _potier;
        private readonly Grid _hote = new Grid { Visibility = Visibility.Collapsed };
        private StatueAtelier _statueA; private TotemAtelier _totemA; private GravureAtelier _gravureA;
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

        public SculptureView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };

            // ----- le choix de l'atelier
            _menu = new Grid();
            var choix = new UniformGrid { Columns = 2, Margin = new Thickness(40, 64, 40, 24) };
            var t1 = MenuKit.ThemeTile("🏺", "Le tour de potier", Color.FromRgb(0xFF, 0xE3, 0xC9), "👀  🫙  🔥");
            t1.Click += (s, e) => OuvrirPotier();
            var t2 = MenuKit.ThemeTile("🗿", "La statue", Color.FromRgb(0xE6, 0xE6, 0xEA), "🪨  🏛️  🧊");
            t2.Click += (s, e) => Essayer(() => OuvrirAtelier(_statueA ?? (_statueA = Brancher(new StatueAtelier())), "statue"));
            var t3 = MenuKit.ThemeTile("🪵", "Le totem en bois", Color.FromRgb(0xF2, 0xDF, 0xC4), "🦉  🐻  🦊");
            t3.Click += (s, e) => Essayer(() => OuvrirAtelier(_totemA ?? (_totemA = Brancher(new TotemAtelier())), "totem"));
            var t4 = MenuKit.ThemeTile("✍️", "La gravure", Color.FromRgb(0xF6, 0xEC, 0xD0), "🪵  🪨  🏖️");
            t4.Click += (s, e) => Essayer(() => OuvrirAtelier(_gravureA ?? (_gravureA = Brancher(new GravureAtelier())), "gravure"));
            choix.Children.Add(t1); choix.Children.Add(t2); choix.Children.Add(t3); choix.Children.Add(t4);
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

            root.Children.Add(_hote);
            root.Children.Add(_fx);
            Content = root;

            _tour.Tick += (s, e) => TickRoue();
            Loaded += (s, e) => { GazeFeed.Sample += SurRegard; if (_mode == "potier") _tour.Start(); };
            Unloaded += (s, e) => { GazeFeed.Sample -= SurRegard; _tour.Stop(); };
            _roue.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regardRoue = e.GetPosition(_roue); };
            NouvelleMotte();
        }

        private void Essayer(Action ouvrir)
        {
            try { ouvrir(); }
            catch (Exception ex) { Log.Write("creer", "Atelier de sculpture : " + ex); Menu(); Speech.Say("Oh, cet atelier ne veut pas s'ouvrir. On le dira à un grand."); }
        }

        private T Brancher<T>(T a) where T : AtelierBase
        {
            a.BackRequested += Menu;
            a.Celebrate += c => Celebrate?.Invoke(_fx);
            return a;
        }

        private void OuvrirAtelier(AtelierBase a, string nom)
        {
            _mode = nom; _tour.Stop();
            _hote.Children.Clear();
            _hote.Children.Add(a);
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Collapsed; _hote.Visibility = Visibility.Visible;
            Usage.Count("creer:" + nom + ":ouvert");
        }

        private Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip, HorizontalAlignment = HorizontalAlignment.Left };
            b.Click += (s, e) => action();
            return b;
        }

        private void Menu()
        {
            _mode = "menu"; _tour.Stop();
            _hote.Children.Clear();
            _menu.Visibility = Visibility.Visible; _potier.Visibility = Visibility.Collapsed; _hote.Visibility = Visibility.Collapsed;
        }

        private void OuvrirPotier()
        {
            _mode = "potier";
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Visible; _hote.Visibility = Visibility.Collapsed;
            _tour.Start();
            Speech.Say("Le tour de potier ! Regarde la terre : elle suit tes yeux.");
        }

        private void SurRegard(Point ecran, bool valide)
        {
            if (!IsVisible) return;
            if (!valide) { _regardRoue = null; return; }
            try
            {
                if (_mode == "potier") { var p = _roue.PointFromScreen(ecran); _regardRoue = p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H ? p : (Point?)null; }
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
    }
}
