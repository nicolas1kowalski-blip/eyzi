using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// « Le jet d'eau » — cause à effet au regard pur, sans échec possible. Le petit pompier tient une
    /// lance : elle vise là où l'enfant regarde, et l'eau coule tant qu'il regarde l'écran. Cinq scènes
    /// s'enchaînent : laver la voiture pleine de boue, éteindre les flammes de la maison, remplir
    /// l'aquarium, le bocal et le bassin du canard, arroser les fleurs, faire tourner la roue à eau
    /// qui allume la fête. À la souris ou au doigt, l'eau suit le pointeur.
    ///
    /// Les dessins viennent de JetArt.cs (fabriqué par desktop/jetdeau) ; tout le mouvement est calculé
    /// ici, image par image.
    /// </summary>
    public sealed class WaterJetGame : UserControl
    {
        private const double W = 1600, H = 900, Horizon = 700, LongueurLance = 150;
        private static readonly Point Pivot = new Point(808, 820);
        private static readonly Point Oeil = new Point(712 + 16 * 0.62, 968 - 258 * 0.62);

        // ------------------------------------------------------------------
        //  Un dessin posé dans la scène : on règle sa place, sa taille, son angle
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, Geometry> Traces = new Dictionary<string, Geometry>();
        private static readonly Dictionary<string, Brush> Brosses = new Dictionary<string, Brush>();

        private static Geometry Trace(string d)
        {
            if (!Traces.TryGetValue(d, out var g))
            {
                g = Geometry.Parse(d);
                if (g.CanFreeze) g.Freeze();
                Traces[d] = g;
            }
            return g;
        }

        private static Color Couleur(string c)
        {
            try { return (Color)ColorConverter.ConvertFromString(c); } catch { return Colors.Magenta; }
        }

        private static Brush Brosse(string c)
        {
            if (!Brosses.TryGetValue(c, out var b))
            {
                var s = new SolidColorBrush(Couleur(c));
                s.Freeze();
                Brosses[c] = b = s;
            }
            return b;
        }

        private static string Choisie(string v, string[] couleurs)
        {
            if (v == null) return null;
            if (v[0] != '$') return v;
            if (couleurs != null)
                for (int i = 0; i + 1 < couleurs.Length; i += 2)
                    if (couleurs[i] == v.Substring(1)) return couleurs[i + 1];
            return "#FF00FF";
        }

        private sealed class Sp
        {
            public readonly Canvas Ui = new Canvas { IsHitTestVisible = false };
            private readonly TranslateTransform _t = new TranslateTransform();
            private readonly ScaleTransform _s = new ScaleTransform(1, 1);
            private readonly RotateTransform _r = new RotateTransform(0);

            public Sp(string dessin, params string[] couleurs)
            {
                foreach (var f in JetArt.Dessins[dessin])
                {
                    var p = new Path
                    {
                        Data = Trace(f.D),
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                    };
                    var fond = Choisie(f.F, couleurs);
                    if (fond != null) p.Fill = Brosse(fond);
                    var trait = Choisie(f.S, couleurs);
                    if (trait != null && f.W > 0) { p.Stroke = Brosse(trait); p.StrokeThickness = f.W; }
                    if (f.O < 1) p.Opacity = f.O;
                    Ui.Children.Add(p);
                }
                var g = new TransformGroup();
                g.Children.Add(_s);
                g.Children.Add(_r);
                g.Children.Add(_t);
                Ui.RenderTransform = g;
            }

            public double X { get => _t.X; set => _t.X = value; }
            public double Y { get => _t.Y; set => _t.Y = value; }
            public double R { get => _r.Angle; set => _r.Angle = value; }
            public double SX { get => _s.ScaleX; set => _s.ScaleX = value; }
            public double SY { get => _s.ScaleY; set => _s.ScaleY = value; }
            public double E { get => _s.ScaleY; set { _s.ScaleX = value; _s.ScaleY = value; } }
            public double Op { get => Ui.Opacity; set => Ui.Opacity = value; }
            public Sp En(double x, double y, double e = 1, double r = 0) { X = x; Y = y; E = e; R = r; return this; }
        }

        // ------------------------------------------------------------------
        //  Les petites choses qui volent : gouttes, bulles, vapeur, étoiles…
        // ------------------------------------------------------------------
        private sealed class Grain { public Sp S; public double X, Y, VX, VY, G, Vie, Age, Taille, Gonfle, Tourne; public bool Vivant; }

        private sealed class Bac
        {
            private readonly List<Grain> _grains = new List<Grain>();

            public Bac(Canvas calque, string dessin, int nombre, params string[] couleurs)
            {
                for (int i = 0; i < nombre; i++)
                {
                    var s = new Sp(dessin, couleurs) { Op = 0 };
                    calque.Children.Add(s.Ui);
                    _grains.Add(new Grain { S = s });
                }
            }

            /// <summary>Lance un grain (s'il en reste un de libre).</summary>
            public void Lance(double x, double y, double vx, double vy, double pesanteur, double vie, double taille = 1, double gonfle = 0, double tourne = 0)
            {
                foreach (var g in _grains)
                {
                    if (g.Vivant) continue;
                    g.X = x; g.Y = y; g.VX = vx; g.VY = vy; g.G = pesanteur; g.Vie = vie; g.Age = 0;
                    g.Taille = taille; g.Gonfle = gonfle; g.Tourne = tourne; g.Vivant = true;
                    return;
                }
            }

            public void Avance(double dt)
            {
                foreach (var g in _grains)
                {
                    if (!g.Vivant) continue;
                    g.Age += dt;
                    if (g.Age >= g.Vie) { g.Vivant = false; g.S.Op = 0; continue; }
                    g.VY += g.G * dt; g.X += g.VX * dt; g.Y += g.VY * dt;
                    double u = g.Age / g.Vie;
                    g.S.X = g.X; g.S.Y = g.Y;
                    g.S.E = g.Taille * (1 + g.Gonfle * u) * Math.Min(1, g.Age * 14);
                    g.S.R += g.Tourne * dt;
                    g.S.Op = Math.Min(1, (1 - u) * 3);
                }
            }

            public void Vide() { foreach (var g in _grains) { g.Vivant = false; g.S.Op = 0; } }
        }

        // ------------------------------------------------------------------
        //  Le jeu
        // ------------------------------------------------------------------
        private readonly Action _fete;
        private readonly Random _alea = new Random();
        private readonly Grid _racine = new Grid();
        private readonly Canvas _monde = new Canvas { Width = W, Height = H };
        private readonly Canvas _ciel = new Canvas(), _decor = new Canvas(), _eau = new Canvas(), _avant = new Canvas(), _effets = new Canvas();
        private readonly GradientStop _cielHaut = new GradientStop(Colors.LightSkyBlue, 0), _cielBas = new GradientStop(Colors.White, 0.78),
            _solHaut = new GradientStop(Colors.Green, 0.78), _solBas = new GradientStop(Colors.Green, 1);
        private readonly Path _jetBord, _jetCorps, _jetReflets;
        private readonly Sp _lance, _pupille, _soleil, _rayons;
        private readonly Sp[] _nuages;
        private readonly StackPanel _points = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 0, 0), IsHitTestVisible = false };
        private readonly DispatcherTimer _tic = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        private readonly Stopwatch _montre = new Stopwatch();
        private Bac _gouttes, _gadoue, _bulles, _vapeur, _fumee, _etoiles, _coeurs;
        private MediaPlayer _son;

        private Point _regard = new Point(W / 2, 400), _cible = new Point(W / 2, 400);
        private double _vu = -10, _flux = -10, _pression, _temps, _dernier, _sansProgres, _indice;
        private readonly List<Scene> _scenes = new List<Scene>();
        private Scene _scene;
        private int _numero = -1, _reussites;
        private enum Etat { Entree, Joue, Fete, Sortie }
        private Etat _etat = Etat.Entree;
        private double _depuis;      // secondes depuis l'entrée dans l'état
        private DateTime _debut = DateTime.Now;

        private sealed class Mot { public Canvas Ui; public ScaleTransform S; public TranslateTransform T; public double Age, X, Y; }
        private readonly List<Mot> _mots = new List<Mot>();

        public WaterJetGame(Action celebrate)
        {
            _fete = celebrate;

            // Le fond de la page : le ciel, puis la couleur du sol sous l'horizon — les bandes autour de la
            // scène (écran plus large ou plus haut) prolongent ainsi le décor.
            var fond = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            fond.GradientStops.Add(_cielHaut); fond.GradientStops.Add(_cielBas); fond.GradientStops.Add(_solHaut); fond.GradientStops.Add(_solBas);
            _racine.Background = fond;
            _racine.SizeChanged += (s, e) => PlaceHorizon();

            foreach (var c in new[] { _ciel, _decor, _eau, _avant, _effets }) { c.IsHitTestVisible = false; _monde.Children.Add(c); }
            _racine.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = _monde });
            _racine.Children.Add(_points);
            Content = _racine;

            // Le ciel : le soleil, ses rayons qui tournent, deux nuages qui passent.
            _rayons = Pose(_ciel, "rayons", 1420, 130);
            _soleil = Pose(_ciel, "soleil", 1420, 130);
            _nuages = new[] { Pose(_ciel, "nuage", 260, 150), Pose(_ciel, "nuage", 900, 110, 0.8) };

            // L'eau : trois traits superposés (le bord, le corps, les reflets qui filent), puis les gouttes.
            _jetBord = Trait("#3E9BE0", 30);
            _jetCorps = Trait("#6FC6FF", 22);
            _jetReflets = Trait("#FFFFFF", 7);
            _jetReflets.Opacity = 0.8;
            _jetReflets.StrokeDashArray = new DoubleCollection { 2, 3.2 };
            _jetReflets.StrokeDashCap = PenLineCap.Round;
            _gadoue = new Bac(_eau, "gadoue", 22);
            _gouttes = new Bac(_eau, "goutte", 46);
            _bulles = new Bac(_eau, "bulle", 26);
            _fumee = new Bac(_eau, "fumee", 18);
            _vapeur = new Bac(_eau, "vapeur", 26);

            // Devant : le tuyau, le pompier, sa pupille (elle suit l'eau) et la lance.
            Pose(_avant, "tuyau", Pivot.X, Pivot.Y);
            Pose(_avant, "pompier", 712, 968, 0.62);
            _pupille = Pose(_avant, "pupille", Oeil.X, Oeil.Y, 0.62);
            _lance = Pose(_avant, "lance", Pivot.X, Pivot.Y);

            _etoiles = new Bac(_effets, "etoile", 40);
            _coeurs = new Bac(_effets, "coeur", 14);

            _scenes.Add(new SceneVoiture(this));
            _scenes.Add(new SceneFeu(this));
            _scenes.Add(new SceneRemplir(this));
            _scenes.Add(new SceneJardin(this));
            _scenes.Add(new SceneRoue(this));
            for (int i = 0; i < _scenes.Count; i++)
                _points.Children.Add(new Ellipse { Width = 18, Height = 18, Margin = new Thickness(5, 0, 5, 0), Stroke = Brushes.White, StrokeThickness = 3, Fill = Brushes.Transparent, Opacity = 0.9 });

            _tic.Tick += (s, e) => Tic();

            Loaded += (s, e) =>
            {
                GazeGate.PushTargetsOnly();      // le regard n'actionne que le bouton retour
                GazeFeed.Sample += SurRegard;
                Usage.GameStart(nameof(WaterJetGame));
                _debut = DateTime.Now; _reussites = 0;
                DemarreSon();
                _montre.Restart(); _dernier = 0;
                if (_scene == null) Ouvre(0);
                _tic.Start();
            };
            Unloaded += (s, e) =>
            {
                _tic.Stop();
                GazeFeed.Sample -= SurRegard;
                GazeGate.PopTargetsOnly();
                try { _son?.Stop(); _son?.Close(); } catch { }
                _son = null;
                GazeStats.RecordGame(nameof(WaterJetGame), (DateTime.Now - _debut).TotalSeconds, _reussites);
                Usage.GameStop();
            };
        }

        private Sp Pose(Canvas calque, string dessin, double x, double y, double e = 1, params string[] couleurs)
        {
            var s = new Sp(dessin, couleurs).En(x, y, e);
            calque.Children.Add(s.Ui);
            return s;
        }

        private Path Trait(string couleur, double epaisseur)
        {
            var p = new Path { Stroke = Brosse(couleur), StrokeThickness = epaisseur, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            _eau.Children.Add(p);
            return p;
        }

        private void PlaceHorizon()
        {
            double w = _racine.ActualWidth, h = _racine.ActualHeight;
            if (w < 10 || h < 10) return;
            double k = Math.Min(w / W, h / H), haut = (h - H * k) / 2;
            double o = Math.Max(0.05, Math.Min(0.98, (haut + Horizon * k) / h));
            _cielBas.Offset = o; _solHaut.Offset = o;
        }

        private void DemarreSon()
        {
            try
            {
                _son = new MediaPlayer { Volume = 0 };
                var lecteur = _son;
                lecteur.MediaEnded += (s, e) => { lecteur.Position = TimeSpan.Zero; lecteur.Play(); };
                lecteur.Open(new Uri(SoundFx.WaterLoopFile()));
                lecteur.Play();
            }
            catch { _son = null; }
        }

        private void SurRegard(Point ecran, bool valide)
        {
            _flux = _temps;      // le moteur du regard tourne (même si l'enfant regarde ailleurs)
            if (!valide) return;
            try { _regard = _monde.PointFromScreen(ecran); _vu = _temps; } catch { }
        }

        // ------------------------------------------------------------------
        //  Ce que les scènes peuvent demander au jeu
        // ------------------------------------------------------------------
        private double Hasard() => _alea.NextDouble();
        private double Entre(double a, double b) => a + (b - a) * _alea.NextDouble();
        private static double Borne(double v, double a = 0, double b = 1) => Math.Max(a, Math.Min(b, v));
        private static double Doux(double x) { x = Borne(x); return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2; }
        private static double Sortant(double x) { x = Borne(x); return 1 - Math.Pow(1 - x, 3); }
        private static double Rebond(double x) => x <= 0 ? 0 : x >= 1 ? 1 : Math.Pow(2, -9 * x) * Math.Sin((x * 10 - 0.75) * (2 * Math.PI / 3)) + 1;
        private static double Distance(Point a, double x, double y) => Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y));

        /// <summary>L'enfant vient de faire avancer la scène : le coup de pouce attend.</summary>
        private void Progres() { _sansProgres = 0; _indice = 0; }

        /// <summary>Une petite réussite (une tache partie, une flamme éteinte…).</summary>
        private void Reussite() { _reussites++; }

        /// <summary>Un éclat d'étoiles.</summary>
        private void Eclat(double x, double y, int n = 7, double force = 260)
        {
            for (int i = 0; i < n; i++)
            {
                double a = Math.PI * 2 * i / n + Hasard();
                _etoiles.Lance(x, y, Math.Cos(a) * force * Entre(0.6, 1), Math.Sin(a) * force * Entre(0.6, 1) - 60, 420, Entre(0.5, 0.8), Entre(0.8, 1.4), 0, Entre(-300, 300));
            }
        }

        /// <summary>Un mot de bande dessinée (« Tut tut ! », « Pschhh ! ») qui saute puis s'envole.</summary>
        private void Ecris(string texte, double x, double y, string couleur, double taille = 64)
        {
            try
            {
                var ft = new FormattedText(texte, CultureInfo.GetCultureInfo("fr-FR"), FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Arial Black, Arial"), FontStyles.Normal, FontWeights.Black, FontStretches.Normal), taille, Brushes.Black, 1.0);
                var g = ft.BuildGeometry(new Point(-ft.Width / 2, -ft.Height / 2));
                if (g.CanFreeze) g.Freeze();
                var c = new Canvas { IsHitTestVisible = false };
                c.Children.Add(new Path { Data = g, Stroke = Brosse("#2A1C4E"), StrokeThickness = 10, StrokeLineJoin = PenLineJoin.Round });
                c.Children.Add(new Path { Data = g, Fill = Brosse(couleur) });
                var m = new Mot { Ui = c, S = new ScaleTransform(0.1, 0.1), T = new TranslateTransform(x, y), X = Borne(x, 60 + ft.Width / 2, W - 60 - ft.Width / 2), Y = Math.Max(70, y) };
                var tg = new TransformGroup(); tg.Children.Add(m.S); tg.Children.Add(m.T);
                c.RenderTransform = tg;
                _effets.Children.Add(c);
                _mots.Add(m);
            }
            catch { }
        }

        private Sp Ajoute(string dessin, double x, double y, double e = 1, params string[] couleurs) => Pose(_decor, dessin, x, y, e, couleurs);

        private void GlisseAvant(Sp repere, UIElement e)
        {
            int i = _decor.Children.IndexOf(repere.Ui);
            if (i < 0) _decor.Children.Add(e); else _decor.Children.Insert(i, e);
        }

        private void GlisseApres(Sp repere, UIElement e)
        {
            int i = _decor.Children.IndexOf(repere.Ui);
            if (i < 0) _decor.Children.Add(e); else _decor.Children.Insert(i + 1, e);
        }

        // ------------------------------------------------------------------
        //  D'une scène à l'autre
        // ------------------------------------------------------------------
        private void Ouvre(int numero)
        {
            _numero = ((numero % _scenes.Count) + _scenes.Count) % _scenes.Count;
            _scene = _scenes[_numero];
            _decor.Children.Clear();
            foreach (var b in new[] { _gouttes, _gadoue, _bulles, _vapeur, _fumee, _etoiles, _coeurs }) b.Vide();

            var objets = new Dictionary<string, Sp>();
            string sol = "#9ADB7C";
            foreach (var p in JetArt.Scenes[_scene.Cle])
            {
                var couleurs = _scene.Couleurs(p.Nom) ?? p.C;
                var s = new Sp(p.Dessin, couleurs).En(p.X, p.Y, p.E, p.R);
                _decor.Children.Add(s.Ui);
                objets[p.Nom] = s;
                if (p.Nom == "sol" && p.C != null) sol = Choisie("$sol", p.C);
            }
            var ciel = JetArt.Ciels[_scene.Cle];
            _cielHaut.Color = Couleur(ciel[0]); _cielBas.Color = Couleur(ciel[1]);
            _solHaut.Color = _solBas.Color = Couleur(sol);
            _ciel.Visibility = ciel[2] == "jour" ? Visibility.Visible : Visibility.Collapsed;

            _scene.Prepare(objets);
            for (int i = 0; i < _points.Children.Count; i++)
                ((Ellipse)_points.Children[i]).Fill = i < _numero ? Brosse("#FFE066") : i == _numero ? Brushes.White : Brushes.Transparent;

            _etat = Etat.Entree; _depuis = 0; _sansProgres = 0; _indice = 0;
            _decor.Opacity = 0;
            Speech.Say(_scene.Consigne);
        }

        // ------------------------------------------------------------------
        //  Une image du jeu
        // ------------------------------------------------------------------
        private void Tic()
        {
            double maintenant = _montre.Elapsed.TotalSeconds;
            double dt = Math.Min(0.1, maintenant - _dernier);
            _dernier = maintenant;
            if (dt <= 0) return;
            bool pause = GameControl.Paused;      // une surcouche (Parler, écoute) est ouverte : tout attend
            if (_son != null) { try { _son.Volume = pause || SoundFx.Hold ? 0 : _pression * 0.5; } catch { } }
            if (pause) return;
            _temps += dt;

            // --- Le regard : l'eau coule tant qu'il est sur la scène ---
            // Sans moteur de regard (souris ou doigt seuls), l'eau suit le pointeur tant qu'il est sur le jeu, même immobile.
            if (_temps - _flux > 1 && _racine.IsMouseOver)
            {
                try { _regard = System.Windows.Input.Mouse.GetPosition(_monde); _vu = _temps; } catch { }
            }
            bool regarde = _temps - _vu < 0.4 && _regard.X > -60 && _regard.X < W + 60 && _regard.Y > -60 && _regard.Y < H + 60;
            _pression = Borne(_pression + (regarde ? dt * 4 : -dt * 5));
            double suit = Math.Min(1, dt * 11);
            var vise = new Point(Borne(_regard.X, 30, W - 30), Borne(_regard.Y, 40, 770));
            _cible = new Point(_cible.X + (vise.X - _cible.X) * suit, _cible.Y + (vise.Y - _cible.Y) * suit);

            // --- La lance et le jet ---
            double dx = _cible.X - Pivot.X, dy = _cible.Y - Pivot.Y, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            double ux = dx / d, uy = dy / d;
            _lance.R = Math.Atan2(ux, -uy) * 180 / Math.PI;
            _pupille.X = Oeil.X + ux * 10; _pupille.Y = Oeil.Y + uy * 10;
            var bout = new Point(Pivot.X + ux * LongueurLance, Pivot.Y + uy * LongueurLance);
            double portee = Distance(_cible, bout.X, bout.Y);
            var sommet = new Point((bout.X + _cible.X) / 2 + ux * 20, (bout.Y + _cible.Y) / 2 - (40 + portee * 0.22));
            bool coule = _pression > 0.03 && portee > 30;
            var vis = coule ? Visibility.Visible : Visibility.Collapsed;
            if (_jetBord.Visibility != vis) { _jetBord.Visibility = vis; _jetCorps.Visibility = vis; _jetReflets.Visibility = vis; }
            if (coule)
            {
                // Le jet s'allonge à mesure que la pression monte : on ne trace que le début de la courbe.
                double p = _pression;
                var c1 = new Point(bout.X + (sommet.X - bout.X) * p, bout.Y + (sommet.Y - bout.Y) * p);
                var c2 = new Point(sommet.X + (_cible.X - sommet.X) * p, sommet.Y + (_cible.Y - sommet.Y) * p);
                var fin = new Point(c1.X + (c2.X - c1.X) * p, c1.Y + (c2.Y - c1.Y) * p);
                var g = new StreamGeometry();
                using (var ctx = g.Open())
                {
                    ctx.BeginFigure(bout, false, false);
                    ctx.QuadraticBezierTo(c1, fin, true, true);
                }
                g.Freeze();
                _jetBord.Data = g; _jetCorps.Data = g; _jetReflets.Data = g;
                _jetReflets.StrokeDashOffset -= dt * 16;
            }
            bool touche = coule && _pression > 0.9;
            if (touche)
            {
                for (int i = 0; i < 2; i++)
                    _gouttes.Lance(_cible.X + Entre(-8, 8), _cible.Y, Entre(-170, 170), Entre(-300, -90), 900, Entre(0.35, 0.6), Entre(0.6, 1.1));
            }

            // --- Le ciel ---
            _rayons.R += dt * 20;
            foreach (var n in _nuages) { n.X += dt * 9; if (n.X > W + 160) n.X = -160; }

            // --- La scène ---
            _depuis += dt;
            if (_scene != null)
            {
                _scene.T += dt;
                switch (_etat)
                {
                    case Etat.Entree:
                        _decor.Opacity = Borne(_depuis / 0.5);
                        if (_depuis >= 0.5) { _decor.Opacity = 1; _etat = Etat.Joue; _depuis = 0; }
                        break;
                    case Etat.Joue:
                        if (touche) _scene.Arrose(_cible, dt);
                        if (_scene.Finie)
                        {
                            _etat = Etat.Fete; _depuis = 0; _indice = 0;
                            _reussites++;
                            RewardStore.Add();
                            Speech.Say(_scene.Bravo);
                            try { _fete?.Invoke(); } catch { }
                            ((Ellipse)_points.Children[_numero]).Fill = Brosse("#FFE066");
                        }
                        else
                        {
                            // Le coup de pouce : rien n'avance depuis un moment → la consigne est redite, ce qui reste à faire remue.
                            _sansProgres += dt;
                            if (_indice > 0) _indice -= dt;
                            if (_sansProgres > 12 && !Speech.IsBusy) { _sansProgres = 0; _indice = 3; Speech.Say(_scene.Consigne); }
                        }
                        break;
                    case Etat.Fete:
                        _scene.Fete(_depuis, dt);
                        if (_depuis >= _scene.DureeFete) { _etat = Etat.Sortie; _depuis = 0; }
                        break;
                    case Etat.Sortie:
                        _scene.Fete(_scene.DureeFete + _depuis, dt);
                        _decor.Opacity = 1 - Borne(_depuis / 0.5);
                        if (_depuis >= 0.5) { Ouvre(_numero + 1); return; }
                        break;
                }
                _scene.Anime(dt);
            }

            foreach (var b in new[] { _gouttes, _gadoue, _bulles, _vapeur, _fumee, _etoiles, _coeurs }) b.Avance(dt);

            // --- Les mots de bande dessinée ---
            for (int i = _mots.Count - 1; i >= 0; i--)
            {
                var m = _mots[i];
                m.Age += dt;
                if (m.Age > 1.3) { _effets.Children.Remove(m.Ui); _mots.RemoveAt(i); continue; }
                double k = Rebond(m.Age / 0.45);
                m.S.ScaleX = m.S.ScaleY = k;
                m.T.X = m.X; m.T.Y = m.Y - 46 * Sortant(m.Age / 1.3);
                m.Ui.Opacity = Borne((1.3 - m.Age) / 0.35);
            }
        }

        // ------------------------------------------------------------------
        //  Les scènes
        // ------------------------------------------------------------------
        private abstract class Scene
        {
            protected readonly WaterJetGame J;
            protected Dictionary<string, Sp> O;
            /// <summary>Secondes depuis l'ouverture de la scène.</summary>
            public double T;
            protected Scene(WaterJetGame jeu) { J = jeu; }

            public abstract string Cle { get; }
            public abstract string Consigne { get; }
            public abstract string Bravo { get; }
            public abstract bool Finie { get; }
            public virtual double DureeFete => 3.4;
            /// <summary>Des couleurs choisies par la scène pour un de ses objets (sinon celles du dessin).</summary>
            public virtual string[] Couleurs(string nom) => null;

            public void Prepare(Dictionary<string, Sp> objets) { O = objets; T = 0; Construit(); }
            protected abstract void Construit();
            /// <summary>L'eau touche le point p pendant dt secondes.</summary>
            public abstract void Arrose(Point p, double dt);
            /// <summary>À chaque image, quel que soit l'état.</summary>
            public virtual void Anime(double dt) { }
            /// <summary>La scène est réussie depuis u secondes.</summary>
            public virtual void Fete(double u, double dt) { }
            /// <summary>Vrai pendant l'image où u passe l'instant t (pour déclencher une chose une seule fois).</summary>
            protected static bool Passe(double u, double dt, double t) => u >= t && u - dt < t;
            protected double Remue => J._indice > 0 ? Math.Sin(J._temps * 22) : 0;
        }

        // ----- 1. La voiture pleine de boue -----
        private sealed class SceneVoiture : Scene
        {
            private static readonly string[] Teintes = { "#F0483E", "#5CC8FF", "#FFB347", "#6EDC8C", "#B58BFF", "#FF8FC1" };
            private static int _tour = -1;
            private sealed class Tache { public Sp S; public double V = 1, E, R0; }
            private readonly List<Tache> _taches = new List<Tache>();
            private Sp _auto, _roue1, _roue2, _brille;
            private double _x0, _y0, _x1, _x2;

            public SceneVoiture(WaterJetGame j) : base(j) { }
            public override string Cle => "voiture";
            public override string Consigne => "La voiture est pleine de boue ! Arrose-la pour la laver !";
            public override string Bravo => "Bravo ! La voiture est toute propre !";
            public override bool Finie => _taches.All(t => t.V <= 0);
            public override double DureeFete => 3.8;
            public override string[] Couleurs(string nom) => nom == "voiture" ? new[] { "corps", Teintes[(_tour + 1) % Teintes.Length] } : null;

            protected override void Construit()
            {
                _tour++;
                _auto = O["voiture"]; _roue1 = O["roue1"]; _roue2 = O["roue2"];
                _x0 = _auto.X; _y0 = _auto.Y; _x1 = _roue1.X; _x2 = _roue2.X;
                _taches.Clear();
                for (int i = 0; O.ContainsKey("boue" + i); i++)
                {
                    var s = O["boue" + i];
                    _taches.Add(new Tache { S = s, E = s.E, R0 = s.R });
                }
                _brille = J.Ajoute("brille", _x0, _y0);
                _brille.Op = 0;
            }

            public override void Arrose(Point p, double dt)
            {
                bool lave = false;
                foreach (var t in _taches)
                {
                    if (t.V <= 0) continue;
                    if (Distance(p, t.S.X, t.S.Y) > 64 * t.E * (0.5 + 0.5 * t.V) + 40) continue;
                    lave = true;
                    t.V -= dt * 1.15;
                    if (J.Hasard() < 0.6) J._gadoue.Lance(t.S.X + J.Entre(-30, 30), t.S.Y + J.Entre(-10, 30), J.Entre(-90, 90), J.Entre(20, 160), 800, J.Entre(0.4, 0.7), J.Entre(0.7, 1.3));
                    if (J.Hasard() < 0.35) J._bulles.Lance(t.S.X + J.Entre(-40, 40), t.S.Y + J.Entre(-30, 30), J.Entre(-40, 40), J.Entre(-110, -40), -60, J.Entre(0.6, 1.1), J.Entre(0.6, 1.5));
                    if (t.V <= 0)
                    {
                        t.S.Op = 0;
                        J.Eclat(t.S.X, t.S.Y, 5, 200);
                        SoundFx.PopSound();
                        J.Reussite();
                    }
                }
                // de la mousse partout où l'eau touche la carrosserie
                if (!lave && Math.Abs(p.X - _x0) < 330 && p.Y > _y0 - 300 && p.Y < _y0 && J.Hasard() < 0.25)
                    J._bulles.Lance(p.X + J.Entre(-20, 20), p.Y, J.Entre(-40, 40), J.Entre(-110, -40), -60, J.Entre(0.5, 0.9), J.Entre(0.5, 1.1));
                if (lave) J.Progres();
            }

            public override void Anime(double dt)
            {
                foreach (var t in _taches)
                {
                    if (t.V <= 0) continue;
                    t.S.E = t.E * (0.45 + 0.55 * t.V);
                    t.S.R = t.R0 + 14 * Remue;
                }
            }

            public override void Fete(double u, double dt)
            {
                if (Passe(u, dt, 0.05)) { SoundFx.Honk(); J.Ecris("Tut tut !", _x0, _y0 - 400, "#FFB347", 84); }
                if (Passe(u, dt, 1.7)) SoundFx.Honk();
                double saut = u < 1.4 ? -16 * Math.Abs(Math.Sin(u * 10)) : 0;
                double v = Math.Max(0, u - 1.7), part = 560 * v * v + 60 * v;
                _auto.X = _x0 + part; _auto.Y = _y0 + saut;
                _roue1.X = _x1 + part; _roue2.X = _x2 + part;
                _roue1.R = _roue2.R = part / 62 * 180 / Math.PI;
                _brille.X = _x0 + part; _brille.Y = _y0 + saut; _brille.Op = u < 1.7 ? 0.55 + 0.45 * Math.Sin(u * 12) : 1;
                if (J.Hasard() < dt * 9 && u < 2.4)
                    J._etoiles.Lance(_x0 + part + J.Entre(-300, 300), _y0 - J.Entre(60, 300), 0, -30, 0, 0.5, J.Entre(0.8, 1.6), 0, 200);
            }
        }

        // ----- 2. Les pompiers -----
        private sealed class SceneFeu : Scene
        {
            private sealed class Feu { public Sp S; public double V = 1, E, X, Y; public int I; }
            private readonly List<Feu> _feux = new List<Feu>();
            private Sp _chat;
            private double _son;

            public SceneFeu(WaterJetGame j) : base(j) { }
            public override string Cle => "feu";
            public override string Consigne => "Au feu ! Éteins toutes les flammes avec ton jet d'eau !";
            public override string Bravo => "Bravo ! Le feu est éteint ! Le chat est sauvé !";
            public override bool Finie => _feux.All(f => f.V <= 0);

            protected override void Construit()
            {
                _feux.Clear();
                for (int i = 0; O.ContainsKey("flamme" + i); i++)
                {
                    var s = O["flamme" + i];
                    _feux.Add(new Feu { S = s, E = s.E, X = s.X, Y = s.Y, I = i });
                }
                _chat = O["chat"];
                _chat.Op = 0;
                _son = 0;
            }

            public override void Arrose(Point p, double dt)
            {
                bool mouille = false;
                _son -= dt;
                foreach (var f in _feux)
                {
                    if (f.V <= 0) continue;
                    if (Distance(p, f.X, f.Y - 80 * f.E) > 84 * f.E + 44) continue;
                    mouille = true;
                    f.V -= dt * 0.9;
                    if (J.Hasard() < 0.5) J._vapeur.Lance(p.X + J.Entre(-30, 30), p.Y + J.Entre(-10, 20), J.Entre(-50, 50), J.Entre(-130, -70), 0, J.Entre(0.6, 1), J.Entre(0.6, 1), 1.4);
                    if (_son <= 0) { SoundFx.Sizzle(); _son = 0.6; }
                    if (f.V <= 0)
                    {
                        f.S.Op = 0;
                        for (int k = 0; k < 7; k++) J._vapeur.Lance(f.X + J.Entre(-30, 30), f.Y - J.Entre(10, 90) * f.E, J.Entre(-90, 90), J.Entre(-170, -60), 0, J.Entre(0.8, 1.3), J.Entre(0.9, 1.5), 1.6);
                        J.Ajoute("suie", f.X, f.Y - 6, f.E);
                        J.Ecris("Pschhh !", f.X, f.Y - 150 * f.E, "#9DC4FF", 56);
                        SoundFx.Sizzle(); _son = 0.6;
                        J.Reussite();
                    }
                }
                if (mouille) J.Progres();
            }

            public override void Anime(double dt)
            {
                foreach (var f in _feux)
                {
                    if (f.V <= 0) continue;
                    double k = 0.35 + 0.65 * Math.Max(0, f.V);
                    f.S.SX = f.E * k * (1 + 0.06 * Math.Sin(T * 17 + f.I * 2));
                    f.S.SY = f.E * k * (1 + 0.1 * Math.Sin(T * 13 + f.I));
                    f.S.R = 5 * Math.Sin(T * 7 + f.I * 1.7) + 8 * Remue;
                    if (J.Hasard() < dt * 1.3)
                        J._fumee.Lance(f.X + J.Entre(-14, 14), f.Y - 170 * f.E * k, J.Entre(-16, 16), J.Entre(-90, -60), 0, J.Entre(1.2, 1.8), J.Entre(0.6, 1), 1.3);
                }
            }

            public override void Fete(double u, double dt)
            {
                if (Passe(u, dt, 0.3)) { SoundFx.PopSound(); J.Ecris("Miaou !", _chat.X + 10, _chat.Y - 110, "#FFB347", 60); }
                _chat.Op = u > 0.3 ? 1 : 0;
                _chat.E = 0.95 * Rebond((u - 0.3) / 0.7);
                _chat.R = 8 * Math.Sin(u * 5);
                if (u > 0.5 && J.Hasard() < dt * 4)
                    J._coeurs.Lance(_chat.X + J.Entre(-40, 40), _chat.Y - 30, J.Entre(-30, 30), J.Entre(-110, -70), 0, J.Entre(1, 1.5), J.Entre(1, 1.6));
            }
        }

        // ----- 3. Remplir -----
        private sealed class SceneRemplir : Scene
        {
            private sealed class Cuve
            {
                public string Nom; public Sp Devant, Hote; public Rect B; public double X, Y, Niveau, TailleHote, Plein = -1;
                public Rectangle Bleu, Reflet;
                public double Surface => Y + B.Bottom - B.Height * Niveau;
            }
            private readonly List<Cuve> _cuves = new List<Cuve>();

            public SceneRemplir(WaterJetGame j) : base(j) { }
            public override string Cle => "remplir";
            public override string Consigne => "Les poissons et le canard n'ont plus d'eau ! Remplis tout avec ton jet d'eau !";
            public override string Bravo => "Bravo ! Tout est rempli ! Ils sont contents !";
            public override bool Finie => _cuves.All(c => c.Plein >= 0);

            protected override void Construit()
            {
                _cuves.Clear();
                Cree("aquarium", new Sp("poisson", "poisson", "#FF8A3D", "poisson2", "#F0683E"), 1);
                Cree("bocal", new Sp("poisson", "poisson", "#5CC8FF", "poisson2", "#3E9BE0"), 0.7);
                Cree("bassin", new Sp("canard"), 1.2);
            }

            private void Cree(string nom, Sp hote, double taille)
            {
                var devant = O[nom];
                var forme = Trace(JetArt.Dessins[nom + "_dedans"][0].D);
                var c = new Cuve { Nom = nom, Devant = devant, Hote = hote, B = forme.Bounds, X = devant.X, Y = devant.Y, TailleHote = taille };
                // L'eau : un rectangle bleu qui monte, découpé à la forme du récipient.
                var eau = new Canvas { Clip = forme, IsHitTestVisible = false, RenderTransform = new TranslateTransform(c.X, c.Y) };
                c.Bleu = new Rectangle { Width = c.B.Width, Height = 0, Fill = Brosse("#6FC6FF"), Opacity = 0.82 };
                c.Reflet = new Rectangle { Width = c.B.Width, Height = 7, Fill = Brosse("#DDF3FF"), Visibility = Visibility.Collapsed };
                Canvas.SetLeft(c.Bleu, c.B.Left); Canvas.SetLeft(c.Reflet, c.B.Left);
                eau.Children.Add(c.Bleu); eau.Children.Add(c.Reflet);
                J.GlisseAvant(devant, eau);
                J.GlisseAvant(devant, hote.Ui);
                hote.En(c.X, c.Y - 40, taille);
                _cuves.Add(c);
            }

            public override void Arrose(Point p, double dt)
            {
                foreach (var c in _cuves)
                {
                    if (c.Plein >= 0) continue;
                    if (p.X < c.X + c.B.Left - 26 || p.X > c.X + c.B.Right + 26 || p.Y < c.Y + c.B.Top - 120 || p.Y > c.Y + c.B.Bottom + 14) continue;
                    c.Niveau = Math.Min(1, c.Niveau + dt * 0.34);
                    J.Progres();
                    if (J.Hasard() < 0.4) J._bulles.Lance(c.X + J.Entre(c.B.Left + 20, c.B.Right - 20), c.Surface + J.Entre(10, 50), J.Entre(-10, 10), J.Entre(-70, -40), 0, J.Entre(0.4, 0.8), J.Entre(0.4, 0.9));
                    if (c.Niveau >= 1)
                    {
                        c.Plein = T;
                        SoundFx.Plouf();
                        J.Ecris(c.Nom == "bassin" ? "Coin coin !" : "Plouf !", c.X, c.Y + c.B.Top - 70, c.Nom == "bassin" ? "#FFB347" : "#5CC8FF", 60);
                        J.Eclat(c.X, c.Y + c.B.Top - 10, 8);
                        J.Reussite();
                    }
                }
            }

            public override void Anime(double dt)
            {
                for (int i = 0; i < _cuves.Count; i++)
                {
                    var c = _cuves[i];
                    double h = c.B.Height * c.Niveau;
                    c.Bleu.Height = h; Canvas.SetTop(c.Bleu, c.B.Bottom - h);
                    Canvas.SetTop(c.Reflet, c.B.Bottom - h);
                    c.Reflet.Visibility = h > 8 ? Visibility.Visible : Visibility.Collapsed;
                    c.Devant.R = c.Plein < 0 ? 1.6 * Remue : 0;

                    double e = c.TailleHote, fond = c.Y + c.B.Bottom - (c.Nom == "bocal" ? 44 : 40) - 32 * e;
                    if (c.Nom == "bassin")
                    {
                        // le canard flotte : il monte avec l'eau, puis tourne sur lui-même quand c'est plein
                        c.Hote.X = c.X + 40 * Math.Sin(T * 0.9) * c.Niveau;
                        c.Hote.Y = Math.Min(c.Y + c.B.Bottom - 18, c.Surface - 4) + 3 * Math.Sin(T * 3.1);
                        c.Hote.R = 6 * Math.Sin(T * 3.1) * Borne(c.Niveau * 4);
                        bool tourne = c.Plein >= 0 && T - c.Plein < 2.2;
                        c.Hote.SX = e * (tourne ? Math.Cos((T - c.Plein) * 9) : 1); c.Hote.SY = e;
                    }
                    else if (c.Niveau < 0.3)
                    {
                        // pas assez d'eau : le poisson gigote sur le sable
                        c.Hote.X = c.X; c.Hote.Y = fond - 4 - 10 * Math.Abs(Math.Sin(T * 7 + i));
                        c.Hote.R = 14 * Math.Sin(T * 14 + i); c.Hote.SX = e; c.Hote.SY = e;
                    }
                    else
                    {
                        // il nage, d'un bord à l'autre, entre le sable et la surface
                        double large = Math.Max(10, c.B.Width / 2 - 86 * e), a = T * 1.5 + i * 2;
                        double saut = c.Plein >= 0 && T - c.Plein < 1.2 ? -150 * e * Math.Sin(Math.PI * (T - c.Plein) / 1.2) : 0;
                        c.Hote.X = c.X + large * Math.Sin(a);
                        c.Hote.Y = (c.Surface + fond) / 2 + 10 * Math.Sin(T * 2.3 + i) + saut;
                        c.Hote.R = saut < 0 ? -30 * Math.Cos(Math.PI * (T - c.Plein) / 1.2) * Math.Sign(Math.Cos(a)) : 0;
                        c.Hote.SX = e * (Math.Cos(a) >= 0 ? 1 : -1); c.Hote.SY = e;
                    }
                }
            }

            public override void Fete(double u, double dt)
            {
                foreach (var c in _cuves)
                    if (J.Hasard() < dt * 2.2)
                        J._coeurs.Lance(c.X + J.Entre(-60, 60), c.Y + c.B.Top - 20, J.Entre(-20, 20), J.Entre(-110, -70), 0, J.Entre(1, 1.5), J.Entre(1, 1.6));
            }
        }

        // ----- 4. Le jardin -----
        private sealed class SceneJardin : Scene
        {
            private static readonly string[] Petales = { "#FF8FC1", "#B58BFF", "#FF4F7B", "#5CC8FF", "#FFB347" };
            private sealed class Plante { public Sp Pot, Tige, F1, F2, Bouton, Fleur, Papillon; public double X, Y, V, Eclose = -1, HautX, HautY; public int I; }
            private readonly List<Plante> _plantes = new List<Plante>();
            private Sp _arc;

            public SceneJardin(WaterJetGame j) : base(j) { }
            public override string Cle => "jardin";
            public override string Consigne => "Les fleurs ont soif ! Arrose les pots pour les faire pousser !";
            public override string Bravo => "Bravo ! Quel beau jardin ! Regarde l'arc-en-ciel !";
            public override bool Finie => _plantes.All(p => p.Eclose >= 0);
            public override double DureeFete => 4.2;

            protected override void Construit()
            {
                _plantes.Clear();
                _arc = new Sp("arcenciel").En(800, 706);
                _arc.Op = 0;
                J.GlisseApres(O["collines"], _arc.Ui);
                for (int i = 0; O.ContainsKey("pot" + i); i++)
                {
                    var pot = O["pot" + i];
                    string petale = Petales[i % Petales.Length], aile = Petales[(i + 2) % Petales.Length];
                    var p = new Plante { Pot = pot, X = pot.X, Y = pot.Y - 98, I = i,
                        Tige = new Sp("tige"), F1 = new Sp("feuille"), F2 = new Sp("feuille"),
                        Bouton = new Sp("bouton", "petale", petale), Fleur = new Sp("fleur", "petale", petale), Papillon = new Sp("papillon", "aile", aile) };
                    foreach (var s in new[] { p.Tige, p.F1, p.F2, p.Bouton, p.Fleur }) J.GlisseAvant(pot, s.Ui);
                    p.Fleur.Op = 0; p.Papillon.Op = 0;
                    J._decor.Children.Add(p.Papillon.Ui);
                    _plantes.Add(p);
                }
            }

            public override void Arrose(Point pt, double dt)
            {
                foreach (var p in _plantes)
                {
                    if (p.Eclose >= 0) continue;
                    if (Math.Abs(pt.X - p.X) > 118 || pt.Y < p.Y - 260 || pt.Y > p.Y + 120) continue;
                    p.V = Math.Min(1, p.V + dt * 0.4);
                    J.Progres();
                    if (p.V >= 1)
                    {
                        p.Eclose = T;
                        SoundFx.PlayNote(p.I * 2 % 8);
                        J.Eclat(p.HautX, p.HautY, 8);
                        J.Reussite();
                    }
                }
            }

            public override void Anime(double dt)
            {
                bool fete = Finie;
                foreach (var p in _plantes)
                {
                    double k = p.V, long_ = 0.18 + 0.82 * Borne(k / 0.75);
                    double penche = 40 * (1 - Borne(k * 2.2)) + (fete ? 9 : 3) * Math.Sin(T * (fete ? 4 : 2) + p.I) + 6 * (p.Eclose < 0 ? Remue : 0);
                    double a = penche * Math.PI / 180, haut = 176 * long_;
                    p.Tige.X = p.X; p.Tige.Y = p.Y; p.Tige.SX = 1; p.Tige.SY = long_; p.Tige.R = penche;
                    p.HautX = p.X + haut * Math.Sin(a); p.HautY = p.Y - haut * Math.Cos(a);
                    double e1 = Sortant((k - 0.25) / 0.15), e2 = Sortant((k - 0.42) / 0.15);
                    p.F1.X = p.X + 0.34 * haut * Math.Sin(a); p.F1.Y = p.Y - 0.34 * haut * Math.Cos(a); p.F1.E = e1; p.F1.R = penche - 12; p.F1.Op = e1 > 0 ? 1 : 0;
                    p.F2.X = p.X + 0.55 * haut * Math.Sin(a); p.F2.Y = p.Y - 0.55 * haut * Math.Cos(a); p.F2.SX = -e2; p.F2.SY = e2; p.F2.R = penche + 12; p.F2.Op = e2 > 0 ? 1 : 0;
                    double eb = p.Eclose >= 0 ? 0 : Sortant((k - 0.5) / 0.2);
                    p.Bouton.X = p.HautX; p.Bouton.Y = p.HautY; p.Bouton.E = eb; p.Bouton.R = penche; p.Bouton.Op = eb > 0 ? 1 : 0;
                    if (p.Eclose >= 0)
                    {
                        double age = T - p.Eclose;
                        p.Fleur.Op = 1; p.Fleur.X = p.HautX; p.Fleur.Y = p.HautY - 14;
                        p.Fleur.E = Rebond(age / 0.7) * (1 + 0.04 * Math.Sin(T * 3 + p.I)); p.Fleur.R = penche + 6 * Math.Sin(T * 2.2 + p.I);
                        // un papillon arrive et tourne autour de la fleur
                        p.Papillon.Op = Borne((age - 0.4) / 0.5);
                        p.Papillon.X = p.HautX + 74 * Math.Sin(T * 1.7 + p.I * 2); p.Papillon.Y = p.HautY - 96 + 34 * Math.Sin(T * 2.9 + p.I);
                        p.Papillon.SX = 0.8 * Math.Max(0.15, Math.Abs(Math.Sin(T * 15 + p.I))); p.Papillon.SY = 0.8;
                        p.Papillon.R = 14 * Math.Sin(T * 1.7 + p.I * 2);
                    }
                }
            }

            public override void Fete(double u, double dt)
            {
                _arc.Op = Borne(u / 1.4);
                _arc.E = 0.9 + 0.1 * Sortant(u / 1.4);
                for (int i = 0; i < 5; i++)
                    if (Passe(u, dt, 0.3 + i * 0.22)) SoundFx.PlayNote(i * 2 > 7 ? 7 : i * 2);
            }
        }

        // ----- 5. La roue à eau -----
        private sealed class SceneRoue : Scene
        {
            private static readonly string[] Verres = { "#FFE066", "#FF8FC1", "#6EDC8C", "#5CC8FF", "#FFB347", "#B58BFF", "#FF4F7B", "#FFE066" };
            private static readonly string[] Robes = { "#FFFFFF", "#FFD0E0", "#CFEFFF" };
            private Sp _roue;
            private readonly Sp[] _eteinte = new Sp[8], _allumee = new Sp[8], _halo = new Sp[8], _chevaux = new Sp[3];
            private readonly Point[] _place = new Point[8];
            private double _vitesse, _angle, _tours, _manege;
            private int _lumieres;

            public SceneRoue(WaterJetGame j) : base(j) { }
            public override string Cle => "roue";
            public override string Consigne => "Arrose la grande roue pour la faire tourner et allumer les lumières !";
            public override string Bravo => "Bravo ! Toute la fête est allumée !";
            public override bool Finie => _lumieres >= 8;
            public override double DureeFete => 5;

            protected override void Construit()
            {
                _roue = O["roue"];
                _vitesse = 0; _angle = 0; _tours = 0; _manege = 0; _lumieres = 0;
                for (int i = 0; i < 8; i++)
                {
                    // les ampoules pendent le long de la guirlande (la même courbe que le fil)
                    double t = (i + 0.5) / 8, x = (1 - t) * (1 - t) * 90 + 2 * t * (1 - t) * 800 + t * t * 1510, y = (1 - t) * (1 - t) * 96 + 2 * t * (1 - t) * 250 + t * t * 96 + 26;
                    _place[i] = new Point(x, y);
                    _halo[i] = J.Ajoute("halo", x, y); _halo[i].Op = 0;
                    _eteinte[i] = J.Ajoute("ampoule", x, y, 1, "verre", "#E3E8F4");
                    _allumee[i] = J.Ajoute("ampoule", x, y, 1, "verre", Verres[i]); _allumee[i].Op = 0;
                }
                for (int i = 0; i < 3; i++)
                {
                    _chevaux[i] = new Sp("cheval", "robe", Robes[i]);
                    J.GlisseAvant(O["manege_toit"], _chevaux[i].Ui);
                }
            }

            public override void Arrose(Point p, double dt)
            {
                if (Distance(p, _roue.X, _roue.Y) > 266) return;
                _vitesse = Math.Min(330, _vitesse + dt * 270);
                J.Progres();
                if (J.Hasard() < 0.5)
                {
                    // l'eau gicle de la roue dans le sens où elle tourne
                    double a = J.Entre(0, Math.PI * 2);
                    J._gouttes.Lance(_roue.X + Math.Cos(a) * 230, _roue.Y + Math.Sin(a) * 230, -Math.Sin(a) * 260, Math.Cos(a) * 260 - 80, 700, J.Entre(0.4, 0.7), J.Entre(0.7, 1.2));
                }
            }

            public override void Anime(double dt)
            {
                bool fete = Finie;
                if (fete) _vitesse = Math.Max(_vitesse, 240);      // la fête tourne toute seule
                else _vitesse -= _vitesse * dt * 0.45;
                _angle += _vitesse * dt; _tours += _vitesse * dt / 360;
                _roue.R = _angle;
                _roue.E = 1 + 0.03 * Remue;

                while (_lumieres < Math.Min(8, (int)_tours))
                {
                    int i = _lumieres++;
                    _allumee[i].Op = 1; _eteinte[i].Op = 0; _halo[i].Op = 1;
                    SoundFx.PlayNote(i);
                    J.Eclat(_place[i].X, _place[i].Y + 8, 6, 180);
                    J.Reussite();
                }
                for (int i = 0; i < _lumieres; i++)
                {
                    _halo[i].E = 1 + 0.14 * Math.Sin(T * 6 + i);
                    if (fete) _allumee[i].Op = 0.55 + 0.45 * Math.Sin(T * 12 + i * 1.3);
                }

                // le manège tourne avec la roue : les chevaux passent devant, puis derrière le mât
                _manege += _vitesse * dt * 0.55;
                for (int i = 0; i < 3; i++)
                {
                    double a = (_manege + i * 120) * Math.PI / 180, c = Math.Cos(a), e = 0.85 + 0.15 * c;
                    var s = _chevaux[i];
                    s.X = 1180 + 150 * Math.Sin(a); s.Y = 560 + 22 * Math.Sin(a * 2 + i);
                    s.SX = e * (c >= 0 ? 1 : -1); s.SY = e;
                    s.Op = c < -0.2 ? 0.5 : 1;
                }
            }

            public override void Fete(double u, double dt)
            {
                // un petit feu d'artifice d'étoiles dans le ciel du soir
                if (J.Hasard() < dt * 3.2)
                {
                    J.Eclat(J.Entre(200, 1400), J.Entre(140, 400), 10, 300);
                    if (J.Hasard() < 0.5) SoundFx.PopSound();
                }
            }
        }
    }
}
