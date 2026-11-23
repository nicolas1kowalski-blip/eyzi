using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    /// L'affichage d'un jeu « dessiné » dans l'application. Le jeu lui-même est un VecMoteur, qui ne connaît
    /// pas l'écran ; la toile fait trois choses : lui donner le regard (ou le pointeur de la souris, le doigt),
    /// dessiner ses objets avec WPF à chaque image, et jouer la voix et les bruits qu'il demande.
    /// </summary>
    internal sealed class VecToile
    {
        private readonly UserControl _hote;
        private readonly string _nom;
        private readonly Func<string> _bruitDeFond;
        private readonly Action _fete;
        private readonly VecMoteur _moteur;
        /// <summary>Appelé à chaque fête du moteur, après l'étoile et les confettis (ex. pour noter le niveau).</summary>
        public Action ApresBravo;
        private readonly Grid _racine = new Grid();
        private readonly Canvas _monde = new Canvas { Width = VecMoteur.W, Height = VecMoteur.H };
        private readonly Canvas[] _calques;
        private readonly Canvas _boutons = new Canvas();      // les zones du moteur : des boutons invisibles, pour le regard
        private int _zonesVues = -1;
        private readonly int[] _etats;
        private readonly Dictionary<VecObjet, Vue> _vues = new Dictionary<VecObjet, Vue>();
        private readonly GradientStop _cielHaut = new GradientStop(Colors.LightSkyBlue, 0), _cielBas = new GradientStop(Colors.White, 0.56),
            _solHaut = new GradientStop(Colors.Green, 0.56), _solBas = new GradientStop(Colors.Green, 1);
        private string _fondVu = "";
        private readonly StackPanel _points = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 0, 0), IsHitTestVisible = false };
        private int _numeroVu = -1;
        private readonly DispatcherTimer _tic = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        private readonly Stopwatch _montre = new Stopwatch();
        private double _dernier, _horloge, _flux = -10;
        private MediaPlayer _son;
        private DateTime _debut = DateTime.Now;

        /// <param name="hote">le jeu (un UserControl) dont la toile devient le contenu</param>
        /// <param name="nom">le nom du jeu pour les statistiques (ex. « WaterJetGame »)</param>
        /// <param name="bruitDeFond">le fichier d'un bruit à jouer en boucle, au volume demandé par le moteur (ou rien)</param>
        public VecToile(UserControl hote, VecMoteur moteur, Action celebrate, string nom, Func<string> bruitDeFond = null)
        {
            _hote = hote; _moteur = moteur; _fete = celebrate; _nom = nom; _bruitDeFond = bruitDeFond;
            _calques = new Canvas[moteur.Calques.Length];
            _etats = new int[moteur.Calques.Length];

            // Le fond de la page : le ciel, puis la couleur du sol sous l'horizon — les bandes autour de la
            // scène (écran plus large ou plus haut) prolongent ainsi le décor.
            var fond = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            fond.GradientStops.Add(_cielHaut); fond.GradientStops.Add(_cielBas); fond.GradientStops.Add(_solHaut); fond.GradientStops.Add(_solBas);
            _racine.Background = fond;
            _racine.SizeChanged += (s, e) => PlaceHorizon();

            for (int i = 0; i < _calques.Length; i++)
            {
                _calques[i] = new Canvas { IsHitTestVisible = false };
                _etats[i] = -1;
                _monde.Children.Add(_calques[i]);
            }
            _monde.Children.Add(_boutons);
            _racine.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = _monde });
            _racine.Children.Add(_points);
            _hote.Content = _racine;

            // Ce que le moteur demande : la voix, les bruits, la fête de fin de scène.
            _moteur.Dit = s => Speech.Say(s);
            _moteur.VoixOccupee = () => Speech.IsBusy;
            _moteur.Son = (nom, n) =>
            {
                switch (nom)
                {
                    case "klaxon": SoundFx.Honk(); break;
                    case "splat": SoundFx.Splat(); break;
                    case "pof": SoundFx.Puff(); break;
                    case "pschhh": SoundFx.Sizzle(); break;
                    case "plouf": SoundFx.Plouf(); break;
                    case "pouet": SoundFx.Honk(); break;
                    case "tambour": SoundFx.Drum(); break;
                    case "boum": SoundFx.Boom(); break;
                    case "sifflet": SoundFx.Whistle(); break;
                    case "clic": SoundFx.Shutter(); break;
                    case "note": SoundFx.PlayNote(n); break;
                    default: SoundFx.PopSound(); break;
                }
            };
            _moteur.Bravo = () => { RewardStore.Add(); try { _fete?.Invoke(); } catch { } try { ApresBravo?.Invoke(); } catch { } };

            _tic.Tick += (s, e) => Tic();
            _hote.Loaded += (s, e) =>
            {
                GazeGate.PushTargetsOnly();      // le regard n'actionne que le bouton retour
                GazeFeed.Sample += SurRegard;
                Usage.GameStart(_nom);
                _debut = DateTime.Now;
                DemarreSon();
                _montre.Restart(); _dernier = 0;
                _moteur.Demarre();
                _tic.Start();
            };
            _hote.Unloaded += (s, e) =>
            {
                _tic.Stop();
                GazeFeed.Sample -= SurRegard;
                GazeGate.PopTargetsOnly();
                try { _son?.Stop(); _son?.Close(); } catch { }
                _son = null;
                GazeStats.RecordGame(_nom, (DateTime.Now - _debut).TotalSeconds, _moteur.Reussites);
                Usage.GameStop();
            };
        }

        private void PlaceHorizon()
        {
            double w = _racine.ActualWidth, h = _racine.ActualHeight;
            if (w < 10 || h < 10) return;
            double k = Math.Min(w / VecMoteur.W, h / VecMoteur.H), haut = (h - VecMoteur.H * k) / 2;
            double o = Math.Max(0.05, Math.Min(0.98, (haut + _moteur.Horizon * k) / h));
            _cielBas.Offset = o; _solHaut.Offset = o;
        }

        private void DemarreSon()
        {
            if (_bruitDeFond == null) return;
            try
            {
                _son = new MediaPlayer { Volume = 0 };
                var lecteur = _son;
                lecteur.MediaEnded += (s, e) => { lecteur.Position = TimeSpan.Zero; lecteur.Play(); };
                lecteur.Open(new Uri(_bruitDeFond()));
                lecteur.Play();
            }
            catch { _son = null; }
        }

        private void SurRegard(Point ecran, bool valide)
        {
            _flux = _horloge;      // le moteur du regard tourne (même si l'enfant regarde ailleurs)
            if (!valide) return;
            try { var p = _monde.PointFromScreen(ecran); _moteur.Regard(p.X, p.Y); } catch { }
        }

        private void Tic()
        {
            double maintenant = _montre.Elapsed.TotalSeconds;
            double dt = Math.Min(0.1, maintenant - _dernier);
            _dernier = maintenant;
            if (dt <= 0) return;
            bool pause = GameControl.Paused;      // une surcouche (Parler, écoute) est ouverte : tout attend
            if (_son != null) { try { _son.Volume = pause || SoundFx.Hold ? 0 : _moteur.Bruit * 0.5; } catch { } }
            if (pause) return;
            _horloge += dt;

            // Sans moteur de regard (souris ou doigt seuls), l'eau suit le pointeur tant qu'il est sur le jeu, même immobile.
            if (_horloge - _flux > 1 && _racine.IsMouseOver)
            {
                try { var p = System.Windows.Input.Mouse.GetPosition(_monde); _moteur.Regard(p.X, p.Y); } catch { }
            }

            _moteur.Avance(dt);
            Dessine();
        }

        // ------------------------------------------------------------------
        //  Le dessin : chaque objet du moteur a sa « vue » WPF, qu'on déplace à chaque image
        // ------------------------------------------------------------------
        private sealed class Vue
        {
            public FrameworkElement Ui;
            public readonly TranslateTransform T = new TranslateTransform();
            public readonly ScaleTransform S = new ScaleTransform(1, 1);
            public readonly RotateTransform R = new RotateTransform(0);
            public int Etat = -1;
            public double Decalage = double.NaN;
            public bool Visible = true;
        }

        private void Dessine()
        {
            var calques = _moteur.Calques;
            bool change = false;
            for (int i = 0; i < calques.Length; i++)
            {
                if (calques[i].Etat == _etats[i]) continue;
                _etats[i] = calques[i].Etat;
                change = true;
                _calques[i].Children.Clear();
                foreach (var o in calques[i].Objets) _calques[i].Children.Add(VueDe(o).Ui);
            }
            if (change)
            {
                // on oublie les vues des objets que le moteur a retirés
                var vivants = new HashSet<VecObjet>();
                foreach (var c in calques) foreach (var o in c.Objets) vivants.Add(o);
                var morts = new List<VecObjet>();
                foreach (var k in _vues.Keys) if (!vivants.Contains(k)) morts.Add(k);
                foreach (var k in morts) _vues.Remove(k);
            }

            foreach (var c in calques)
                foreach (var o in c.Objets)
                {
                    var v = _vues[o];
                    bool visible = o.Op > 0.003;
                    if (visible != v.Visible) { v.Visible = visible; v.Ui.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
                    if (!visible) continue;
                    if (v.T.X != o.X) v.T.X = o.X;
                    if (v.T.Y != o.Y) v.T.Y = o.Y;
                    if (v.R.Angle != o.R) v.R.Angle = o.R;
                    if (v.S.ScaleX != o.SX) v.S.ScaleX = o.SX;
                    if (v.S.ScaleY != o.SY) v.S.ScaleY = o.SY;
                    if (v.Ui.Opacity != o.Op) v.Ui.Opacity = o.Op;
                    if ((o.Genre == VecGenre.Plein || o.Genre == VecGenre.Ligne) && v.Etat != o.Etat)
                    {
                        v.Etat = o.Etat;
                        ((Path)v.Ui).Data = Contour(o.Points, o.Genre == VecGenre.Plein);
                    }
                    if (o.Genre == VecGenre.Ligne && v.Decalage != o.Decalage) { v.Decalage = o.Decalage; ((Path)v.Ui).StrokeDashOffset = o.Decalage; }
                }

            // les zones que l'enfant peut choisir : un bouton invisible sur chacune (le regard le choisit comme partout ailleurs)
            if (_moteur.Zones != null && _moteur.EtatDesZones != _zonesVues)
            {
                _zonesVues = _moteur.EtatDesZones;
                _boutons.Children.Clear();
                foreach (var z in _moteur.Zones)
                {
                    string id = z.Id;
                    var b = new Button { Width = z.L, Height = z.H, Template = BoutonInvisible, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand };
                    b.Click += (s, e) => { if (!GameControl.Paused) _moteur.Appuie(id); };
                    Canvas.SetLeft(b, z.X); Canvas.SetTop(b, z.Y);
                    _boutons.Children.Add(b);
                }
            }

            // le fond de la page et les points d'avancement
            string fond = _moteur.CielHaut + _moteur.CielBas + _moteur.Sol;
            if (fond != _fondVu)
            {
                _fondVu = fond;
                _cielHaut.Color = Couleur(_moteur.CielHaut); _cielBas.Color = Couleur(_moteur.CielBas);
                _solHaut.Color = _solBas.Color = Couleur(_moteur.Sol);
            }
            if (_moteur.Etape != _numeroVu)
            {
                _numeroVu = _moteur.Etape;
                _points.Children.Clear();
                for (int i = 0; i < _moteur.Etapes; i++)
                    _points.Children.Add(new Ellipse { Width = 18, Height = 18, Margin = new Thickness(5, 0, 5, 0), Stroke = Brushes.White, StrokeThickness = 3, Opacity = 0.9,
                        Fill = i < _numeroVu ? Brosse("#FFE066") : i == _numeroVu ? Brushes.White : Brushes.Transparent });
            }
        }

        private static readonly ControlTemplate BoutonInvisible = FaisBoutonInvisible();
        private static ControlTemplate FaisBoutonInvisible()
        {
            var bord = new FrameworkElementFactory(typeof(Border));
            bord.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            bord.SetValue(Border.CornerRadiusProperty, new CornerRadius(26));
            var t = new ControlTemplate(typeof(Button)) { VisualTree = bord };
            t.Seal();
            return t;
        }

        /// <summary>Une image de la scène telle qu'elle est dessinée (x, y, largeur, hauteur dans le repère du jeu).</summary>
        public System.Windows.Media.Imaging.BitmapSource Capture(int x, int y, int largeur, int hauteur)
        {
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)VecMoteur.W, (int)VecMoteur.H, 96, 96, PixelFormats.Pbgra32);
            var dessin = new DrawingVisual();
            using (var c = dessin.RenderOpen())
            {
                c.DrawRectangle(_racine.Background, null, new Rect(0, 0, VecMoteur.W, VecMoteur.H));
                c.DrawRectangle(new VisualBrush(_monde) { Stretch = Stretch.Fill, ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, VecMoteur.W, VecMoteur.H) }, null, new Rect(0, 0, VecMoteur.W, VecMoteur.H));
            }
            image.Render(dessin);
            var morceau = new System.Windows.Media.Imaging.CroppedBitmap(image, new Int32Rect(x, y, largeur, hauteur));
            morceau.Freeze();
            return morceau;
        }

        private Vue VueDe(VecObjet o)
        {
            if (_vues.TryGetValue(o, out var v)) return v;
            v = new Vue();
            switch (o.Genre)
            {
                case VecGenre.Dessin: v.Ui = DessineDessin(o); break;
                case VecGenre.Plein:
                    var plein = new Path { Fill = Brosse(o.Couleur) };
                    if (o.Decoupe != null) plein.Clip = Trace(_moteur.Dessins[o.Decoupe][0].D);
                    v.Ui = plein;
                    break;
                case VecGenre.Ligne:
                    var ligne = new Path { Stroke = Brosse(o.Couleur), StrokeThickness = o.Epaisseur, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
                    if (o.Tiret > 0) { ligne.StrokeDashArray = new DoubleCollection { o.Tiret, o.Espace }; ligne.StrokeDashCap = PenLineCap.Round; }
                    v.Ui = ligne;
                    break;
                default: v.Ui = DessineMot(o); break;
            }
            v.Ui.IsHitTestVisible = false;
            var g = new TransformGroup();
            g.Children.Add(v.S); g.Children.Add(v.R); g.Children.Add(v.T);
            v.Ui.RenderTransform = g;
            _vues[o] = v;
            return v;
        }

        private Canvas DessineDessin(VecObjet o)
        {
            var c = new Canvas();
            foreach (var f in _moteur.Dessins[o.Dessin])
            {
                var p = new Path { Data = Trace(f.D), StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
                if (f.G != null) p.Fill = Degrade(f.G, o.Couleurs);
                else if (f.F != null) p.Fill = Brosse(Choisie(f.F, o.Couleurs));
                if (f.S != null && f.W > 0) { p.Stroke = Brosse(Choisie(f.S, o.Couleurs)); p.StrokeThickness = f.W; }
                if (f.O < 1) p.Opacity = f.O;
                c.Children.Add(p);
            }
            return c;
        }

        /// <summary>Un mot de bande dessinée : les lettres cernées de sombre, centrées sur l'origine.</summary>
        private static Canvas DessineMot(VecObjet o)
        {
            var c = new Canvas();
            try
            {
                var ft = new FormattedText(o.Texte ?? "", CultureInfo.GetCultureInfo("fr-FR"), FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Arial Black, Arial"), FontStyles.Normal, FontWeights.Black, FontStretches.Normal), o.Taille, Brushes.Black, 1.0);
                var g = ft.BuildGeometry(new Point(-ft.Width / 2, -ft.Height / 2));
                if (g.CanFreeze) g.Freeze();
                c.Children.Add(new Path { Data = g, Stroke = Brosse("#2A1C4E"), StrokeThickness = 10, StrokeLineJoin = PenLineJoin.Round });
                c.Children.Add(new Path { Data = g, Fill = Brosse(o.Couleur) });
            }
            catch { }
            return c;
        }

        private static Geometry Contour(double[] pts, bool ferme)
        {
            var g = new StreamGeometry();
            if (pts != null && pts.Length >= 4)
                using (var ctx = g.Open())
                {
                    ctx.BeginFigure(new Point(pts[0], pts[1]), ferme, ferme);
                    for (int i = 2; i + 1 < pts.Length; i += 2) ctx.LineTo(new Point(pts[i], pts[i + 1]), true, true);
                }
            g.Freeze();
            return g;
        }

        // ------------------------------------------------------------------
        //  Tracés, couleurs et dégradés (gardés en mémoire : on ne les refait pas à chaque objet)
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

        private static Color Couleur(string c, double opacite = 1)
        {
            Color k;
            try { k = (Color)ColorConverter.ConvertFromString(c); } catch { k = Colors.Magenta; }
            if (opacite < 1) k.A = (byte)Math.Round(255 * Math.Max(0, opacite));
            return k;
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

        /// <summary>Un dégradé écrit « L;x1;y1;x2;y2|position;couleur;opacité|… » (droit) ou « R;cx;cy;r|… » (rond), dans le repère du dessin.</summary>
        private static Brush Degrade(string g, string[] couleurs)
        {
            string cle = couleurs == null ? g : g + "#" + string.Join(",", couleurs);
            if (Brosses.TryGetValue(cle, out var b)) return b;
            var inv = CultureInfo.InvariantCulture;
            var parts = g.Split('|');
            var e = parts[0].Split(';');
            GradientBrush d;
            if (e[0] == "L")
                d = new LinearGradientBrush { MappingMode = BrushMappingMode.Absolute, StartPoint = new Point(double.Parse(e[1], inv), double.Parse(e[2], inv)), EndPoint = new Point(double.Parse(e[3], inv), double.Parse(e[4], inv)) };
            else
            {
                var centre = new Point(double.Parse(e[1], inv), double.Parse(e[2], inv));
                double r = double.Parse(e[3], inv);
                d = new RadialGradientBrush { MappingMode = BrushMappingMode.Absolute, Center = centre, GradientOrigin = centre, RadiusX = r, RadiusY = r };
            }
            for (int i = 1; i < parts.Length; i++)
            {
                var a = parts[i].Split(';');
                d.GradientStops.Add(new GradientStop(Couleur(Choisie(a[1], couleurs), double.Parse(a[2], inv)), double.Parse(a[0], inv)));
            }
            d.Freeze();
            Brosses[cle] = d;
            return d;
        }
    }
}
