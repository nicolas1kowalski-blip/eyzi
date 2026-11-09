using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// ✍️ LA GRAVURE : une planche de bois (pyrogravure), une dalle de pierre (au
    /// ciseau) ou le sable de la plage. Le regard creuse un sillon là où il passe,
    /// avec une vraie ombre et une vraie lumière sur les bords. Des poinçons (étoile,
    /// cœur, spirale, soleil) se frappent d'un regard posé. Le miroir grave des deux
    /// côtés. Sur le sable, la vague efface tout.
    /// </summary>
    public sealed class GravureAtelier : AtelierBase
    {
        private const int W = 1280, H = 800;

        private readonly WriteableBitmap _bmp = new WriteableBitmap(W, H, 96, 96, PixelFormats.Pbgra32, null);
        private readonly Image _img = new Image { Width = W, Height = H, Stretch = Stretch.Fill };
        private byte[] _tex = new byte[W * H * 4];
        private readonly byte[] _px = new byte[W * H * 4];
        private float[] _prof = new float[W * H];
        private readonly List<float[]> _undo = new List<float[]>();
        private Matiere.Sorte _m = Matiere.Bois;
        private string _outil = "trait";
        private bool _miroir, _vide = true, _vague;
        private double _lx = -1, _ly = -1, _sx, _sy; private bool _lisse;
        private DateTime _dernierOutil = DateTime.MinValue, _dernierToc = DateTime.MinValue;
        private readonly Dictionary<string, Button> _btnOutils = new Dictionary<string, Button>();
        private Button _btnMiroir, _btnLisser;
        private readonly TextBlock _aide;

        public GravureAtelier()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xEC, 0xE0)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 190 };
            gauche.Children.Add(BoutonRetour("⬅  Ateliers"));
            gauche.Children.Add(new TextBlock { Text = "Sur quoi ?", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(4, 2, 0, 4) });
            foreach (var (m, nom) in new[] { (Matiere.Bois, "Le bois"), (Matiere.Pierre, "La pierre"), (Matiere.Sable, "Le sable") })
            {
                var mm = m; var nn = nom;
                var b = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = 150, Height = 64, Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Left, ToolTip = nom, Background = new ImageBrush(Matiere.Image(Matiere.Texture(m, 150, 64, 4), 150, 64)) };
                b.Click += (s, e) => { Lever(); Changer(mm); Speech.Say(nn + " !"); };
                gauche.Children.Add(b);
            }
            gauche.Children.Add(Outil("↩️", "Annuler", Annuler));
            _btnLisser = Outil("🧹", "Tout lisser", Lisser);
            gauche.Children.Add(_btnLisser);
            gauche.Children.Add(Outil("💾", "Garder dans ma galerie", Garder));
            root.Children.Add(gauche);

            var cadre = new Border { CornerRadius = new CornerRadius(14), Margin = new Thickness(2), ClipToBounds = true, BorderBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x6A, 0x4A)), BorderThickness = new Thickness(3), Child = new Viewbox { Stretch = Stretch.Uniform, Child = _img } };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);
            _aide = new TextBlock { Text = "Regarde la planche : tes yeux gravent !", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Opacity = 0.8, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 1, Opacity = 0.6 } };
            Grid.SetColumn(_aide, 1);
            root.Children.Add(_aide);

            var droite = new StackPanel { Margin = new Thickness(6, 8, 10, 8), Width = 120 };
            droite.Children.Add(new TextBlock { Text = "Outil", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(4, 6, 0, 2) });
            foreach (var (cle, emoji, nom) in new[] { ("trait", "✏️", "Le trait fin"), ("large", "🖌️", "Le trait large"), ("etoile", "⭐", "Le poinçon étoile"), ("coeur", "❤️", "Le poinçon cœur"), ("spirale", "🌀", "Le poinçon spirale"), ("soleil", "☀️", "Le poinçon soleil") })
            {
                var k = cle; var n = nom;
                var b = Outil(emoji, nom, () => { _outil = k; Lever(); Marquer(); Speech.Say(n + (Poincon ? ". Pose ton regard pour le frapper." : ".")); });
                _btnOutils[k] = b;
                droite.Children.Add(b);
            }
            _btnMiroir = Outil("🦋", "Le miroir", () => { _miroir = !_miroir; Lever(); Marquer(); Speech.Say(_miroir ? "Le miroir : des deux côtés !" : "Sans miroir."); });
            droite.Children.Add(_btnMiroir);
            Grid.SetColumn(droite, 2);
            root.Children.Add(droite);
            Grid.SetColumnSpan(Fx, 3);
            root.Children.Add(Fx);
            Content = root;

            _img.Source = _bmp;
            _img.MouseLeftButtonDown += (s, e) => { var p = e.GetPosition(_img); if (Poincon) Frapper(p); else { _lx = -1; Tracer(p, true); } };
            _img.MouseMove += (s, e) => { if (!Poincon && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) Tracer(e.GetPosition(_img), true); };
            Changer(Matiere.Bois);
            Marquer();
        }

        private bool Poincon => _outil != "trait" && _outil != "large";

        private void Marquer()
        {
            foreach (var kv in _btnOutils) kv.Value.Opacity = kv.Key == _outil ? 1 : 0.55;
            if (_btnMiroir != null) _btnMiroir.Opacity = _miroir ? 1 : 0.55;
        }

        private void Lever() { _dernierOutil = DateTime.Now; _lx = -1; _lisse = false; }

        private void Changer(Matiere.Sorte m)
        {
            _m = m;
            _tex = Matiere.Texture(m, W, H, 11 + Rng.Next(50));
            _prof = new float[W * H];
            _undo.Clear();
            Array.Copy(_tex, _px, _px.Length);
            _bmp.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
            _vide = true;
            _aide.Text = m == Matiere.Sable ? "Regarde le sable : tes yeux dessinent !" : m == Matiere.Pierre ? "Regarde la pierre : tes yeux gravent !" : "Regarde la planche : tes yeux gravent !";
            _aide.Visibility = Visibility.Visible;
            if (_btnLisser != null) _btnLisser.Content = new Viewbox { Height = 40, Child = Art.ForEmoji(m == Matiere.Sable ? "🌊" : "🧹", 40) };
        }

        // ------------------------------------------------------------------ le regard grave
        protected override void SurRegard(Point ecran, bool valide)
        {
            if (!valide || Poincon || _vague) { _lx = -1; _lisse = false; return; }
            var p = Dans(_img, ecran, W, H);
            if (!p.HasValue) { _lx = -1; _lisse = false; return; }
            if ((DateTime.Now - _dernierOutil).TotalMilliseconds < 500) return;
            var pt = p.Value;
            Dispatcher.BeginInvoke(new Action(() => Tracer(pt, false)));
        }

        private void Memoriser() { _undo.Add((float[])_prof.Clone()); if (_undo.Count > 8) _undo.RemoveAt(0); }

        private void Tracer(Point p, bool souris)
        {
            if (_vague) return;
            if (!souris)
            {
                if (!_lisse || Math.Abs(p.X - _sx) > 260 || Math.Abs(p.Y - _sy) > 260) { _sx = p.X; _sy = p.Y; _lisse = true; _lx = -1; }
                else { _sx += (p.X - _sx) * 0.30; _sy += (p.Y - _sy) * 0.30; }
                p = new Point(_sx, _sy);
            }
            if (_lx >= 0 && Math.Abs(p.X - _lx) < 1.5 && Math.Abs(p.Y - _ly) < 1.5) return;
            if (_lx < 0) Memoriser();
            double r = _outil == "large" ? 24 : 12;
            int x0 = W, y0 = H, x1 = 0, y1 = 0;
            void Un(double x, double y) { Creuser(x, y, r, ref x0, ref y0, ref x1, ref y1); if (_miroir) Creuser(W - x, y, r, ref x0, ref y0, ref x1, ref y1); }
            if (_lx < 0) Un(p.X, p.Y);
            else
            {
                double d = Math.Sqrt((p.X - _lx) * (p.X - _lx) + (p.Y - _ly) * (p.Y - _ly));
                int n = Math.Max(1, (int)(d / (r * 0.4)));
                for (int i = 1; i <= n; i++) Un(_lx + (p.X - _lx) * i / n, _ly + (p.Y - _ly) * i / n);
            }
            _lx = p.X; _ly = p.Y;
            Rendre(x0, y0, x1, y1);
            if (_vide) { _vide = false; _aide.Visibility = Visibility.Collapsed; }
            if ((DateTime.Now - _dernierToc).TotalMilliseconds > 380) { _dernierToc = DateTime.Now; Matiere.Toc(_m); }
        }

        private void Creuser(double x, double y, double r, ref int x0, ref int y0, ref int x1, ref int y1)
        {
            int ax = (int)Math.Max(0, x - r), bx = (int)Math.Min(W - 1, x + r), ay = (int)Math.Max(0, y - r), by = (int)Math.Min(H - 1, y + r);
            if (bx < ax || by < ay) return;
            for (int yy = ay; yy <= by; yy++)
                for (int xx = ax; xx <= bx; xx++)
                {
                    double d = Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)) / r;
                    if (d > 1) continue;
                    float v = (float)(1 - d * d);
                    int p = yy * W + xx;
                    if (v > _prof[p]) _prof[p] = v;
                }
            if (ax < x0) x0 = ax; if (ay < y0) y0 = ay; if (bx > x1) x1 = bx; if (by > y1) y1 = by;
        }

        /// <summary>Redessine un rectangle de la planche : la matière assombrie au fond du sillon, l'ombre et la lumière sur ses bords.</summary>
        private void Rendre(int x0, int y0, int x1, int y1)
        {
            x0 = Math.Max(0, x0 - 3); y0 = Math.Max(0, y0 - 3); x1 = Math.Min(W - 1, x1 + 3); y1 = Math.Min(H - 1, y1 + 3);
            if (x1 < x0 || y1 < y0) return;
            double k = _m == Matiere.Bois ? 0.72 : _m == Matiere.Sable ? 0.22 : 0.38;
            double g = _m == Matiere.Bois ? 35 : _m == Matiere.Sable ? 110 : 120;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int p = y * W + x, i = p * 4;
                    double d = _prof[p];
                    int xa = Math.Max(0, x - 2), ya = Math.Max(0, y - 2), xb = Math.Min(W - 1, x + 2), yb = Math.Min(H - 1, y + 2);
                    double e = _prof[ya * W + xa] - _prof[yb * W + xb];
                    double f = 1 - k * d, l = g * e;
                    _px[i] = Oct(_tex[i] * f + l); _px[i + 1] = Oct(_tex[i + 1] * f + l); _px[i + 2] = Oct(_tex[i + 2] * f + l); _px[i + 3] = 255;
                }
            _bmp.WritePixels(new Int32Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), _px, W * 4, x0, y0);
        }

        private static byte Oct(double v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

        // ------------------------------------------------------------------ les poinçons
        private void Frapper(Point c)
        {
            if (_vague) return;
            Memoriser();
            const double R = 115;
            int x0 = W, y0 = H, x1 = 0, y1 = 0;
            void Un(double cx, double cy)
            {
                int ax = (int)Math.Max(0, cx - R - 4), bx = (int)Math.Min(W - 1, cx + R + 4), ay = (int)Math.Max(0, cy - R - 4), by = (int)Math.Min(H - 1, cy + R + 4);
                if (bx < ax || by < ay) return;
                for (int y = ay; y <= by; y++)
                    for (int x = ax; x <= bx; x++)
                    {
                        float v = (float)Forme(x - cx, y - cy, R);
                        if (v <= 0) continue;
                        int p = y * W + x;
                        if (v > _prof[p]) _prof[p] = v;
                    }
                if (ax < x0) x0 = ax; if (ay < y0) y0 = ay; if (bx > x1) x1 = bx; if (by > y1) y1 = by;
            }
            Un(c.X, c.Y);
            if (_miroir) Un(W - c.X, c.Y);
            Rendre(x0, y0, x1, y1);
            if (_vide) { _vide = false; _aide.Visibility = Visibility.Collapsed; }
            Matiere.Toc(_m);
            SoundFx.PopSound();
        }

        /// <summary>La profondeur (0..1) du poinçon choisi au point (dx, dy) de son centre.</summary>
        private double Forme(double dx, double dy, double R)
        {
            double rho = Math.Sqrt(dx * dx + dy * dy), th = Math.Atan2(dy, dx);
            if (rho > R + 4) return 0;
            switch (_outil)
            {
                case "etoile":
                {
                    double bord = R * (0.62 + 0.38 * Math.Cos(5 * (th + Math.PI / 2)));
                    return Math.Max(0, 1 - Math.Abs(rho - bord) / 9);
                }
                case "coeur":
                {
                    double u = dx / (R * 0.78), v = -dy / (R * 0.78) + 0.15;
                    double q = u * u + v * v - 1;
                    double F = q * q * q - u * u * v * v * v;
                    return Math.Max(0, 1 - Math.Abs(F) / 0.14);
                }
                case "spirale":
                {
                    if (rho > R) return 0;
                    double s = rho / R * 3 - th / (2 * Math.PI);
                    double q = Math.Abs(s - Math.Round(s));
                    return Math.Max(0, 1 - q / 0.16);
                }
                default: // soleil : un rond et huit rayons
                {
                    double rond = Math.Max(0, 1 - Math.Abs(rho - R * 0.42) / 9);
                    if (rho < R * 0.58 || rho > R) return rond;
                    double pas = Math.PI / 4;
                    double a = Math.Abs(th - Math.Round(th / pas) * pas) * rho;
                    return Math.Max(rond, Math.Max(0, 1 - a / 8));
                }
            }
        }

        // ------------------------------------------------------------------ annuler, lisser, garder
        private void Annuler()
        {
            Lever();
            if (_undo.Count == 0) { Speech.Say("Il n'y a rien à annuler."); return; }
            _prof = _undo[_undo.Count - 1]; _undo.RemoveAt(_undo.Count - 1);
            Rendre(0, 0, W - 1, H - 1);
            Speech.Say("Le dernier trait s'efface.");
        }

        private void Lisser()
        {
            Lever();
            if (_vide || _vague) return;
            Memoriser();
            if (_m != Matiere.Sable)
            {
                _prof = new float[W * H];
                Rendre(0, 0, W - 1, H - 1);
                _vide = true;
                Speech.Say(_m == Matiere.Bois ? "Une planche toute neuve !" : "Une pierre toute lisse !");
                return;
            }
            // Sur le sable : la vague passe de gauche à droite et efface tout.
            _vague = true;
            Speech.Say("La vague arrive… et efface tout !");
            int x = 0;
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            t.Tick += (s, e) =>
            {
                int fin = Math.Min(W - 1, x + 59);
                for (int y = 0; y < H; y++) for (int xx = x; xx <= fin; xx++) _prof[y * W + xx] = 0;
                Rendre(x, 0, fin, H - 1);
                x = fin + 1;
                if (x >= W) { t.Stop(); _vague = false; _vide = true; }
            };
            t.Start();
        }

        private void Garder()
        {
            Lever();
            if (_vide) { Speech.Say("Grave d'abord quelque chose !"); return; }
            var path = Creations.Garder(_bmp, "gravure");
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la gravure."); return; }
            Speech.Say("Ta gravure est gardée dans ta galerie !");
            Fete();
        }
    }
}
