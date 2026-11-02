using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// « Tartes à la crème » (inspiré de Look to Learn) : UN visage rigolo
    /// surgit À UN ENDROIT AU HASARD de l'écran — l'enfant doit le chercher
    /// des yeux (ça fait travailler le balayage du regard), puis le regarder
    /// pour lancer la tarte : SPLAT, grimace, crème qui dégouline… et un
    /// nouveau visage surgit ailleurs. Zéro échec, que du rire.
    /// </summary>
    public sealed class PieFaceGame : GameControl
    {
        // La largeur suit la FORME DE L'ÉCRAN : la scène remplit tout l'écran.
        private double W = 1500;
        private const double H = 720;
        // LA PRÉCISION SE TRAVAILLE : les visages rapetissent au fil des tartes
        // (dans la manche) et d'une manche à l'autre (niveau mémorisé), jusqu'au
        // tiers de la taille de départ ; et la ZONE qui lance la tarte se resserre
        // sur le milieu du visage : il faut poser le regard dedans, pas à côté.
        private const double BaseFace = 290;
        private double _scale = 1;
        private double _zone = 0.9;                  // la part du visage qui compte comme cible
        private double FaceSize => BaseFace * _scale;
        private double Inner => FaceSize * 0.914;    // l'image dans sa cible
        private double ZoneSize => FaceSize * _zone; // la cible du regard, au centre
        private int _aides;                          // visages qu'il a fallu agrandir dans la manche
        private Grid _holder;                        // le visage à l'écran (pour l'aide)

        private static readonly string[] Faces = { "😀", "🤠", "🤓", "😮", "🥸", "😊", "🧐", "😁" };
        private static readonly string[] Hit = { "😵", "🤪", "😝", "😜" };

        private readonly Random _rng = new Random();
        private Canvas _canvas;
        private int _splats;
        private double _lastX = -999, _lastY = -999;

        public PieFaceGame(Action celebrate) : base(celebrate)
        {
            GateVoice = false; // la tarte part toujours tout de suite — c'est le jeu !
        }

        private void UpdateScale()
        {
            int level = Math.Min(2, Adaptive.Level("tartes-precision"));
            double start = 1 - 0.2 * level;                              // 1 → 0,8 → 0,6 selon le niveau
            _scale = Math.Max(0.32, start * (1 - 0.06 * _splats));       // puis 6 % de moins par tarte (plancher : un tiers)
            _zone = Math.Max(0.5, (0.9 - 0.12 * level) * (1 - 0.02 * _splats)); // 90 % du visage au début, 50 % au plus serré
        }

        protected override void NewRound()
        {
            Locked = false;
            _splats = 0;
            _aides = 0;
            UpdateScale();
            Question.Text = "🥧 Tartes à la crème";
            SetConsigne(new TextBlock { Text = "🔍🥧" },
                () => "Cherche le visage rigolo... et regarde-le pour lancer la tarte ! SPLAT !");

            W = Math.Round(H * ScreenAspect);
            _canvas = new Canvas { Width = W, Height = H };
            // Plein écran : la page autour de la scène continue le décor.
            SetPageBackground(new LinearGradientBrush(
                Color.FromRgb(0xFF, 0xE9, 0xF2), Color.FromRgb(0xFF, 0xF6, 0xDE), 90));
            // Décor : une IMAGE de scène du parent (decor-tartes.png) si elle
            // existe — sinon le fond dessiné.
            var decor = Art.Find("decor-tartes");
            if (decor != null)
                _canvas.Children.Add(new Image { Source = decor, Width = W, Height = H, Stretch = Stretch.UniformToFill });
            else
                _canvas.Children.Add(new Rectangle
                {
                    Width = W,
                    Height = H,
                    RadiusX = 24,
                    RadiusY = 24,
                    Fill = new LinearGradientBrush(Color.FromRgb(0xFF, 0xE9, 0xF2), Color.FromRgb(0xFF, 0xF6, 0xDE), 90),
                });
            // Le rideau de cirque en haut.
            var curtain = new TextBlock { Text = "🎪🎈🎪🎈🎪", FontSize = 46, IsHitTestVisible = false, Opacity = 0.8 };
            Canvas.SetLeft(curtain, W / 2 - 180);
            Canvas.SetTop(curtain, 8);
            _canvas.Children.Add(curtain);
            // L'assiette de tartes en bas.
            var pies = new TextBlock { Text = "🥧🥧🥧", FontSize = 58, IsHitTestVisible = false };
            Canvas.SetLeft(pies, W / 2 - 110);
            Canvas.SetTop(pies, H - 86);
            _canvas.Children.Add(pies);

            SpawnFace();

            SetBody(_canvas);
            Speak("Regarde un visage pour lancer la tarte !");
        }

        // Un endroit au hasard, loin du bord, du plat de tartes… et du visage
        // précédent (pour obliger le regard à VOYAGER).
        private (double X, double Y) RandomSpot()
        {
            for (int tries = 0; tries < 40; tries++)
            {
                double x = 30 + _rng.NextDouble() * (W - FaceSize - 60);
                double y = 60 + _rng.NextDouble() * (H - FaceSize - 90);
                bool nearPlate = y > H - FaceSize - 130 && x > W / 2 - 320 && x < W / 2 + 180;
                double dx = x - _lastX, dy = y - _lastY;
                if (!nearPlate && Math.Sqrt(dx * dx + dy * dy) > 420) return (x, y);
            }
            return (60, 80);
        }

        // Combien de visages en IMAGES sont disponibles (21 fournis avec
        // l'application, remplaçables par le parent) — compté une fois.
        private static int _imgFaces = -1;

        private static int ImgFaces()
        {
            if (_imgFaces >= 0) return _imgFaces;
            int n = 0;
            while (n < 40 && Art.Find("visage-" + (n + 1)) != null) n++;
            _imgFaces = n;
            return n;
        }

        private void SpawnFace()
        {
            var (fx, fy) = RandomSpot();
            _lastX = fx;
            _lastY = fy;

            int count = ImgFaces();
            int fi = _rng.Next(count > 0 ? count : Faces.Length);
            // Le visage : image (visage-N.png, 21 fournis) → dessin vectoriel
            // intégré (FaceArt) ; l'emoji n'est plus qu'un secours.
            var imgF = Art.Find("visage-" + (fi + 1));
            var visual = imgF != null
                ? (FrameworkElement)new Image { Source = imgF, Width = Inner, Height = Inner, Stretch = Stretch.Uniform }
                : FaceArt.Make(fi, Inner);
            visual.IsHitTestVisible = false;
            var contenu = new ContentControl { Content = visual, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            // Le visage (l'image, qu'on ne peut pas viser) et, au milieu, la CIBLE :
            // un bouton invisible plus petit que le visage, que le regard doit atteindre.
            // Un fin anneau clair montre où viser.
            var holder = new Grid { Width = FaceSize, Height = FaceSize, RenderTransformOrigin = new Point(0.5, 0.5) };
            holder.Children.Add(contenu);
            var anneau = new Ellipse
            {
                Width = ZoneSize, Height = ZoneSize, Stroke = new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF)), StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 3, 3 }, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            };
            holder.Children.Add(anneau);
            var face = SceneButton(null, ZoneSize, ZoneSize);
            face.HorizontalAlignment = HorizontalAlignment.Center; face.VerticalAlignment = VerticalAlignment.Center;
            // Cible de précision : l'aimant du regard ne l'attrape pas de loin, il
            // faut poser le regard dedans — c'est tout l'intérêt du jeu.
            Gaze.GazeTargets.SetPrecise(face, true);
            holder.Children.Add(face);
            var captured = holder;
            face.Click += (s, e) => ThrowPie(captured, contenu, fx, fy, fi);
            Canvas.SetLeft(holder, fx);
            Canvas.SetTop(holder, fy);
            _canvas.Children.Add(holder);
            _holder = holder;

            // Il SURGIT (pop) avec un petit bruit, pour attirer l'œil.
            SoundFx.PopSound();
            var sc = new ScaleTransform(0.15, 0.15);
            holder.RenderTransform = sc;
            var grow = new DoubleAnimation(0.15, 1, TimeSpan.FromMilliseconds(450))
            { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.8 } };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, grow);

            // L'AIDE : au bout de 12 s sans tarte, le visage grossit d'un tiers et sa
            // cible s'élargit à tout le visage ; deux aides dans la manche, et le
            // niveau redescend d'un cran à la prochaine.
            Schedule(12000, () =>
            {
                if (Closed || Locked || _holder != holder || !holder.IsHitTestVisible) return;
                _aides++;
                Speak("Regarde bien le visage !");
                face.Width = face.Height = FaceSize; anneau.Width = anneau.Height = FaceSize;
                var big = new DoubleAnimation(1, 1.35, TimeSpan.FromMilliseconds(500)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 } };
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, big);
                sc.BeginAnimation(ScaleTransform.ScaleYProperty, big);
            });
        }

        // La secousse du choc, sans compter d'erreur (Shake, lui, en compte une).
        private static void Tremble(FrameworkElement el)
        {
            var tr = new TranslateTransform();
            var old = el.RenderTransform;
            var grp = new TransformGroup();
            if (old != null && !(old is MatrixTransform)) grp.Children.Add(old);
            grp.Children.Add(tr);
            el.RenderTransform = grp;
            tr.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-10, 10, TimeSpan.FromMilliseconds(50)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(5), FillBehavior = FillBehavior.Stop });
        }

        // Bouton transparent (cible de regard sans habillage).
        private static Button SceneButton(UIElement content, double w, double h)
        {
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            tpl.VisualTree = border;
            return new Button { Template = tpl, Width = w, Height = h, Focusable = false, Content = content, Cursor = System.Windows.Input.Cursors.Hand };
        }

        private void ThrowPie(Grid face, ContentControl contenu, double fx, double fy, int fi)
        {
            if (Locked) return;
            face.IsHitTestVisible = false;

            // La tarte part du plat (en bas au centre) et file vers le visage
            // en tournant sur elle-même. Image du parent (tarte.png) sinon le
            // dessin vectoriel intégré.
            var imgP = Art.Find("tarte");
            FrameworkElement pie = imgP != null
                ? new Image { Source = imgP, Width = 140, Height = 140, Stretch = Stretch.Uniform }
                : FaceArt.Pie(140);
            pie.IsHitTestVisible = false;
            pie.RenderTransformOrigin = new Point(0.5, 0.5);
            var rot = new RotateTransform(0);
            pie.RenderTransform = rot;
            double px0 = W / 2 - 70, py0 = H - 110;
            double px1 = fx + FaceSize / 2 - 70, py1 = fy + FaceSize * 0.45 - 70;
            Canvas.SetLeft(pie, px0);
            Canvas.SetTop(pie, py0);
            pie.SetValue(Panel.ZIndexProperty, 70);
            _canvas.Children.Add(pie);

            var dur = TimeSpan.FromMilliseconds(420);
            var ax = new DoubleAnimation(px0, px1, dur) { EasingFunction = new SineEase() };
            var ay = new DoubleAnimation(py0, py1, dur) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
            rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 540, dur));
            ay.Completed += (s, e) =>
            {
                _canvas.Children.Remove(pie);
                Splat(face, contenu, fx, fy, fi);
            };
            pie.BeginAnimation(Canvas.LeftProperty, ax);
            pie.BeginAnimation(Canvas.TopProperty, ay);
        }

        private void Splat(Grid face, ContentControl contenu, double fx, double fy, int fi)
        {
            SoundFx.Splat();
            // La grimace du MÊME personnage (visage-touche-N assorti) ; sinon
            // une grimace au hasard, comme avant.
            var imgH = Art.Find("visage-touche-" + (fi + 1));
            int hi = _rng.Next(Hit.Length);
            if (imgH == null) imgH = Art.Find("visage-touche-" + (hi + 1));
            contenu.Content = imgH != null
                ? (FrameworkElement)new Image { Source = imgH, Width = Inner, Height = Inner, Stretch = Stretch.Uniform }
                : FaceArt.Hit(hi, Inner);

            // Le visage tremble sous le choc (sans compter d'erreur : la tarte est partie, c'est gagné).
            Tremble(face);

            // La crème : un gros nuage blanc qui s'écrase sur le visage…
            double cx = fx + FaceSize / 2, cy = fy + FaceSize * 0.45;
            var cream = new Canvas { IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0.5) };
            var csc = new ScaleTransform(0.25, 0.25);
            cream.RenderTransform = csc;
            var creamBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFA, 0xEE));
            foreach (var (dx, dy, r) in new[]
            {
                (0.0, 0.0, 95.0), (-70.0, -30.0, 55.0), (70.0, -25.0, 58.0),
                (-45.0, 50.0, 50.0), (50.0, 55.0, 52.0), (0.0, -70.0, 48.0),
            })
            {
                var blob = new Ellipse { Width = r * 2, Height = r * 2, Fill = creamBrush };
                Canvas.SetLeft(blob, dx - r);
                Canvas.SetTop(blob, dy - r);
                cream.Children.Add(blob);
            }
            Canvas.SetLeft(cream, cx);
            Canvas.SetTop(cream, cy);
            cream.SetValue(Panel.ZIndexProperty, 75);
            _canvas.Children.Add(cream);
            var pop = new DoubleAnimation(0.25, 1.05, TimeSpan.FromMilliseconds(220))
            { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 } };
            csc.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            csc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);

            // … des gouttes qui dégoulinent…
            for (int k = 0; k < 4; k++)
            {
                var drip = new Ellipse { Width = 22 - k * 3, Height = 30, Fill = creamBrush, IsHitTestVisible = false };
                Canvas.SetLeft(drip, cx - 60 + k * 40);
                Canvas.SetTop(drip, cy + 60);
                drip.SetValue(Panel.ZIndexProperty, 74);
                _canvas.Children.Add(drip);
                var fall = new DoubleAnimation(cy + 60, cy + 150 + k * 28, TimeSpan.FromMilliseconds(900 + k * 180))
                { BeginTime = TimeSpan.FromMilliseconds(250 + k * 120), EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
                var dfade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
                { BeginTime = TimeSpan.FromMilliseconds(900 + k * 240) };
                var capturedDrip = drip;
                dfade.Completed += (s, e) => _canvas.Children.Remove(capturedDrip);
                drip.BeginAnimation(Canvas.TopProperty, fall);
                drip.BeginAnimation(OpacityProperty, dfade);
            }

            _splats++;
            UpdateScale(); // le prochain visage sera un peu plus petit
            if (_splats == 5) Speak("Ils deviennent tout petits… vise bien !");
            else if (_splats % 3 == 0) Speak("Splat ! En pleine figure ! Hi hi hi !");

            // … puis la crème glisse, le visage s'en va, et un autre surgit
            // AILLEURS sur l'écran.
            Schedule(1500, () =>
            {
                var slide = new DoubleAnimation(cy, cy + 260, TimeSpan.FromMilliseconds(700))
                { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
                var cfade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700));
                var capturedCream = cream;
                cfade.Completed += (s, e) => _canvas.Children.Remove(capturedCream);
                cream.BeginAnimation(Canvas.TopProperty, slide);
                cream.BeginAnimation(OpacityProperty, cfade);
            });
            Schedule(2100, () =>
            {
                _canvas.Children.Remove(face);
                if (_splats >= 8 && !Locked)
                {
                    Locked = true;
                    // Manche gagnée sans aide : la prochaine commence plus petit (3 manches = un cran) ;
                    // deux aides ou plus : c'était trop dur, le niveau redescend.
                    Adaptive.Report("tartes-precision", _aides < 2);
                    Speak("Quelle bataille de tartes ! Bravo !");
                    GameKit.Success();
                    Celebrate();
                    Schedule(2600, NewRound);
                    return;
                }
                SpawnFace();
            });
        }
    }
}
