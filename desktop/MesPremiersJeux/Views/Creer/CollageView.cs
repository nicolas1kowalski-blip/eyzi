using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🧩 TAMPONS ET COLLAGE : une scène (la ferme, la mer, la forêt, la nuit, la
    /// maison, Noël, sous la mer, l'espace, ou une feuille blanche) et des choses à
    /// coller d'un regard là où l'on veut : animaux, nature, gens (les photos de la
    /// famille aussi), mer et espace, formes, et des PAPIERS DÉCHIRÉS de toutes les
    /// couleurs (unis, à pois, à rayures), jamais deux fois le même morceau. Chaque
    /// chose se pose un peu de travers, comme un vrai collage, et la tablette la nomme.
    /// </summary>
    public sealed class CollageView : UserControl
    {
        private const int W = 1280, H = 800;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private sealed class Scene { public string Nom, Emoji; public Action<Canvas> Peindre; }
        private sealed class Tampon { public string Emoji, Nom, Photo, Papier, Motif; } // Papier : couleur d'un papier déchiré ; Motif : uni | pois | rayures

        private readonly Canvas _scene = new Canvas { Width = W, Height = H, ClipToBounds = true, Background = Brushes.White };
        private readonly Canvas _fond = new Canvas { Width = W, Height = H, IsHitTestVisible = false };
        private readonly Canvas _fx = new Canvas { Width = W, Height = H, IsHitTestVisible = false, ClipToBounds = true };
        private readonly UniformGrid _tampons = new UniformGrid { Columns = 3 };
        private readonly UniformGrid _onglets = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 6) }; // sept familles : deux rangées
        private readonly TextBlock _titre;
        private readonly List<FrameworkElement> _poses = new List<FrameworkElement>();
        private readonly Random _rng = new Random();
        private Tampon _choisi;
        private int _sceneIdx;
        private bool _grand;
        private Border _apercu;

        private readonly Scene[] _scenes =
        {
            new Scene { Nom = "La ferme", Emoji = "🐄", Peindre = c => { Ciel(c, "#BFE6FF", "#EAF6FF"); Colline(c, "#8FD47A", 520, 1.0); Colline(c, "#6DBF5A", 640, 0.7); Soleil(c, 1080, 130); } },
            new Scene { Nom = "La mer", Emoji = "🌊", Peindre = c => { Ciel(c, "#9FD8FF", "#E6F5FF"); Rect(c, 0, 420, W, 240, "#3FA6E8"); Rect(c, 0, 640, W, 160, "#F6E0A8"); Soleil(c, 200, 120); } },
            new Scene { Nom = "La forêt", Emoji = "🌲", Peindre = c => { Ciel(c, "#CFEFD8", "#F0FAF2"); Colline(c, "#5FAE63", 480, 1.1); Colline(c, "#3E8E48", 620, 0.8); Rect(c, 0, 700, W, 100, "#8B6B3E"); } },
            new Scene { Nom = "La nuit", Emoji = "🌙", Peindre = c => { Ciel(c, "#1B2A5C", "#3A4F9B"); Colline(c, "#20355A", 600, 1.0); Lune(c, 1050, 140); for (int i = 0; i < 40; i++) Etoile(c); } },
            new Scene { Nom = "La maison", Emoji = "🏠", Peindre = c => { Rect(c, 0, 0, W, 560, "#FFF0D6"); Rect(c, 0, 560, W, 240, "#D9A66B"); Rect(c, 120, 90, 300, 260, "#BFE6FF"); Rect(c, 120, 90, 300, 260, null, "#FFFFFF", 14); } },
            new Scene { Nom = "Noël", Emoji = "🎄", Peindre = c => { Ciel(c, "#2B3F7E", "#6D86C9"); Rect(c, 0, 600, W, 200, "#FFFFFF"); for (int i = 0; i < 60; i++) Flocon(c); } },
            new Scene { Nom = "Sous la mer", Emoji = "🐠", Peindre = c => { Ciel(c, "#49B6E8", "#0B3F7A"); Rect(c, 0, 690, W, 110, "#E8D29A"); for (int i = 0; i < 26; i++) Bulle(c); } },
            new Scene { Nom = "L'espace", Emoji = "🚀", Peindre = c => { Ciel(c, "#0B1030", "#2A1B5C"); for (int i = 0; i < 90; i++) Etoile(c, H); Planete(c, 1040, 150, 120, "#F2A65A"); Planete(c, 190, 610, 200, "#6FB7E8"); } },
            new Scene { Nom = "La feuille blanche", Emoji = "📄", Peindre = c => { Rect(c, 0, 0, W, H, "#FFFDF7"); } },
        };

        private static readonly (string Onglet, string Emoji, List<Tampon> Items)[] Categories =
        {
            ("Animaux", "🐾", L(("🐱", "un chat"), ("🐶", "un chien"), ("🐰", "un lapin"), ("🐴", "un cheval"), ("🐔", "une poule"), ("🐮", "une vache"), ("🐑", "un mouton"), ("🐷", "un cochon"), ("🦆", "un canard"), ("🐦", "un oiseau"), ("🐟", "un poisson"), ("🦋", "un papillon"), ("🐞", "une coccinelle"), ("🦁", "un lion"), ("🐘", "un éléphant"), ("🦒", "une girafe"), ("🐢", "une tortue"), ("🦄", "une licorne"))),
            ("Nature", "🌳", L(("🌳", "un arbre"), ("🌲", "un sapin"), ("🌸", "une fleur"), ("🌻", "un tournesol"), ("🌈", "un arc-en-ciel"), ("☀️", "le soleil"), ("⛅", "un nuage"), ("🌙", "la lune"), ("⭐", "une étoile"), ("❄️", "un flocon"), ("🍄", "un champignon"), ("🪨", "un rocher"), ("🌊", "une vague"), ("🍎", "une pomme"), ("🌷", "une tulipe"), ("🍂", "une feuille"))),
            ("Gens", "🧒", L(("🧒", "un enfant"), ("👧", "une fille"), ("👦", "un garçon"), ("👩", "une maman"), ("👨", "un papa"), ("👵", "une mamie"), ("👴", "un papi"), ("👶", "un bébé"), ("🏠", "une maison"), ("🚗", "une voiture"), ("🚲", "un vélo"), ("⛵", "un bateau"), ("🎈", "un ballon"), ("🎁", "un cadeau"), ("🎄", "un sapin de Noël"), ("🎅", "le Père Noël"), ("⛄", "un bonhomme de neige"), ("🍰", "un gâteau"))),
            ("Mer et espace", "🚀", L(("🐠", "un poisson"), ("🐙", "une pieuvre"), ("🐬", "un dauphin"), ("🐳", "une baleine"), ("🦀", "un crabe"), ("🐚", "un coquillage"), ("⭐", "une étoile de mer"), ("🪸", "du corail"), ("⚓", "une ancre"), ("🚀", "une fusée"), ("🛸", "une soucoupe"), ("👽", "un extraterrestre"), ("🪐", "une planète"), ("🌍", "la Terre"), ("🌙", "la lune"), ("☄️", "une comète"), ("👩‍🚀", "une astronaute"), ("🛰️", "un satellite"))),
            ("Papiers", "📄", Papiers()),
            ("Formes", "⭐", L(("❤️", "un cœur"), ("⭐", "une étoile"), ("🔵", "un rond bleu"), ("🟡", "un rond jaune"), ("🟢", "un rond vert"), ("🔴", "un rond rouge"), ("🟣", "un rond violet"), ("🔺", "un triangle"), ("🟦", "un carré bleu"), ("🟨", "un carré jaune"), ("💜", "un cœur violet"), ("✨", "des étincelles"))),
        };
        private static List<Tampon> L(params (string, string)[] items) => items.Select(i => new Tampon { Emoji = i.Item1, Nom = i.Item2 }).ToList();

        /// <summary>Les papiers à déchirer : douze couleurs unies, trois à pois, trois à rayures.</summary>
        private static List<Tampon> Papiers()
        {
            var l = new List<Tampon>();
            foreach (var (hex, nom) in new[] { ("#E53935", "rouge"), ("#FB8C00", "orange"), ("#FDD835", "jaune"), ("#43A047", "vert"), ("#1E88E5", "bleu"), ("#8E24AA", "violet"), ("#EC6FA8", "rose"), ("#6D4C41", "marron"), ("#FFFFFF", "blanc"), ("#263238", "noir"), ("#4FC3F7", "bleu clair"), ("#A5D66B", "vert clair") })
                l.Add(new Tampon { Emoji = "📄", Nom = "un papier " + nom, Papier = hex, Motif = "uni" });
            foreach (var (hex, nom) in new[] { ("#E53935", "rouge"), ("#1E88E5", "bleu"), ("#43A047", "vert") })
                l.Add(new Tampon { Emoji = "📄", Nom = "un papier " + nom + " à pois", Papier = hex, Motif = "pois" });
            foreach (var (hex, nom) in new[] { ("#FB8C00", "orange"), ("#8E24AA", "violet"), ("#EC6FA8", "rose") })
                l.Add(new Tampon { Emoji = "📄", Nom = "un papier " + nom + " à rayures", Papier = hex, Motif = "rayures" });
            return l;
        }

        /// <summary>Un morceau de papier DÉCHIRÉ : un contour irrégulier, jamais deux fois le même, dans la couleur et le motif du papier.</summary>
        private FrameworkElement Dechirer(Tampon t, double size)
        {
            var c = C(t.Papier);
            int n = 14 + _rng.Next(6);
            var pts = new PointCollection();
            double rx = size * (0.36 + _rng.NextDouble() * 0.12), ry = size * (0.28 + _rng.NextDouble() * 0.14);
            for (int i = 0; i < n; i++)
            {
                double a = i * 2 * Math.PI / n, k = 0.78 + _rng.NextDouble() * 0.30;
                pts.Add(new Point(size / 2 + Math.Cos(a) * rx * k, size / 2 + Math.Sin(a) * ry * k));
            }
            Brush fond = new SolidColorBrush(c);
            var clair = Color.FromRgb((byte)Math.Min(255, c.R + 70), (byte)Math.Min(255, c.G + 70), (byte)Math.Min(255, c.B + 70));
            if (t.Motif == "pois")
            {
                var g = new DrawingGroup();
                g.Children.Add(new GeometryDrawing(new SolidColorBrush(c), null, new RectangleGeometry(new Rect(0, 0, 28, 28))));
                g.Children.Add(new GeometryDrawing(new SolidColorBrush(clair), null, new EllipseGeometry(new Point(14, 14), 6, 6)));
                fond = new DrawingBrush(g) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 28, 28), ViewportUnits = BrushMappingMode.Absolute };
            }
            else if (t.Motif == "rayures")
            {
                var g = new DrawingGroup();
                g.Children.Add(new GeometryDrawing(new SolidColorBrush(c), null, new RectangleGeometry(new Rect(0, 0, 24, 24))));
                g.Children.Add(new GeometryDrawing(new SolidColorBrush(clair), null, new RectangleGeometry(new Rect(0, 0, 24, 9))));
                fond = new DrawingBrush(g) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 24, 24), ViewportUnits = BrushMappingMode.Absolute };
            }
            // Le bord blanc du papier déchiré, puis le papier ; une ombre légère le décolle de la scène.
            var bord = new Polygon { Points = pts, Fill = Brushes.White, Stroke = Brushes.White, StrokeThickness = size * 0.045, StrokeLineJoin = PenLineJoin.Round };
            var papier = new Polygon { Points = pts, Fill = fond };
            return new Grid { Width = size, Height = size, Children = { bord, papier }, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 3, Opacity = 0.28 } };
        }

        private FrameworkElement Visuel(Tampon t, double size)
            => t.Papier != null ? Dechirer(t, size) : t.Photo != null ? Photo(t.Photo, size) : Art.ForEmoji(t.Emoji, size);

        public CollageView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF7, 0xFF)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Gauche : retour, scène, outils
            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 170 };
            var back = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            back.Click += (s, e) => BackRequested?.Invoke();
            gauche.Children.Add(back);
            _titre = new TextBlock { FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 0, 8) };
            gauche.Children.Add(_titre);
            gauche.Children.Add(Outil("🎲", "Une autre scène", () => ChangerScene(_sceneIdx + 1)));
            gauche.Children.Add(Outil("🔍", "Petit ou grand", () => { _grand = !_grand; Speech.Say(_grand ? "En grand !" : "En petit !"); }));
            gauche.Children.Add(Outil("↩️", "Enlever le dernier", EnleverDernier));
            gauche.Children.Add(Outil("🧹", "Tout enlever", () => { foreach (var p in _poses) _scene.Children.Remove(p); _poses.Clear(); Speech.Say("La scène est vide !"); }));
            gauche.Children.Add(Outil("💾", "Garder dans ma galerie", Garder));
            root.Children.Add(gauche);

            // Centre : la scène
            _scene.Children.Add(_fond);
            var pile = new Grid { Children = { _scene, _fx } };
            var cadre = new Border { CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.FromRgb(0xCF, 0xDF, 0xF5)), BorderThickness = new Thickness(2), Margin = new Thickness(2), Child = new Viewbox { Stretch = Stretch.Uniform, Child = pile } };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);
            _scene.MouseLeftButtonDown += (s, e) => Poser(e.GetPosition(_scene));

            // Droite : les tampons par famille
            var droite = new DockPanel { Margin = new Thickness(6, 8, 10, 8), Width = 330 };
            DockPanel.SetDock(_onglets, Dock.Top);
            droite.Children.Add(_onglets);
            _apercu = new Border { Height = 70, CornerRadius = new CornerRadius(12), Background = Brushes.White, BorderBrush = Dark, BorderThickness = new Thickness(2), Margin = new Thickness(4, 0, 4, 6) };
            DockPanel.SetDock(_apercu, Dock.Top);
            droite.Children.Add(_apercu);
            droite.Children.Add(_tampons);
            Grid.SetColumn(droite, 2);
            root.Children.Add(droite);
            Content = root;

            BuildOnglets();
            MontrerCategorie(0);
            ChangerScene(0, dire: false);
        }

        private Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip, HorizontalAlignment = HorizontalAlignment.Left };
            b.Click += (s, e) => action();
            return b;
        }

        private void BuildOnglets()
        {
            _onglets.Children.Clear();
            for (int i = 0; i < Categories.Length; i++)
            {
                int idx = i;
                var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 34, Child = Art.ForEmoji(Categories[i].Emoji, 34) }, MinWidth = 0, Margin = new Thickness(2), ToolTip = Categories[i].Onglet };
                b.Click += (s, e) => { MontrerCategorie(idx); Speech.Say(Categories[idx].Onglet); };
                _onglets.Children.Add(b);
            }
            var fam = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 34, Child = Art.ForEmoji("📷", 34) }, MinWidth = 0, Margin = new Thickness(2), ToolTip = "Ma famille" };
            fam.Click += (s, e) => { MontrerFamille(); Speech.Say("Ma famille"); };
            _onglets.Children.Add(fam);
        }

        private void MontrerCategorie(int idx)
        {
            _tampons.Children.Clear();
            foreach (var t in Categories[idx].Items) _tampons.Children.Add(TuileTampon(t, Visuel(t, 64)));
            if (_choisi == null) Choisir(Categories[idx].Items[0]);
        }

        private void MontrerFamille()
        {
            _tampons.Children.Clear();
            var fam = UserContent.LoadFamily();
            if (fam.Count == 0) { _tampons.Children.Add(new TextBlock { Text = "Ajoute des photos de la famille dans l'administration (page Contenu).", FontSize = 16, Foreground = Dark, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) }); return; }
            foreach (var (nom, path) in fam)
            {
                var t = new Tampon { Nom = nom, Photo = path, Emoji = "🙂" };
                _tampons.Children.Add(TuileTampon(t, Photo(path, 64)));
            }
        }

        private static FrameworkElement Photo(string path, double size)
        {
            try { return new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Child = new Image { Source = UserContent.LoadBitmap(path, 400), Stretch = Stretch.UniformToFill }, ClipToBounds = true }; }
            catch { return Art.ForEmoji("🙂", size); }
        }

        private Button TuileTampon(Tampon t, FrameworkElement visuel)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = double.NaN, Height = double.NaN, MinHeight = 70, Margin = new Thickness(4), Background = Brushes.White, Content = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(8), Child = visuel }, ToolTip = t.Nom };
            b.Click += (s, e) => { Choisir(t); Speech.Say(t.Nom); };
            return b;
        }

        private void Choisir(Tampon t)
        {
            _choisi = t;
            _apercu.Child = new Viewbox { Margin = new Thickness(6), Child = Visuel(t, 56) };
        }

        private void ChangerScene(int idx, bool dire = true)
        {
            _sceneIdx = ((idx % _scenes.Length) + _scenes.Length) % _scenes.Length;
            var sc = _scenes[_sceneIdx];
            _fond.Children.Clear();
            sc.Peindre(_fond);
            foreach (var p in _poses) _scene.Children.Remove(p);
            _poses.Clear();
            _titre.Text = sc.Emoji + " " + sc.Nom;
            Emoji.Fix(_titre);
            if (dire) Speech.Say(sc.Nom + " !");
        }

        /// <summary>Un regard posé sur la scène : le tampon s'y colle, un peu de travers, en grandissant.</summary>
        private void Poser(Point p)
        {
            if (_choisi == null) return;
            double size = _grand ? 230 : 140;
            if (_choisi.Papier != null) size *= 1.5; // un papier déchiré est un grand morceau
            FrameworkElement vis = Visuel(_choisi, size);
            var holder = new Grid { Width = size, Height = size, RenderTransformOrigin = new Point(0.5, 0.5), Children = { vis } };
            var tg = new TransformGroup();
            var sc = new ScaleTransform(0.2, 0.2);
            tg.Children.Add(sc);
            tg.Children.Add(new RotateTransform(_rng.Next(-14, 15)));
            holder.RenderTransform = tg;
            Canvas.SetLeft(holder, p.X - size / 2);
            Canvas.SetTop(holder, p.Y - size / 2);
            _scene.Children.Add(holder);
            _poses.Add(holder);
            var pop = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut } };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            SoundFx.PopSound();
            Speech.Say(_choisi.Nom);
        }

        private void EnleverDernier()
        {
            if (_poses.Count == 0) { Speech.Say("Il n'y a rien à enlever."); return; }
            var last = _poses[_poses.Count - 1];
            _poses.RemoveAt(_poses.Count - 1);
            _scene.Children.Remove(last);
            SoundFx.Puff();
        }

        private void Garder()
        {
            if (_poses.Count == 0) { Speech.Say("Pose d'abord des tampons sur la scène !"); return; }
            var path = Creations.Garder(_scene, W, H, "collage");
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder le collage."); return; }
            Speech.Say("C'est gardé dans ta galerie ! Quel beau collage !");
            Celebrate?.Invoke(_fx);
        }

        // ------------------------------------------------------------------ les fonds de scène
        private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
        private static void Ciel(Canvas c, string haut, string bas) => c.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(C(haut), C(bas), 90) });
        private static void Rect(Canvas c, double x, double y, double w, double h, string fill, string stroke = null, double ep = 0)
        {
            var r = new Rectangle { Width = w, Height = h, Fill = fill == null ? null : new SolidColorBrush(C(fill)), Stroke = stroke == null ? null : new SolidColorBrush(C(stroke)), StrokeThickness = ep, RadiusX = 6, RadiusY = 6 };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y); c.Children.Add(r);
        }
        private static void Colline(Canvas c, string fill, double y, double k)
        {
            var e = new Ellipse { Width = W * 1.6 * k, Height = 520, Fill = new SolidColorBrush(C(fill)) };
            Canvas.SetLeft(e, (W - e.Width) / 2 + (k < 1 ? 240 : -120)); Canvas.SetTop(e, y); c.Children.Add(e);
            // Le bas de la colline est caché par le sol : une bande de la même couleur jusqu'en bas.
            double hSol = H - (y + 260);
            if (hSol <= 0) return; // la colline descend déjà sous le bas de la scène
            var sol = new Rectangle { Width = W, Height = hSol, Fill = new SolidColorBrush(C(fill)) };
            Canvas.SetLeft(sol, 0); Canvas.SetTop(sol, y + 260); c.Children.Add(sol);
        }
        private static void Soleil(Canvas c, double x, double y)
        {
            var halo = new Ellipse { Width = 220, Height = 220, Fill = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xE0, 0x66)) };
            Canvas.SetLeft(halo, x - 110); Canvas.SetTop(halo, y - 110); c.Children.Add(halo);
            var s = new Ellipse { Width = 140, Height = 140, Fill = new SolidColorBrush(C("#FFD54A")) };
            Canvas.SetLeft(s, x - 70); Canvas.SetTop(s, y - 70); c.Children.Add(s);
        }
        private static void Lune(Canvas c, double x, double y)
        {
            var s = new Ellipse { Width = 130, Height = 130, Fill = new SolidColorBrush(C("#FFF3B0")) };
            Canvas.SetLeft(s, x - 65); Canvas.SetTop(s, y - 65); c.Children.Add(s);
        }
        private static readonly Random Hasard = new Random(7);
        private static void Etoile(Canvas c, double hauteur = 520)
        {
            double r = 3 + Hasard.NextDouble() * 5;
            var e = new Ellipse { Width = r, Height = r, Fill = Brushes.White, Opacity = 0.5 + Hasard.NextDouble() * 0.5 };
            Canvas.SetLeft(e, Hasard.NextDouble() * W); Canvas.SetTop(e, Hasard.NextDouble() * hauteur); c.Children.Add(e);
        }
        private static void Bulle(Canvas c)
        {
            double r = 10 + Hasard.NextDouble() * 26;
            var e = new Ellipse { Width = r, Height = r, Stroke = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)), StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF)) };
            Canvas.SetLeft(e, Hasard.NextDouble() * W); Canvas.SetTop(e, Hasard.NextDouble() * 640); c.Children.Add(e);
        }
        private static void Planete(Canvas c, double x, double y, double d, string hex)
        {
            var col = C(hex);
            var sombre = Color.FromRgb((byte)(col.R * 0.55), (byte)(col.G * 0.55), (byte)(col.B * 0.55));
            var e = new Ellipse { Width = d, Height = d, Fill = new RadialGradientBrush(col, sombre) { GradientOrigin = new Point(0.3, 0.3) } };
            Canvas.SetLeft(e, x - d / 2); Canvas.SetTop(e, y - d / 2); c.Children.Add(e);
        }
        private static void Flocon(Canvas c)
        {
            double r = 6 + Hasard.NextDouble() * 10;
            var e = new Ellipse { Width = r, Height = r, Fill = Brushes.White, Opacity = 0.6 + Hasard.NextDouble() * 0.4 };
            Canvas.SetLeft(e, Hasard.NextDouble() * W); Canvas.SetTop(e, Hasard.NextDouble() * 600); c.Children.Add(e);
        }
    }
}
