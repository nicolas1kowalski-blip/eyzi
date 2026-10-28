using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎪 « Chamboule-tout » : la pyramide de boîtes de la fête foraine. L'enfant
    /// regarde une boîte, la balle part et la fait tomber, avec celles qu'elle
    /// portait. Tout tombe → fanfare et nouvelle pyramide. La précision se
    /// travaille : boîtes plus petites et aimant du regard coupé aux niveaux
    /// suivants (niveau mémorisé, comme les tartes).
    /// </summary>
    public sealed class ChambouleGame : GameControl
    {
        private double W = 1500;
        private const double H = 740;
        private readonly Random _rng = new Random();
        private Canvas _canvas;
        private readonly List<Can> _cans = new List<Can>();
        private int _level, _rows, _down, _balls;
        private double _size;
        private TextBlock _score;
        private bool _flying;

        private sealed class Can { public Button Btn; public int Row, Col; public bool Down; public double X, Y; }

        public ChambouleGame(Action celebrate) : base(celebrate) { GateVoice = false; }

        protected override void NewRound()
        {
            Locked = false;
            _flying = false;
            _down = 0; _balls = 0;
            _level = Math.Min(2, Adaptive.Level("chamboule"));
            _rows = 3 + _level;
            _size = new[] { 140.0, 112.0, 90.0 }[_level];
            Question.Text = "🎪 Chamboule-tout";
            SetConsigne(new TextBlock { Text = "🎯📦" }, () => "Regarde une boîte… et la balle part ! Fais tout tomber !");

            W = Math.Round(H * ScreenAspect);
            _canvas = new Canvas { Width = W, Height = H, ClipToBounds = true };
            var bg = MakeDecorBrush("decor-bonbons", 0x40);
            _canvas.Background = bg ?? new LinearGradientBrush(Color.FromRgb(0xFF, 0xE6, 0xC8), Color.FromRgb(0xFF, 0xF6, 0xE8), 90);

            // L'étal : une nappe rayée et la planche.
            var cloth = new Rectangle { Width = W * 0.62, Height = 150, RadiusX = 14, RadiusY = 14, Fill = Stripes() };
            Canvas.SetLeft(cloth, W * 0.19); Canvas.SetTop(cloth, H - 170); _canvas.Children.Add(cloth);
            var plank = new Rectangle { Width = W * 0.66, Height = 22, RadiusX = 8, RadiusY = 8, Fill = new SolidColorBrush(Color.FromRgb(0x8A, 0x5A, 0x2B)) };
            Canvas.SetLeft(plank, W * 0.17); Canvas.SetTop(plank, H - 180); _canvas.Children.Add(plank);

            _score = new TextBlock { FontSize = 26, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)) };
            Canvas.SetLeft(_score, 24); Canvas.SetTop(_score, 16); _canvas.Children.Add(_score);

            BuildPyramid();
            UpdateScore();
            SetBody(_canvas);
        }

        private static Brush Stripes()
        {
            var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.06, 0), SpreadMethod = GradientSpreadMethod.Repeat };
            b.GradientStops.Add(new GradientStop(Color.FromRgb(0xF2, 0x3B, 0x5F), 0));
            b.GradientStops.Add(new GradientStop(Color.FromRgb(0xF2, 0x3B, 0x5F), 0.5));
            b.GradientStops.Add(new GradientStop(Colors.White, 0.5));
            b.GradientStops.Add(new GradientStop(Colors.White, 1));
            return b;
        }

        private static readonly string[] Layouts = { "pyramide", "mur", "tours", "deux-pyramides", "chateau" };
        private string _layout = "pyramide";
        private readonly List<Button> _gaps = new List<Button>();
        private readonly List<(Button Btn, Can A, Can B)> _gapLinks = new List<(Button, Can, Can)>();

        // Les positions (colonne en demi-boîtes, rangée) de chaque disposition.
        private List<(double Col, int Row)> Positions(string layout, int level)
        {
            var p = new List<(double, int)>();
            switch (layout)
            {
                case "mur":
                {
                    int cols = 3 + level, rows = 3;
                    for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) p.Add((c + (r % 2 == 1 ? 0.5 : 0), r));
                    break;
                }
                case "tours":
                {
                    int towers = 2 + level, height = 3;
                    for (int t = 0; t < towers; t++) for (int r = 0; r < height + (t % 2); r++) p.Add((t * 1.9, r));
                    break;
                }
                case "deux-pyramides":
                {
                    int n = 2 + level;
                    for (int side = 0; side < 2; side++)
                        for (int r = 0; r < n; r++) for (int c = 0; c < n - r; c++) p.Add((side * (n + 0.8) + c + r * 0.5, r));
                    break;
                }
                case "chateau":
                {
                    int n = 2 + level;
                    for (int r = 0; r < n; r++) for (int c = 0; c < n - r; c++) p.Add((1.6 + c + r * 0.5, r));
                    for (int r = 0; r < n + 1; r++) { p.Add((0, r)); p.Add((n + 2.2, r)); }
                    break;
                }
                default:
                {
                    int n = 3 + level;
                    for (int r = 0; r < n; r++) for (int c = 0; c < n - r; c++) p.Add((c + r * 0.5, r));
                    break;
                }
            }
            return p;
        }

        private void BuildPyramid()
        {
            foreach (var c in _cans) _canvas.Children.Remove(c.Btn);
            foreach (var g in _gaps) _canvas.Children.Remove(g);
            _cans.Clear(); _gaps.Clear(); _gapLinks.Clear();
            _layout = Layouts[_rng.Next(Layouts.Length)];
            double size = _size, gap = size * 0.35;          // de la place ENTRE les boîtes : on peut viser l'interstice
            double step = size + gap;
            var pos = Positions(_layout, _level);
            double minCol = pos.Min(q => q.Col), maxCol = pos.Max(q => q.Col);
            double totalW = (maxCol - minCol) * step + size;
            double left = W / 2 - totalW / 2;
            double baseY = H - 180 - size;
            int seed = 0;
            foreach (var (col, row) in pos)
            {
                var can = new Can { Row = row, Col = (int)Math.Round(col * 2), X = left + (col - minCol) * step, Y = baseY - row * (size + 2) };
                can.Btn = CanButton(size, seed++);
                Gaze.GazeTargets.SetPrecise(can.Btn, _level >= 1); // dès le niveau 1 : il faut viser dedans
                Gaze.GazeTargets.SetDwellFactor(can.Btn, 0.6);     // on vise vite : 40 % de fixation en moins
                var captured = can;
                can.Btn.Click += (s, e) => Throw(Aim(new Point(captured.X + _size / 2, captured.Y + _size / 2)));
                Canvas.SetLeft(can.Btn, can.X); Canvas.SetTop(can.Btn, can.Y);
                _canvas.Children.Add(can.Btn);
                _cans.Add(can);
            }
            // Les interstices : entre deux boîtes voisines d'une même rangée, une cible
            // invisible ; la balle qui y passe fait tomber les deux.
            foreach (var a in _cans)
            {
                var b = _cans.FirstOrDefault(o => o != a && Math.Abs(o.Y - a.Y) < 1 && o.X > a.X && o.X - (a.X + size) > size * 0.2 && o.X - (a.X + size) < size * 0.6);
                if (b == null) continue;
                double gx = a.X + size, gw = b.X - gx;
                var tpl = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                tpl.VisualTree = border;
                var g = new Button { Template = tpl, Width = gw, Height = size, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand };
                Gaze.GazeTargets.SetPrecise(g, true);
                Gaze.GazeTargets.SetDwellFactor(g, 0.6);
                double cx = gx + gw / 2, cy = a.Y + size / 2;
                g.Click += (s, e) => Throw(Aim(new Point(cx, cy)));
                Canvas.SetLeft(g, gx); Canvas.SetTop(g, a.Y);
                Panel.SetZIndex(g, 5);
                _canvas.Children.Add(g);
                _gaps.Add(g);
                _gapLinks.Add((g, a, b));
            }
        }

        /// <summary>Là où la balle part : le point regardé s'il est tout près de la cible, sinon le centre de la cible.</summary>
        private Point Aim(Point center)
        {
            try
            {
                if ((DateTime.Now - LastGazeAt).TotalMilliseconds < 400)
                {
                    var p = _canvas.PointFromScreen(LastGazeScreen);
                    if ((p - center).Length < _size * 0.9) return p;
                }
            }
            catch { }
            return center;
        }

        // Une boîte-personnage : un cylindre coloré avec un petit visage (FaceArt).
        private Button CanButton(double size, int seed)
        {
            Color[] cs = { Color.FromRgb(0xFF, 0x9F, 0x43), Color.FromRgb(0x3F, 0x8E, 0xF2), Color.FromRgb(0x2E, 0xB8, 0x8A), Color.FromRgb(0xF2, 0x5F, 0x8F), Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0xFF, 0xD2, 0x4D) };
            var color = cs[seed % cs.Length];
            var g = new Grid { Width = size, Height = size };
            g.Children.Add(new Rectangle
            {
                RadiusX = size * 0.12, RadiusY = size * 0.12,
                Fill = new LinearGradientBrush(Lighten(color, 0.35).Color, color, 0), Stroke = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), StrokeThickness = 3,
            });
            g.Children.Add(new Ellipse { Height = size * 0.18, Margin = new Thickness(4, 2, 4, 0), VerticalAlignment = VerticalAlignment.Top, Fill = Lighten(color, 0.55), Stroke = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), StrokeThickness = 2 });
            var face = FaceArt.Make(seed % 8, size * 0.6);
            face.VerticalAlignment = VerticalAlignment.Bottom; face.HorizontalAlignment = HorizontalAlignment.Center; face.Margin = new Thickness(0, 0, 0, size * 0.08);
            g.Children.Add(face);
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            border.AppendChild(cp);
            tpl.VisualTree = border;
            return new Button { Template = tpl, Content = g, Width = size, Height = size, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand, RenderTransformOrigin = new Point(0.5, 0.5) };
        }

        private static SolidColorBrush Lighten(Color c, double k) =>
            new SolidColorBrush(Color.FromRgb((byte)(c.R + (255 - c.R) * k), (byte)(c.G + (255 - c.G) * k), (byte)(c.B + (255 - c.B) * k)));

        private void Throw(Point aim)
        {
            if (Locked || _flying) return;
            _flying = true;
            _balls++;
            SoundFx.Whistle();
            double sx = W / 2, sy = H + 40, tx = aim.X, ty = aim.Y;
            var ball = new Ellipse
            {
                Width = 70, Height = 70,
                Fill = new RadialGradientBrush(Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xE0, 0x3B, 0x3B)) { GradientOrigin = new Point(0.3, 0.3), Center = new Point(0.35, 0.35) },
                Stroke = new SolidColorBrush(Color.FromRgb(0x8A, 0x1A, 0x1A)), StrokeThickness = 3, RenderTransformOrigin = new Point(0.5, 0.5),
            };
            var scale = new ScaleTransform(1.6, 1.6);
            ball.RenderTransform = scale;
            Canvas.SetLeft(ball, sx - 35); Canvas.SetTop(ball, sy - 35);
            Panel.SetZIndex(ball, 1000);
            _canvas.Children.Add(ball);
            var dur = TimeSpan.FromMilliseconds(520);
            ball.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(sx - 35, tx - 35, dur) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            ball.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(sy - 35, ty - 35, dur) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            var shrink = new DoubleAnimation(1.6, 0.8, dur);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink); scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
            Schedule(520, () =>
            {
                _canvas.Children.Remove(ball);
                // Tout ce que la balle touche tombe : une boîte, ou les deux de chaque côté de l'interstice.
                double reach = _size * 0.5 + 30;
                var hit = _cans.Where(c => !c.Down && Math.Abs(c.X + _size / 2 - tx) < reach && Math.Abs(c.Y + _size / 2 - ty) < reach).ToList();
                if (hit.Count == 0) { SoundFx.Puff(); Speak(new[] { "Raté !", "À côté !", "Encore !" }[_rng.Next(3)]); _flying = false; UpdateScore(); return; }
                SoundFx.Boom();
                foreach (var c in hit) Knock(c);
                _flying = false;
                UpdateScore();
                if (_cans.All(c => c.Down))
                {
                    Locked = true;
                    Adaptive.Report("chamboule", _balls <= _cans.Count); // efficace (pas plus de balles que de boîtes) : le niveau monte
                    Speak("Tout est tombé ! Quel champion !");
                    Confetti();
                    GameKit.Success();
                    RewardStar();
                    Celebrate();
                    Schedule(3000, NewRound);
                }
                else if (hit.Count >= 2) Speak("Deux d'un coup ! Bien visé !");
                else if (_down == 1) Speak("Et une ! Continue !");
                else if (_rng.Next(3) == 0) Speak(new[] { "Boum !", "Elle tombe !", "Bien visé !", "Encore une !" }[_rng.Next(4)]);
            });
        }

        // La boîte tombe, et celles qu'elle portait avec elle.
        private void Knock(Can c)
        {
            if (c.Down) return;
            c.Down = true; _down++;
            var btn = c.Btn;
            btn.IsHitTestVisible = false;
            var rot = new RotateTransform(0);
            var tr = new TranslateTransform();
            var tg = new TransformGroup(); tg.Children.Add(rot); tg.Children.Add(tr);
            btn.RenderTransform = tg;
            int dir = c.X + _size / 2 < W / 2 ? -1 : 1;
            rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, dir * (90 + _rng.Next(60)), TimeSpan.FromMilliseconds(600)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            tr.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, dir * (120 + _rng.Next(140)), TimeSpan.FromMilliseconds(700)));
            tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, H - c.Y, TimeSpan.FromMilliseconds(700)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromMilliseconds(600) };
            fade.Completed += (s, e) => btn.Visibility = Visibility.Hidden;
            btn.BeginAnimation(OpacityProperty, fade);
            // Au-dessus : les boîtes posées sur celle-ci (juste la rangée du dessus, qui la chevauchent).
            foreach (var above in _cans.Where(a => !a.Down && a.Row == c.Row + 1 && Math.Abs((a.Y + _size) - c.Y) < 6 && Math.Min(a.X + _size, c.X + _size) - Math.Max(a.X, c.X) > _size * 0.2).ToList())
                Schedule(120, () => Knock(above));
        }

        private void UpdateScore()
        {
            _score.Text = $"Boîtes tombées : {_down} / {_cans.Count}   ·   Balles : {_balls}   ·   Niveau {_level + 1}";
            foreach (var (btn, a, b) in _gapLinks) if (a.Down && b.Down) btn.IsHitTestVisible = false;
        }

        private void Confetti()
        {
            Color[] cs = { Color.FromRgb(0xFF, 0x6B, 0x6B), Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0x2E, 0xB8, 0x8A), Color.FromRgb(0x3F, 0x8E, 0xF2), Color.FromRgb(0xFF, 0x9F, 0x43) };
            for (int i = 0; i < 50; i++)
            {
                var r = new Rectangle { Width = 10 + _rng.Next(8), Height = 16 + _rng.Next(10), RadiusX = 3, RadiusY = 3, Fill = new SolidColorBrush(cs[i % cs.Length]), RenderTransform = new RotateTransform(_rng.Next(360)) };
                Canvas.SetLeft(r, _rng.NextDouble() * W); Canvas.SetTop(r, -30);
                Panel.SetZIndex(r, 2000);
                _canvas.Children.Add(r);
                var captured = r;
                var fall = new DoubleAnimation(-30, H + 30, TimeSpan.FromMilliseconds(1800 + _rng.Next(1500))) { BeginTime = TimeSpan.FromMilliseconds(_rng.Next(700)) };
                fall.Completed += (s, e) => _canvas.Children.Remove(captured);
                r.BeginAnimation(Canvas.TopProperty, fall);
            }
        }
    }
}
