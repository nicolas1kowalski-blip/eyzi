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
    /// 🎣 « La pêche » : des poissons passent dans la mare, à des vitesses et des
    /// profondeurs différentes. L'enfant en SUIT un du regard ; quand la
    /// fixation aboutit, la ligne descend, le poisson mord et sort de l'eau dans
    /// une gerbe. Cinq poissons → le seau est plein, fête, et ils nagent plus
    /// vite (niveau mémorisé). Un canard farceur fait coin-coin si on le regarde.
    /// Le premier jeu qui travaille la POURSUITE du regard.
    /// </summary>
    public sealed class FishingGame : GameControl
    {
        private double W = 1500;
        private const double H = 740;
        private readonly Random _rng = new Random();
        private Canvas _canvas;
        private readonly List<Fish> _fish = new List<Fish>();
        private DispatcherTimer _timer;
        private int _caught, _level;
        private double _speedK;
        private TextBlock _bucket;
        private bool _busy;

        private sealed class Fish { public Button Btn; public double X, Y, Speed, Phase, Scale; public int Dir; public bool Caught, Duck; public string Name; }

        private static readonly (string Img, string Name)[] Kinds =
        {
            ("poisson", "un poisson rouge"), ("poisson-2", "un poisson bleu"), ("poisson-3", "un poisson rayé"),
        };

        public FishingGame(Action celebrate) : base(celebrate)
        {
            GateVoice = false;
            Unloaded += (s, e) => _timer?.Stop();
        }

        protected override void NewRound()
        {
            Locked = false; _busy = false;
            _caught = 0;
            _level = Math.Min(2, Adaptive.Level("peche"));
            _speedK = new[] { 1.0, 1.7, 2.6 }[_level];
            Question.Text = "🎣 La pêche";
            SetConsigne(new TextBlock { Text = "👀🐟" }, () => "Suis un poisson des yeux… et il mord ! Remplis le seau !");

            W = Math.Round(H * ScreenAspect);
            _canvas = new Canvas { Width = W, Height = H, ClipToBounds = true };
            _canvas.Background = MakeDecorBrush("decor-mare", 0x10) ?? new LinearGradientBrush(Color.FromRgb(0x8A, 0xD6, 0xFF), Color.FromRgb(0x1E, 0x6F, 0xB8), 90);
            // L'eau : un voile bleu, plus sombre vers le fond.
            var water = new Rectangle { Width = W, Height = H * 0.72, Fill = new LinearGradientBrush(Color.FromArgb(0x60, 0x3F, 0x8E, 0xF2), Color.FromArgb(0xA0, 0x10, 0x3A, 0x80), 90), IsHitTestVisible = false };
            Canvas.SetTop(water, H * 0.28); _canvas.Children.Add(water);
            // La canne, en haut à droite.
            var rod = new Line { X1 = W - 60, Y1 = 20, X2 = W * 0.55, Y2 = H * 0.2, Stroke = new SolidColorBrush(Color.FromRgb(0x8A, 0x5A, 0x2B)), StrokeThickness = 8, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false };
            _canvas.Children.Add(rod);
            _bucket = new TextBlock { FontSize = 28, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.6 } };
            Canvas.SetLeft(_bucket, 24); Canvas.SetTop(_bucket, 16); _canvas.Children.Add(_bucket);
            UpdateBucket();

            _fish.Clear();
            for (int i = 0; i < 5; i++) AddFish(false, i);
            AddFish(true, 5); // le canard farceur
            SetBody(_canvas);

            _timer?.Stop();
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(30) };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
        }

        private void AddFish(bool duck, int stagger)
        {
            var f = new Fish { Duck = duck };
            var kind = Kinds[_rng.Next(Kinds.Length)];
            f.Name = duck ? "le canard" : kind.Name;
            var src = Art.Find(duck ? "canard-de-bain" : kind.Img);
            FrameworkElement visual = src != null
                ? new Image { Source = src, Width = 170, Height = 120, Stretch = Stretch.Uniform }
                : (FrameworkElement)new TextBlock { Text = duck ? "🦆" : "🐟", FontSize = 100 };
            var holder = new Grid { Width = 170, Height = 120, RenderTransformOrigin = new Point(0.5, 0.5), Children = { visual } };
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            border.AppendChild(cp);
            tpl.VisualTree = border;
            f.Btn = new Button { Template = tpl, Content = holder, Width = 170, Height = 120, Focusable = false, Cursor = System.Windows.Input.Cursors.Hand, RenderTransformOrigin = new Point(0.5, 0.5) };
            var captured = f;
            f.Btn.Click += (s, e) => Catch(captured);
            _canvas.Children.Add(f.Btn);
            _fish.Add(f);
            Respawn(f, stagger);
        }

        private void Respawn(Fish f, int stagger = 0)
        {
            f.Caught = false;
            f.Dir = _rng.Next(2) == 0 ? 1 : -1;
            f.Scale = f.Duck ? 1.0 : 0.7 + _rng.NextDouble() * 0.7;      // gros = lent, petit = vif
            f.Speed = (f.Duck ? 0.7 : (2.2 - f.Scale) * 1.3) * _speedK;
            f.Y = f.Duck ? H * 0.22 : H * 0.34 + _rng.NextDouble() * (H * 0.56);
            f.X = f.Dir > 0 ? -200 - stagger * 260 : W + 30 + stagger * 260;
            f.Phase = _rng.NextDouble() * 6.28;
            var holder = (Grid)f.Btn.Content;
            holder.RenderTransform = new ScaleTransform(f.Dir > 0 ? f.Scale : -f.Scale, f.Scale); // tourné dans le sens de la nage
            f.Btn.Opacity = 1; f.Btn.IsHitTestVisible = true; f.Btn.RenderTransform = Transform.Identity;
            Panel.SetZIndex(f.Btn, (int)(f.Scale * 100));
            Place(f);
        }

        private void Place(Fish f)
        {
            Canvas.SetLeft(f.Btn, f.X);
            Canvas.SetTop(f.Btn, f.Y + Math.Sin(f.Phase) * 10);
        }

        private void Tick()
        {
            if (Closed) { _timer.Stop(); return; }
            foreach (var f in _fish)
            {
                if (f.Caught) continue;
                f.X += f.Speed * f.Dir;
                f.Phase += 0.04;
                if ((f.Dir > 0 && f.X > W + 40) || (f.Dir < 0 && f.X < -220)) Respawn(f);
                else Place(f);
            }
        }

        private void Catch(Fish f)
        {
            if (Locked || f.Caught) return;
            if (f.Duck)
            {
                SoundFx.Puff();
                Speak("Coin coin ! Ce n'est pas un poisson, c'est le canard !");
                var holder = (Grid)f.Btn.Content;
                Shake(holder);
                return;
            }
            f.Caught = true;
            f.Btn.IsHitTestVisible = false;
            // La ligne descend de la canne jusqu'au poisson, puis le remonte.
            double fx = f.X + 85, fy = Canvas.GetTop(f.Btn) + 60;
            var line = new Line { X1 = W * 0.55, Y1 = H * 0.2, X2 = fx, Y2 = H * 0.2, Stroke = Brushes.White, StrokeThickness = 3, IsHitTestVisible = false };
            Panel.SetZIndex(line, 900);
            _canvas.Children.Add(line);
            line.BeginAnimation(Line.Y2Property, new DoubleAnimation(H * 0.2, fy, TimeSpan.FromMilliseconds(350)));
            SoundFx.PopSound();
            Schedule(380, () =>
            {
                SoundFx.Splat();
                Speak("Ça mord ! " + f.Name + " !");
                // Le poisson monte avec la ligne, frétille, puis file dans le seau.
                var tr = new TranslateTransform();
                var rot = new RotateTransform(0);
                var tg = new TransformGroup(); tg.Children.Add(rot); tg.Children.Add(tr);
                f.Btn.RenderTransform = tg;
                Panel.SetZIndex(f.Btn, 950);
                tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -(fy - H * 0.2), TimeSpan.FromMilliseconds(700)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                line.BeginAnimation(Line.Y2Property, new DoubleAnimation(fy, H * 0.2, TimeSpan.FromMilliseconds(700)));
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-12, 12, TimeSpan.FromMilliseconds(120)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(5) });
                Splash(fx, Canvas.GetTop(f.Btn) + 100);
                Schedule(800, () =>
                {
                    _canvas.Children.Remove(line);
                    tr.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, -f.X + 40, TimeSpan.FromMilliseconds(500)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
                    tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-(fy - H * 0.2), -(fy - 60), TimeSpan.FromMilliseconds(500)));
                    f.Btn.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500)) { BeginTime = TimeSpan.FromMilliseconds(200) });
                    Schedule(600, () =>
                    {
                        _caught++;
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

        private void Splash(double x, double y)
        {
            for (int i = 0; i < 12; i++)
            {
                var d = new Ellipse { Width = 10 + _rng.Next(10), Height = 10 + _rng.Next(10), Fill = new SolidColorBrush(Color.FromArgb(0xC0, 0xCF, 0xEC, 0xFF)), IsHitTestVisible = false };
                Canvas.SetLeft(d, x); Canvas.SetTop(d, y);
                Panel.SetZIndex(d, 940);
                _canvas.Children.Add(d);
                var captured = d;
                var up = new DoubleAnimation(y, y - 60 - _rng.Next(90), TimeSpan.FromMilliseconds(350)) { AutoReverse = true, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                up.Completed += (s, e) => _canvas.Children.Remove(captured);
                d.BeginAnimation(Canvas.TopProperty, up);
                d.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(x, x + (_rng.NextDouble() - 0.5) * 160, TimeSpan.FromMilliseconds(700)));
            }
        }

        private void UpdateBucket() => _bucket.Text = $"🪣 Seau : {_caught} / 5   ·   Niveau {_level + 1}";
    }
}
