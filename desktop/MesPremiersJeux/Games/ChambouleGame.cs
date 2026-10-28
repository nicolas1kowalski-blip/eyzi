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
            _rows = 3 + _level;                       // 3 rangées (6 boîtes) → 4 (10) → 5 (15)
            _size = new[] { 150.0, 118.0, 92.0 }[_level];
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

        private void BuildPyramid()
        {
            foreach (var c in _cans) _canvas.Children.Remove(c.Btn);
            _cans.Clear();
            double size = _size, gap = 6;
            double baseY = H - 180 - size;
            double cx = W / 2;
            for (int r = 0; r < _rows; r++)
            {
                int n = _rows - r;
                double rowW = n * size + (n - 1) * gap;
                for (int i = 0; i < n; i++)
                {
                    var can = new Can { Row = r, Col = i, X = cx - rowW / 2 + i * (size + gap), Y = baseY - r * (size + 2) };
                    can.Btn = CanButton(size, r * 7 + i);
                    Gaze.GazeTargets.SetPrecise(can.Btn, _level >= 1); // dès le niveau 1 : il faut viser dedans
                    var captured = can;
                    can.Btn.Click += (s, e) => Throw(captured);
                    Canvas.SetLeft(can.Btn, can.X); Canvas.SetTop(can.Btn, can.Y);
                    _canvas.Children.Add(can.Btn);
                    _cans.Add(can);
                }
            }
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

        private void Throw(Can target)
        {
            if (Locked || _flying || target.Down) return;
            _flying = true;
            _balls++;
            SoundFx.Whistle();
            double sx = W / 2, sy = H + 40, tx = target.X + _size / 2, ty = target.Y + _size / 2;
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
                SoundFx.Boom();
                Knock(target);
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
            // Au-dessus : les boîtes posées sur celle-ci (rangée r+1, colonnes col-1 et col).
            foreach (var above in _cans.Where(a => a.Row == c.Row + 1 && (a.Col == c.Col - 1 || a.Col == c.Col) && !a.Down).ToList())
                Schedule(120, () => Knock(above));
        }

        private void UpdateScore() => _score.Text = $"Boîtes tombées : {_down} / {_cans.Count}   ·   Niveau {_level + 1}";

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
