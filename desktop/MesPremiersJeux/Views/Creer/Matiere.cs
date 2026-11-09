using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// Les MATIÈRES de l'atelier de sculpture : pierre, marbre, bois, glace, or,
    /// sable. Chacune a sa texture (calculée, pas d'image à livrer), ses deux tons
    /// (ombre et lumière) pour rendre une figure « en relief » dans la matière, et
    /// son bruit d'outil.
    /// </summary>
    public static class Matiere
    {
        public sealed class Sorte { public string Cle, Nom, Emoji; public Color Sombre, Clair, Eclat; }

        private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
        public static readonly Sorte Pierre = new Sorte { Cle = "pierre", Nom = "la pierre", Emoji = "🪨", Sombre = C(0x4A, 0x4C, 0x55), Clair = C(0xD8, 0xD9, 0xDE), Eclat = C(0x9A, 0x9A, 0xA2) };
        public static readonly Sorte Marbre = new Sorte { Cle = "marbre", Nom = "le marbre", Emoji = "🏛️", Sombre = C(0x8A, 0x8E, 0x9C), Clair = C(0xFF, 0xFF, 0xFF), Eclat = C(0xEE, 0xEE, 0xF2) };
        public static readonly Sorte Bois = new Sorte { Cle = "bois", Nom = "le bois", Emoji = "🪵", Sombre = C(0x4E, 0x2E, 0x14), Clair = C(0xE2, 0xB0, 0x74), Eclat = C(0xC8, 0x96, 0x5A) };
        public static readonly Sorte Glace = new Sorte { Cle = "glace", Nom = "la glace", Emoji = "🧊", Sombre = C(0x4A, 0x8E, 0xC2), Clair = C(0xF2, 0xFB, 0xFF), Eclat = C(0xD6, 0xF0, 0xFF) };
        public static readonly Sorte Or = new Sorte { Cle = "or", Nom = "l'or", Emoji = "🏆", Sombre = C(0x8A, 0x5A, 0x00), Clair = C(0xFF, 0xF0, 0x9A), Eclat = C(0xFF, 0xD5, 0x4A) };
        public static readonly Sorte Sable = new Sorte { Cle = "sable", Nom = "le sable", Emoji = "🏖️", Sombre = C(0xB8, 0x92, 0x5A), Clair = C(0xF6, 0xE2, 0xB0), Eclat = C(0xE8, 0xCF, 0x98) };

        /// <summary>Le bruit de l'outil sur la matière.</summary>
        public static void Toc(Sorte m)
        {
            try
            {
                if (m == Bois) SoundFx.DrumKit(2);
                else if (m == Glace) SoundFx.PlayNote(11, "xylophone", 0.2);
                else if (m == Sable) { }
                else SoundFx.Drum();
            }
            catch { }
        }

        // ------------------------------------------------------------------ bruit lissé
        private static double Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535.0;
            }
        }

        private static double Bruit(double x, double y, int s)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            double a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        /// <summary>La texture d'une matière : w × h pixels BGRA opaques.</summary>
        public static byte[] Texture(Sorte m, int w, int h, int graine)
        {
            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double t;
                    if (m == Marbre)
                    {
                        double v = Math.Pow(1 - Math.Abs(Math.Sin(x * 0.012 + y * 0.006 + 4 * Bruit(x / 90.0, y / 90.0, graine))), 6);
                        t = 0.95 - 0.55 * v + 0.04 * (Hash(x, y, graine) - 0.5);
                    }
                    else if (m == Bois)
                    {
                        double g = Math.Sin(x * 0.11 + 3.5 * Bruit(x / 160.0, y / 40.0, graine) + 6 * Bruit(x / 400.0, y / 400.0, graine + 1));
                        t = 0.5 + 0.22 * g + 0.10 * (Bruit(x / 3.0, y / 40.0, graine + 2) - 0.5);
                    }
                    else if (m == Glace)
                    {
                        t = 0.68 + 0.2 * Bruit(x / 120.0, y / 120.0, graine) + 0.4 * Math.Pow(Bruit(x / 8.0, y / 200.0, graine + 1), 8);
                    }
                    else if (m == Or)
                    {
                        t = 0.5 + 0.35 * Math.Sin((x + y) * 0.02 + 3 * Bruit(x / 100.0, y / 100.0, graine)) + 0.05 * (Hash(x, y, graine) - 0.5);
                    }
                    else if (m == Sable)
                    {
                        t = 0.6 + 0.25 * (Bruit(x / 2.0, y / 2.0, graine) - 0.5) + 0.15 * (Bruit(x / 80.0, y / 30.0, graine + 1) - 0.5);
                    }
                    else // pierre
                    {
                        t = 0.45 + 0.25 * (Bruit(x / 60.0, y / 60.0, graine) - 0.5) + 0.25 * (Bruit(x / 6.0, y / 6.0, graine + 1) - 0.5) + 0.10 * (Hash(x, y, graine) - 0.5);
                    }
                    t = Math.Max(0, Math.Min(1, t));
                    int i = (y * w + x) * 4;
                    px[i] = (byte)(m.Sombre.B + (m.Clair.B - m.Sombre.B) * t);
                    px[i + 1] = (byte)(m.Sombre.G + (m.Clair.G - m.Sombre.G) * t);
                    px[i + 2] = (byte)(m.Sombre.R + (m.Clair.R - m.Sombre.R) * t);
                    px[i + 3] = 255;
                }
            return px;
        }

        public static BitmapSource Image(byte[] px, int w, int h)
        {
            var b = BitmapSource.Create(w, h, 96, 96, PixelFormats.Pbgra32, null, px, w * 4);
            b.Freeze();
            return b;
        }

        /// <summary>Rend un élément (un dessin, un emoji) dans un carré de « taille » pixels, hors écran.</summary>
        public static RenderTargetBitmap Rendre(FrameworkElement el, int taille)
        {
            var host = new Grid { Width = taille, Height = taille };
            host.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = el, Margin = new Thickness(taille * 0.04) });
            host.Measure(new Size(taille, taille));
            host.Arrange(new Rect(0, 0, taille, taille));
            host.UpdateLayout();
            var rtb = new RenderTargetBitmap(taille, taille, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(host);
            return rtb;
        }

        /// <summary>
        /// La figure SCULPTÉE dans la matière : ses couleurs deviennent les tons de la matière, avec une
        /// lumière qui vient d'en haut à gauche (les bords ressortent). Null si le dessin n'a rien rendu.
        /// </summary>
        public static BitmapSource Relief(FrameworkElement visuel, int taille, Sorte m)
        {
            try
            {
                var rtb = Rendre(visuel, taille);
                var src = new byte[taille * taille * 4];
                rtb.CopyPixels(src, taille * 4, 0);
                var haut = new double[taille * taille];
                int pleins = 0;
                for (int i = 0, p = 0; p < haut.Length; p++, i += 4)
                {
                    int a = src[i + 3];
                    if (a == 0) continue;
                    if (a > 40) pleins++;
                    double lum = (0.114 * src[i] + 0.587 * src[i + 1] + 0.299 * src[i + 2]) / a; // dé-prémultiplié, 0..1
                    haut[p] = a / 255.0 * (0.55 + 0.45 * Math.Min(1, lum));
                }
                if (pleins < taille * taille / 100) return null; // rien de visible : l'appelant montrera le dessin en couleurs
                var res = new byte[src.Length];
                for (int y = 0; y < taille; y++)
                    for (int x = 0; x < taille; x++)
                    {
                        int p = y * taille + x, i = p * 4;
                        int a = src[i + 3];
                        if (a == 0) continue;
                        int x0 = Math.Max(0, x - 2), y0 = Math.Max(0, y - 2), x1 = Math.Min(taille - 1, x + 2), y1 = Math.Min(taille - 1, y + 2);
                        double ombre = haut[y0 * taille + x0] - haut[y1 * taille + x1];
                        double lum = (0.114 * src[i] + 0.587 * src[i + 1] + 0.299 * src[i + 2]) / a;
                        double grain = m == Bois ? 0.06 * Math.Sin(x * 0.11 + 3 * Bruit(x / 160.0, y / 40.0, 5)) : 0.04 * (Bruit(x / 5.0, y / 5.0, 5) - 0.5);
                        double t = Math.Max(0, Math.Min(1, 0.32 + 0.42 * Math.Min(1, lum) + ombre * 1.7 + grain));
                        double k = a / 255.0;
                        res[i] = (byte)((m.Sombre.B + (m.Clair.B - m.Sombre.B) * t) * k);
                        res[i + 1] = (byte)((m.Sombre.G + (m.Clair.G - m.Sombre.G) * t) * k);
                        res[i + 2] = (byte)((m.Sombre.R + (m.Clair.R - m.Sombre.R) * t) * k);
                        res[i + 3] = (byte)a;
                    }
                return Image(res, taille, taille);
            }
            catch (Exception ex) { Log.Write("creations", "Relief : " + ex.Message); return null; }
        }
    }

    /// <summary>
    /// Un BLOC de matière à tailler : une image dont le regard enlève des morceaux
    /// (la transparence grandit là où l'on regarde), pour découvrir ce qu'il y a dessous.
    /// </summary>
    public sealed class Bloc
    {
        public readonly int W, H;
        public readonly WriteableBitmap Image;
        private readonly byte[] _tex, _px;
        private int _reste, _total;

        public Bloc(int w, int h)
        {
            W = Math.Max(1, w); H = Math.Max(1, h);
            Image = new WriteableBitmap(W, H, 96, 96, PixelFormats.Pbgra32, null);
            _tex = new byte[W * H * 4]; _px = new byte[W * H * 4];
        }

        /// <summary>Un bloc tout neuf dans cette texture ; « arrondi » adoucit les coins (pixels).</summary>
        public void Remplir(byte[] texture, int arrondi = 0)
        {
            Array.Copy(texture, _tex, Math.Min(texture.Length, _tex.Length));
            _total = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = (y * W + x) * 4;
                    bool dedans = true;
                    if (arrondi > 0)
                    {
                        int dx = x < arrondi ? arrondi - x : x >= W - arrondi ? x - (W - arrondi - 1) : 0;
                        int dy = y < arrondi ? arrondi - y : y >= H - arrondi ? y - (H - arrondi - 1) : 0;
                        dedans = dx * dx + dy * dy <= arrondi * arrondi;
                    }
                    if (dedans) { _px[i] = _tex[i]; _px[i + 1] = _tex[i + 1]; _px[i + 2] = _tex[i + 2]; _px[i + 3] = 255; _total++; }
                    else { _px[i] = _px[i + 1] = _px[i + 2] = _px[i + 3] = 0; }
                }
            _reste = _total;
            Image.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
        }

        /// <summary>La part du bloc qui reste (1 = entier, 0 = tout enlevé).</summary>
        public double Reste => _total == 0 ? 0 : (double)_reste / _total;

        /// <summary>Un coup d'outil au point (x, y) du bloc. Vrai si de la matière est partie.</summary>
        public bool Tailler(double x, double y, int r, Random rng)
        {
            int x0 = (int)Math.Max(0, x - r), x1 = (int)Math.Min(W - 1, x + r), y0 = (int)Math.Max(0, y - r), y1 = (int)Math.Min(H - 1, y + r);
            if (x1 < x0 || y1 < y0) return false;
            int enleves = 0; bool touche = false;
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                {
                    double d = Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)) / r;
                    if (d > 1) continue;
                    int i = (yy * W + xx) * 4;
                    int a = _px[i + 3];
                    if (a == 0) continue;
                    int na = a - (int)(70 * (1 - d * d)) - rng.Next(0, 20);
                    if (na <= 40) { na = 0; enleves++; }
                    _px[i] = (byte)(_tex[i] * na / 255); _px[i + 1] = (byte)(_tex[i + 1] * na / 255); _px[i + 2] = (byte)(_tex[i + 2] * na / 255); _px[i + 3] = (byte)na;
                    touche = true;
                }
            if (!touche) return false;
            _reste -= enleves;
            Image.WritePixels(new Int32Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), _px, W * 4, x0, y0);
            return true;
        }

        public void Vider()
        {
            Array.Clear(_px, 0, _px.Length);
            _reste = 0;
            Image.WritePixels(new Int32Rect(0, 0, W, H), _px, W * 4, 0);
        }
    }

    /// <summary>Ce que les ateliers de sculpture ont en commun : le retour, la fête, les boutons d'outil, le regard.</summary>
    public abstract class AtelierBase : UserControl
    {
        protected static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        public event Action BackRequested;
        public event Action<Canvas> Celebrate;
        protected readonly Canvas Fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        protected readonly Random Rng = new Random();

        protected AtelierBase()
        {
            Loaded += (s, e) => { GazeFeed.Sample += Regard; Entrer(); };
            Unloaded += (s, e) => { GazeFeed.Sample -= Regard; Sortir(); };
        }

        private void Regard(Point ecran, bool valide) { if (IsVisible) { try { SurRegard(ecran, valide); } catch { } } }
        protected abstract void SurRegard(Point ecran, bool valide);
        protected virtual void Entrer() { }
        protected virtual void Sortir() { }
        protected void Retour() => BackRequested?.Invoke();
        protected void Fete() => Celebrate?.Invoke(Fx);

        protected Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip, HorizontalAlignment = HorizontalAlignment.Left };
            b.Click += (s, e) => action();
            return b;
        }

        protected Button BoutonRetour(string texte)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = texte, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            b.Click += (s, e) => Retour();
            return b;
        }

        /// <summary>Le point du regard dans les coordonnées d'un élément, ou null s'il est dehors.</summary>
        protected static Point? Dans(FrameworkElement el, Point ecran, double w, double h)
        {
            try { var p = el.PointFromScreen(ecran); return p.X >= 0 && p.Y >= 0 && p.X < w && p.Y < h ? p : (Point?)null; }
            catch { return null; }
        }

        /// <summary>Des éclats de matière qui tombent du point taillé.</summary>
        protected void Eclats(Canvas couche, Point p, Color couleur, double sol, int n = 3)
        {
            for (int k = 0; k < n; k++)
            {
                var e = new System.Windows.Shapes.Rectangle { Width = 8 + Rng.Next(12), Height = 6 + Rng.Next(10), Fill = new SolidColorBrush(couleur), RadiusX = 2, RadiusY = 2, RenderTransform = new RotateTransform(Rng.Next(360)) };
                double x = p.X + Rng.Next(-40, 41), y = p.Y + Rng.Next(-40, 41);
                Canvas.SetLeft(e, x); Canvas.SetTop(e, y);
                couche.Children.Add(e);
                var captured = e;
                var chute = new System.Windows.Media.Animation.DoubleAnimation(y, sol + 40, TimeSpan.FromMilliseconds(600 + Rng.Next(500))) { EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn } };
                chute.Completed += (s, ev) => couche.Children.Remove(captured);
                e.BeginAnimation(Canvas.TopProperty, chute);
                e.BeginAnimation(Canvas.LeftProperty, new System.Windows.Media.Animation.DoubleAnimation(x, x + Rng.Next(-120, 121), TimeSpan.FromMilliseconds(900)));
            }
        }
    }
}
