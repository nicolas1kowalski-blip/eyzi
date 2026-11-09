using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MesPremiersJeux.Data;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🖌️ PEINDRE AU REGARD : la couleur suit les yeux. Pas de précision demandée :
    /// gros pinceau doux, traits lissés, symétrie (miroir, quatre, rosace) qui fait
    /// que « c'est toujours joli ». Des pinceaux magiques (arc-en-ciel, paillettes,
    /// bulles). On peint quand le regard est sur la toile ; dès qu'il va vers les
    /// outils, le pinceau se lève. 💾 garde la peinture dans la galerie.
    /// </summary>
    public sealed class PeintureView : UserControl
    {
        private const int W = 1280, H = 800;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private readonly WriteableBitmap _bmp = new WriteableBitmap(W, H, 96, 96, PixelFormats.Pbgra32, null);
        private readonly byte[] _px = new byte[W * H * 4];
        private readonly Image _img = new Image { Width = W, Height = H, Stretch = Stretch.Fill };
        private readonly UniformGrid _palette = new UniformGrid { Columns = 2 };
        private readonly StackPanel _outils = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(6, 8, 10, 8) };
        private readonly TextBlock _aide;
        private readonly Canvas _fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private Color _couleur = Color.FromRgb(0xE5, 0x39, 0x35);
        private string _pinceau = "doux";   // doux | arcenciel | paillettes | bulles | gros
        private int _symetrie = 2;           // 1 | 2 | 4 | 6
        private double _lx = -1, _ly = -1;   // dernier point peint (toile)
        private double _sx, _sy; bool _lisse;  // point lissé
        private DateTime _dernierOutil = DateTime.MinValue, _dernierPoint = DateTime.MinValue;
        private double _teinte;
        private readonly Random _rng = new Random();
        private bool _vide = true, _actif;
        private Border _apercu;
        private readonly List<byte[]> _undo = new List<byte[]>();

        public PeintureView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xE6)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Gauche : retour + palette
            var gauche = new DockPanel { Margin = new Thickness(10, 8, 10, 8) };
            var back = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            back.Click += (s, e) => BackRequested?.Invoke();
            DockPanel.SetDock(back, Dock.Top);
            gauche.Children.Add(back);
            _apercu = new Border { Height = 56, CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(_couleur), BorderBrush = Dark, BorderThickness = new Thickness(2), Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(_apercu, Dock.Top);
            gauche.Children.Add(_apercu);
            _palette.Width = 300;
            gauche.Children.Add(_palette);
            root.Children.Add(gauche);

            // Centre : la toile
            var cadre = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.FromRgb(0xE4, 0xCC, 0xF2)), BorderThickness = new Thickness(2), Margin = new Thickness(2) };
            var pile = new Grid();
            pile.Children.Add(_img);
            _fx.Width = W; _fx.Height = H;
            pile.Children.Add(_fx);
            cadre.Child = new Viewbox { Stretch = Stretch.Uniform, Child = pile };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);
            _aide = new TextBlock { Text = "Regarde la toile : la couleur suit tes yeux !", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.55, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
            Grid.SetColumn(_aide, 1);
            root.Children.Add(_aide);

            // Droite : pinceaux, symétrie, annuler, effacer, garder
            Grid.SetColumn(_outils, 2);
            root.Children.Add(_outils);
            Content = root;

            Effacer(false);
            _img.Source = _bmp;
            BuildPalette();
            BuildOutils();

            Loaded += (s, e) => { GazeFeed.Sample += SurRegard; _actif = true; };
            Unloaded += (s, e) => { GazeFeed.Sample -= SurRegard; _actif = false; };
            _img.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) Peindre(e.GetPosition(_img), true); };
            _img.MouseLeftButtonDown += (s, e) => { _lx = -1; Peindre(e.GetPosition(_img), true); };
        }

        // ------------------------------------------------------------------ outils
        private void BuildPalette()
        {
            _palette.Children.Clear();
            foreach (var sw in Palette.All.Where(x => x.Kind == SwatchKind.Solid).Take(16))
            {
                var c = (Color)ColorConverter.ConvertFromString(sw.Color);
                var b = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = double.NaN, Height = double.NaN, MinHeight = 62, Margin = new Thickness(5), Background = new SolidColorBrush(c), ToolTip = sw.Name, BorderBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xB0, 0xC8)), BorderThickness = new Thickness(1.5) };
                var nom = sw.Name;
                b.Click += (s, e) => { _couleur = c; _apercu.Background = new SolidColorBrush(c); if (_pinceau == "arcenciel") _pinceau = "doux"; MarquerOutil(); Speech.Say(nom); MarquerPinceau(); };
                _palette.Children.Add(b);
            }
            // Blanc : pour « gommer » en douceur.
            var blanc = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = double.NaN, Height = double.NaN, MinHeight = 62, Margin = new Thickness(5), Background = Brushes.White, ToolTip = "blanc", BorderBrush = Dark, BorderThickness = new Thickness(1.5) };
            blanc.Click += (s, e) => { _couleur = Colors.White; _apercu.Background = Brushes.White; MarquerOutil(); Speech.Say("blanc"); };
            _palette.Children.Add(blanc);
            var noir = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = double.NaN, Height = double.NaN, MinHeight = 62, Margin = new Thickness(5), Background = Brushes.Black, ToolTip = "noir" };
            noir.Click += (s, e) => { _couleur = Colors.Black; _apercu.Background = Brushes.Black; MarquerOutil(); Speech.Say("noir"); };
            _palette.Children.Add(noir);
        }

        private readonly Dictionary<string, Button> _btnPinceaux = new Dictionary<string, Button>();
        private readonly Dictionary<int, Button> _btnSym = new Dictionary<int, Button>();

        private void BuildOutils()
        {
            _outils.Children.Clear();
            _btnPinceaux.Clear(); _btnSym.Clear();
            _outils.Children.Add(Titre("Pinceau"));
            foreach (var (cle, emoji, nom) in new[] { ("doux", "🖌️", "Pinceau doux"), ("gros", "🧽", "Gros pinceau"), ("arcenciel", "🌈", "Arc-en-ciel"), ("paillettes", "✨", "Paillettes"), ("bulles", "🫧", "Bulles") })
            {
                var k = cle; var n = nom;
                var b = Outil(emoji, nom, () => { _pinceau = k; MarquerOutil(); MarquerPinceau(); Speech.Say(n); });
                _btnPinceaux[k] = b;
                _outils.Children.Add(b);
            }
            _outils.Children.Add(Titre("Miroir"));
            var rang = new UniformGrid { Columns = 4 };
            foreach (var (n, emoji, nom) in new[] { (1, "1️⃣", "Un seul"), (2, "🦋", "Miroir"), (4, "➕", "Quatre"), (6, "❄️", "Rosace") })
            {
                var k = n; var nm = nom;
                var b = Outil(emoji, nom, () => { _symetrie = k; MarquerOutil(); MarquerPinceau(); Speech.Say(nm); });
                b.MinWidth = 60; b.Margin = new Thickness(2);
                _btnSym[k] = b;
                rang.Children.Add(b);
            }
            _outils.Children.Add(rang);
            _outils.Children.Add(new Border { Height = 14 });
            _outils.Children.Add(Outil("↩️", "Annuler le dernier trait", Annuler));
            _outils.Children.Add(Outil("🧹", "Tout effacer", () => { MarquerOutil(); Effacer(true); Speech.Say("Une toile toute neuve !"); }));
            _outils.Children.Add(Outil("💾", "Garder dans ma galerie", Garder));
            MarquerPinceau();
        }

        private static TextBlock Titre(string t) => new TextBlock { Text = t, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(4, 6, 0, 2) };

        private Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip };
            b.Click += (s, e) => action();
            return b;
        }

        private void MarquerPinceau()
        {
            foreach (var kv in _btnPinceaux) kv.Value.Opacity = kv.Key == _pinceau ? 1 : 0.55;
            foreach (var kv in _btnSym) kv.Value.Opacity = kv.Key == _symetrie ? 1 : 0.55;
        }

        /// <summary>On vient de toucher un outil : le pinceau se lève une demi-seconde (le regard revient sur la toile).</summary>
        private void MarquerOutil() { _dernierOutil = DateTime.Now; _lx = -1; _lisse = false; }

        // ------------------------------------------------------------------ le regard peint
        private void SurRegard(Point ecran, bool valide)
        {
            if (!_actif || !IsVisible) return;
            if (!valide) { _lx = -1; _lisse = false; return; }
            Point p;
            try { p = _img.PointFromScreen(ecran); } catch { return; }
            if (p.X < 0 || p.Y < 0 || p.X >= W || p.Y >= H) { _lx = -1; _lisse = false; return; }
            if ((DateTime.Now - _dernierOutil).TotalMilliseconds < 500) return;
            Dispatcher.BeginInvoke(new Action(() => Peindre(p, false)));
        }

        private void Peindre(Point p, bool souris)
        {
            if (!souris)
            {
                // Lissage : le regard tremble, le pinceau non. Un grand saut (saccade) commence un nouveau trait.
                if (!_lisse || Math.Abs(p.X - _sx) > 260 || Math.Abs(p.Y - _sy) > 260) { _sx = p.X; _sy = p.Y; _lisse = true; _lx = -1; }
                else { _sx += (p.X - _sx) * 0.30; _sy += (p.Y - _sy) * 0.30; }
                p = new Point(_sx, _sy);
            }
            if (_lx >= 0 && Math.Abs(p.X - _lx) < 1.5 && Math.Abs(p.Y - _ly) < 1.5) return;
            if (_lx < 0) { _undo.Add((byte[])_px.Clone()); if (_undo.Count > 12) _undo.RemoveAt(0); }
            double rayon = _pinceau == "gros" ? 46 : _pinceau == "bulles" ? 30 : 22;
            if (_lx < 0) { Tampon(p.X, p.Y, rayon); }
            else
            {
                double d = Math.Sqrt((p.X - _lx) * (p.X - _lx) + (p.Y - _ly) * (p.Y - _ly));
                int n = Math.Max(1, (int)(d / (rayon * 0.35)));
                for (int i = 1; i <= n; i++) Tampon(_lx + (p.X - _lx) * i / n, _ly + (p.Y - _ly) * i / n, rayon);
            }
            _lx = p.X; _ly = p.Y;
            _bmp.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
            if (_vide) { _vide = false; _aide.Visibility = Visibility.Collapsed; }
        }

        /// <summary>Un coup de pinceau, répété selon la symétrie choisie.</summary>
        private void Tampon(double x, double y, double rayon)
        {
            Color c = _couleur;
            if (_pinceau == "arcenciel") { _teinte = (_teinte + 2.2) % 360; c = Hsv(_teinte, 0.85, 1); }
            double cx = W / 2.0, cy = H / 2.0;
            var pts = new List<(double, double)> { (x, y) };
            if (_symetrie == 2) pts.Add((W - x, y));
            else if (_symetrie == 4) { pts.Add((W - x, y)); pts.Add((x, H - y)); pts.Add((W - x, H - y)); }
            else if (_symetrie == 6)
            {
                double dx = x - cx, dy = y - cy;
                for (int k = 1; k < 6; k++)
                {
                    double a = k * Math.PI / 3;
                    pts.Add((cx + dx * Math.Cos(a) - dy * Math.Sin(a), cy + dx * Math.Sin(a) + dy * Math.Cos(a)));
                }
            }
            foreach (var (px, py) in pts)
            {
                if (_pinceau == "paillettes") Paillettes(px, py, rayon, c);
                else if (_pinceau == "bulles") Bulle(px, py, rayon, c);
                else Disque(px, py, rayon, c, _pinceau == "gros" ? 0.35 : 0.6);
            }
        }

        private void Disque(double x, double y, double r, Color c, double alpha)
        {
            int x0 = (int)Math.Max(0, x - r), x1 = (int)Math.Min(W - 1, x + r), y0 = (int)Math.Max(0, y - r), y1 = (int)Math.Min(H - 1, y + r);
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                {
                    double d = Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)) / r;
                    if (d > 1) continue;
                    double a = alpha * (1 - d * d); // bord fondu
                    Melange(xx, yy, c, a);
                }
        }

        private void Paillettes(double x, double y, double r, Color c)
        {
            Disque(x, y, r * 0.8, c, 0.25);
            for (int i = 0; i < 6; i++)
            {
                double a = _rng.NextDouble() * Math.PI * 2, d = _rng.NextDouble() * r;
                var etoile = i % 3 == 0 ? Colors.White : Hsv((_teinte + _rng.Next(60)) % 360, 0.5, 1);
                Disque(x + Math.Cos(a) * d, y + Math.Sin(a) * d, 2.5 + _rng.NextDouble() * 3, etoile, 0.95);
            }
            _teinte = (_teinte + 7) % 360;
        }

        private void Bulle(double x, double y, double r, Color c)
        {
            if (_rng.NextDouble() > 0.35) return; // pas une bulle à chaque pas : des bulles espacées
            double rr = r * (0.5 + _rng.NextDouble());
            int x0 = (int)Math.Max(0, x - rr), x1 = (int)Math.Min(W - 1, x + rr), y0 = (int)Math.Max(0, y - rr), y1 = (int)Math.Min(H - 1, y + rr);
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                {
                    double d = Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)) / rr;
                    if (d > 1) continue;
                    double a = d > 0.82 ? 0.75 : 0.10 + 0.15 * (1 - d); // un anneau net, un cœur presque transparent
                    Melange(xx, yy, c, a);
                }
            Disque(x - rr * 0.35, y - rr * 0.35, rr * 0.18, Colors.White, 0.9); // le reflet
        }

        private void Melange(int x, int y, Color c, double a)
        {
            int i = (y * W + x) * 4;
            _px[i] = (byte)(_px[i] + (c.B - _px[i]) * a);
            _px[i + 1] = (byte)(_px[i + 1] + (c.G - _px[i + 1]) * a);
            _px[i + 2] = (byte)(_px[i + 2] + (c.R - _px[i + 2]) * a);
            _px[i + 3] = 255;
        }

        private static Color Hsv(double h, double s, double v)
        {
            double c = v * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = v - c;
            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; } else if (h < 120) { r = x; g = c; b = 0; } else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; } else if (h < 300) { r = x; g = 0; b = c; } else { r = c; g = 0; b = x; }
            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        // ------------------------------------------------------------------ effacer, annuler, garder
        private void Effacer(bool memoriser)
        {
            if (memoriser && !_vide) { _undo.Add((byte[])_px.Clone()); if (_undo.Count > 12) _undo.RemoveAt(0); }
            for (int i = 0; i < _px.Length; i++) _px[i] = 255;
            _bmp.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
            _lx = -1; _lisse = false; _vide = true;
            _aide.Visibility = Visibility.Visible;
        }

        private void Annuler()
        {
            MarquerOutil();
            if (_undo.Count == 0) { Speech.Say("Il n'y a rien à annuler."); return; }
            var last = _undo[_undo.Count - 1]; _undo.RemoveAt(_undo.Count - 1);
            Array.Copy(last, _px, _px.Length);
            _bmp.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
            Speech.Say("Le dernier trait s'efface.");
        }

        private void Garder()
        {
            MarquerOutil();
            if (_vide) { Speech.Say("Peins d'abord quelque chose !"); return; }
            var path = Creations.Garder(_bmp, "peinture");
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la peinture."); return; }
            Speech.Say("C'est gardé dans ta galerie ! Quelle belle peinture !");
            Celebrate?.Invoke(_fx);
        }
    }
}
