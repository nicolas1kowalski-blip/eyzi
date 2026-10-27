using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🪞 « Le miroir magique » : la caméra avant montre l'enfant en miroir. Son
    /// regard pose sur son visage des déguisements qui le suivent (oreilles,
    /// couronne, lunettes, nez de clown…), change le monde autour (rose, nuit
    /// étoilée, vieille photo…), déforme le miroir, fait jaillir des étincelles
    /// là où il regarde, tire une surprise au sort et prend la photo, rangée
    /// dans l'album de la famille. Cause-à-effet et plaisir de se voir : aucune
    /// consigne, aucun échec.
    /// </summary>
    public sealed class MagicMirrorGame : GameControl
    {
        // Le miroir travaille en pixels de l'image caméra (mis à l'échelle ensuite).
        private double W = 640, H = 480;

        private MirrorCamera _cam;
        private WriteableBitmap _bitmap;
        private Image _image;
        private Grid _mirrorBox;      // image + couches, à la taille de l'image
        private Border _mirrorFrame;  // le cadre (c'est lui que la photo capture)
        private Grid _worldLayer;     // le « monde » (filtre de couleur)
        private Canvas _overlay;      // les déguisements
        private Canvas _fx;           // étincelles, flash, polaroïds
        private UniformGrid _zones;   // zones invisibles : le regard sur le miroir fait jaillir des étincelles
        private TextBlock _status;    // message quand la caméra manque
        private StackPanel _album;    // les dernières photos
        private readonly List<Button> _tiles = new List<Button>();
        private readonly Random _rng = new Random();

        private readonly List<MirrorCamera.Face> _faces = new List<MirrorCamera.Face>();
        private DateTime _lastFaceAt = DateTime.MinValue;
        private DateTime _lastGreeting = DateTime.MinValue;
        private bool _greeted;
        private bool _framePending;
        private int _photos;

        private int _hat, _eyes, _nose, _world, _deform, _spark;
        private static readonly string[] HatNames = { "sans chapeau", "des oreilles de chat", "des oreilles de lapin", "une couronne de princesse", "un chapeau de fête", "un chapeau de sorcière", "une couronne d'étoiles" };
        private static readonly string[] EyeNames = { "sans lunettes", "des lunettes rondes", "des lunettes de soleil", "des lunettes étoiles", "des lunettes cœurs" };
        private static readonly string[] NoseNames = { "rien sur le nez", "un nez de clown", "une moustache", "un papillon sur le nez", "des moustaches de chat" };
        private static readonly string[] WorldNames = { "le monde normal", "le monde rose", "le monde bleu", "la nuit étoilée", "la vieille photo", "le monde arc-en-ciel" };
        private static readonly string[] DeformNames = { "le miroir normal", "tout large", "tout fin", "à l'envers", "le miroir qui tremble", "le miroir géant" };
        private static readonly string[] SparkNames = { "des étoiles", "des cœurs", "des bulles" };

        public MagicMirrorGame(Action celebrate) : base(celebrate)
        {
            GateVoice = false; // le miroir répond toujours tout de suite
            Unloaded += (s, e) => StopCamera();
        }

        protected override void NewRound()
        {
            Locked = false;
            Question.Text = "🪞 Le miroir magique";
            SetConsigne(null, null);

            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // --- Le miroir ---
            _mirrorBox = new Grid { Width = W, Height = H, RenderTransformOrigin = new Point(0.5, 0.5), ClipToBounds = true };
            _mirrorBox.Background = new LinearGradientBrush(Color.FromRgb(0x2A, 0x1A, 0x4A), Color.FromRgb(0x4A, 0x2A, 0x6A), 90);
            _image = new Image { Stretch = Stretch.Fill, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform(-1, 1) }; // en miroir
            _mirrorBox.Children.Add(_image);
            _worldLayer = new Grid { IsHitTestVisible = false };
            _mirrorBox.Children.Add(_worldLayer);
            _overlay = new Canvas { IsHitTestVisible = false };
            _mirrorBox.Children.Add(_overlay);
            _fx = new Canvas { IsHitTestVisible = false };
            _mirrorBox.Children.Add(_fx);
            _status = new TextBlock
            {
                Text = "Je cherche la caméra…", FontSize = 26, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(40), IsHitTestVisible = false,
            };
            _mirrorBox.Children.Add(_status);
            // Zones invisibles : fixer le miroir fait jaillir des étincelles à cet endroit.
            _zones = new UniformGrid { Columns = 5, Rows = 3 };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 5; c++)
                {
                    var zone = InvisibleZone();
                    int cc = c, rr = r;
                    zone.Click += (s, e) => Sparkle((cc + 0.5) * W / 5, (rr + 0.5) * H / 3);
                    _zones.Children.Add(zone);
                }
            _mirrorBox.Children.Add(_zones);

            _mirrorFrame = new Border
            {
                Background = Brushes.Black,
                CornerRadius = new CornerRadius(28),
                BorderBrush = new LinearGradientBrush(Color.FromRgb(0xFF, 0xD2, 0x4D), Color.FromRgb(0xFF, 0x8A, 0x3D), 45),
                BorderThickness = new Thickness(8),
                ClipToBounds = true,
                Margin = new Thickness(10, 6, 10, 6),
                Child = new Viewbox { Child = _mirrorBox, Stretch = Stretch.Uniform },
            };
            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.Children.Add(_mirrorFrame);
            _album = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 4), Height = 0 };
            Grid.SetRow(_album, 1);
            left.Children.Add(_album);
            root.Children.Add(left);

            // --- Les tuiles (à droite) : chaque regard fait défiler l'option suivante ---
            var rail = new StackPanel { Margin = new Thickness(4, 4, 10, 4), VerticalAlignment = VerticalAlignment.Center };
            _tiles.Clear();
            rail.Children.Add(Tile("Chapeau", Color.FromRgb(0xFF, 0x9F, 0x43), IconHat(), () => { _hat = (_hat + 1) % HatNames.Length; Redraw(); Speak(HatNames[_hat] + " !"); }));
            rail.Children.Add(Tile("Lunettes", Color.FromRgb(0x3F, 0x8E, 0xF2), IconGlasses(), () => { _eyes = (_eyes + 1) % EyeNames.Length; Redraw(); Speak(EyeNames[_eyes] + " !"); }));
            rail.Children.Add(Tile("Nez", Color.FromRgb(0xF2, 0x5F, 0x8F), IconNose(), () => { _nose = (_nose + 1) % NoseNames.Length; Redraw(); Speak(NoseNames[_nose] + " !"); }));
            rail.Children.Add(Tile("Monde", Color.FromRgb(0x2E, 0xB8, 0x8A), IconRainbow(), () => { _world = (_world + 1) % WorldNames.Length; ApplyWorld(); Speak(WorldNames[_world] + " !"); }));
            rail.Children.Add(Tile("Miroir fou", Color.FromRgb(0x7E, 0x3F, 0xF2), IconWave(), () => { _deform = (_deform + 1) % DeformNames.Length; ApplyDeform(); Speak(DeformNames[_deform] + " !"); }));
            rail.Children.Add(Tile("Étincelles", Color.FromRgb(0xE0, 0xA8, 0x00), IconStar(), () => { _spark = (_spark + 1) % SparkNames.Length; Speak(SparkNames[_spark] + " ! Regarde le miroir !"); Sparkle(W / 2, H / 2); }));
            rail.Children.Add(Tile("Surprise !", Color.FromRgb(0xFF, 0x6B, 0x6B), IconDice(), Surprise));
            rail.Children.Add(Tile("Photo", Color.FromRgb(0x3B, 0x2A, 0x5A), IconCamera(), TakePhoto));
            Grid.SetColumn(rail, 1);
            root.Children.Add(rail);

            SetBodyFill(root);
            ApplyWorld();
            ApplyDeform();
            StartCamera();
            Speak("Bienvenue dans le miroir magique ! Regarde une tuile pour te déguiser.");
        }

        // ------------------------------------------------------------------
        //  Caméra
        // ------------------------------------------------------------------
        private void StartCamera()
        {
            StopCamera();
            _cam = new MirrorCamera();
            _cam.Frame += OnFrame;
            _cam.Faces += OnFaces;
            _cam.Failed += msg => Dispatcher.BeginInvoke(new Action(() =>
            {
                _status.Text = "Je ne vois pas la caméra 😕\n" + msg + "\n\nLes mondes et les étincelles marchent quand même !";
                _status.Visibility = Visibility.Visible;
            }));
            _cam.Start();
        }

        private void StopCamera()
        {
            try { _cam?.Stop(); } catch { }
            _cam = null;
        }

        private void OnFrame(int w, int h, byte[] pixels)
        {
            if (Closed || _framePending) return;
            _framePending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _framePending = false;
                if (Closed) return;
                try
                {
                    if (_bitmap == null || _bitmap.PixelWidth != w || _bitmap.PixelHeight != h)
                    {
                        _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                        _image.Source = _bitmap;
                        W = w; H = h;
                        _mirrorBox.Width = W; _mirrorBox.Height = H;
                        _status.Visibility = Visibility.Collapsed;
                    }
                    _bitmap.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);
                }
                catch (Exception ex) { Log.Write("miroir", "Affichage : " + ex.Message); }
            }), DispatcherPriority.Render);
        }

        private void OnFaces(List<MirrorCamera.Face> faces)
        {
            if (Closed) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Closed) return;
                // L'image est en miroir : les visages aussi. Et on lisse, pour que les
                // déguisements ne tremblent pas.
                var mirrored = faces.Select(f => new MirrorCamera.Face { X = W - f.X - f.W, Y = f.Y, W = f.W, H = f.H }).OrderBy(f => f.X).ToList();
                if (mirrored.Count == _faces.Count)
                    for (int i = 0; i < _faces.Count; i++)
                    {
                        var a = _faces[i]; var b = mirrored[i];
                        mirrored[i] = new MirrorCamera.Face { X = a.X * 0.5 + b.X * 0.5, Y = a.Y * 0.5 + b.Y * 0.5, W = a.W * 0.5 + b.W * 0.5, H = a.H * 0.5 + b.H * 0.5 };
                    }
                if (mirrored.Count > 0)
                {
                    _lastFaceAt = DateTime.Now;
                    _faces.Clear(); _faces.AddRange(mirrored);
                    Greet(mirrored.Count);
                }
                else if ((DateTime.Now - _lastFaceAt).TotalSeconds > 1.5) _faces.Clear();
                Redraw();
            }));
        }

        private void Greet(int count)
        {
            string name = Settings.Load().ChildName ?? "toi";
            if (!_greeted)
            {
                _greeted = true;
                _lastGreeting = DateTime.Now;
                Speak("Coucou " + name + " ! C'est toi, dans le miroir magique !");
                SoundFx.Whistle();
            }
            else if (count >= 2 && (DateTime.Now - _lastGreeting).TotalSeconds > 60)
            {
                _lastGreeting = DateTime.Now;
                Speak("Coucou vous deux !");
            }
        }

        // ------------------------------------------------------------------
        //  Les déguisements, posés sur chaque visage
        // ------------------------------------------------------------------
        private void Redraw()
        {
            _overlay.Children.Clear();
            foreach (var f in _faces)
            {
                DrawHat(f);
                DrawEyes(f);
                DrawNose(f);
            }
        }

        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Pink = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0xD6));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xFF, 0xD2, 0x4D));
        private static readonly Brush Red = new SolidColorBrush(Color.FromRgb(0xF2, 0x3B, 0x3B));

        private void Add(Shape s, double left, double top) { Canvas.SetLeft(s, left); Canvas.SetTop(s, top); _overlay.Children.Add(s); }

        private Polygon Poly(Brush fill, params Point[] pts)
        {
            var p = new Polygon { Fill = fill, Stroke = Ink, StrokeThickness = 3, StrokeLineJoin = PenLineJoin.Round };
            foreach (var pt in pts) p.Points.Add(pt);
            return p;
        }

        private Ellipse Dot(Brush fill, double d, Brush stroke = null, double thick = 3) =>
            new Ellipse { Width = d, Height = d, Fill = fill, Stroke = stroke ?? Ink, StrokeThickness = thick };

        private void DrawHat(MirrorCamera.Face f)
        {
            double cx = f.X + f.W / 2, top = f.Y;
            switch (_hat)
            {
                case 1: // oreilles de chat
                    foreach (int side in new[] { -1, 1 })
                    {
                        double ex = cx + side * f.W * 0.32;
                        Add(Poly(Gold, new Point(ex - f.W * 0.16, top + f.H * 0.12), new Point(ex, top - f.H * 0.32), new Point(ex + f.W * 0.16, top + f.H * 0.12)), 0, 0);
                        Add(Poly(Pink, new Point(ex - f.W * 0.08, top + f.H * 0.09), new Point(ex, top - f.H * 0.18), new Point(ex + f.W * 0.08, top + f.H * 0.09)), 0, 0);
                    }
                    break;
                case 2: // oreilles de lapin
                    foreach (int side in new[] { -1, 1 })
                    {
                        double ex = cx + side * f.W * 0.22, eh = f.H * 0.75, ew = f.W * 0.2;
                        Add(new Ellipse { Width = ew, Height = eh, Fill = Brushes.White, Stroke = Ink, StrokeThickness = 3 }, ex - ew / 2, top - eh + f.H * 0.1);
                        Add(new Ellipse { Width = ew * 0.5, Height = eh * 0.7, Fill = Pink }, ex - ew * 0.25, top - eh * 0.85 + f.H * 0.1);
                    }
                    break;
                case 3: // couronne
                    {
                        double w = f.W * 0.9, h = f.H * 0.4, x0 = cx - w / 2, y0 = top - h * 0.75;
                        var pts = new List<Point> { new Point(x0, y0 + h), new Point(x0, y0 + h * 0.35) };
                        for (int i = 0; i < 4; i++) { pts.Add(new Point(x0 + w * (i + 0.5) / 4, y0)); pts.Add(new Point(x0 + w * (i + 1) / 4, y0 + h * 0.35)); }
                        pts.Add(new Point(x0 + w, y0 + h));
                        Add(Poly(Gold, pts.ToArray()), 0, 0);
                        for (int i = 0; i < 4; i++) Add(Dot(i % 2 == 0 ? Red : new SolidColorBrush(Color.FromRgb(0x3F, 0x8E, 0xF2)), h * 0.22, Brushes.White, 2), x0 + w * (i + 0.5) / 4 - h * 0.11, y0 + h * 0.45);
                        break;
                    }
                case 4: // chapeau de fête
                    {
                        double w = f.W * 0.6, h = f.H * 0.9, x0 = cx - w / 2, y0 = top - h + f.H * 0.12;
                        var cone = Poly(new LinearGradientBrush(Color.FromRgb(0xFF, 0x6B, 0x6B), Color.FromRgb(0x7E, 0x3F, 0xF2), 90), new Point(x0, y0 + h), new Point(cx, y0), new Point(x0 + w, y0 + h));
                        Add(cone, 0, 0);
                        for (int i = 1; i <= 3; i++)
                        {
                            double yy = y0 + h * i / 4, half = w / 2 * i / 4;
                            Add(new Line { X1 = cx - half, Y1 = yy, X2 = cx + half, Y2 = yy, Stroke = Gold, StrokeThickness = 4 }, 0, 0);
                        }
                        Add(Dot(Gold, f.W * 0.16), cx - f.W * 0.08, y0 - f.W * 0.08);
                        break;
                    }
                case 5: // chapeau de sorcière
                    {
                        double w = f.W * 0.8, h = f.H * 1.0, y0 = top - h + f.H * 0.15;
                        Add(new Ellipse { Width = f.W * 1.4, Height = f.H * 0.22, Fill = Ink, Stroke = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)), StrokeThickness = 3 }, cx - f.W * 0.7, y0 + h - f.H * 0.12);
                        Add(Poly(new SolidColorBrush(Color.FromRgb(0x4A, 0x2A, 0x6A)), new Point(cx - w / 2, y0 + h), new Point(cx + w * 0.1, y0), new Point(cx + w / 2, y0 + h)), 0, 0);
                        Add(new Rectangle { Width = w * 0.7, Height = f.H * 0.12, Fill = Gold, RadiusX = 4, RadiusY = 4 }, cx - w * 0.35, y0 + h - f.H * 0.2);
                        break;
                    }
                case 6: // couronne d'étoiles
                    for (int i = 0; i < 7; i++)
                    {
                        double a = Math.PI * (0.1 + 0.8 * i / 6), r = f.W * 0.72;
                        var st = new System.Windows.Shapes.Path { Data = StarGeometry, Fill = Gold, Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)), StrokeThickness = 1, RenderTransform = new ScaleTransform(f.W * 0.012, f.W * 0.012) };
                        Add(st, cx - Math.Cos(a) * r, top + f.H * 0.15 - Math.Sin(a) * r);
                    }
                    break;
            }
        }

        private void DrawEyes(MirrorCamera.Face f)
        {
            if (_eyes == 0) return;
            double ey = f.Y + f.H * 0.4, r = f.W * 0.19;
            double lx = f.X + f.W * 0.3, rx = f.X + f.W * 0.7;
            switch (_eyes)
            {
                case 1:
                    Add(new Ellipse { Width = 2 * r, Height = 2 * r, Stroke = Ink, StrokeThickness = 5, Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x7E, 0x3F, 0xF2)) }, lx - r, ey - r);
                    Add(new Ellipse { Width = 2 * r, Height = 2 * r, Stroke = Ink, StrokeThickness = 5, Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x7E, 0x3F, 0xF2)) }, rx - r, ey - r);
                    Add(new Line { X1 = lx + r, Y1 = ey, X2 = rx - r, Y2 = ey, Stroke = Ink, StrokeThickness = 5 }, 0, 0);
                    break;
                case 2:
                    Add(new Rectangle { Width = 2.1 * r, Height = 1.5 * r, RadiusX = r * 0.5, RadiusY = r * 0.5, Fill = new SolidColorBrush(Color.FromArgb(0xDD, 0x20, 0x20, 0x30)), Stroke = Ink, StrokeThickness = 4 }, lx - 1.05 * r, ey - 0.7 * r);
                    Add(new Rectangle { Width = 2.1 * r, Height = 1.5 * r, RadiusX = r * 0.5, RadiusY = r * 0.5, Fill = new SolidColorBrush(Color.FromArgb(0xDD, 0x20, 0x20, 0x30)), Stroke = Ink, StrokeThickness = 4 }, rx - 1.05 * r, ey - 0.7 * r);
                    Add(new Line { X1 = lx + r, Y1 = ey - 0.3 * r, X2 = rx - r, Y2 = ey - 0.3 * r, Stroke = Ink, StrokeThickness = 5 }, 0, 0);
                    break;
                case 3:
                    foreach (var x in new[] { lx, rx })
                        Add(new System.Windows.Shapes.Path { Data = StarGeometry, Fill = new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xD2, 0x4D)), Stroke = Ink, StrokeThickness = 0.4, RenderTransform = new ScaleTransform(r * 0.14, r * 0.14) }, x, ey);
                    Add(new Line { X1 = lx + r * 0.6, Y1 = ey, X2 = rx - r * 0.6, Y2 = ey, Stroke = Ink, StrokeThickness = 5 }, 0, 0);
                    break;
                case 4:
                    foreach (var x in new[] { lx, rx })
                        Add(new System.Windows.Shapes.Path { Data = HeartGeometry, Fill = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0x5F, 0x8F)), Stroke = Ink, StrokeThickness = 0.4, RenderTransform = new ScaleTransform(r * 0.14, r * 0.14) }, x, ey - r * 0.2);
                    Add(new Line { X1 = lx + r * 0.6, Y1 = ey, X2 = rx - r * 0.6, Y2 = ey, Stroke = Ink, StrokeThickness = 5 }, 0, 0);
                    break;
            }
        }

        private void DrawNose(MirrorCamera.Face f)
        {
            double nx = f.X + f.W / 2, ny = f.Y + f.H * 0.62;
            switch (_nose)
            {
                case 1:
                    {
                        double d = f.W * 0.24;
                        Add(Dot(Red, d), nx - d / 2, ny - d / 2);
                        Add(Dot(new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)), d * 0.3, Brushes.Transparent, 0), nx - d * 0.3, ny - d * 0.35);
                        break;
                    }
                case 2:
                    {
                        double w = f.W * 0.6, h = f.H * 0.14, y = ny + f.H * 0.14;
                        Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(Fmt("M {0},{1} C {2},{3} {4},{3} {5},{6} C {7},{8} {9},{10} {0},{1} C {11},{10} {12},{8} {13},{6} C {14},{3} {15},{3} {0},{1} Z",
                            nx, y, nx - w * 0.15, y - h, nx - w * 0.4, nx - w * 0.5, y - h * 0.3, nx - w * 0.35, y + h * 0.2, nx - w * 0.15, y + h * 0.15, nx + w * 0.15, nx + w * 0.35, nx + w * 0.5, nx + w * 0.4, nx + w * 0.15)), Fill = Ink }, 0, 0);
                        break;
                    }
                case 3:
                    {
                        double w = f.W * 0.22, h = f.H * 0.16;
                        var wing = new SolidColorBrush(Color.FromArgb(0xE0, 0xB3, 0x88, 0xFF));
                        Add(new Ellipse { Width = w, Height = h, Fill = wing, Stroke = Ink, StrokeThickness = 2, RenderTransform = new RotateTransform(-25) }, nx - w * 0.95, ny - h * 0.7);
                        Add(new Ellipse { Width = w, Height = h, Fill = wing, Stroke = Ink, StrokeThickness = 2, RenderTransform = new RotateTransform(25) }, nx + w * 0.05, ny - h * 0.5);
                        Add(new Ellipse { Width = w * 0.8, Height = h * 0.8, Fill = Pink, Stroke = Ink, StrokeThickness = 2, RenderTransform = new RotateTransform(20) }, nx - w * 0.85, ny + h * 0.1);
                        Add(new Ellipse { Width = w * 0.8, Height = h * 0.8, Fill = Pink, Stroke = Ink, StrokeThickness = 2, RenderTransform = new RotateTransform(-20) }, nx + w * 0.1, ny + h * 0.1);
                        Add(new Ellipse { Width = w * 0.18, Height = h * 1.3, Fill = Ink }, nx - w * 0.09, ny - h * 0.5);
                        break;
                    }
                case 4:
                    {
                        double d = f.W * 0.12;
                        Add(Dot(Pink, d), nx - d / 2, ny - d / 2);
                        foreach (int side in new[] { -1, 1 })
                            for (int i = -1; i <= 1; i++)
                                Add(new Line { X1 = nx + side * d * 0.6, Y1 = ny + i * d * 0.5, X2 = nx + side * f.W * 0.42, Y2 = ny + i * d * 1.4, Stroke = Ink, StrokeThickness = 3 }, 0, 0);
                        break;
                    }
            }
        }

        private static string Fmt(string pattern, params double[] v) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, pattern, v.Cast<object>().ToArray());

        private static readonly Geometry StarGeometry = Geometry.Parse("M 0,-10 L 2.9,-3.1 10,-3.1 4.3,1.2 6.2,8.1 0,4 -6.2,8.1 -4.3,1.2 -10,-3.1 -2.9,-3.1 Z");
        private static readonly Geometry HeartGeometry = Geometry.Parse("M 0,8 C -10,-2 -8,-10 -3,-9 C -1,-8.5 0,-6 0,-5 C 0,-6 1,-8.5 3,-9 C 8,-10 10,-2 0,8 Z");

        // ------------------------------------------------------------------
        //  Les mondes et le miroir déformant
        // ------------------------------------------------------------------
        private void ApplyWorld()
        {
            _worldLayer.Children.Clear();
            switch (_world)
            {
                case 1: _worldLayer.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x5F, 0xB0)) }); break;
                case 2: _worldLayer.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(0x55, 0x3F, 0x8E, 0xF2)) }); break;
                case 3:
                    _worldLayer.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(0x70, 0x10, 0x10, 0x50)) });
                    var sky = new Canvas();
                    for (int i = 0; i < 40; i++)
                    {
                        var st = new System.Windows.Shapes.Path { Data = StarGeometry, Fill = Gold, Opacity = 0.9, RenderTransform = new ScaleTransform(0.3 + _rng.NextDouble() * 0.9, 0.3 + _rng.NextDouble() * 0.9) };
                        Canvas.SetLeft(st, _rng.NextDouble() * W); Canvas.SetTop(st, _rng.NextDouble() * H * 0.6);
                        st.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(600 + _rng.Next(1400))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                        sky.Children.Add(st);
                    }
                    _worldLayer.Children.Add(sky);
                    break;
                case 4:
                    _worldLayer.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x5A, 0x2B)) });
                    _worldLayer.Children.Add(new Rectangle { Fill = new RadialGradientBrush(Color.FromArgb(0x00, 0, 0, 0), Color.FromArgb(0xA0, 0x20, 0x10, 0x00)) { RadiusX = 0.75, RadiusY = 0.75 } });
                    break;
                case 5:
                    {
                        var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1), Opacity = 0.35 };
                        Color[] cs = { Colors.Red, Colors.Orange, Colors.Yellow, Colors.LimeGreen, Colors.DeepSkyBlue, Colors.BlueViolet };
                        for (int i = 0; i < cs.Length; i++) b.GradientStops.Add(new GradientStop(cs[i], i / (double)(cs.Length - 1)));
                        _worldLayer.Children.Add(new Rectangle { Fill = b });
                        break;
                    }
            }
        }

        private void ApplyDeform()
        {
            var scale = new ScaleTransform(1, 1);
            var rot = new RotateTransform(0);
            var tg = new TransformGroup();
            tg.Children.Add(scale); tg.Children.Add(rot);
            _mirrorBox.RenderTransform = tg;
            switch (_deform)
            {
                case 1: scale.ScaleX = 1.6; break;
                case 2: scale.ScaleX = 0.55; break;
                case 3: scale.ScaleY = -1; break;
                case 4:
                    rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-4, 4, TimeSpan.FromMilliseconds(110)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1.04, TimeSpan.FromMilliseconds(170)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                    break;
                case 5: scale.ScaleX = 1.6; scale.ScaleY = 1.6; break;
            }
        }

        // ------------------------------------------------------------------
        //  Étincelles, surprise, photo
        // ------------------------------------------------------------------
        private void Sparkle(double x, double y)
        {
            if (Closed) return;
            if (_spark == 2) SoundFx.PopSound(); else SoundFx.Note(_rng.Next(8));
            for (int i = 0; i < 10; i++)
            {
                double px = x + (_rng.NextDouble() - 0.5) * W * 0.18, py = y + (_rng.NextDouble() - 0.5) * H * 0.2;
                Shape s;
                double size = 10 + _rng.Next(18);
                if (_spark == 0) s = new System.Windows.Shapes.Path { Data = StarGeometry, Fill = Gold, Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)), StrokeThickness = 0.5 };
                else if (_spark == 1) s = new System.Windows.Shapes.Path { Data = HeartGeometry, Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x5F, 0x8F)), Stroke = Red, StrokeThickness = 0.5 };
                else s = new Ellipse { Width = 20, Height = 20, Stroke = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF)), StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(0x50, 0x8A, 0xD6, 0xFF)) };
                var sc = new ScaleTransform(0.2, 0.2);
                var tr = new TranslateTransform();
                var tg = new TransformGroup(); tg.Children.Add(sc); tg.Children.Add(tr);
                s.RenderTransform = tg; s.RenderTransformOrigin = new Point(0.5, 0.5);
                Canvas.SetLeft(s, px); Canvas.SetTop(s, py);
                _fx.Children.Add(s);
                double k = size / 10.0;
                var grow = new DoubleAnimation(0.2, k, TimeSpan.FromMilliseconds(500)) { EasingFunction = new BackEase { Amplitude = 0.6 } };
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, grow); sc.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
                tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -40 - _rng.Next(60), TimeSpan.FromMilliseconds(1300)));
                tr.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, (_rng.NextDouble() - 0.5) * 60, TimeSpan.FromMilliseconds(1300)));
                var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700)) { BeginTime = TimeSpan.FromMilliseconds(600) };
                var captured = s;
                fade.Completed += (o, e) => _fx.Children.Remove(captured);
                s.BeginAnimation(OpacityProperty, fade);
            }
        }

        private void Surprise()
        {
            if (Closed) return;
            SoundFx.Drum();
            Speak("Roulement de tambour…");
            Schedule(900, () =>
            {
                _hat = 1 + _rng.Next(HatNames.Length - 1);
                _eyes = _rng.Next(EyeNames.Length);
                _nose = _rng.Next(NoseNames.Length);
                _world = _rng.Next(WorldNames.Length);
                _deform = _rng.Next(3) == 0 ? _rng.Next(DeformNames.Length) : 0;
                Redraw(); ApplyWorld(); ApplyDeform();
                SoundFx.Boom();
                Sparkle(W / 2, H / 2);
                Speak("Surprise ! " + HatNames[_hat] + ", " + EyeNames[_eyes] + " et " + NoseNames[_nose] + " !");
            });
        }

        private void TakePhoto()
        {
            if (Closed) return;
            try
            {
                SoundFx.PopSound();
                // Le flash.
                var flash = new Rectangle { Width = W, Height = H, Fill = Brushes.White };
                _fx.Children.Add(flash);
                var fade = new DoubleAnimation(0.95, 0, TimeSpan.FromMilliseconds(450));
                fade.Completed += (s, e) => _fx.Children.Remove(flash);
                flash.BeginAnimation(OpacityProperty, fade);

                // La photo : le miroir tel qu'il est à l'écran (déguisements, monde, déformation).
                var target = _mirrorFrame;
                int pw = Math.Max(2, (int)target.ActualWidth), ph = Math.Max(2, (int)target.ActualHeight);
                var rtb = new RenderTargetBitmap(pw, ph, 96, 96, PixelFormats.Pbgra32);
                target.Measure(new Size(pw, ph));
                rtb.Render(target);
                rtb.Freeze();
                var dir = System.IO.Path.Combine(UserContent.RootDir, "Miroir");
                Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "miroir-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
                using (var fs = File.Create(path))
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    enc.Save(fs);
                }
                _photos++;
                Log.Write("miroir", "Photo : " + path);

                // Le polaroïd qui rejoint l'album sous le miroir.
                var polaroid = new Border
                {
                    Background = Brushes.White, Padding = new Thickness(6, 6, 6, 16), Margin = new Thickness(6, 0, 6, 0),
                    Child = new Image { Source = rtb, Width = 120, Height = 120 * ph / Math.Max(1.0, pw), Stretch = Stretch.UniformToFill },
                    RenderTransform = new RotateTransform(_rng.Next(-6, 7)), RenderTransformOrigin = new Point(0.5, 0.5),
                };
                _album.Height = double.NaN;
                _album.Children.Insert(0, polaroid);
                while (_album.Children.Count > 5) _album.Children.RemoveAt(_album.Children.Count - 1);
                Schedule(500, () => { Speak("Clic-clac ! Quelle belle photo ! Elle est dans l'album."); if (_photos == 1) RewardStar(); });
            }
            catch (Exception ex)
            {
                Log.Write("miroir", "Photo impossible : " + ex);
                Speak("Oups, la photo n'a pas marché.");
            }
        }

        // ------------------------------------------------------------------
        //  Les tuiles et leurs icônes (dessinées : nettes et en couleur partout)
        // ------------------------------------------------------------------
        private Button Tile(string label, Color color, UIElement icon, Action onClick)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var box = new Viewbox { Width = 44, Height = 44, Child = icon, Margin = new Thickness(0, 0, 12, 0) };
            sp.Children.Add(box);
            sp.Children.Add(new TextBlock { Text = label, FontSize = 24, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Content = sp,
                Width = 230, Height = 84,
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(4, 4, 4, 4),
            };
            b.Click += (s, e) => { if (!Locked) onClick(); };
            _tiles.Add(b);
            return b;
        }

        // Un bouton sans aucun dessin : le regard le « clique », l'œil ne le voit pas.
        private static Button InvisibleZone()
        {
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            tpl.VisualTree = border;
            return new Button { Template = tpl, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand };
        }

        private static Canvas Icon() => new Canvas { Width = 48, Height = 48 };
        private static Brush White => Brushes.White;

        private static UIElement IconHat()
        {
            var c = Icon();
            c.Children.Add(new Polygon { Points = new PointCollection { new Point(8, 42), new Point(24, 6), new Point(40, 42) }, Fill = White });
            c.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = White }); Canvas.SetLeft(c.Children[1], 19); Canvas.SetTop(c.Children[1], 0);
            return c;
        }
        private static UIElement IconGlasses()
        {
            var c = Icon();
            foreach (var x in new[] { 2.0, 26.0 }) { var e = new Ellipse { Width = 20, Height = 20, Stroke = White, StrokeThickness = 4 }; Canvas.SetLeft(e, x); Canvas.SetTop(e, 14); c.Children.Add(e); }
            c.Children.Add(new Line { X1 = 22, Y1 = 24, X2 = 26, Y2 = 24, Stroke = White, StrokeThickness = 4 });
            return c;
        }
        private static UIElement IconNose()
        {
            var c = Icon();
            var e = new Ellipse { Width = 26, Height = 26, Fill = White }; Canvas.SetLeft(e, 11); Canvas.SetTop(e, 11); c.Children.Add(e);
            return c;
        }
        private static UIElement IconRainbow()
        {
            var c = Icon();
            for (int i = 0; i < 3; i++)
            {
                double r = 20 - i * 6;
                c.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(Fmt("M {0},40 A {1},{1} 0 0 1 {2},40", 24 - r, r, 24 + r)), Stroke = White, StrokeThickness = 4, Opacity = 1 - i * 0.25 });
            }
            return c;
        }
        private static UIElement IconWave()
        {
            var c = Icon();
            c.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 4,24 C 12,8 20,8 24,24 C 28,40 36,40 44,24"), Stroke = White, StrokeThickness = 5, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            return c;
        }
        private static UIElement IconStar()
        {
            var c = Icon();
            c.Children.Add(new System.Windows.Shapes.Path { Data = StarGeometry, Fill = White, RenderTransform = new ScaleTransform(2.1, 2.1) });
            Canvas.SetLeft(c.Children[0], 24); Canvas.SetTop(c.Children[0], 25);
            return c;
        }
        private static UIElement IconDice()
        {
            var c = Icon();
            c.Children.Add(new Rectangle { Width = 40, Height = 40, RadiusX = 8, RadiusY = 8, Stroke = White, StrokeThickness = 4 });
            Canvas.SetLeft(c.Children[0], 4); Canvas.SetTop(c.Children[0], 4);
            foreach (var (x, y) in new[] { (12.0, 12.0), (30.0, 12.0), (21.0, 21.0), (12.0, 30.0), (30.0, 30.0) })
            { var d = new Ellipse { Width = 7, Height = 7, Fill = White }; Canvas.SetLeft(d, x - 3.5); Canvas.SetTop(d, y - 3.5); c.Children.Add(d); }
            return c;
        }
        private static UIElement IconCamera()
        {
            var c = Icon();
            c.Children.Add(new Rectangle { Width = 42, Height = 30, RadiusX = 6, RadiusY = 6, Fill = White }); Canvas.SetLeft(c.Children[0], 3); Canvas.SetTop(c.Children[0], 12);
            c.Children.Add(new Rectangle { Width = 14, Height = 8, RadiusX = 3, RadiusY = 3, Fill = White }); Canvas.SetLeft(c.Children[1], 17); Canvas.SetTop(c.Children[1], 6);
            c.Children.Add(new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)) }); Canvas.SetLeft(c.Children[2], 16); Canvas.SetTop(c.Children[2], 19);
            return c;
        }
    }
}
