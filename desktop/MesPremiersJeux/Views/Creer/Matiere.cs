using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views.Creer.Volume;

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
        public static readonly Sorte Bronze = new Sorte { Cle = "bronze", Nom = "le bronze", Emoji = "🥉", Sombre = C(0x6A, 0x42, 0x10), Clair = C(0xF2, 0xC8, 0x6A), Eclat = C(0xD9, 0x9A, 0x3A) };
        public static readonly Sorte Sable = new Sorte { Cle = "sable", Nom = "le sable", Emoji = "🏖️", Sombre = C(0xB8, 0x92, 0x5A), Clair = C(0xF6, 0xE2, 0xB0), Eclat = C(0xE8, 0xCF, 0x98) };

        /// <summary>La matière de l'atelier (ses éclats, son bruit, son nom) pour un genre de volume.</summary>
        public static Sorte De(Genre g) => g == Genre.Marbre ? Marbre : g == Genre.Bois ? Bois : g == Genre.Glace ? Glace : g == Genre.Bronze ? Bronze : Pierre;

        /// <summary>Une boule de matière éclairée (pour choisir sa matière en la voyant).</summary>
        public static BitmapSource Boule(Genre g, int taille = 150)
        {
            var v = new Volume3D(taille, taille, 3) { Matiere = g };
            float c = (taille - 1) / 2f, r = taille * 0.44f;
            for (int y = 0; y < taille; y++)
                for (int x = 0; x < taille; x++)
                {
                    float d2 = ((x - c) * (x - c) + (y - c) * (y - c)) / (r * r);
                    v.T[y * taille + x] = v.C[y * taille + x] = d2 >= 1 ? 0 : 0.42f * (float)Math.Sqrt(1 - d2);
                }
            v.Figer(); v.ToutOmbrer();
            return Image((byte[])v.Px.Clone(), taille, taille);
        }

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
                    else if (m == Bronze)
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

        /// <summary>Le ciseau du sculpteur : un manche de bois, une lame d'acier. Il suit le regard et recule à chaque coup.</summary>
        protected static Canvas Ciseau()
        {
            var c = new Canvas { Width = 1, Height = 1, IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            var g = new Canvas { RenderTransform = new TransformGroup { Children = { new TranslateTransform(0, 0), new RotateTransform(38) } } };
            var lame = new System.Windows.Shapes.Polygon { Points = new PointCollection { new Point(-7, 0), new Point(7, 0), new Point(9, -46), new Point(-9, -46) }, Fill = new LinearGradientBrush(Color.FromRgb(0xF2, 0xF4, 0xF8), Color.FromRgb(0x8E, 0x96, 0xA4), 0), Stroke = new SolidColorBrush(Color.FromRgb(0x5A, 0x60, 0x6C)), StrokeThickness = 1.5 };
            var manche = new System.Windows.Shapes.Rectangle { Width = 26, Height = 84, RadiusX = 11, RadiusY = 11, Fill = new LinearGradientBrush(Color.FromRgb(0xD9, 0x9A, 0x55), Color.FromRgb(0x8A, 0x55, 0x22), 0), Stroke = new SolidColorBrush(Color.FromRgb(0x5E, 0x38, 0x14)), StrokeThickness = 1.5 };
            Canvas.SetLeft(manche, -13); Canvas.SetTop(manche, -130);
            var virole = new System.Windows.Shapes.Rectangle { Width = 22, Height = 10, Fill = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x3C)) };
            Canvas.SetLeft(virole, -11); Canvas.SetTop(virole, -54);
            g.Children.Add(manche); g.Children.Add(lame); g.Children.Add(virole);
            c.Children.Add(g);
            c.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 6, ShadowDepth = 3, Opacity = 0.35 };
            return c;
        }

        /// <summary>Pose le ciseau au point taillé ; « coup » le fait reculer d'un petit choc.</summary>
        protected static void PoserCiseau(Canvas ciseau, Point p, bool coup)
        {
            if (ciseau == null) return;
            ciseau.Visibility = Visibility.Visible;
            Canvas.SetLeft(ciseau, p.X + 6); Canvas.SetTop(ciseau, p.Y - 6);
            if (!coup || ciseau.Children.Count == 0) return;
            if (ciseau.Children[0].RenderTransform is TransformGroup tg && tg.Children[0] is TranslateTransform tt)
                tt.BeginAnimation(TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(-9, 0, TimeSpan.FromMilliseconds(110)));
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
