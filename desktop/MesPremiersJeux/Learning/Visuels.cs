using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Learning
{
    /// <summary>Un objet à compter, à ranger, à comparer : image de la banque + nom parlé.</summary>
    public sealed class ObjetDef
    {
        public string Nom, Emoji, Sing, Plur;
        public bool Fem;
        public ObjetDef(string nom, string emoji, string sing, string plur, bool fem = false) { Nom = nom; Emoji = emoji; Sing = sing; Plur = plur; Fem = fem; }
        /// <summary>« 3 pommes », « 1 pomme ».</summary>
        public string N(int n) => n <= 1 ? Sing : n + " " + Plur;
        public FrameworkElement Visuel(double size) => Art.Visual(Nom, Emoji, size);
    }

    /// <summary>Les dessins des exercices : collections, chiffres, dés, boîtes de 10, formes, perles…</summary>
    public static class Visuels
    {
        public static readonly Random Rng = new Random();

        public static readonly ObjetDef[] Objets =
        {
            new ObjetDef("pomme", "🍎", "une pomme", "pommes", true),
            new ObjetDef("etoile", "⭐", "une étoile", "étoiles", true),
            new ObjetDef("ballon", "🎈", "un ballon", "ballons"),
            new ObjetDef("fleur", "🌸", "une fleur", "fleurs", true),
            new ObjetDef("poisson", "🐟", "un poisson", "poissons"),
            new ObjetDef("voiture", "🚗", "une voiture", "voitures", true),
            new ObjetDef("fraise", "🍓", "une fraise", "fraises", true),
            new ObjetDef("coeur", "❤️", "un cœur", "cœurs"),
            new ObjetDef("banane", "🍌", "une banane", "bananes", true),
            new ObjetDef("orange", "🍊", "une orange", "oranges", true),
            new ObjetDef("carotte", "🥕", "une carotte", "carottes", true),
            new ObjetDef("cadeau", "🎁", "un cadeau", "cadeaux"),
            new ObjetDef("canard", "🦆", "un canard", "canards"),
            new ObjetDef("papillon", "🦋", "un papillon", "papillons"),
        };

        public static ObjetDef Objet() => Objets[Rng.Next(Objets.Length)];
        public static ObjetDef AutreObjet(ObjetDef sauf) { ObjetDef o; do o = Objet(); while (o == sauf); return o; }

        public static readonly (string Nom, Color C)[] Couleurs =
        {
            ("rouge", Color.FromRgb(0xE8, 0x3A, 0x3A)), ("bleu", Color.FromRgb(0x3B, 0x9B, 0xFF)),
            ("jaune", Color.FromRgb(0xFF, 0xC1, 0x07)), ("vert", Color.FromRgb(0x4C, 0xB8, 0x5E)),
            ("orange", Color.FromRgb(0xFF, 0x8A, 0x2B)), ("violet", Color.FromRgb(0x9B, 0x5D, 0xE5)),
            ("rose", Color.FromRgb(0xFF, 0x6F, 0xB5)),
        };

        public static readonly string[] Formes = { "rond", "carre", "triangle", "rectangle", "ovale", "losange" };

        public static string NomForme(string f, bool article = true)
        {
            switch (f)
            {
                case "rond": return article ? "le rond" : "rond";
                case "carre": return article ? "le carré" : "carré";
                case "triangle": return article ? "le triangle" : "triangle";
                case "rectangle": return article ? "le rectangle" : "rectangle";
                case "ovale": return article ? "l'ovale" : "ovale";
                case "losange": return article ? "le losange" : "losange";
                default: return f;
            }
        }

        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        // ------------------------------------------------------------------
        //  Collections
        // ------------------------------------------------------------------
        /// <summary>n objets : en ligne (par 5), ou en désordre sur une scène.</summary>
        public static FrameworkElement Collection(ObjetDef o, int n, double item = 110, bool desordre = false, ObjetDef[] varies = null)
        {
            if (n <= 0) return new Border { Width = item, Height = item, Child = new TextBlock { Text = "·", FontSize = item * 0.6, Opacity = 0.3, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            if (desordre)
            {
                int cols = n <= 4 ? 3 : n <= 6 ? 4 : 5, rows = (n + cols - 1) / cols;
                double cell = item * 1.25;
                var c = new Canvas { Width = cols * cell, Height = Math.Max(rows, 2) * cell };
                var slots = Enumerable.Range(0, cols * Math.Max(rows, 2)).OrderBy(_ => Rng.Next()).Take(n).ToList();
                for (int i = 0; i < n; i++)
                {
                    var od = varies != null ? varies[i % varies.Length] : o;
                    var v = od.Visuel(item * (0.8 + Rng.NextDouble() * 0.25));
                    int sx = slots[i] % cols, sy = slots[i] / cols;
                    Canvas.SetLeft(v, sx * cell + Rng.NextDouble() * (cell - item));
                    Canvas.SetTop(v, sy * cell + Rng.NextDouble() * (cell - item));
                    c.Children.Add(v);
                }
                return c;
            }
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 5 * (item + 12) };
            for (int i = 0; i < n; i++)
            {
                var od = varies != null ? varies[i % varies.Length] : o;
                var v = od.Visuel(item);
                v.Margin = new Thickness(6);
                wrap.Children.Add(v);
            }
            return wrap;
        }

        /// <summary>Deux groupes côte à côte (« 2 pommes et 1 pomme »), avec un « + » ou un « − ».</summary>
        public static FrameworkElement DeuxGroupes(ObjetDef o, int a, int b, string signe, bool barre = false)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(Collection(o, a, 90));
            row.Children.Add(new TextBlock { Text = signe, FontSize = 80, FontWeight = FontWeights.Bold, Foreground = Ink, Margin = new Thickness(18, 0, 18, 0), VerticalAlignment = VerticalAlignment.Center });
            var g = Collection(o, b, 90);
            if (barre)
            {
                var grid = new Grid();
                grid.Children.Add(g);
                grid.Children.Add(new Line { X1 = 0, Y1 = 0, X2 = 1, Y2 = 1, Stretch = Stretch.Fill, Stroke = new SolidColorBrush(Color.FromRgb(0xE8, 0x3A, 0x3A)), StrokeThickness = 10, Opacity = 0.8 });
                row.Children.Add(grid);
            }
            else row.Children.Add(g);
            return row;
        }

        /// <summary>Des éléments côte à côte, centrés verticalement.</summary>
        public static FrameworkElement CoteACote(params FrameworkElement[] items)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var it in items) { it.VerticalAlignment = VerticalAlignment.Center; it.Margin = new Thickness(14, 0, 14, 0); row.Children.Add(it); }
            return row;
        }

        /// <summary>Un signe (+, −, et…) entre deux groupes.</summary>
        public static FrameworkElement Signe(string s) => new TextBlock { Text = s, FontSize = 70, FontWeight = FontWeights.Bold, Foreground = Ink, VerticalAlignment = VerticalAlignment.Center };

        /// <summary>La case « ? » (ce qui manque).</summary>
        public static FrameworkElement Trou(double size) => new Border
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC4)), BorderBrush = Ink, BorderThickness = new Thickness(3),
            Child = new TextBlock { Text = "?", FontSize = size * 0.6, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };

        // ------------------------------------------------------------------
        //  Les nombres : chiffre, dé, boîte de 10
        // ------------------------------------------------------------------
        public static FrameworkElement Chiffre(int n, double size = 150, Color? color = null)
        {
            if (Art.Find("chiffre-" + n) is ImageSource img)
                return new Image { Source = img, Width = size, Height = size, Stretch = Stretch.Uniform };
            return new Viewbox
            {
                Width = size, Height = size,
                Child = new TextBlock
                {
                    Text = n.ToString(), FontSize = 120, FontWeight = FontWeights.ExtraBold,
                    Foreground = new SolidColorBrush(color ?? Color.FromRgb(0xE8, 0x5A, 0x2C)),
                    Margin = new Thickness(8, 0, 8, 0),
                },
            };
        }

        private static readonly Dictionary<int, (double X, double Y)[]> Spots = new Dictionary<int, (double, double)[]>
        {
            [1] = new[] { (100.0, 100.0) },
            [2] = new[] { (58.0, 58.0), (142.0, 142.0) },
            [3] = new[] { (52.0, 52.0), (100.0, 100.0), (148.0, 148.0) },
            [4] = new[] { (58.0, 58.0), (142.0, 58.0), (58.0, 142.0), (142.0, 142.0) },
            [5] = new[] { (55.0, 55.0), (145.0, 55.0), (100.0, 100.0), (55.0, 145.0), (145.0, 145.0) },
            [6] = new[] { (58.0, 50.0), (142.0, 50.0), (58.0, 100.0), (142.0, 100.0), (58.0, 150.0), (142.0, 150.0) },
        };

        public static FrameworkElement De(int n, double size = 150)
        {
            var c = new Canvas { Width = 200, Height = 200 };
            c.Children.Add(new Rectangle { Width = 200, Height = 200, RadiusX = 34, RadiusY = 34, Fill = Brushes.White, Stroke = Ink, StrokeThickness = 8 });
            foreach (var (x, y) in Spots[Math.Max(1, Math.Min(6, n))])
            {
                var dot = new Ellipse { Width = 34, Height = 34, Fill = new SolidColorBrush(Color.FromRgb(0x2A, 0x22, 0x30)) };
                Canvas.SetLeft(dot, x - 17); Canvas.SetTop(dot, y - 17);
                c.Children.Add(dot);
            }
            return new Viewbox { Width = size, Height = size, Child = c };
        }

        /// <summary>La boîte de 10 (deux rangées de cinq cases), n jetons.</summary>
        public static FrameworkElement Boite10(int n, double width = 260)
        {
            var g = new UniformGrid { Columns = 5, Rows = 2, Width = 250, Height = 100 };
            for (int i = 0; i < 10; i++)
            {
                var cell = new Border { BorderBrush = Ink, BorderThickness = new Thickness(2), Background = Brushes.White };
                if (i < n) cell.Child = new Ellipse { Width = 34, Height = 34, Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x9B, 0xFF)), Margin = new Thickness(6) };
                g.Children.Add(cell);
            }
            return new Viewbox { Width = width, Child = g };
        }

        /// <summary>Une bande numérique : les nombres de a à b, avec un trou (« ? ») en « trou ».</summary>
        public static FrameworkElement Bande(int a, int b, int trou, double cell = 110)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = a; i <= b; i++)
            {
                var bd = new Border
                {
                    Width = cell, Height = cell, Margin = new Thickness(4), CornerRadius = new CornerRadius(14),
                    Background = i == trou ? new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC4)) : Brushes.White,
                    BorderBrush = Ink, BorderThickness = new Thickness(3),
                    Child = new Viewbox
                    {
                        Margin = new Thickness(10),
                        Child = new TextBlock { Text = i == trou ? "?" : i.ToString(), FontSize = 80, FontWeight = FontWeights.ExtraBold, Foreground = i == trou ? new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)) : Ink },
                    },
                };
                row.Children.Add(bd);
            }
            return row;
        }

        // ------------------------------------------------------------------
        //  Formes, grandeurs, perles
        // ------------------------------------------------------------------
        public static FrameworkElement Forme(string kind, Color color, double size = 150, double rotation = 0)
        {
            var fill = new SolidColorBrush(color);
            var stroke = new SolidColorBrush(Color.FromArgb(0x60, 0, 0, 0));
            Shape s;
            double w = size, h = size;
            switch (kind)
            {
                case "rond": s = new Ellipse(); break;
                case "ovale": s = new Ellipse(); h = size * 0.6; break;
                case "carre": s = new Rectangle(); break;
                case "rectangle": s = new Rectangle(); h = size * 0.55; break;
                case "triangle": s = new Polygon { Points = new PointCollection { new Point(size / 2, 0), new Point(size, size), new Point(0, size) } }; break;
                case "losange": s = new Polygon { Points = new PointCollection { new Point(size / 2, 0), new Point(size * 0.85, size / 2), new Point(size / 2, size), new Point(size * 0.15, size / 2) } }; break;
                default: s = new Rectangle(); break;
            }
            s.Width = w; s.Height = h; s.Fill = fill; s.Stroke = stroke; s.StrokeThickness = 4;
            s.StrokeLineJoin = PenLineJoin.Round;
            s.HorizontalAlignment = HorizontalAlignment.Center; s.VerticalAlignment = VerticalAlignment.Center;
            if (rotation != 0) { s.RenderTransformOrigin = new Point(0.5, 0.5); s.RenderTransform = new RotateTransform(rotation); }
            return new Grid { Width = size, Height = size, Children = { s } };
        }

        /// <summary>Un objet de taille donnée dans une case fixe (pour grand/petit, ordonner).</summary>
        public static FrameworkElement Taille(ObjetDef o, double size, double box = 170)
        {
            var g = new Grid { Width = box, Height = box };
            var v = o.Visuel(size);
            v.VerticalAlignment = VerticalAlignment.Bottom;
            g.Children.Add(v);
            return g;
        }

        /// <summary>Une barre (long / court) ou une colonne (gros / mince).</summary>
        public static FrameworkElement Barre(double length, double thick, Color color, bool vertical = false)
        {
            var r = new Rectangle { Width = vertical ? thick : length, Height = vertical ? length : thick, RadiusX = thick / 2, RadiusY = thick / 2, Fill = new SolidColorBrush(color), Stroke = new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0)), StrokeThickness = 3 };
            return new Grid { Width = 260, Height = 170, Children = { r }, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        }

        /// <summary>Un verre plus ou moins rempli (plein / vide).</summary>
        public static FrameworkElement Verre(double fraction, double size = 150)
        {
            var c = new Canvas { Width = 120, Height = 160 };
            double lvl = 150 - 140 * fraction;
            c.Children.Add(new Rectangle { Width = 90, Height = 140 * fraction, Fill = new SolidColorBrush(Color.FromRgb(0x6F, 0xC3, 0xFF)) });
            Canvas.SetLeft(c.Children[0], 15); Canvas.SetTop(c.Children[0], lvl);
            c.Children.Add(new Rectangle { Width = 90, Height = 140, Stroke = Ink, StrokeThickness = 6, RadiusX = 6, RadiusY = 6 });
            Canvas.SetLeft(c.Children[1], 15); Canvas.SetTop(c.Children[1], 10);
            return new Viewbox { Width = size, Height = size, Child = c };
        }

        /// <summary>Une perle ronde de couleur (suites, colliers).</summary>
        public static FrameworkElement Perle(Color color, double size = 90) => Forme("rond", color, size);

        /// <summary>Une rangée de cartes avec un trou « ? » (suites, bandes).</summary>
        public static FrameworkElement Rangee(IList<Func<FrameworkElement>> items, int trou, double size = 100)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < items.Count; i++)
            {
                FrameworkElement v;
                if (i == trou)
                    v = new Border
                    {
                        Width = size, Height = size, CornerRadius = new CornerRadius(16),
                        Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC4)), BorderBrush = Ink, BorderThickness = new Thickness(3),
                        Child = new TextBlock { Text = "?", FontSize = size * 0.6, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    };
                else { v = items[i](); v.Width = size; v.Height = size; }
                v.Margin = new Thickness(6);
                row.Children.Add(v);
            }
            return row;
        }

        /// <summary>Les cartes OUI (✔ vert) et NON (✘ rouge).</summary>
        public static FrameworkElement OuiNon(bool oui, double size = 150)
        {
            var c = new Canvas { Width = 200, Height = 200 };
            c.Children.Add(new Ellipse { Width = 200, Height = 200, Fill = new SolidColorBrush(oui ? Color.FromRgb(0x4C, 0xB8, 0x5E) : Color.FromRgb(0xE8, 0x3A, 0x3A)) });
            var pen = new SolidColorBrush(Colors.White);
            if (oui)
                c.Children.Add(new Polyline { Points = new PointCollection { new Point(50, 105), new Point(88, 145), new Point(152, 62) }, Stroke = pen, StrokeThickness = 22, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            else
            {
                c.Children.Add(new Line { X1 = 62, Y1 = 62, X2 = 138, Y2 = 138, Stroke = pen, StrokeThickness = 22, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
                c.Children.Add(new Line { X1 = 138, Y1 = 62, X2 = 62, Y2 = 138, Stroke = pen, StrokeThickness = 22, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            }
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            st.Children.Add(new Viewbox { Width = size * 0.7, Height = size * 0.7, Child = c });
            st.Children.Add(new TextBlock { Text = oui ? "oui" : "non", FontSize = size * 0.2, FontWeight = FontWeights.Bold, Foreground = Ink, HorizontalAlignment = HorizontalAlignment.Center });
            return st;
        }

        /// <summary>Une boîte de tri : une étiquette visuelle (forme, couleur…) sur un panier.</summary>
        public static FrameworkElement Boite(FrameworkElement etiquette, string label, double size = 190)
        {
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var box = new Border
            {
                Width = size * 0.8, Height = size * 0.6, CornerRadius = new CornerRadius(10, 10, 26, 26),
                Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xE4, 0xC8)), BorderBrush = new SolidColorBrush(Color.FromRgb(0xB5, 0x7A, 0x45)), BorderThickness = new Thickness(4),
                Child = new Viewbox { Margin = new Thickness(12), Child = etiquette },
            };
            st.Children.Add(box);
            if (!string.IsNullOrEmpty(label)) st.Children.Add(new TextBlock { Text = label, FontSize = size * 0.14, FontWeight = FontWeights.Bold, Foreground = Ink, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            return st;
        }

        /// <summary>Un pavé de couleur (étiquette de boîte « rouge »).</summary>
        public static FrameworkElement Pave(Color c, double size = 120) => new Rectangle { Width = size, Height = size * 0.7, RadiusX = 16, RadiusY = 16, Fill = new SolidColorBrush(c) };

        /// <summary>Des nombres distincts autour de « bon » : proches si demandé.</summary>
        public static List<int> Nombres(int bon, int count, int min, int max, bool proches = false)
        {
            var set = new HashSet<int> { bon };
            var pool = Enumerable.Range(min, max - min + 1).Where(i => i != bon).ToList();
            if (proches) pool = pool.OrderBy(i => Math.Abs(i - bon)).ThenBy(_ => Rng.Next()).ToList();
            else pool = pool.OrderBy(_ => Rng.Next()).ToList();
            foreach (var p in pool) { if (set.Count >= count) break; set.Add(p); }
            return set.OrderBy(_ => Rng.Next()).ToList();
        }

        public static T Un<T>(IList<T> l) => l[Rng.Next(l.Count)];
        public static List<T> Melange<T>(IEnumerable<T> l) => l.OrderBy(_ => Rng.Next()).ToList();
        public static int Entre(int min, int maxInclus) => Rng.Next(min, maxInclus + 1);
    }
}
