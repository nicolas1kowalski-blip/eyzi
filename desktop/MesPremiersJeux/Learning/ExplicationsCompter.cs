using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Learning
{
    /// <summary>
    /// Deuxième série d'explications animées : DÉNOMBRER (compter un par un, ranger en
    /// comptant, peu importe le sens) et RECONNAÎTRE LES CHIFFRES (le chiffre qui s'écrit,
    /// le même nombre sous toutes ses formes, sa place sur la bande).
    /// </summary>
    public static partial class Explications
    {
        // ------------------------------------------------------------------
        //  Les familles (elles dépendent du palier : jusqu'à 3, 6 ou 10)
        // ------------------------------------------------------------------
        public static Famille Denombrer(int max) => new Famille("denombrer", ("un-par-un", () => CompterUnParUn(max)), ("ranger", () => RangerEnComptant(max, false)), ("deux-sens", () => DeuxSens(max)));
        public static Famille MontrerCollection(int max) => new Famille("montrer-collection", ("chiffre", () => NombreVersCollection(max)), ("un-par-un", () => CompterUnParUn(max)));
        public static Famille DeAutant(int max) => new Famille("de-autant", ("de", () => CommeLeDe(max)), ("formes", () => Representations(Math.Min(6, max), "de")));
        public static Famille BoiteAutant(int max) => new Famille("boite-autant", ("ranger", () => RangerEnComptant(max, true)), ("formes", () => Representations(max, "boite")));
        public static Famille Chiffres(int max) => new Famille("chiffres", ("trace", () => Trace(max)), ("formes", () => Representations(max, "collection")), ("bande", () => SurLaBande(max)));
        public static Famille ChiffreDe(int max) => new Famille("chiffre-de", ("formes", () => Representations(Math.Min(6, max), "de")), ("de", () => CommeLeDe(max)));
        public static Famille ChiffreBoite(int max) => new Famille("chiffre-boite", ("formes", () => Representations(max, "boite")), ("ranger", () => RangerEnComptant(max, true)));
        public static readonly Famille RangerNombres = new Famille("ranger-nombres", ("tours", ToursRangees), ("bande", Bande));

        // ------------------------------------------------------------------
        //  Petits outils communs
        // ------------------------------------------------------------------
        /// <summary>Le nombre en toutes lettres jusqu'à dix (la voix lit bien les autres en chiffres).</summary>
        private static string Dit(int k) => k >= 0 && k <= 10 ? Mots[k] : k.ToString();

        /// <summary>n places en ligne, centrées, espacées de <paramref name="pas"/>.</summary>
        private static double[] Rang(int n, double pas, double cx = 640)
        {
            var xs = new double[n];
            for (int i = 0; i < n; i++) xs[i] = cx + (i - (n - 1) / 2.0) * pas;
            return xs;
        }

        /// <summary>Un dessin haut de <paramref name="h"/>, réduit s'il est plus large que la place qu'on lui laisse.</summary>
        private static Acteur Obj(Demo d, string nom, string emoji, double h, double largeurMax)
        {
            var a = d.Image(nom, emoji, h);
            if (a.W <= largeurMax) return a;
            d.Scene.Children.Remove(a.Boite);
            return d.Image(nom, emoji, h * largeurMax / a.W);
        }

        /// <summary>La pastille du compteur : un nombre dans un rond, posé au-dessus de ce qu'on vient de compter.</summary>
        private static Acteur Pastille(Demo d, int k, Color? fond = null, double t = 58)
        {
            var g = new Grid { Width = t, Height = t };
            g.Children.Add(new Ellipse { Fill = new SolidColorBrush(fond ?? Demo.Or), Stroke = Brushes.White, StrokeThickness = 4 });
            g.Children.Add(new Viewbox { Margin = new Thickness(t * 0.2), Child = new TextBlock { Text = k.ToString(), FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Demo.Encre) } });
            return d.Chose(g, t, t);
        }

        private static Acteur Chiffre(Demo d, int k, double t) => d.Chose(Visuels.Chiffre(k, t), t, t);

        /// <summary>Compte de <paramref name="de"/> à <paramref name="jusqua"/> (exclu) : la voix dit le nombre, l'objet saute, sa pastille apparaît.</summary>
        private static void Compter(Demo d, IList<Acteur> a, IList<Acteur> pastilles, int de, int jusqua, int ms = 850)
        {
            for (int i = de; i < jusqua; i++)
            {
                int k = i;
                d.Dire(Maj(Dit(k + 1)) + ".", () => { a[k].Sauter(22, 400); pastilles[k].Pop(); }, ms);
            }
        }

        /// <summary>Un compteur : un grand chiffre qui change à chaque objet compté.</summary>
        private sealed class Compteur
        {
            private readonly Acteur[] _c; private int _vu = -1;
            public Compteur(Demo d, int de, int a, double x, double sol, double t = 150)
            {
                _de = de; _c = new Acteur[a - de + 1];
                for (int k = de; k <= a; k++) _c[k - de] = Chiffre(d, k, t).En(x, sol).Opacite(0);
            }
            private readonly int _de;
            public void Montrer(int k)
            {
                if (_vu >= 0) _c[_vu].Fondu(0, 200);
                _vu = k - _de;
                if (_vu >= 0 && _vu < _c.Length) _c[_vu].Pop(120); else _vu = -1;
            }
            public Acteur Actuel => _vu >= 0 ? _c[_vu] : null;
            public Acteur De(int k) => _c[k - _de];
        }

        /// <summary>La bande des nombres posée sur le sol ; rend la fonction qui donne le milieu de chaque case.</summary>
        private static Func<int, double> PoserBande(Demo d, int de, int a, double cell, int trou = -1)
        {
            int n = a - de + 1; double pas = cell + 8;
            d.Chose(Visuels.Bande(de, a, trou, cell), n * pas, pas).En(640, Sol - 8 + pas);
            return k => 640 - n * pas / 2 + (k - de + 0.5) * pas;
        }

        private static int Combien(int max) => max <= 3 ? Visuels.Entre(2, 3) : max <= 6 ? Visuels.Entre(4, 6) : Visuels.Entre(6, 9);

        /// <summary>Les cubes d'une tour (posés, invisibles) : le cube i est à la hauteur i.</summary>
        private static Acteur[] Tour(Demo d, double x, int n, double t, Color c, Color? sommet = null)
        {
            var b = new Acteur[n];
            for (int i = 0; i < n; i++) b[i] = d.Bloc(t, i == n - 1 && sommet.HasValue ? sommet.Value : c).En(x, Sol - i * t).Opacite(0);
            return b;
        }

        // ==================================================================
        //  DÉNOMBRER
        // ==================================================================
        /// <summary>Un nombre par objet, dans l'ordre ; le dernier nombre dit combien il y en a.</summary>
        private static Demo CompterUnParUn(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int n = NombreCourant(1, 10, () => Combien(max));
            double pas = Math.Min(170, 1040.0 / n), t = Math.Min(130, pas - 24);
            var xs = Rang(n, pas);
            var a = new Acteur[n]; var p = new Acteur[n];
            for (int i = 0; i < n; i++)
            {
                a[i] = Obj(d, o.Nom, o.Emoji, t, pas - 14).En(xs[i], Sol - 40).Opacite(0);
                p[i] = Pastille(d, i + 1).En(xs[i], Sol - 40 - a[i].H - 14).Opacite(0);
            }
            var total = Chiffre(d, n, 150).En(640, 190).Opacite(0);
            d.Dire("Combien de " + o.Plur + " ? On compte, " + (o.Fem ? "une par une." : "un par un."), () => { for (int i = 0; i < n; i++) a[i].Pop(i * 120); }, 1900);
            Compter(d, a, p, 0, n - 1);
            d.Toi(n == 1 ? "À toi ! Regarde-" + (o.Fem ? "la" : "le") + " pour compter." : "À toi ! Regarde " + (o.Fem ? "la dernière" : "le dernier") + " pour finir de compter.", a[n - 1], () => { a[n - 1].Sauter(22, 400); p[n - 1].Pop(); }, 700);
            d.Dire(Maj(Dit(n)) + " !", () => total.Pop(), 1100);
            d.Dire("Le dernier nombre dit combien il y en a : " + o.N(n) + ".", () => { for (int i = 0; i < n; i++) a[i].Sauter(16, 380, i * 70); }, 1800);
            return d;
        }

        /// <summary>En désordre, on range chaque objet en le comptant : aucun oublié, aucun compté deux fois.</summary>
        private static Demo RangerEnComptant(int max, bool boite)
        {
            var d = new Demo("table");
            var o = ObjetCourant(); int n = NombreCourant(2, 10, () => boite ? (max <= 6 ? Visuels.Entre(3, 6) : Visuels.Entre(6, 10)) : Combien(max));
            const double C = 104;
            double x0 = 640 - 2.5 * C, y0 = 330;
            if (boite)
            {
                var cv = new Canvas { Width = 5 * C, Height = 2 * C };
                for (int i = 0; i < 10; i++)
                {
                    var r = new Rectangle { Width = C, Height = C, Fill = Brushes.White, Stroke = new SolidColorBrush(Demo.Encre), StrokeThickness = 4 };
                    Canvas.SetLeft(r, (i % 5) * C); Canvas.SetTop(r, (i / 5) * C); cv.Children.Add(r);
                }
                d.Chose(cv, 5 * C, 2 * C).En(640, y0 + 2 * C);
            }
            double t = boite ? 78 : Math.Min(100, 900.0 / n - 20);
            var ligne = Rang(n, Math.Min(130, 1000.0 / n));
            (double x, double y) Place(int i) => boite ? (x0 + (i % 5 + 0.5) * C, y0 + (i / 5 + 1) * C - 12) : (ligne[i], Sol - 30);
            double[] jx = { 0, 40, -30, 20, -40, 30, -20, 10, -35, 25 }, jy = { 0, -30, 20, -45, 10, -20, 30, -10, 40, -35 };
            var ordre = Visuels.Melange(Enumerable.Range(0, 10)).Take(n).ToList();
            var a = new Acteur[n];
            for (int i = 0; i < n; i++)
            {
                int s = ordre[i];
                a[i] = Obj(d, o.Nom, o.Emoji, t, t * 1.3).En(230 + (s % 5) * 190 + jx[s], 170 + (s / 5) * 105 + jy[s] * 0.6).Opacite(0);
            }
            var compteur = new Compteur(d, 1, n, 1160, boite ? 300 : 440, 130);
            d.Dire(Maj(o.Plur) + " en désordre. Combien y en a-t-il ?", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 100); }, 1900);
            d.Dire("Pour ne pas se tromper, on les range en comptant.", null, 1500);
            for (int i = 0; i < n - 1; i++)
            {
                int k = i; var pl = Place(i);
                d.Dire(Maj(Dit(k + 1)) + ".", () => { a[k].Devant(); a[k].Bond(pl.x, pl.y, 60, 650); compteur.Montrer(k + 1); }, 950);
            }
            var der = Place(n - 1);
            d.Toi("À toi ! Regarde " + (o.Fem ? "la dernière pour la ranger." : "le dernier pour le ranger."), a[n - 1], () => { a[n - 1].Devant(); a[n - 1].Bond(der.x, der.y, 60, 650); compteur.Montrer(n); }, 900);
            d.Dire(Maj(Dit(n)) + " !", null, 800);
            if (boite && n > 5) d.Dire("Cinq en haut, et " + Dit(n - 5) + " en bas : " + Dit(n) + ".", () => { for (int i = 0; i < n; i++) a[i].Sauter(12, 360, i * 60); }, 2000);
            else d.Dire("On n'en a oublié aucun, et on n'en a compté aucun deux fois.", () => { for (int i = 0; i < n; i++) a[i].Sauter(12, 360, i * 60); }, 2200);
            return d;
        }

        /// <summary>Dans un sens ou dans l'autre, on trouve le même nombre.</summary>
        private static Demo DeuxSens(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int n = max <= 3 ? 3 : Visuels.Entre(3, 5);
            double pas = 180;
            var xs = Rang(n, pas);
            var a = new Acteur[n]; var p = new Acteur[n]; var q = new Acteur[n];
            var bleu = Color.FromRgb(0x9F, 0xD2, 0xFF);
            for (int i = 0; i < n; i++)
            {
                a[i] = Obj(d, o.Nom, o.Emoji, 130, pas - 20).En(xs[i], Sol - 40).Opacite(0);
                p[i] = Pastille(d, i + 1).En(xs[i], Sol - 40 - a[i].H - 14).Opacite(0);
            }
            // Dans l'autre sens : le premier compté est celui de droite.
            var inv = new Acteur[n];
            for (int i = 0; i < n; i++) { inv[i] = a[n - 1 - i]; q[i] = Pastille(d, i + 1, bleu).En(xs[n - 1 - i], Sol - 40 - a[n - 1 - i].H - 14).Opacite(0); }
            var fleche = d.Image("", "➡️", 80).En(xs[0] - 150, Sol - 70).Opacite(0);
            var fleche2 = d.Image("", "⬅️", 80).En(xs[n - 1] + 150, Sol - 70).Opacite(0);
            d.Dire("Voici " + o.N(n) + ". On compte dans ce sens.", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 120); fleche.Pop(n * 120 + 200); }, 2000);
            Compter(d, a, p, 0, n);
            d.Dire(Maj(Dit(n)) + ".", null, 800);
            d.Toi("Et dans l'autre sens ? À toi ! Regarde " + (o.Fem ? "celle" : "celui") + " du bout pour recompter.", a[n - 1], () =>
            {
                for (int i = 0; i < n; i++) p[i].Fondu(0, 300);
                fleche.Fondu(0, 300); fleche2.Pop(200);
                inv[0].Sauter(22, 400, 400); q[0].Pop(400);
            }, 1100);
            d.Dire("Un.", null, 500);
            Compter(d, inv, q, 1, n);
            d.Dire("Encore " + Dit(n) + " !", () => { for (int i = 0; i < n; i++) a[i].Sauter(16, 380, i * 70); }, 1300);
            d.Dire("Peu importe par où on commence : c'est toujours " + Dit(n) + ".", null, 1800);
            return d;
        }

        /// <summary>Du chiffre à la collection : « trois », c'est un, deux, trois objets.</summary>
        private static Demo NombreVersCollection(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int n = NombreCourant(1, 10, () => Combien(max));
            var c = Chiffre(d, n, 230).En(230, Sol - 70).Opacite(0);
            (double x, double y) Place(int i) => (520 + (i % 5) * 150, Sol - 40 - (n > 5 ? (1 - i / 5) * 170 : 60));
            var a = new Acteur[n];
            for (int i = 0; i < n; i++) a[i] = Obj(d, o.Nom, o.Emoji, 120, 130).En(1420, Place(i).y);
            d.Dire("Voici le " + Dit(n) + ".", () => c.Pop(), 1200);
            d.Dire(Maj(Dit(n)) + ", c'est combien de " + o.Plur + " ?", () => c.Sauter(16), 1600);
            for (int i = 0; i < n - 1; i++) { int k = i; d.Dire(Maj(Dit(k + 1)) + ".", () => a[k].Aller(Place(k).x, Place(k).y, 650, 0, Acteur.Sortie), 900); }
            d.Toi(n == 1 ? "À toi ! Regarde le chiffre pour " + (o.Fem ? "la" : "le") + " faire venir." : "À toi ! Regarde le chiffre pour faire venir " + (o.Fem ? "la dernière." : "le dernier."), c, () => a[n - 1].Aller(Place(n - 1).x, Place(n - 1).y, 650, 0, Acteur.Sortie), 800);
            d.Dire(Maj(Dit(n)) + " !", () => { for (int i = 0; i < n; i++) a[i].Sauter(16, 380, i * 70); }, 1200);
            d.Dire("Le chiffre " + Dit(n) + ", et " + o.N(n) + " : c'est pareil.", () => c.Briller(), 2000);
            return d;
        }

        private static readonly Dictionary<int, (double X, double Y)[]> PointsDe = new Dictionary<int, (double, double)[]>
        {
            [1] = new[] { (100.0, 100.0) },
            [2] = new[] { (58.0, 58.0), (142.0, 142.0) },
            [3] = new[] { (52.0, 52.0), (100.0, 100.0), (148.0, 148.0) },
            [4] = new[] { (58.0, 58.0), (142.0, 58.0), (58.0, 142.0), (142.0, 142.0) },
            [5] = new[] { (55.0, 55.0), (145.0, 55.0), (100.0, 100.0), (55.0, 145.0), (145.0, 145.0) },
            [6] = new[] { (58.0, 50.0), (142.0, 50.0), (58.0, 100.0), (142.0, 100.0), (58.0, 150.0), (142.0, 150.0) },
        };

        /// <summary>Les objets se rangent comme les points du dé : c'est autant.</summary>
        private static Demo CommeLeDe(int max)
        {
            var d = new Demo("table");
            var o = ObjetCourant(); int n = NombreCourant(1, 6, () => max <= 3 ? Visuels.Entre(2, 3) : Visuels.Entre(3, 6));
            const double K = 1.7, X0 = 220, Y0 = 170;                    // le cadre : 340 × 340
            var cadre = d.Rect(200 * K, 200 * K, Colors.White, 50).En(X0 + 100 * K, Y0 + 200 * K).Opacite(0);
            var a = new Acteur[n];
            double[] sx = { 250, 420, 560, 300, 480, 620 }, sy = { 230, 180, 260, 420, 470, 400 };
            for (int i = 0; i < n; i++) a[i] = Obj(d, o.Nom, o.Emoji, 84, 96).En(sx[i], sy[i]).Opacite(0);
            var de = d.Chose(Visuels.De(n, 280), 280, 280).En(960, Y0 + 200 * K - 30).Opacite(0);
            var pts = PointsDe[n];
            d.Dire("Voici " + o.N(n) + ".", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 130); }, 1600);
            d.Dire("On les range comme les points du dé.", () =>
            {
                cadre.Fondu(1, 500);
                for (int i = 0; i < n; i++) { a[i].Devant(); a[i].Aller(X0 + pts[i].X * K, Y0 + pts[i].Y * K + 42, 1000, 500 + i * 180); }
            }, 1900 + n * 180);
            d.Dire("Et voici le dé.", () => de.Pop(), 1300);
            d.Dire("Le dé montre " + Dit(n) + " : c'est autant que les " + o.Plur + ".", () => { de.Briller(); cadre.Briller(); }, 2200);
            d.Toi("À toi ! Regarde le dé qui montre " + Dit(n) + ".", de, () => de.Sauter(24, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Dit(n)) + " !", () => { for (int i = 0; i < n; i++) a[i].Sauter(14, 380, i * 80); }, 1300);
            return d;
        }

        // ==================================================================
        //  RECONNAÎTRE LES CHIFFRES
        // ==================================================================
        /// <summary>Le tracé de chaque chiffre, comme on l'écrit à l'école (dans une case de 100 × 150).</summary>
        private static readonly string[] Traits =
        {
            "M 50,10 C 18,10 10,50 10,75 C 10,108 20,140 50,140 C 80,140 90,108 90,75 C 90,45 82,10 50,10",
            "M 22,48 L 58,10 L 58,140",
            "M 14,42 C 14,2 86,2 86,42 C 86,72 32,100 12,140 L 90,140",
            "M 16,26 C 34,0 86,8 82,40 C 80,62 56,70 40,72 C 62,72 92,84 86,110 C 80,146 30,148 12,120",
            "M 64,10 L 12,98 L 92,98 M 68,52 L 68,140",
            "M 82,10 L 26,10 L 18,66 C 40,50 88,58 86,100 C 84,142 34,150 12,122",
            "M 76,12 C 36,20 12,70 14,104 C 16,136 40,142 54,140 C 80,138 90,114 84,94 C 76,70 38,66 16,98",
            "M 12,12 L 88,12 L 42,140 M 34,76 L 76,76",
            "M 50,72 C 14,60 14,12 50,10 C 86,12 86,60 50,72 C 8,86 8,138 50,140 C 92,138 92,86 50,72",
            "M 84,48 C 80,4 16,4 16,48 C 16,88 76,86 84,48 C 88,92 76,132 28,140",
        };

        private static double Longueur(Geometry g)
        {
            double len = 0;
            foreach (var fig in g.GetFlattenedPathGeometry().Figures)
            {
                var pt = fig.StartPoint;
                foreach (var seg in fig.Segments)
                {
                    if (seg is PolyLineSegment pl) foreach (var q in pl.Points) { len += (q - pt).Length; pt = q; }
                    else if (seg is LineSegment ls) { len += (ls.Point - pt).Length; pt = ls.Point; }
                }
            }
            return len;
        }

        /// <summary>Un chiffre qui s'écrit trait par trait, sur son modèle en gris ; <paramref name="tracer"/> (durée, attente) lance l'écriture.</summary>
        private static Acteur ChiffreTrace(Demo d, int n, double k, out Action<int, int> tracer)
        {
            string s = n.ToString(); double w = 100 * s.Length + 16 * (s.Length - 1);
            var cv = new Canvas { Width = w, Height = 150 };
            var plumes = new List<(Path P, double L)>();
            for (int j = 0; j < s.Length; j++)
            {
                var geo = Geometry.Parse(Traits[s[j] - '0']);
                var fond = new Path { Data = geo, Stroke = new SolidColorBrush(Color.FromArgb(0x30, 0x3B, 0x2A, 0x5A)), StrokeThickness = 15, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
                var p = new Path { Data = geo, Stroke = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), StrokeThickness = 15, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
                double L = Longueur(geo) / 15 + 3;                       // en épaisseurs de trait, l'unité des pointillés
                p.StrokeDashArray = new DoubleCollection { L, L }; p.StrokeDashOffset = L;   // tout le trait est « en attente »
                Canvas.SetLeft(fond, j * 116); Canvas.SetLeft(p, j * 116);
                cv.Children.Add(fond); cv.Children.Add(p);
                plumes.Add((p, L));
            }
            tracer = (ms, delai) => { foreach (var pl in plumes) Acteur.K(pl.P, Shape.StrokeDashOffsetProperty, pl.L, 0, ms, delai, Acteur.Lineaire); };
            return d.Chose(new Viewbox { Child = cv, Width = w * k, Height = 150 * k, Stretch = Stretch.Fill }, w * k, 150 * k);
        }

        /// <summary>Le chiffre s'écrit sous les yeux de l'enfant, puis montre ce qu'il vaut.</summary>
        private static Demo Trace(int max)
        {
            var d = new Demo("table");
            var o = ObjetCourant(); int n = NombreCourant(1, 10, () => max <= 3 ? Visuels.Entre(1, 3) : max <= 6 ? Visuels.Entre(1, 6) : Visuels.Entre(1, 9));
            int col = n > 9 ? 4 : 3;
            var c = ChiffreTrace(d, n, 2.5, out var tracer).En(340, Sol - 30);
            var a = new Acteur[n];
            for (int i = 0; i < n; i++) a[i] = Obj(d, o.Nom, o.Emoji, 110, col == 4 ? 112 : 130).En((col == 4 ? 740 : 760) + (i % col) * (col == 4 ? 126 : 160), Sol - 50 - (i / col) * 140).Opacite(0);
            d.Dire("Voici le " + Dit(n) + ".", null, 1200);
            d.Dire("Il s'écrit comme ça.", () => tracer(2400, 300), 3000);
            d.Dire(Maj(Dit(n)) + ", c'est ça :", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 260); }, 900 + n * 260);
            d.Dire(Maj(o.N(n)) + ".", () => { for (int i = 0; i < n; i++) a[i].Sauter(14, 380, i * 70); }, 1300);
            d.Toi("À toi ! Regarde le chiffre pour l'écrire encore.", c, () => tracer(2200, 0), 2500);
            d.Dire("Le " + Dit(n) + " !", () => c.Sauter(20), 1100);
            return d;
        }

        /// <summary>Le même nombre sous toutes ses formes : le chiffre, la collection, le dé, la boîte de dix.</summary>
        private static Demo Representations(int max, string accent)
        {
            var d = new Demo("piece");
            var o = ObjetCourant();
            int n = NombreCourant(1, accent == "de" ? 6 : 10, () => accent == "de" ? Visuels.Entre(1, Math.Min(6, max)) : max <= 3 ? Visuels.Entre(1, 3) : max <= 6 ? Visuels.Entre(2, 6) : Visuels.Entre(3, 10));
            var c = Chiffre(d, n, 200).En(640, 240).Opacite(0);
            var cartes = new List<(string Cle, Acteur A, string Dit, string Cible)>();
            Acteur Carte(FrameworkElement v, double w, double h)
            {
                var g = new Grid { Width = w + 40, Height = h + 40 };
                g.Children.Add(new Rectangle { RadiusX = 26, RadiusY = 26, Fill = Brushes.White, Stroke = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)), StrokeThickness = 3 });
                g.Children.Add(new Viewbox { Child = v, Width = w, Height = h, Stretch = Stretch.Uniform });
                return d.Chose(g, w + 40, h + 40).Opacite(0);
            }
            cartes.Add(("collection", Carte(Visuels.Collection(o, n, 70), 270, 180), Maj(o.N(n)) + ".", "les " + o.Plur));
            if (n <= 6) cartes.Add(("de", Carte(Visuels.De(n, 180), 180, 180), "Le " + Dit(n) + " du dé.", "le dé"));
            cartes.Add(("boite", Carte(Visuels.Boite10(n, 280), 280, 120), n <= 1 ? "Un jeton dans la boîte." : Maj(Dit(n)) + " jetons dans la boîte.", "la boîte"));
            // La forme mise en avant passe en dernier.
            var mise = cartes.FirstOrDefault(x => x.Cle == accent);
            if (mise.A != null) { cartes.Remove(mise); cartes.Add(mise); } else mise = cartes[cartes.Count - 1];
            var xs = cartes.Count == 3 ? new double[] { 250, 640, 1030 } : new double[] { 390, 890 };
            var liens = new List<Line>();
            for (int i = 0; i < cartes.Count; i++)
            {
                cartes[i].A.En(xs[i], Sol - 20);
                liens.Add(d.Trait(640 + (xs[i] - 640) * 0.25, 250, xs[i], Sol - 20 - cartes[i].A.H - 12, Demo.Or, 6, true));
            }
            d.Dire("Voici le " + Dit(n) + ".", () => c.Pop(), 1300);
            for (int i = 0; i < cartes.Count; i++) { int k = i; d.Dire(cartes[k].Dit, () => { cartes[k].A.Pop(); Demo.Voir(liens[k], 400, 300); }, 1700); }
            d.Dire("Tout ça, c'est " + Dit(n) + ".", () => { c.Briller(); for (int i = 0; i < cartes.Count; i++) cartes[i].A.Sauter(14, 400, i * 150); }, 1900);
            d.Toi("À toi ! Regarde " + mise.Cible + ".", mise.A, () => mise.A.Sauter(22, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Dit(n)) + " !", () => c.Sauter(18), 1200);
            return d;
        }

        /// <summary>Le chiffre a sa place sur la bande : après celui d'avant, avant celui d'après.</summary>
        private static Demo SurLaBande(int max)
        {
            var d = new Demo();
            int fin = max <= 3 ? 5 : max <= 6 ? 6 : 10, n = NombreCourant(1, fin, () => Visuels.Entre(2, fin - 1));
            var X = PoserBande(d, 1, fin, fin <= 6 ? 130 : 96);
            double y = Sol - 12;
            var gre = d.Image("grenouille", "🐸", 110).En(X(1), y).Opacite(0);
            var grand = Chiffre(d, n, 190).En(640, 250).Opacite(0);
            d.Dire("Voici la bande des nombres. Ils sont rangés dans l'ordre.", () => gre.Pop(700), 2100);
            d.Dire("Un.", () => gre.Sauter(16, 360), 700);
            for (int k = 2; k <= n; k++) { int kk = k; d.Dire(Maj(Dit(kk)) + ".", () => gre.Bond(X(kk), y, 60, 420), 750); }
            d.Dire("La grenouille s'arrête sur le " + Dit(n) + ".", () => grand.Pop(), 1600);
            d.Dire(n == 1 ? "Le un est le premier, juste avant le deux." : "Le " + Dit(n) + " vient après le " + Dit(n - 1) + ", et avant le " + Dit(n + 1) + ".", () => gre.Sauter(16, 360, 0, 2), 2400);
            d.Toi("À toi ! Regarde le grand " + Dit(n) + ".", grand, () => grand.Sauter(22, 460, 0, 2), 1100);
            d.Dire("Oui ! Le " + Dit(n) + " !", () => grand.Briller(), 1300);
            return d;
        }

        /// <summary>Trois nombres, trois tours : rangés, ça monte comme un escalier.</summary>
        private static Demo ToursRangees()
        {
            var d = new Demo("piece");
            const double T = 52;
            var v = ListeCourante(3, 9) ?? Visuels.Nombres(Visuels.Entre(1, 8), 3, 1, 8).OrderBy(x => x).ToArray();
            double[] xs = { 380, 640, 900 }; int[] dep = { 1, 2, 0 };          // le plus petit part au milieu, le moyen à droite, le plus grand à gauche
            var cols = new[] { Bleu, Vert, Orange };
            var tours = new Acteur[3][]; var ch = new Acteur[3];
            for (int r = 0; r < 3; r++) { tours[r] = Tour(d, xs[dep[r]], v[r], T, cols[r]); ch[r] = Chiffre(d, v[r], 92).Au(xs[dep[r]], Sol + 62).Opacite(0); }
            d.Dire("Trois nombres : " + Dit(v[2]) + ", " + Dit(v[0]) + ", " + Dit(v[1]) + ".", () => { ch[2].Pop(); ch[0].Pop(600); ch[1].Pop(1200); }, 2400);
            d.Dire("Pour les ranger, on fait des tours.", () => { for (int r = 0; r < 3; r++) for (int i = 0; i < v[r]; i++) tours[r][i].Pop(dep[r] * 500 + i * 110); }, 2600);
            d.Dire("On range, du plus petit au plus grand.", () =>
            {
                for (int r = 0; r < 3; r++)
                {
                    ch[r].Aller(xs[r], ch[r].Y, 1400);
                    for (int i = 0; i < v[r]; i++) tours[r][i].Aller(xs[r], Sol - i * T, 1400);
                }
            }, 1900);
            d.Dire("Ça monte comme un escalier.", () => { for (int r = 0; r < 3; r++) ch[r].Sauter(16, 380, r * 400); }, 1700);
            for (int r = 0; r < 3; r++) { int rr = r; d.Dire(Maj(Dit(v[rr])) + (rr == 2 ? " !" : "."), () => ch[rr].Briller(0, 1000), 900); }
            d.Toi("À toi ! Regarde le plus grand nombre.", ch[2], () => ch[2].Sauter(20, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Dit(v[2])) + ".", () => { for (int i = 0; i < v[2]; i++) tours[2][i].Sauter(10, 360, i * 60); }, 1300);
            return d;
        }
    }
}
