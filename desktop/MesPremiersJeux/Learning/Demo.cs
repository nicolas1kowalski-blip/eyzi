using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Learning
{
    /// <summary>Une explication animée : un « angle » pour faire comprendre une notion.</summary>
    public sealed class Explication
    {
        public string Id;
        public Func<Demo> Make;
    }

    /// <summary>
    /// Une FAMILLE d'explications : la même notion montrée sous plusieurs angles
    /// (le ballon qui gonfle, l'éléphant et la souris, les ours et leurs bols…).
    /// D'une fois sur l'autre, l'angle change.
    /// </summary>
    public sealed class Famille
    {
        public string Cle;
        public Explication[] Angles;

        public Famille(string cle, params (string Id, Func<Demo> Make)[] angles)
        {
            Cle = cle;
            Angles = new Explication[angles.Length];
            for (int i = 0; i < angles.Length; i++) Angles[i] = new Explication { Id = cle + "." + angles[i].Id, Make = angles[i].Make };
        }

        /// <summary>L'angle suivant (on tourne, et on s'en souvient d'un jour à l'autre).</summary>
        public Explication Prochaine() => Angles[Tours.Suivant(Cle, Angles.Length)];
    }

    /// <summary>Où en est-on dans la ronde des angles (un petit fichier, à côté des apprentissages).</summary>
    internal static class Tours
    {
        private static Dictionary<string, int> _vu;
        private static string FilePath => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "explications.ini");

        public static int Suivant(string cle, int n)
        {
            if (n <= 1) return 0;
            if (_vu == null)
            {
                _vu = new Dictionary<string, int>();
                try
                {
                    if (System.IO.File.Exists(FilePath))
                        foreach (var line in System.IO.File.ReadAllLines(FilePath))
                        {
                            int eq = line.IndexOf('=');
                            if (eq > 0 && int.TryParse(line.Substring(eq + 1), out var v)) _vu[line.Substring(0, eq)] = v;
                        }
                }
                catch { }
            }
            _vu.TryGetValue(cle, out var k);
            _vu[cle] = k + 1;
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
                var lines = new List<string>();
                foreach (var kv in _vu) lines.Add(kv.Key + "=" + kv.Value);
                System.IO.File.WriteAllLines(FilePath, lines);
            }
            catch { }
            return ((k % n) + n) % n;
        }
    }

    /// <summary>Un temps de l'explication : ce que la voix dit, ce qui bouge, et combien de temps ça prend.
    /// Avec une cible, c'est un « à toi » : l'enfant la regarde pour déclencher la suite.</summary>
    public sealed class Temps
    {
        public string Dit;
        public Action Jouer;
        public int Ms;
        public Acteur Cible;
    }

    /// <summary>
    /// UNE EXPLICATION ANIMÉE : une scène (1280 × 680) et une suite de temps.
    /// Une seule idée, des phrases courtes dites au moment où la chose se passe,
    /// des mouvements lents, aucun clignotement.
    /// </summary>
    public sealed class Demo
    {
        public const double W = 1280, H = 680, Sol = 560;
        public readonly Canvas Scene = new Canvas { Width = W, Height = H };
        public readonly List<Temps> Temps = new List<Temps>();

        public static Color Encre = Color.FromRgb(0x3B, 0x2A, 0x5A);
        public static Color Or = Color.FromRgb(0xFF, 0xB3, 0x00);

        public Demo(string decor = "pre")
        {
            Color haut, bas, sol, solBord;
            switch (decor)
            {
                case "piece": haut = Color.FromRgb(0xFF, 0xF1, 0xD6); bas = Color.FromRgb(0xFF, 0xE3, 0xC2); sol = Color.FromRgb(0xD9, 0xA8, 0x74); solBord = Color.FromRgb(0xC2, 0x8E, 0x5A); break;
                case "ciel": haut = Color.FromRgb(0xBF, 0xE6, 0xFF); bas = Color.FromRgb(0xEF, 0xFA, 0xFF); sol = Color.FromRgb(0x9B, 0xDB, 0x7A); solBord = Color.FromRgb(0x7C, 0xC6, 0x5C); break;
                case "table": haut = Color.FromRgb(0xEE, 0xE8, 0xFF); bas = Color.FromRgb(0xF8, 0xF4, 0xFF); sol = Color.FromRgb(0xC9, 0xB8, 0xF0); solBord = Color.FromRgb(0xAE, 0x9A, 0xE0); break;
                default: haut = Color.FromRgb(0xCF, 0xEC, 0xFF); bas = Color.FromRgb(0xF4, 0xFB, 0xFF); sol = Color.FromRgb(0xA6, 0xDF, 0x86); solBord = Color.FromRgb(0x86, 0xCB, 0x66); break;
            }
            Scene.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(haut, bas, 90) });
            var s = new Rectangle { Width = W, Height = H - Sol + 14, Fill = new SolidColorBrush(sol) };
            Canvas.SetTop(s, Sol - 14); Scene.Children.Add(s);
            var b = new Rectangle { Width = W, Height = 8, Fill = new SolidColorBrush(solBord), Opacity = 0.7 };
            Canvas.SetTop(b, Sol - 14); Scene.Children.Add(b);
        }

        // ------------------------------------------------------------------
        //  Le déroulé
        // ------------------------------------------------------------------
        /// <summary>La voix dit <paramref name="dit"/> pendant que <paramref name="jouer"/> se joue ; on attend au moins <paramref name="ms"/> et la fin de la phrase.</summary>
        public Demo Dire(string dit, Action jouer = null, int ms = 900) { Temps.Add(new Temps { Dit = dit, Jouer = jouer, Ms = ms }); return this; }

        /// <summary>Un mouvement sans parole.</summary>
        public Demo Faire(Action jouer, int ms = 600) => Dire(null, jouer, ms);

        /// <summary>« À toi ! » : l'enfant regarde <paramref name="cible"/> et la suite se joue (toute seule au bout d'un moment : on ne bloque jamais).</summary>
        public Demo Toi(string invite, Acteur cible, Action jouer, int ms = 1200)
        {
            cible.Cliquable();
            Temps.Add(new Temps { Dit = invite, Jouer = jouer, Ms = ms, Cible = cible });
            return this;
        }

        // ------------------------------------------------------------------
        //  Les acteurs
        // ------------------------------------------------------------------
        public Acteur Chose(FrameworkElement contenu, double w, double h) => new Acteur(Scene, contenu, w, h);

        /// <summary>Un dessin de la banque, haut de <paramref name="hauteur"/> : ses marges vides sont rognées,
        /// pour qu'il pose vraiment sur le sol et que sa taille soit celle qu'on voit. (Sans dessin : l'emoji.)</summary>
        public Acteur Image(string nom, string emoji, double hauteur)
        {
            var b = Rogne(nom);
            if (b == null) return new Acteur(Scene, Art.Visual(nom, emoji, hauteur), hauteur, hauteur);
            double w = hauteur * b.PixelWidth / b.PixelHeight;
            return new Acteur(Scene, new Image { Source = b, Width = w, Height = hauteur, Stretch = Stretch.Fill }, w, hauteur);
        }

        private static readonly Dictionary<string, BitmapSource> Rognes = new Dictionary<string, BitmapSource>();

        private static BitmapSource Rogne(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return null;
            if (Rognes.TryGetValue(nom, out var deja)) return deja;
            BitmapSource res = null;
            try
            {
                if (Art.Find(nom) is BitmapSource src && src.PixelWidth * (long)src.PixelHeight <= 4000000)
                {
                    var f = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                    int w = f.PixelWidth, h = f.PixelHeight;
                    var px = new byte[w * h * 4];
                    f.CopyPixels(px, w * 4, 0);
                    int x0 = w, y0 = h, x1 = -1, y1 = -1;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if (px[(y * w + x) * 4 + 3] > 24) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                    if (x1 > x0 && y1 > y0)
                    {
                        // Une copie indépendante des pixels utiles (toujours figeable, quel que soit l'original).
                        int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
                        var coupe = new byte[cw * ch * 4];
                        for (int y = 0; y < ch; y++) Buffer.BlockCopy(px, ((y0 + y) * w + x0) * 4, coupe, y * cw * 4, cw * 4);
                        var c = BitmapSource.Create(cw, ch, 96, 96, PixelFormats.Bgra32, null, coupe, cw * 4);
                        c.Freeze();
                        res = c;
                    }
                }
            }
            catch { res = null; }
            Rognes[nom] = res;
            return res;
        }

        public Acteur Rect(double w, double h, Color c, double rayon = 12, bool bord = true)
            => new Acteur(Scene, new Rectangle { Width = w, Height = h, RadiusX = rayon, RadiusY = rayon, Fill = new SolidColorBrush(c), Stroke = bord ? new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)) : null, StrokeThickness = bord ? 3 : 0 }, w, h);

        public Acteur Rond(double d, Color c)
            => new Acteur(Scene, new Ellipse { Width = d, Height = d, Fill = new SolidColorBrush(c), Stroke = new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)), StrokeThickness = 3 }, d, d);

        /// <summary>Un cube de construction (pour les tours).</summary>
        public Acteur Bloc(double t, Color c)
        {
            var g = new Grid { Width = t, Height = t };
            g.Children.Add(new Rectangle { RadiusX = t * 0.18, RadiusY = t * 0.18, Fill = new SolidColorBrush(c), Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)), StrokeThickness = 3 });
            g.Children.Add(new Rectangle { RadiusX = t * 0.1, RadiusY = t * 0.1, Fill = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), Margin = new Thickness(t * 0.14, t * 0.12, t * 0.14, t * 0.55) });
            return new Acteur(Scene, g, t, t);
        }

        /// <summary>Une toise : une barre de mesure qui monte à côté de ce qu'on mesure (elle part de zéro).</summary>
        public Acteur Toise(double hauteur, Color c)
        {
            var g = new Grid { Width = 34, Height = hauteur };
            g.Children.Add(new Rectangle { Width = 16, RadiusX = 8, RadiusY = 8, Fill = new SolidColorBrush(c), HorizontalAlignment = HorizontalAlignment.Center });
            g.Children.Add(new Rectangle { Width = 34, Height = 8, RadiusX = 4, RadiusY = 4, Fill = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Top });
            g.Children.Add(new Rectangle { Width = 34, Height = 8, RadiusX = 4, RadiusY = 4, Fill = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Bottom });
            var a = new Acteur(Scene, g, 34, hauteur);
            a.EchelleXY(1, 0.001); a.Opacite(0);
            return a;
        }

        /// <summary>Un trait (repère, ligne de départ, lien) : posé invisible, à faire apparaître avec Voir.</summary>
        public Line Trait(double x1, double y1, double x2, double y2, Color c, double ep = 6, bool pointille = false)
        {
            var l = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = new SolidColorBrush(c), StrokeThickness = ep, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Opacity = 0, IsHitTestVisible = false };
            if (pointille) { l.StrokeDashArray = new DoubleCollection { 0.1, 2.2 }; l.StrokeDashCap = PenLineCap.Round; }
            Scene.Children.Add(l);
            return l;
        }

        /// <summary>Fait apparaître (ou disparaître) en douceur n'importe quel élément de la scène.</summary>
        public static void Voir(UIElement el, int ms = 400, int delai = 0, double de = 0, double a = 1)
            => Acteur.K(el, UIElement.OpacityProperty, de, a, ms, delai, null);
    }

    /// <summary>
    /// UN ACTEUR de la scène : une case posée par son centre et son pied, qui
    /// peut grandir, glisser, sauter, tourner, apparaître… Chaque mouvement part
    /// de l'état connu (pas de saut d'image), avec un départ différé possible.
    /// </summary>
    public sealed class Acteur
    {
        public readonly Grid Boite;
        public readonly double W, H;
        public Button Bouton { get; private set; }

        private readonly FrameworkElement _contenu;
        private readonly ScaleTransform _s = new ScaleTransform(1, 1);
        private readonly RotateTransform _r = new RotateTransform(0);
        private readonly TranslateTransform _t = new TranslateTransform(0, 0);
        private double _x0, _y0, _kx = 1, _ky = 1, _tx, _ty, _rot, _op = 1;
        private static int _z = 10;

        private static readonly IEasingFunction Doux = new SineEase { EasingMode = EasingMode.EaseInOut };
        public static readonly IEasingFunction Ressort = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.7 };
        public static readonly IEasingFunction Rebond = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 3 };
        public static readonly IEasingFunction Sortie = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        public static readonly IEasingFunction Entree = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        public static readonly IEasingFunction Lineaire = new PowerEase { Power = 1, EasingMode = EasingMode.EaseIn };

        internal Acteur(Canvas scene, FrameworkElement contenu, double w, double h)
        {
            _contenu = contenu; W = w; H = h;
            Boite = new Grid { Width = w, Height = h, RenderTransformOrigin = new Point(0.5, 1) };
            Boite.Children.Add(contenu);
            var g = new TransformGroup();
            g.Children.Add(_s); g.Children.Add(_r); g.Children.Add(_t);
            Boite.RenderTransform = g;
            scene.Children.Add(Boite);
        }

        /// <summary>Où il est maintenant : le centre (X) et le pied (Y).</summary>
        public double X => _x0 + _tx;
        public double Y => _y0 + _ty;

        // ----- la pose (sans animation) -----
        /// <summary>Pose l'acteur : <paramref name="cx"/> = son centre, <paramref name="sol"/> = son pied.</summary>
        public Acteur En(double cx, double sol)
        {
            _x0 = cx; _y0 = sol; _tx = 0; _ty = 0;
            Fixer(_t, TranslateTransform.XProperty, 0); Fixer(_t, TranslateTransform.YProperty, 0);
            Canvas.SetLeft(Boite, cx - W / 2); Canvas.SetTop(Boite, sol - H);
            return this;
        }

        /// <summary>Pose l'acteur par son centre.</summary>
        public Acteur Au(double cx, double cy) => En(cx, cy + H / 2);

        /// <summary>Le point autour duquel il grandit et tourne (0,5 ; 1 = le pied, par défaut).</summary>
        public Acteur Origine(double ox, double oy) { Boite.RenderTransformOrigin = new Point(ox, oy); return this; }

        public Acteur Echelle(double k) => EchelleXY(k, k);
        public Acteur EchelleXY(double kx, double ky)
        {
            _kx = kx; _ky = ky;
            Fixer(_s, ScaleTransform.ScaleXProperty, kx); Fixer(_s, ScaleTransform.ScaleYProperty, ky);
            return this;
        }

        public Acteur Angle(double deg) { _rot = deg; Fixer(_r, RotateTransform.AngleProperty, deg); return this; }

        public Acteur Opacite(double o) { _op = o; Boite.BeginAnimation(UIElement.OpacityProperty, null); Boite.Opacity = o; return this; }

        public Acteur Devant() { Panel.SetZIndex(Boite, ++_z); return this; }

        /// <summary>Devient une cible du regard (un bouton sans habillage), endormie tant que ce n'est pas son « à toi ».</summary>
        public Acteur Cliquable()
        {
            if (Bouton != null) return this;
            Boite.Children.Remove(_contenu);
            Bouton = new Button { Style = (Style)Application.Current.Resources["BalloonButton"], Content = _contenu, Width = W, Height = H, Focusable = false, IsHitTestVisible = false };
            Boite.Children.Add(Bouton);
            return this;
        }

        // ----- les mouvements -----
        public void Grandir(double k, int ms, int delai = 0, IEasingFunction e = null) => GrandirXY(k, k, ms, delai, e);

        public void GrandirXY(double kx, double ky, int ms, int delai = 0, IEasingFunction e = null)
        {
            K(_s, ScaleTransform.ScaleXProperty, _kx, kx, ms, delai, e);
            K(_s, ScaleTransform.ScaleYProperty, _ky, ky, ms, delai, e);
            _kx = kx; _ky = ky;
        }

        /// <summary>Glisse jusqu'à (centre, pied).</summary>
        public void Aller(double cx, double sol, int ms, int delai = 0, IEasingFunction e = null)
        {
            K(_t, TranslateTransform.XProperty, _tx, cx - _x0, ms, delai, e);
            K(_t, TranslateTransform.YProperty, _ty, sol - _y0, ms, delai, e);
            _tx = cx - _x0; _ty = sol - _y0;
        }

        public void AllerAu(double cx, double cy, int ms, int delai = 0, IEasingFunction e = null) => Aller(cx, cy + H / 2, ms, delai, e);

        /// <summary>Va jusqu'à (centre, pied) en faisant un bond par-dessus.</summary>
        public void Bond(double cx, double sol, double haut, int ms, int delai = 0)
        {
            K(_t, TranslateTransform.XProperty, _tx, cx - _x0, ms, delai, Doux);
            double ty = sol - _y0, sommet = Math.Min(_ty, ty) - haut;
            var kf = Debut(_ty, delai);
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(sommet, T(delai + ms / 2), Sortie));
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(ty, T(delai + ms), Entree));
            _t.BeginAnimation(TranslateTransform.YProperty, kf);
            _tx = cx - _x0; _ty = ty;
        }

        /// <summary>Saute sur place.</summary>
        public void Sauter(double haut = 36, int ms = 480, int delai = 0, int fois = 1)
        {
            var kf = Debut(_ty, delai);
            for (int i = 0; i < fois; i++)
            {
                kf.KeyFrames.Add(new EasingDoubleKeyFrame(_ty - haut, T(delai + i * ms + ms / 2), Sortie));
                kf.KeyFrames.Add(new EasingDoubleKeyFrame(_ty, T(delai + (i + 1) * ms), Entree));
            }
            _t.BeginAnimation(TranslateTransform.YProperty, kf);
        }

        public void Tourner(double deg, int ms, int delai = 0, IEasingFunction e = null)
        {
            K(_r, RotateTransform.AngleProperty, _rot, deg, ms, delai, e);
            _rot = deg;
        }

        /// <summary>Se dandine (non, trop lourd, pas content…).</summary>
        public void Secouer(int delai = 0, int fois = 3, double amplitude = 7)
        {
            var kf = Debut(_rot, delai);
            int pas = 110;
            for (int i = 0; i < fois; i++)
            {
                kf.KeyFrames.Add(new EasingDoubleKeyFrame(_rot - amplitude, T(delai + i * 2 * pas + pas), Doux));
                kf.KeyFrames.Add(new EasingDoubleKeyFrame(_rot + amplitude, T(delai + i * 2 * pas + 2 * pas), Doux));
            }
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(_rot, T(delai + fois * 2 * pas + pas), Doux));
            _r.BeginAnimation(RotateTransform.AngleProperty, kf);
        }

        public void Fondu(double op, int ms, int delai = 0)
        {
            K(Boite, UIElement.OpacityProperty, _op, op, ms, delai, null);
            _op = op;
        }

        /// <summary>Apparaît d'un coup de ressort.</summary>
        public void Pop(int delai = 0)
        {
            Fondu(1, 160, delai);
            K(_s, ScaleTransform.ScaleXProperty, _kx * 0.3, _kx, 420, delai, Ressort);
            K(_s, ScaleTransform.ScaleYProperty, _ky * 0.3, _ky, 420, delai, Ressort);
        }

        /// <summary>S'écrase un peu en touchant le sol, puis reprend sa forme.</summary>
        public void Ecraser(int delai = 0)
        {
            var kx = Debut(_kx, delai); var ky = Debut(_ky, delai);
            kx.KeyFrames.Add(new EasingDoubleKeyFrame(_kx * 1.12, T(delai + 110), Sortie)); kx.KeyFrames.Add(new EasingDoubleKeyFrame(_kx, T(delai + 300), Doux));
            ky.KeyFrames.Add(new EasingDoubleKeyFrame(_ky * 0.84, T(delai + 110), Sortie)); ky.KeyFrames.Add(new EasingDoubleKeyFrame(_ky, T(delai + 300), Doux));
            _s.BeginAnimation(ScaleTransform.ScaleXProperty, kx); _s.BeginAnimation(ScaleTransform.ScaleYProperty, ky);
        }

        /// <summary>« Respire » doucement tant qu'on attend le regard de l'enfant.</summary>
        public void Respirer()
        {
            var ax = new DoubleAnimation(_kx, _kx * 1.08, TimeSpan.FromMilliseconds(650)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = Doux };
            var ay = new DoubleAnimation(_ky, _ky * 1.08, TimeSpan.FromMilliseconds(650)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = Doux };
            _s.BeginAnimation(ScaleTransform.ScaleXProperty, ax); _s.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }

        public void Calme() => EchelleXY(_kx, _ky);

        /// <summary>Se montre un moment puis s'efface (un repère, un souffle de vent…).</summary>
        public void Montrer(int delai = 0, int tenue = 900, double op = 1)
        {
            var kf = Debut(0, delai);
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(op, T(delai + 350), Doux));
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(op, T(delai + 350 + tenue), Doux));
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, T(delai + 700 + tenue), Doux));
            Boite.BeginAnimation(UIElement.OpacityProperty, kf);
            _op = 0;
        }

        /// <summary>Un halo doré qui monte et redescend : « c'est de lui qu'on parle ».</summary>
        public void Briller(int delai = 0, int ms = 1500)
        {
            var fx = new DropShadowEffect { Color = Demo.Or, BlurRadius = 46, ShadowDepth = 0, Opacity = 0 };
            Boite.Effect = fx;
            var kf = Debut(0, delai);
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(1, T(delai + 350), Doux));
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(1, T(delai + ms - 400), Doux));
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, T(delai + ms), Doux));
            fx.BeginAnimation(DropShadowEffect.OpacityProperty, kf);
        }

        // ----- l'outillage -----
        private static KeyTime T(int ms) => KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(Math.Max(0, ms)));

        private static DoubleAnimationUsingKeyFrames Debut(double de, int delai)
        {
            var kf = new DoubleAnimationUsingKeyFrames();
            kf.KeyFrames.Add(new DiscreteDoubleKeyFrame(de, T(0)));
            if (delai > 0) kf.KeyFrames.Add(new DiscreteDoubleKeyFrame(de, T(delai)));
            return kf;
        }

        /// <summary>Va de <paramref name="de"/> à <paramref name="a"/> en <paramref name="ms"/>, après <paramref name="delai"/> (la valeur de départ est tenue pendant l'attente).</summary>
        internal static void K(IAnimatable cible, DependencyProperty p, double de, double a, int ms, int delai, IEasingFunction e)
        {
            var kf = Debut(de, delai);
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(a, T(delai + Math.Max(1, ms)), e ?? Doux));
            cible.BeginAnimation(p, kf);
        }

        private static void Fixer(Animatable cible, DependencyProperty p, double v)
        {
            cible.BeginAnimation(p, null);
            cible.SetValue(p, v);
        }
    }
}
