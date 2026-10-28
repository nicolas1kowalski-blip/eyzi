using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎣 « La pêche » : des poissons nagent dans la mare — ils ondulent, accélèrent,
    /// font demi-tour, lâchent des bulles, sous un voile d'eau qui les met vraiment
    /// dedans. Le bouchon de la ligne suit le poisson que l'enfant regarde ; quand
    /// la fixation aboutit, la ligne plonge, le poisson mord, remonte en frétillant
    /// dans une gerbe, et file dans le seau. Cinq poissons → le seau est plein,
    /// fête, et ils nagent plus vite (niveau mémorisé). Un canard farceur flotte
    /// à la surface et fait coin-coin si on le regarde.
    /// Le premier jeu qui travaille la POURSUITE du regard.
    /// </summary>
    public sealed class FishingGame : GameControl
    {
        private double W = 1500;
        private const double H = 740;
        private const double Surface = H * 0.30;   // la ligne d'eau
        private readonly Random _rng = new Random();
        private Canvas _canvas;
        private readonly List<Fish> _fish = new List<Fish>();
        private DispatcherTimer _timer;
        private int _caught, _level;
        private double _speedK;
        private TextBlock _bucketText;
        private StackPanel _bucketFish;
        private Line _line;
        private Ellipse _bobber;
        private Path _hook;
        private Path _wave;
        private double _bobX, _bobTargetX, _t;
        private bool _reeling;

        private sealed class Fish
        {
            public Button Btn; public Grid Holder; public ScaleTransform Flip; public RotateTransform Tilt;
            public double X, Y, Speed, Phase, Burst, Scale, Amp, TurnAt; public int Dir; public bool Caught, Duck, Turning; public string Name, Img;
        }

        private static readonly (string Img, string Name)[] Kinds =
        {
            ("poisson", "un poisson rouge"), ("poisson-2", "un poisson orange"), ("poisson-3", "un poisson à nageoires bleues"),
        };

        public FishingGame(Action celebrate) : base(celebrate)
        {
            GateVoice = false;
            Unloaded += (s, e) => _timer?.Stop();
        }

        protected override void NewRound()
        {
            Locked = false; _reeling = false;
            _caught = 0;
            _level = Math.Min(2, Adaptive.Level("peche"));
            _speedK = new[] { 1.0, 1.6, 2.4 }[_level];
            Question.Text = "🎣 La pêche";
            SetConsigne(new TextBlock { Text = "👀🐟" }, () => "Suis un poisson des yeux… et il mord ! Remplis le seau !");

            W = Math.Round(H * ScreenAspect);
            _canvas = new Canvas { Width = W, Height = H, ClipToBounds = true };
            _canvas.Background = MakeDecorBrush("decor-mare", 0x10) ?? new LinearGradientBrush(Color.FromRgb(0x8A, 0xD6, 0xFF), Color.FromRgb(0x1E, 0x6F, 0xB8), 90);

            // L'eau (derrière les poissons) : plus sombre vers le fond.
            var water = new Rectangle { Width = W, Height = H - Surface, Fill = new LinearGradientBrush(Color.FromArgb(0x70, 0x4F, 0xA8, 0xF2), Color.FromArgb(0xB0, 0x0E, 0x34, 0x78), 90), IsHitTestVisible = false };
            Canvas.SetTop(water, Surface); Panel.SetZIndex(water, 1); _canvas.Children.Add(water);
            Seaweed();
            Rays();

            // Le voile d'eau PAR-DESSUS les poissons : ils sont dedans, pas collés dessus.
            var veil = new Rectangle { Width = W, Height = H - Surface, Fill = new LinearGradientBrush(Color.FromArgb(0x28, 0x7F, 0xC8, 0xFF), Color.FromArgb(0x58, 0x10, 0x40, 0x90), 90), IsHitTestVisible = false };
            Canvas.SetTop(veil, Surface); Panel.SetZIndex(veil, 400); _canvas.Children.Add(veil);
            // La surface : une vague claire qui glisse.
            _wave = new Path { Fill = new SolidColorBrush(Color.FromArgb(0x90, 0xE8, 0xF6, 0xFF)), IsHitTestVisible = false, Data = WaveGeometry() };
            Canvas.SetTop(_wave, Surface - 14); Panel.SetZIndex(_wave, 410); _canvas.Children.Add(_wave);

            // La canne, la ligne, le bouchon et l'hameçon.
            var rod = new Line { X1 = W - 60, Y1 = 20, X2 = W * 0.6, Y2 = H * 0.17, Stroke = new SolidColorBrush(Color.FromRgb(0x8A, 0x5A, 0x2B)), StrokeThickness = 9, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false };
            Panel.SetZIndex(rod, 500); _canvas.Children.Add(rod);
            _bobX = _bobTargetX = W * 0.6;
            _line = new Line { X1 = W * 0.6, Y1 = H * 0.17, X2 = _bobX, Y2 = Surface, Stroke = new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF)), StrokeThickness = 2.5, IsHitTestVisible = false };
            Panel.SetZIndex(_line, 500); _canvas.Children.Add(_line);
            _bobber = new Ellipse { Width = 26, Height = 34, Fill = new LinearGradientBrush(Color.FromRgb(0xE0, 0x3B, 0x3B), Colors.White, 90), Stroke = new SolidColorBrush(Color.FromRgb(0x5A, 0x1A, 0x1A)), StrokeThickness = 2, IsHitTestVisible = false };
            Panel.SetZIndex(_bobber, 510); _canvas.Children.Add(_bobber);
            _hook = new Path { Data = Geometry.Parse("M 0,0 L 0,40 C 0,58 22,58 22,42"), Stroke = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xD0)), StrokeThickness = 3, IsHitTestVisible = false };
            Panel.SetZIndex(_hook, 505); _canvas.Children.Add(_hook);

            Bucket();

            _fish.Clear();
            for (int i = 0; i < 5; i++) AddFish(false, i);
            AddFish(true, 5); // le canard farceur
            SetBody(_canvas);
            var pads = _fish.Where(f => !f.Duck).ToList();
            SetGazePads(pads.Select(f => (FrameworkElement)f.Btn).ToList(), idx =>
            {
                // Le bouchon glisse vers le poisson regardé : on voit qu'on vise.
                if (idx >= 0 && idx < pads.Count && !pads[idx].Caught) _bobTargetX = pads[idx].X + 85;
            });

            _timer?.Stop();
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(30) };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
        }

        // ------------------------------------------------------------------
        //  Le décor vivant : algues, rayons de lumière, vague, seau
        // ------------------------------------------------------------------
        private void Seaweed()
        {
            for (int i = 0; i < 5; i++)
            {
                double x = W * (0.08 + i * 0.21) + _rng.Next(60), h = 120 + _rng.Next(120);
                var weed = new Path
                {
                    Data = Geometry.Parse(FormattableString.Invariant($"M 0,0 C -18,{-h * 0.3:0} 18,{-h * 0.6:0} 0,{-h:0} C 10,{-h * 0.6:0} -10,{-h * 0.3:0} 8,0 Z")),
                    Fill = new SolidColorBrush(Color.FromArgb(0xC0, 0x2E, 0x8B, 0x57)), Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0x1B, 0x5E, 0x3A)), StrokeThickness = 2,
                    RenderTransformOrigin = new Point(0.5, 1), IsHitTestVisible = false,
                };
                var rot = new RotateTransform(0);
                weed.RenderTransform = rot;
                Canvas.SetLeft(weed, x); Canvas.SetTop(weed, H + 4);
                Panel.SetZIndex(weed, 2);
                _canvas.Children.Add(weed);
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-7, 7, TimeSpan.FromMilliseconds(1800 + _rng.Next(1400))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            }
        }

        private void Rays()
        {
            for (int i = 0; i < 3; i++)
            {
                double x = W * (0.2 + i * 0.3);
                var ray = new Polygon { Points = new PointCollection { new Point(x - 30, Surface), new Point(x + 30, Surface), new Point(x + 160, H), new Point(x - 60, H) }, Fill = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)), IsHitTestVisible = false };
                Panel.SetZIndex(ray, 3);
                _canvas.Children.Add(ray);
                ray.BeginAnimation(OpacityProperty, new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(2200 + i * 700)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            }
        }

        private Geometry WaveGeometry()
        {
            var sb = new System.Text.StringBuilder("M -200,14");
            for (double x = -200; x <= W + 200; x += 60)
                sb.Append(FormattableString.Invariant($" Q {x + 15},0 {x + 30},14 Q {x + 45},28 {x + 60},14"));
            sb.Append(FormattableString.Invariant($" L {W + 260},40 L -200,40 Z"));
            return Geometry.Parse(sb.ToString());
        }

        private void Bucket()
        {
            var g = new Grid { Width = 150, Height = 130, IsHitTestVisible = false };
            g.Children.Add(new Path
            {
                Data = Geometry.Parse("M 10,30 L 140,30 L 125,125 L 25,125 Z"),
                Fill = new LinearGradientBrush(Color.FromRgb(0x9A, 0xA8, 0xB8), Color.FromRgb(0x5A, 0x6A, 0x7A), 0), Stroke = new SolidColorBrush(Color.FromRgb(0x2A, 0x32, 0x40)), StrokeThickness = 3,
            });
            g.Children.Add(new Path { Data = Geometry.Parse("M 20,32 C 20,-10 130,-10 130,32"), Stroke = new SolidColorBrush(Color.FromRgb(0x2A, 0x32, 0x40)), StrokeThickness = 5, Fill = null });
            _bucketFish = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 14) };
            g.Children.Add(_bucketFish);
            Canvas.SetLeft(g, 18); Canvas.SetTop(g, 8); Panel.SetZIndex(g, 600);
            _canvas.Children.Add(g);
            _bucketText = new TextBlock { FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.6 } };
            Canvas.SetLeft(_bucketText, 180); Canvas.SetTop(_bucketText, 40); Panel.SetZIndex(_bucketText, 600); _canvas.Children.Add(_bucketText);
            UpdateBucket();
        }

        // ------------------------------------------------------------------
        //  Les poissons
        // ------------------------------------------------------------------
        private void AddFish(bool duck, int stagger)
        {
            var f = new Fish { Duck = duck };
            var kind = Kinds[_rng.Next(Kinds.Length)];
            f.Name = duck ? "le canard" : kind.Name;
            f.Img = duck ? "canard-de-bain" : kind.Img;
            var src = Art.Find(f.Img);
            FrameworkElement visual = src != null
                ? new Image { Source = src, Width = 170, Height = 120, Stretch = Stretch.Uniform }
                : (FrameworkElement)new TextBlock { Text = duck ? "🦆" : "🐟", FontSize = 100 };
            f.Flip = new ScaleTransform(1, 1);
            f.Tilt = new RotateTransform(0);
            var tg = new TransformGroup(); tg.Children.Add(f.Tilt); tg.Children.Add(f.Flip);
            f.Holder = new Grid { Width = 170, Height = 120, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = tg, Children = { visual } };
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            border.AppendChild(cp);
            tpl.VisualTree = border;
            f.Btn = new Button { Template = tpl, Content = f.Holder, Width = 170, Height = 120, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand, RenderTransformOrigin = new Point(0.5, 0.5) };
            var captured = f;
            f.Btn.Click += (s, e) => Catch(captured);
            _canvas.Children.Add(f.Btn);
            _fish.Add(f);
            Respawn(f, stagger);
        }

        private void Respawn(Fish f, int stagger = 0)
        {
            f.Caught = false; f.Turning = false;
            f.Dir = _rng.Next(2) == 0 ? 1 : -1;
            f.Scale = f.Duck ? 1.0 : 0.7 + _rng.NextDouble() * 0.7;      // gros = lent, petit = vif
            f.Speed = (f.Duck ? 0.6 : (2.2 - f.Scale) * 1.3) * _speedK;
            f.Amp = f.Duck ? 6 : 12 + _rng.NextDouble() * 14;
            f.Y = f.Duck ? Surface - 70 : Surface + 40 + _rng.NextDouble() * (H - Surface - 190);
            f.X = f.Dir > 0 ? -200 - stagger * 260 : W + 30 + stagger * 260;
            f.Phase = _rng.NextDouble() * 6.28;
            f.Burst = _rng.NextDouble() * 6.28;
            f.TurnAt = _t + 3 + _rng.NextDouble() * 6;
            // Les dessins regardent vers la GAUCHE : on les retourne quand ils nagent vers la droite.
            f.Flip.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            f.Flip.ScaleX = f.Dir > 0 ? -f.Scale : f.Scale; f.Flip.ScaleY = f.Scale;
            f.Tilt.Angle = 0;
            f.Btn.BeginAnimation(OpacityProperty, null);
            f.Btn.Opacity = f.Duck ? 1 : 0.8 + f.Scale * 0.15; f.Btn.IsHitTestVisible = true; f.Btn.RenderTransform = Transform.Identity;
            f.Btn.RenderTransformOrigin = new Point(0.5, 0.5);
            Panel.SetZIndex(f.Btn, f.Duck ? 420 : 100 + (int)(f.Scale * 100)); // le canard flotte au-dessus de l'eau
            Place(f);
        }

        private void Place(Fish f)
        {
            Canvas.SetLeft(f.Btn, f.X);
            Canvas.SetTop(f.Btn, f.Y + Math.Sin(f.Phase) * f.Amp);
        }

        private void Turn(Fish f)
        {
            if (f.Turning) return;
            f.Turning = true;
            f.Dir = -f.Dir;
            double to = f.Dir > 0 ? -f.Scale : f.Scale;
            var a = new DoubleAnimation(f.Flip.ScaleX, to, TimeSpan.FromMilliseconds(380)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
            a.Completed += (s, e) => { f.Flip.BeginAnimation(ScaleTransform.ScaleXProperty, null); f.Flip.ScaleX = to; f.Turning = false; };
            f.Flip.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            f.TurnAt = _t + 4 + _rng.NextDouble() * 7;
        }

        private void Tick()
        {
            if (Closed) { _timer.Stop(); return; }
            _t += 0.03;
            foreach (var f in _fish)
            {
                if (f.Caught) continue;
                // Vitesse qui varie : coups de queue puis glissade.
                f.Burst += f.Duck ? 0.01 : 0.025;
                double v = f.Speed * (f.Turning ? 0.25 : 0.55 + 0.45 * (0.5 + 0.5 * Math.Sin(f.Burst)));
                f.X += v * f.Dir;
                f.Phase += f.Duck ? 0.035 : 0.045 + v * 0.01;
                // Le nez monte quand le poisson monte, descend quand il descend.
                f.Tilt.Angle = f.Duck ? Math.Sin(f.Phase) * 3 : -f.Dir * Math.Cos(f.Phase) * (f.Amp * 0.55);
                // Demi-tour de temps en temps, au milieu de la mare.
                if (!f.Duck && _t > f.TurnAt)
                {
                    if (f.X > W * 0.15 && f.X < W * 0.75 && _rng.Next(100) < 50) Turn(f);
                    else f.TurnAt = _t + 1.5;
                }
                // Au bord : la moitié du temps on fait demi-tour, sinon on repart de l'autre côté.
                if ((f.Dir > 0 && f.X > W + 40) || (f.Dir < 0 && f.X < -220)) Respawn(f);
                else if (!f.Duck && ((f.Dir > 0 && f.X > W - 190) || (f.Dir < 0 && f.X < 20)) && !f.Turning && _rng.Next(100) < 4) Turn(f);
                Place(f);
                // Des bulles sortent de la bouche de temps en temps.
                if (!f.Duck && _rng.Next(100) < 2) Bubble(f.X + (f.Dir > 0 ? 150 : 20), Canvas.GetTop(f.Btn) + 50, f.Scale);
            }
            if (_rng.Next(100) < 2) Bubble(_rng.NextDouble() * W, H - 10, 0.6);
            // Le bouchon glisse vers le poisson visé, et danse sur la vague.
            if (!_reeling)
            {
                _bobX += (_bobTargetX - _bobX) * 0.08;
                double by = Surface - 4 + Math.Sin(_t * 2.2) * 5;
                _line.X2 = _bobX; _line.Y2 = by;
                Canvas.SetLeft(_bobber, _bobX - 13); Canvas.SetTop(_bobber, by - 20);
                Canvas.SetLeft(_hook, _bobX); Canvas.SetTop(_hook, by + 8);
            }
            Canvas.SetLeft(_wave, -60 + (_t * 40) % 60);
        }

        private void Bubble(double x, double y, double k)
        {
            double d = (8 + _rng.Next(10)) * k;
            var b = new Ellipse { Width = d, Height = d, Stroke = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)), StrokeThickness = 1.5, Fill = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)), IsHitTestVisible = false };
            Canvas.SetLeft(b, x); Canvas.SetTop(b, y);
            Panel.SetZIndex(b, 300);
            _canvas.Children.Add(b);
            var captured = b;
            double dur = 1400 + _rng.Next(1200);
            var up = new DoubleAnimation(y, Surface + 4, TimeSpan.FromMilliseconds(dur)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
            up.Completed += (s, e) => _canvas.Children.Remove(captured);
            b.BeginAnimation(Canvas.TopProperty, up);
            b.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(x, x + (_rng.NextDouble() - 0.5) * 50, TimeSpan.FromMilliseconds(dur)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            b.BeginAnimation(OpacityProperty, new DoubleAnimation(0.9, 0.2, TimeSpan.FromMilliseconds(dur)));
        }

        // ------------------------------------------------------------------
        //  Ça mord !
        // ------------------------------------------------------------------
        private void Catch(Fish f)
        {
            if (Locked || f.Caught || _reeling) return;
            if (f.Duck)
            {
                SoundFx.Puff();
                Speak("Coin coin ! Ce n'est pas un poisson, c'est le canard !");
                Shake(f.Holder);
                return;
            }
            f.Caught = true; _reeling = true;
            f.Btn.IsHitTestVisible = false;
            double fx = f.X + 85, fy = Canvas.GetTop(f.Btn) + 60;
            // La ligne plonge du bouchon jusqu'au poisson.
            _bobTargetX = fx;
            var dive = TimeSpan.FromMilliseconds(320);
            _line.BeginAnimation(Line.X2Property, new DoubleAnimation(_line.X2, fx, dive));
            _line.BeginAnimation(Line.Y2Property, new DoubleAnimation(_line.Y2, fy, dive) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            _bobber.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(_bobber), fx - 13, dive));
            _bobber.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(_bobber), Surface + 10, dive));
            _hook.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(_hook), fx, dive));
            _hook.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(_hook), fy - 30, dive));
            SoundFx.PopSound();
            Schedule(340, () =>
            {
                SoundFx.Splat();
                Speak("Ça mord ! " + f.Name + " !");
                // Le poisson remonte avec la ligne en frétillant, la queue en bas.
                var tr = new TranslateTransform();
                var rot = new RotateTransform(0);
                var tg = new TransformGroup(); tg.Children.Add(rot); tg.Children.Add(tr);
                f.Btn.RenderTransform = tg;
                f.Btn.RenderTransformOrigin = new Point(0.5, 0.3);
                Panel.SetZIndex(f.Btn, 950);
                var lift = TimeSpan.FromMilliseconds(800);
                double dy = -(fy - (Surface - 50));
                tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, dy, lift) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                _line.BeginAnimation(Line.Y2Property, new DoubleAnimation(fy, Surface - 50, lift) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                _hook.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(fy - 30, Surface - 80, lift) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                _bobber.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Surface + 10, Surface - 60, lift));
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-(f.Dir * 70) - 18, -(f.Dir * 70) + 18, TimeSpan.FromMilliseconds(110)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(8) });
                Schedule(300, () => Splash(fx, Surface));
                Schedule(900, () =>
                {
                    // Hop, dans le seau : un arc jusqu'en haut à gauche, puis le poisson rapetisse.
                    var fly = TimeSpan.FromMilliseconds(650);
                    double tx = 70 - f.X, ty = 40 - fy;
                    var ay = new DoubleAnimationUsingKeyFrames { Duration = fly };
                    ay.KeyFrames.Add(new SplineDoubleKeyFrame(dy - 180, KeyTime.FromPercent(0.45), new KeySpline(0.2, 0.8, 0.5, 1)));
                    ay.KeyFrames.Add(new SplineDoubleKeyFrame(ty, KeyTime.FromPercent(1), new KeySpline(0.5, 0, 0.8, 1)));
                    tr.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, tx, fly) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
                    tr.BeginAnimation(TranslateTransform.YProperty, ay);
                    rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-(f.Dir * 70), -(f.Dir * 70) - 360 * f.Dir, fly));
                    f.Btn.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromMilliseconds(420) });
                    // La ligne revient au bouchon.
                    _line.BeginAnimation(Line.X2Property, null); _line.BeginAnimation(Line.Y2Property, null);
                    _bobber.BeginAnimation(Canvas.LeftProperty, null); _bobber.BeginAnimation(Canvas.TopProperty, null);
                    _hook.BeginAnimation(Canvas.LeftProperty, null); _hook.BeginAnimation(Canvas.TopProperty, null);
                    _reeling = false;
                    Schedule(700, () =>
                    {
                        _caught++;
                        AddToBucket(f.Img);
                        UpdateBucket();
                        if (_caught >= 5)
                        {
                            Locked = true;
                            Adaptive.Report("peche", true);
                            Speak("Le seau est plein ! Quelle pêche !");
                            GameKit.Success();
                            RewardStar();
                            Celebrate();
                            Schedule(3000, NewRound);
                        }
                        else Respawn(f, 1);
                    });
                });
            });
        }

        private void AddToBucket(string img)
        {
            var src = Art.Find(img);
            FrameworkElement v = src != null ? new Image { Source = src, Width = 34, Height = 24, Stretch = Stretch.Uniform } : (FrameworkElement)new TextBlock { Text = "🐟", FontSize = 20 };
            v.Margin = new Thickness(-6, 0, -6, 0);
            v.RenderTransformOrigin = new Point(0.5, 0.5);
            var sc = new ScaleTransform(0.2, 0.2); v.RenderTransform = sc;
            _bucketFish.Children.Add(v);
            var grow = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, grow); sc.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        }

        private void Splash(double x, double y)
        {
            for (int i = 0; i < 16; i++)
            {
                var d = new Ellipse { Width = 8 + _rng.Next(12), Height = 8 + _rng.Next(12), Fill = new SolidColorBrush(Color.FromArgb(0xD0, 0xDA, 0xF2, 0xFF)), IsHitTestVisible = false };
                Canvas.SetLeft(d, x); Canvas.SetTop(d, y);
                Panel.SetZIndex(d, 940);
                _canvas.Children.Add(d);
                var captured = d;
                var up = new DoubleAnimation(y, y - 70 - _rng.Next(110), TimeSpan.FromMilliseconds(380)) { AutoReverse = true, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                up.Completed += (s, e) => _canvas.Children.Remove(captured);
                d.BeginAnimation(Canvas.TopProperty, up);
                d.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(x, x + (_rng.NextDouble() - 0.5) * 220, TimeSpan.FromMilliseconds(760)));
            }
            // Les ronds dans l'eau.
            for (int i = 0; i < 3; i++)
            {
                var ring = new Ellipse { Width = 20, Height = 8, Stroke = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF)), StrokeThickness = 2, IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0.5) };
                var sc = new ScaleTransform(1, 1); ring.RenderTransform = sc;
                Canvas.SetLeft(ring, x - 10); Canvas.SetTop(ring, y - 4);
                Panel.SetZIndex(ring, 415);
                _canvas.Children.Add(ring);
                var captured = ring;
                var grow = new DoubleAnimation(1, 6 + i * 2, TimeSpan.FromMilliseconds(900)) { BeginTime = TimeSpan.FromMilliseconds(i * 220) };
                var fade = new DoubleAnimation(0.9, 0, TimeSpan.FromMilliseconds(900)) { BeginTime = TimeSpan.FromMilliseconds(i * 220) };
                fade.Completed += (s, e) => _canvas.Children.Remove(captured);
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, grow); sc.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
                ring.BeginAnimation(OpacityProperty, fade);
            }
        }

        private void UpdateBucket() => _bucketText.Text = $"{_caught} / 5   ·   Niveau {_level + 1}";
    }
}
