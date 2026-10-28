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
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🥔 « Patatou » : un bonhomme patate à habiller au regard. L'enfant choisit
    /// dans des galeries d'images les yeux, le nez, la bouche, le chapeau, le
    /// nœud, la bulle… Dès qu'il a des yeux, un nez et une bouche, Patatou prend
    /// vie, se dandine et dit bonjour. Une photo range la création dans le
    /// dossier de la famille. Schéma corporel, choix et création, sans échec.
    /// </summary>
    public sealed class PotatoGame : GameControl
    {
        private const double W = 900, H = 740;
        private const double BW = 400, BH = 500; // le corps
        private readonly Random _rng = new Random();
        private Canvas _canvas;
        private Canvas _parts;
        private Grid _scene;
        private PropGallery _gallery;
        private StackPanel _album;
        private bool _alive;
        private int _photos;

        private sealed class Part { public string Img, Name; public char Anchor; public double Wk, Yk; public Part(string i, string n, char a, double w, double y) { Img = i; Name = n; Anchor = a; Wk = w; Yk = y; } }

        private static readonly Part[] Eyes =
        {
            null,
            new Part("miroir-yeux-rigolos", "des yeux rigolos", 'c', 0.7, 0.34),
            new Part("miroir-lunettes-yeux", "des gros yeux à lunettes", 'c', 0.8, 0.34),
            new Part("miroir-lunettes-couleurs", "des lunettes de couleurs", 'c', 0.8, 0.34),
            new Part("miroir-lunettes-coeurs", "des lunettes cœurs", 'c', 0.8, 0.34),
            new Part("miroir-lunettes-etoiles", "des lunettes étoiles", 'c', 0.85, 0.34),
            new Part("miroir-lunettes-bleues", "des lunettes de soleil", 'c', 0.8, 0.34),
            new Part("miroir-masque-violet", "un masque violet", 'c', 0.85, 0.34),
            new Part("miroir-masque-rouge", "un masque rouge", 'c', 0.85, 0.34),
            new Part("miroir-cache-oeil", "un œil de pirate", 'c', 0.65, 0.34),
            new Part("miroir-lunettes-pixel", "des lunettes de star", 'c', 0.8, 0.34),
        };
        private static readonly Part[] Noses =
        {
            null,
            new Part("miroir-nez-rouge", "un nez rouge", 'c', 0.25, 0.52),
            new Part("miroir-lunettes-groucho", "un nez à lunettes et moustache", 'c', 0.8, 0.45),
            new Part("carotte", "un nez carotte", 'c', 0.3, 0.52),
            new Part("fraise", "un nez fraise", 'c', 0.22, 0.52),
        };
        private static readonly Part[] Mouths =
        {
            null,
            new Part("miroir-sourire", "un grand sourire", 'c', 0.5, 0.7),
            new Part("miroir-petit-sourire", "un petit sourire", 'c', 0.35, 0.7),
            new Part("miroir-langue", "une langue qui dépasse", 'c', 0.45, 0.72),
            new Part("miroir-bisou", "une bouche à bisous", 'c', 0.4, 0.7),
            new Part("miroir-cri", "une bouche qui crie", 'c', 0.3, 0.72),
            new Part("miroir-bouche-clown", "une bouche de clown", 'c', 0.55, 0.66),
            new Part("miroir-moustache-marron", "une moustache", 'c', 0.55, 0.64),
            new Part("miroir-moustache-frisee", "une moustache frisée", 'c', 0.55, 0.64),
            new Part("miroir-barbe-rousse", "une barbe rousse", 't', 1.0, 0.58),
            new Part("miroir-barbe-noire", "une barbe noire", 't', 1.0, 0.58),
        };
        private static readonly Part[] Hats =
        {
            null,
            new Part("miroir-couronne", "une couronne", 'b', 1.0, 0.14),
            new Part("miroir-chapeau-fete", "un chapeau de fête", 'b', 0.65, 0.1),
            new Part("miroir-haut-de-forme", "un haut-de-forme", 'b', 1.1, 0.14),
            new Part("miroir-cowboy", "un chapeau de cowboy", 'b', 1.4, 0.2),
            new Part("miroir-pirate", "un chapeau de pirate", 'b', 1.3, 0.2),
            new Part("miroir-bouffon", "un chapeau de bouffon", 'b', 1.3, 0.2),
            new Part("miroir-perruque-clown", "une perruque de clown", 'b', 1.5, 0.5),
            new Part("miroir-oreilles-lapin", "des oreilles de lapin", 'b', 1.1, 0.2),
            new Part("miroir-oreilles-ours", "des oreilles d'ours", 'b', 1.1, 0.18),
            new Part("miroir-serre-tete-etoiles", "un serre-tête à étoiles", 'b', 1.2, 0.2),
            new Part("miroir-antennes", "des antennes", 'b', 1.0, 0.12),
            new Part("miroir-casquette-detective", "une casquette de détective", 'b', 1.2, 0.22),
            new Part("miroir-gendarme", "un chapeau de gendarme", 'b', 1.3, 0.2),
            new Part("miroir-haut-de-forme-etoile", "un haut-de-forme étoilé", 'b', 1.1, 0.14),
            new Part("miroir-cowboy-bleu", "un chapeau de cowboy bleu", 'b', 1.4, 0.2),
        };
        private static readonly Part[] Bows =
        {
            null,
            new Part("miroir-noeud-rouge", "un nœud papillon rouge", 'c', 0.55, 0.93),
            new Part("miroir-noeud-arc-en-ciel", "un nœud arc-en-ciel", 'c', 0.55, 0.93),
            new Part("miroir-noeud-vert", "un nœud papillon vert", 'c', 0.55, 0.93),
            new Part("miroir-noeud-violet", "un nœud papillon violet", 'c', 0.55, 0.93),
            new Part("miroir-cravate-rayee", "une cravate rayée", 't', 0.28, 0.86),
            new Part("miroir-cravate-pois", "une cravate à pois", 't', 0.28, 0.86),
            new Part("miroir-cravate-bleue", "une cravate bleue", 't', 0.28, 0.86),
        };
        private static readonly Part[] Bubbles =
        {
            null,
            new Part("miroir-bulle-wow", "une bulle Waouh", 'r', 0.7, 0),
            new Part("miroir-bulle-paf", "une bulle Paf", 'r', 0.7, 0),
            new Part("miroir-bulle-fun", "une bulle Fun", 'r', 0.75, 0),
            new Part("miroir-bulle-oulala", "une bulle Oh là là", 'r', 0.75, 0),
            new Part("miroir-bulle-points", "une bulle qui réfléchit", 'r', 0.6, 0),
        };
        private int _eye, _nose, _mouth, _hat, _bow, _bubble;

        public PotatoGame(Action celebrate) : base(celebrate) { GateVoice = false; }

        protected override void NewRound()
        {
            Locked = false;
            _alive = false;
            _eye = _nose = _mouth = _hat = _bow = _bubble = 0;
            Question.Text = "🥔 Patatou";
            SetConsigne(new TextBlock { Text = "🥔✨" }, () => "Voici Patatou ! Choisis ses yeux, son nez, sa bouche, son chapeau… et il prend vie !");

            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _canvas = new Canvas { Width = W, Height = H, ClipToBounds = true };
            _canvas.Background = MakeDecorBrush("decor-ferme", 0x30) ?? new LinearGradientBrush(Color.FromRgb(0xD8, 0xF0, 0xFF), Color.FromRgb(0xCF, 0xF2, 0xC8), 90);
            BuildBody();
            _parts = new Canvas { IsHitTestVisible = false };
            _canvas.Children.Add(_parts);
            _scene = new Grid();
            _scene.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _scene.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var frame = new Border { CornerRadius = new CornerRadius(28), ClipToBounds = true, Margin = new Thickness(10, 6, 10, 6), Child = new Viewbox { Child = _canvas, Stretch = Stretch.Uniform } };
            _scene.Children.Add(frame);
            _album = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 4) };
            Grid.SetRow(_album, 1);
            _scene.Children.Add(_album);
            root.Children.Add(_scene);

            var rail = new UniformGrid { Columns = 2, Margin = new Thickness(4, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center };
            rail.Children.Add(Tile("Yeux", Color.FromRgb(0x3F, 0x8E, 0xF2), () => Choose("des yeux", Eyes, _eye, i => _eye = i)));
            rail.Children.Add(Tile("Nez", Color.FromRgb(0xF2, 0x5F, 0x8F), () => Choose("un nez", Noses, _nose, i => _nose = i)));
            rail.Children.Add(Tile("Bouche", Color.FromRgb(0xF2, 0x3B, 0x5F), () => Choose("une bouche", Mouths, _mouth, i => _mouth = i)));
            rail.Children.Add(Tile("Chapeau", Color.FromRgb(0xFF, 0x9F, 0x43), () => Choose("un chapeau", Hats, _hat, i => _hat = i)));
            rail.Children.Add(Tile("Nœud", Color.FromRgb(0x8E, 0x5A, 0xF2), () => Choose("un nœud", Bows, _bow, i => _bow = i)));
            rail.Children.Add(Tile("Bulle", Color.FromRgb(0x5A, 0x8A, 0xA8), () => Choose("une bulle", Bubbles, _bubble, i => _bubble = i)));
            rail.Children.Add(Tile("Surprise !", Color.FromRgb(0xFF, 0x6B, 0x6B), Surprise));
            rail.Children.Add(Tile("Photo", Color.FromRgb(0x3B, 0x2A, 0x5A), TakePhoto));
            rail.Children.Add(Tile("Tout enlever", Color.FromRgb(0x8A, 0x8A, 0x9A), () => { _eye = _nose = _mouth = _hat = _bow = _bubble = 0; _alive = false; Redraw(); Speak("Patatou est tout nu !"); }));
            Grid.SetColumn(rail, 1);
            root.Children.Add(rail);

            _gallery = new PropGallery();
            Grid.SetColumnSpan(_gallery, 2);
            root.Children.Add(_gallery);

            SetBodyFill(root);
            Redraw();
        }

        // Le corps : une grosse patate avec des bras, des mains et des chaussures.
        private double BodyTop => H * 0.5 - BH / 2 + 40;
        private double BodyCx => W / 2;

        private void BuildBody()
        {
            double cx = BodyCx, top = BodyTop;
            var ink = new SolidColorBrush(Color.FromRgb(0x5A, 0x3A, 0x1A));
            // Les chaussures.
            foreach (int side in new[] { -1, 1 })
            {
                var shoe = new Ellipse { Width = 150, Height = 70, Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0x8E, 0xF2)), Stroke = ink, StrokeThickness = 4 };
                Canvas.SetLeft(shoe, cx + side * 90 - 75 + side * 20); Canvas.SetTop(shoe, top + BH - 30);
                _canvas.Children.Add(shoe);
            }
            // Les bras et les mains (images de la planche).
            foreach (int side in new[] { -1, 1 })
            {
                var arm = new Line { X1 = cx + side * BW * 0.45, Y1 = top + BH * 0.55, X2 = cx + side * BW * 0.75, Y2 = top + BH * 0.42, Stroke = ink, StrokeThickness = 14, StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round };
                _canvas.Children.Add(arm);
                var hand = Art.Find(side < 0 ? "miroir-main" : "miroir-pouce");
                if (hand != null)
                {
                    var img = new Image { Source = hand, Width = 110, Stretch = Stretch.Uniform, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform(side < 0 ? 1 : 1, 1) };
                    Canvas.SetLeft(img, cx + side * BW * 0.8 - 55); Canvas.SetTop(img, top + BH * 0.42 - 80);
                    _canvas.Children.Add(img);
                }
            }
            // Le corps.
            var body = new Ellipse
            {
                Width = BW, Height = BH,
                Fill = new RadialGradientBrush(Color.FromRgb(0xE8, 0xB8, 0x7A), Color.FromRgb(0xB0, 0x7A, 0x3E)) { GradientOrigin = new Point(0.35, 0.3), Center = new Point(0.4, 0.35), RadiusX = 0.7, RadiusY = 0.7 },
                Stroke = ink, StrokeThickness = 5,
            };
            Canvas.SetLeft(body, cx - BW / 2); Canvas.SetTop(body, top);
            _canvas.Children.Add(body);
            // Quelques petits points de patate.
            for (int i = 0; i < 7; i++)
            {
                var dot = new Ellipse { Width = 10 + _rng.Next(8), Height = 8 + _rng.Next(6), Fill = new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x5A, 0x2B)) };
                double a = _rng.NextDouble() * 6.28, r = 0.55 + _rng.NextDouble() * 0.3;
                Canvas.SetLeft(dot, cx + Math.Cos(a) * BW / 2 * r); Canvas.SetTop(dot, top + BH / 2 + Math.Sin(a) * BH / 2 * r);
                _canvas.Children.Add(dot);
            }
        }

        private void Choose(string what, Part[] list, int current, Action<int> pick)
        {
            if (Closed) return;
            var items = list.Select(p => p == null ? null : new PropGallery.Item { Name = p.Name, Image = p.Img }).ToList();
            // On ne propose que les images réellement présentes.
            var map = new List<int>(); var shown = new List<PropGallery.Item>();
            for (int i = 0; i < items.Count; i++)
                if (items[i] == null || Art.Find(items[i].Image) != null) { map.Add(i); shown.Add(items[i]); }
            _gallery.Open(what, shown, map.IndexOf(current), k => { pick(map[k]); Redraw(); CheckAlive(); });
        }

        private void Redraw()
        {
            _parts.Children.Clear();
            Place(Bows[_bow]); Place(Mouths[_mouth]); Place(Eyes[_eye]); Place(Noses[_nose]); Place(Hats[_hat]); Place(Bubbles[_bubble]);
        }

        private void Place(Part p)
        {
            if (p == null) return;
            var src = Art.Find(p.Img);
            if (src == null) return;
            double width = BW * p.Wk, height = width * src.Height / Math.Max(1.0, src.Width);
            double cx = BodyCx, top = BodyTop;
            var img = new Image { Source = src, Width = width, Height = height, Stretch = Stretch.Fill };
            double left, y;
            switch (p.Anchor)
            {
                case 'b': left = cx - width / 2; y = top + BH * p.Yk - height; break;
                case 't': left = cx - width / 2; y = top + BH * p.Yk; break;
                case 'r': left = cx + BW * 0.45; y = top - BH * 0.1; break;
                default: left = cx - width / 2; y = top + BH * p.Yk - height / 2; break;
            }
            Canvas.SetLeft(img, left); Canvas.SetTop(img, y);
            _parts.Children.Add(img);
        }

        private void CheckAlive()
        {
            if (_alive || _eye == 0 || _nose == 0 || _mouth == 0) return;
            _alive = true;
            string name = Settings.Load().ChildName ?? "toi";
            SoundFx.Whistle();
            Speak("Oh ! Patatou prend vie ! Bonjour " + name + ", je m'appelle Patatou ! Je suis trop beau !");
            Wobble();
            RewardStar();
        }

        private void Wobble()
        {
            var rot = new RotateTransform(0);
            _canvas.RenderTransformOrigin = new Point(0.5, 0.9);
            _canvas.RenderTransform = rot;
            rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-6, 6, TimeSpan.FromMilliseconds(220)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(6), EasingFunction = new SineEase() });
        }

        private void Surprise()
        {
            if (Closed) return;
            SoundFx.Drum();
            Speak("Roulement de tambour…");
            Schedule(900, () =>
            {
                _eye = 1 + _rng.Next(Eyes.Length - 1);
                _nose = 1 + _rng.Next(Noses.Length - 1);
                _mouth = 1 + _rng.Next(Mouths.Length - 1);
                _hat = 1 + _rng.Next(Hats.Length - 1);
                _bow = _rng.Next(Bows.Length);
                _bubble = _rng.Next(3) == 0 ? _rng.Next(Bubbles.Length) : 0;
                Redraw();
                SoundFx.Boom();
                Speak("Surprise ! " + Eyes[_eye].Name + ", " + Noses[_nose].Name + ", " + Mouths[_mouth].Name + " et " + Hats[_hat].Name + " !");
                _alive = false;
                CheckAlive();
            });
        }

        private void TakePhoto()
        {
            if (Closed) return;
            try
            {
                SoundFx.PopSound();
                var target = (FrameworkElement)_scene.Children[0];
                int pw = Math.Max(2, (int)target.ActualWidth), ph = Math.Max(2, (int)target.ActualHeight);
                var rtb = new RenderTargetBitmap(pw, ph, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(target); rtb.Freeze();
                var dir = System.IO.Path.Combine(UserContent.RootDir, "Patatou");
                Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "patatou-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
                using (var fs = File.Create(path)) { var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb)); enc.Save(fs); }
                _photos++;
                var polaroid = new Border
                {
                    Background = Brushes.White, Padding = new Thickness(6, 6, 6, 14), Margin = new Thickness(6, 0, 6, 0),
                    Child = new Image { Source = rtb, Width = 110, Height = 110 * ph / Math.Max(1.0, pw), Stretch = Stretch.UniformToFill },
                    RenderTransform = new RotateTransform(_rng.Next(-6, 7)), RenderTransformOrigin = new Point(0.5, 0.5),
                };
                _album.Children.Insert(0, polaroid);
                while (_album.Children.Count > 5) _album.Children.RemoveAt(_album.Children.Count - 1);
                Speak("Clic-clac ! La photo de Patatou est dans l'album.");
                if (_photos == 1) RewardStar();
            }
            catch (Exception ex) { Log.Write("patatou", "Photo : " + ex.Message); }
        }

        private Button Tile(string label, Color color, Action onClick)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Content = new TextBlock { Text = label, FontSize = 22, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
                Width = 172, Height = 92, Background = new SolidColorBrush(color), BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)), Margin = new Thickness(4),
            };
            b.Click += (s, e) => { if (!Locked) onClick(); };
            return b;
        }
    }
}
