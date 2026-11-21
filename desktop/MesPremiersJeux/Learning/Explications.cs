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
    /// LES EXPLICATIONS ANIMÉES : avant l'exercice, la notion est MONTRÉE — par
    /// une transformation (le ballon gonfle), une comparaison (l'éléphant et la
    /// souris), un usage (le gros bâton ne passe pas dans le petit trou)…
    /// Chaque notion a plusieurs angles, qui tournent d'une fois sur l'autre.
    /// L'enfant qui ne peut pas manipuler manipule ici avec les yeux : chaque
    /// scène a son « à toi », déclenché au regard.
    /// Première série : les grandeurs et la comparaison des quantités.
    /// </summary>
    public static partial class Explications
    {
        private const double Sol = Demo.Sol;
        private static readonly Color Bleu = Color.FromRgb(0x3B, 0x9B, 0xFF), Orange = Color.FromRgb(0xFF, 0x8A, 0x2B), Vert = Color.FromRgb(0x4C, 0xB8, 0x5E),
            Rose = Color.FromRgb(0xFF, 0x6F, 0xB5), Violet = Color.FromRgb(0x9B, 0x5D, 0xE5), Brun = Color.FromRgb(0xB5, 0x7A, 0x45), Gris = Color.FromRgb(0x8E, 0x99, 0xAB),
            Rouge = Color.FromRgb(0xE8, 0x3A, 0x3A), Jaune = Color.FromRgb(0xFF, 0xC1, 0x07);
        private static readonly string[] Mots = { "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix" };

        // ------------------------------------------------------------------
        //  LE SUJET DE L'EXERCICE : posé par la leçon juste avant de fabriquer l'explication.
        //  S'il est là, l'animation prend le même nombre, le même objet, la même forme que
        //  l'exercice qui suit ; sinon elle choisit au hasard comme avant.
        // ------------------------------------------------------------------
        internal static Sujet Courant;
        private static ObjetDef ObjetCourant() => Courant?.O ?? Visuels.Objet();
        private static int NombreCourant(int min, int max, Func<int> sinon)
        {
            int? n = Courant?.N;
            return n.HasValue && n.Value >= min && n.Value <= max ? n.Value : sinon();
        }
        private static int[] ListeCourante(int combien, int max)
        {
            var l = Courant?.Liste;
            if (l == null) return null;
            var v = l.Distinct().OrderBy(x => x).ToArray();
            return v.Length == combien && v[0] >= 1 && v[combien - 1] <= max ? v : null;
        }

        // ------------------------------------------------------------------
        //  Les familles (une notion, plusieurs angles)
        // ------------------------------------------------------------------
        public static readonly Famille GrandPetit = new Famille("grand-petit", ("ballon", Gonfle), ("animaux", ElephantSouris), ("ours", OursEtBols));
        public static readonly Famille LongCourt = new Famille("long-court", ("train", Train), ("chenille", Chenille), ("depart", MemeDepart));
        public static readonly Famille GrosMince = new Famille("gros-mince", ("grossit", Grossit), ("trous", PlancheATrous));
        public static readonly Famille LourdLeger = new Famille("lourd-leger", ("balance", Balance), ("vent", Vent));
        public static readonly Famille RangerTailles3 = new Famille("ranger-tailles", ("escalier", () => Escalier(3)), ("animaux", () => AnimauxRanges(3)));
        public static readonly Famille RangerTailles5 = new Famille("ranger-tailles", ("escalier", () => Escalier(5)), ("animaux", () => AnimauxRanges(5)));
        public static readonly Famille RangerLongueurs3 = new Famille("ranger-longueurs", ("depart", () => BarresRangees(3)), ("crayons", () => Crayons(3)));
        public static readonly Famille RangerLongueurs5 = new Famille("ranger-longueurs", ("depart", () => BarresRangees(5)), ("crayons", () => Crayons(5)));
        public static readonly Famille RangerPoids = new Famille("ranger-poids", ("plateaux", Plateaux), ("balance", Balance));
        public static readonly Famille Beaucoup = new Famille("beaucoup-peu", ("paniers", Paniers), ("tours", () => Tours(2, 6)));
        public static readonly Famille PlusMoins = new Famille("plus-moins", ("lignes", () => Lignes(5, 3)), ("tours", () => Tours(3, 5)));
        public static readonly Famille Autant = new Famille("autant", ("paires", Paires), ("tours", ToursEgales));
        public static readonly Famille PlusGrandNombre = new Famille("plus-grand-nombre", ("tours", ToursNombres), ("bande", Bande));

        // ==================================================================
        //  GRAND / PETIT
        // ==================================================================
        /// <summary>Le même ballon devient grand, puis petit : la taille change, pas l'objet.</summary>
        private static Demo Gonfle()
        {
            var d = new Demo("ciel");
            var o = Courant?.O;                                   // l'objet de l'exercice, s'il y en a un
            string nom = o?.Nom ?? "ballon", emoji = o?.Emoji ?? "🎈";
            var b = Obj(d, nom, emoji, 230, 300).En(640, Sol - 30).Echelle(0.9).Opacite(0);
            var p = Obj(d, nom, emoji, 230, 300).En(960, Sol - 30).Echelle(0.5).Opacite(0);
            d.Dire(o == null ? "Regarde le ballon." : "Regarde bien.", () => b.Pop(), 900);
            d.Dire(o == null ? "Il gonfle, il gonfle…" : "Ça grandit, ça grandit…", () => b.Grandir(1.8, 2400), 2500);
            d.Dire(o == null ? "Il est grand !" : "C'est grand !", () => b.Sauter(34), 800);
            d.Dire(o == null ? "Il se dégonfle…" : "Ça rapetisse…", () => b.Grandir(0.5, 2200), 2300);
            d.Dire(o == null ? "Il est petit." : "C'est petit.", () => b.Secouer(0, 2, 5), 800);
            d.Toi(o == null ? "À toi ! Regarde le ballon pour le faire grandir." : "À toi ! Regarde pour faire grandir.", b, () => b.Grandir(1.8, 1800), 1900);
            d.Dire("Grand !", () => b.Sauter(34), 800);
            d.Dire(o == null ? "Un grand ballon…" : o.Fem ? "Une grande…" : "Un grand…", () => { b.Aller(430, Sol - 30, 900); b.Briller(900); }, 1900);
            d.Dire(o == null ? "et un petit ballon." : o.Fem ? "et une petite." : "et un petit.", () => { p.Pop(); p.Briller(300); }, 1600);
            return d;
        }

        /// <summary>Deux animaux côte à côte, mesurés à la toise.</summary>
        private static Demo ElephantSouris()
        {
            var d = new Demo();
            var e = d.Image("elephant", "🐘", 330).En(1560, Sol);
            var s = d.Image("souris", "🐭", 110).En(-140, Sol);
            var te = d.Toise(330, Demo.Or).En(640, Sol);
            var ts = d.Toise(110, Demo.Or).En(470, Sol);
            d.Dire("Voici l'éléphant.", () => e.Aller(870, Sol, 1500, 0, Acteur.Sortie), 1600);
            d.Dire("Et voici la souris.", () => s.Aller(370, Sol, 1300, 0, Acteur.Sortie), 1400);
            d.Dire("L'éléphant est grand.", () => { te.Opacite(1); te.GrandirXY(1, 1, 1300); e.Briller(200); }, 1800);
            d.Dire("La souris est petite.", () => { ts.Opacite(1); ts.GrandirXY(1, 1, 700); s.Briller(200); }, 1700);
            d.Toi("À toi ! Regarde le grand.", e, () => e.Sauter(40, 520, 0, 2), 1200);
            d.Dire("Oui ! Le grand, c'est l'éléphant.", null, 700);
            d.Dire("La petite, c'est la souris.", () => s.Sauter(26, 420, 0, 2), 1000);
            return d;
        }

        /// <summary>Grand avec grand, petit avec petit : la taille sert à quelque chose.</summary>
        private static Demo OursEtBols()
        {
            var d = new Demo("piece");
            var go = d.Image("ours", "🐻", 340).En(280, Sol).Opacite(0);
            var po = d.Image("ours", "🐻", 340).En(1010, Sol).Echelle(0.5).Opacite(0);
            var gb = d.Image("bol", "🥣", 120).En(600, Sol).Opacite(0);
            var pb = d.Image("bol", "🥣", 120).En(735, Sol).Echelle(0.5).Opacite(0);
            d.Dire("Voici un grand ours…", () => go.Pop(), 1000);
            d.Dire("et un petit ours.", () => po.Pop(), 1000);
            d.Dire("Voici deux bols : un grand bol, un petit bol.", () => { gb.Pop(); pb.Pop(900); }, 1800);
            d.Dire("Le grand bol, c'est pour le grand ours.", () => { gb.Bond(520, Sol, 90, 1000); go.Sauter(24, 420, 1100); }, 1700);
            d.Toi("À toi ! Regarde le petit ours pour lui donner son bol.", po, () => pb.Bond(890, Sol, 90, 1000), 1200);
            d.Dire("Le petit bol, c'est pour le petit ours.", () => po.Sauter(20, 420), 900);
            d.Dire("Grand avec grand.", () => { go.Briller(); gb.Briller(); }, 1500);
            d.Dire("Petit avec petit.", () => { po.Briller(); pb.Briller(); }, 1500);
            return d;
        }

        // ==================================================================
        //  LONG / COURT
        // ==================================================================
        private static FrameworkElement Wagon(Color c)
        {
            var cv = new Canvas { Width = 150, Height = 110 };
            void Add(Shape s, double x, double y) { Canvas.SetLeft(s, x); Canvas.SetTop(s, y); cv.Children.Add(s); }
            var encre = new SolidColorBrush(Demo.Encre);
            Add(new Rectangle { Width = 16, Height = 8, Fill = encre }, -8, 62);
            Add(new Rectangle { Width = 142, Height = 68, RadiusX = 14, RadiusY = 14, Fill = new SolidColorBrush(c), Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)), StrokeThickness = 3 }, 4, 12);
            Add(new Rectangle { Width = 40, Height = 28, RadiusX = 6, RadiusY = 6, Fill = new SolidColorBrush(Color.FromRgb(0xEA, 0xF6, 0xFF)) }, 22, 24);
            Add(new Rectangle { Width = 40, Height = 28, RadiusX = 6, RadiusY = 6, Fill = new SolidColorBrush(Color.FromRgb(0xEA, 0xF6, 0xFF)) }, 88, 24);
            Add(new Ellipse { Width = 34, Height = 34, Fill = encre }, 20, 76);
            Add(new Ellipse { Width = 34, Height = 34, Fill = encre }, 96, 76);
            Add(new Ellipse { Width = 12, Height = 12, Fill = Brushes.White }, 31, 87);
            Add(new Ellipse { Width = 12, Height = 12, Fill = Brushes.White }, 107, 87);
            return cv;
        }

        /// <summary>On accroche des wagons : le train s'allonge, et une règle le mesure dessous.</summary>
        private static Demo Train()
        {
            var d = new Demo();
            var rail = d.Trait(50, Sol - 3, 1230, Sol - 3, Gris, 6); rail.Opacity = 1;
            var loco = d.Image("locomotive", "🚂", 150).En(190, Sol).Opacite(0);
            var cols = new[] { Rouge, Bleu, Jaune, Vert, Violet };
            var w = new Acteur[5];
            double Place(int i) => 371 + 156 * i;
            for (int i = 0; i < 5; i++) w[i] = d.Chose(Wagon(cols[i]), 150, 110).En(i == 0 ? Place(0) : 1500, Sol);
            w[0].Opacite(0);
            const double Max = 206 + 156 * 5;
            var regle = d.Rect(Max, 22, Demo.Or, 11, false).Origine(0, 0.5).Au(90 + Max / 2, Sol + 56).EchelleXY(0.001, 1);
            double K(int n) => (206 + 156.0 * n) / Max;
            d.Dire("Voici un petit train.", () => { loco.Pop(); w[0].Pop(350); }, 1200);
            d.Dire("Il est court.", () => regle.GrandirXY(K(1), 1, 900), 1200);
            string[] dit = { null, "J'accroche un wagon…", "Encore un…", "Encore un !" };
            for (int i = 1; i <= 3; i++) { int k = i; d.Dire(dit[i], () => { w[k].Aller(Place(k), Sol, 900, 0, Acteur.Sortie); regle.GrandirXY(K(k + 1), 1, 900); }, 1100); }
            d.Dire("Le train est long !", () => { for (int i = 0; i < 4; i++) w[i].Sauter(18, 380, i * 110); }, 1200);
            d.Toi("À toi ! Regarde la locomotive pour accrocher encore un wagon.", loco, () => { w[4].Aller(Place(4), Sol, 900, 0, Acteur.Sortie); regle.GrandirXY(K(5), 1, 900); }, 1100);
            d.Dire("Long, long, long !", () => { for (int i = 0; i < 5; i++) w[i].Sauter(18, 380, i * 110); }, 1300);
            // Pour comparer : un train court, au-dessus.
            var rail2 = d.Trait(50, 327, 520, 327, Gris, 6);
            var loco2 = d.Image("locomotive", "🚂", 150).En(190, 330).Opacite(0);
            var w2 = d.Chose(Wagon(Orange), 150, 110).En(Place(0), 330).Opacite(0);
            d.Dire("Celui-ci est court.", () => { Demo.Voir(rail2); loco2.Pop(); w2.Pop(250); loco2.Briller(500); w2.Briller(500); }, 1900);
            d.Dire("Celui-là est long.", () => { loco.Briller(); for (int i = 0; i < 5; i++) w[i].Briller(); }, 1700);
            return d;
        }

        private static FrameworkElement TeteChenille()
        {
            var cv = new Canvas { Width = 104, Height = 124 };
            void Add(Shape s, double x, double y) { Canvas.SetLeft(s, x); Canvas.SetTop(s, y); cv.Children.Add(s); }
            var encre = new SolidColorBrush(Demo.Encre);
            cv.Children.Add(new Line { X1 = 36, Y1 = 30, X2 = 26, Y2 = 4, Stroke = encre, StrokeThickness = 5, StrokeEndLineCap = PenLineCap.Round });
            cv.Children.Add(new Line { X1 = 68, Y1 = 30, X2 = 78, Y2 = 4, Stroke = encre, StrokeThickness = 5, StrokeEndLineCap = PenLineCap.Round });
            Add(new Ellipse { Width = 100, Height = 100, Fill = new SolidColorBrush(Color.FromRgb(0x8B, 0xD4, 0x4A)), Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)), StrokeThickness = 3 }, 2, 22);
            Add(new Ellipse { Width = 18, Height = 22, Fill = encre }, 34, 52);
            Add(new Ellipse { Width = 18, Height = 22, Fill = encre }, 62, 52);
            Add(new Ellipse { Width = 7, Height = 7, Fill = Brushes.White }, 38, 55);
            Add(new Ellipse { Width = 7, Height = 7, Fill = Brushes.White }, 66, 55);
            Add(new Ellipse { Width = 16, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0x8F, 0xA8)) }, 16, 80);
            Add(new Ellipse { Width = 16, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0x8F, 0xA8)) }, 78, 80);
            cv.Children.Add(new Path { Data = Geometry.Parse("M 42,88 Q 56,100 70,88"), Stroke = encre, StrokeThickness = 4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            return cv;
        }

        /// <summary>La chenille grandit anneau après anneau : court devient long.</summary>
        private static Demo Chenille()
        {
            var d = new Demo();
            const int Total = 12;
            double X(int i) => 200 + 66 * i;
            var seg = new Acteur[Total];
            for (int i = 0; i < Total; i++)
                seg[i] = d.Rond(84, i % 2 == 0 ? Color.FromRgb(0x6C, 0xC2, 0x3C) : Color.FromRgb(0xA4, 0xDE, 0x5A)).En(X(i), Sol).Opacite(0);
            var tete = d.Chose(TeteChenille(), 104, 124).En(X(2) + 12, Sol + 6).Opacite(0);
            d.Dire("Voici une chenille.", () => { seg[0].Pop(); seg[1].Pop(150); tete.Pop(300); }, 1100);
            d.Dire("Elle est toute courte.", () => tete.Sauter(16), 900);
            d.Dire("Elle mange, elle grandit…", () =>
            {
                tete.Aller(X(9) + 12, Sol + 6, 7 * 260 + 200, 0, Acteur.Lineaire);
                for (int i = 2; i < 9; i++) seg[i].Pop((i - 2) * 260 + 220);
            }, 2400);
            d.Dire("Elle est longue maintenant !", () => { for (int i = 0; i < 9; i++) seg[i].Sauter(18, 360, i * 70); tete.Sauter(18, 360, 9 * 70); }, 1500);
            d.Toi("À toi ! Regarde la chenille pour l'allonger encore.", tete, () =>
            {
                tete.Aller(X(12) + 12, Sol + 6, 3 * 260 + 200, 0, Acteur.Lineaire);
                for (int i = 9; i < 12; i++) seg[i].Pop((i - 9) * 260 + 220);
            }, 1300);
            d.Dire("Longue, longue, longue !", () => { for (int i = 0; i < 12; i++) seg[i].Sauter(18, 360, i * 60); }, 1500);
            // Pour comparer : une chenille courte, au-dessus.
            var sol2 = d.Trait(150, 322, 520, 322, Color.FromRgb(0x86, 0xCB, 0x66), 8);
            var c0 = d.Rond(84, Color.FromRgb(0xFF, 0xB0, 0x4A)).En(X(0), 320).Opacite(0);
            var c1 = d.Rond(84, Color.FromRgb(0xFF, 0xCF, 0x7A)).En(X(1), 320).Opacite(0);
            var t2 = d.Chose(TeteChenille(), 104, 124).En(X(2) + 12, 326).Opacite(0);
            d.Dire("Une chenille courte…", () => { Demo.Voir(sol2); c0.Pop(); c1.Pop(120); t2.Pop(240); t2.Briller(600); }, 1700);
            d.Dire("et une chenille longue.", () => { tete.Devant(); tete.Briller(); for (int i = 0; i < 12; i += 2) seg[i].Briller(); }, 1700);
            return d;
        }

        /// <summary>Pour comparer deux longueurs, on les met au même départ : ce qui dépasse se voit.</summary>
        private static Demo MemeDepart() => DepartDeux(Bleu, 560, 330, true);

        private static Demo DepartDeux(Color col, double longue, double courte, bool toi)
        {
            var d = new Demo("table");
            const double X0 = 250;
            var a = d.Rect(longue, 60, col, 30).Origine(0.5, 0.5).Au(770, 200).Angle(-14).Opacite(0);
            var b = d.Rect(courte, 60, col, 30).Origine(0.5, 0.5).Au(430, 430).Angle(10).Opacite(0);
            var depart = d.Trait(X0, 120, X0, 500, Demo.Encre, 7, true);
            var fin = d.Trait(X0 + courte, 190, X0 + courte, 450, Demo.Or, 6, true);
            var plus = d.Rect(longue - courte, 60, Demo.Or, 30, false).Au(X0 + courte + (longue - courte) / 2, 250).Opacite(0);
            plus.Boite.IsHitTestVisible = false;
            d.Dire("Voici deux barres.", () => { a.Pop(); b.Pop(300); }, 1200);
            d.Dire("Laquelle est la plus longue ? Ce n'est pas facile à voir.", () => { a.Secouer(300, 1, 4); b.Secouer(700, 1, 4); }, 1600);
            d.Dire("On les met au même départ.", () =>
            {
                Demo.Voir(depart);
                a.Tourner(0, 900, 300); a.AllerAu(X0 + longue / 2, 250, 1300, 300);
                b.Tourner(0, 900, 300); b.AllerAu(X0 + courte / 2, 390, 1300, 300);
            }, 1900);
            d.Dire("Regarde : celle-ci dépasse.", () => { Demo.Voir(fin); plus.Devant(); plus.Fondu(0.75, 700, 300); }, 1700);
            d.Dire("C'est la plus longue.", () => a.Briller(), 1500);
            d.Dire("Et celle-là, c'est la plus courte.", () => b.Briller(), 1600);
            if (toi)
            {
                d.Toi("À toi ! Regarde la plus longue.", a, () => { a.Sauter(22, 460, 0, 2); plus.Sauter(22, 460, 0, 2); }, 1100);
                d.Dire("Oui ! Longue… et courte.", null, 900);
            }
            return d;
        }

        // ==================================================================
        //  GROS / MINCE
        // ==================================================================
        /// <summary>Le même bâton grossit puis redevient mince.</summary>
        private static Demo Grossit()
        {
            var d = new Demo();
            var b = d.Rect(150, 300, Brun, 30).En(640, Sol).EchelleXY(0.2, 1).Opacite(0);
            var m = d.Rect(150, 300, Brun, 30).En(950, Sol).EchelleXY(0.2, 1).Opacite(0);
            d.Dire("Voici un bâton.", () => b.Pop(), 900);
            d.Dire("Il est mince.", () => b.Secouer(0, 1, 4), 900);
            d.Dire("Il grossit, il grossit…", () => b.GrandirXY(1, 1, 2300), 2400);
            d.Dire("Il est gros !", () => b.Sauter(24), 800);
            d.Dire("Il redevient mince.", () => b.GrandirXY(0.2, 1, 2000), 2100);
            d.Toi("À toi ! Regarde le bâton pour le faire grossir.", b, () => b.GrandirXY(1, 1, 1700), 1800);
            d.Dire("Gros !", () => b.Sauter(24), 800);
            d.Dire("Un gros bâton…", () => { b.Aller(430, Sol, 900); b.Briller(900); }, 1900);
            d.Dire("et un bâton mince.", () => { m.Pop(); m.Briller(300); }, 1600);
            return d;
        }

        /// <summary>La planche à trous : le mince passe partout, le gros a besoin du grand trou.</summary>
        private static Demo PlancheATrous()
        {
            var d = new Demo("piece");
            d.Rect(22, Sol - 388, Brun, 6).En(170, Sol);
            d.Rect(22, Sol - 388, Brun, 6).En(1110, Sol);
            var mince = d.Rect(40, 190, Vert, 18).En(500, 300).Opacite(0);
            var gros = d.Rect(150, 190, Orange, 30).En(500, -30);
            d.Rect(320, 30, Brun, 8).Au(310, 375);
            d.Rect(270, 30, Brun, 8).Au(665, 375);
            d.Rect(150, 30, Brun, 8).Au(1055, 375);
            var t1 = d.Rect(56, 26, Demo.Or, 8, false).Au(500, 375).Opacite(0);
            var t2 = d.Rect(176, 26, Demo.Or, 8, false).Au(890, 375).Opacite(0);
            d.Dire("Voici une planche avec deux trous.", null, 1000);
            d.Dire("Un petit trou…", () => t1.Montrer(), 1500);
            d.Dire("et un grand trou.", () => t2.Montrer(), 1500);
            d.Dire("Le bâton mince passe dans le petit trou.", () => { mince.Pop(); mince.Aller(500, Sol, 1300, 600, Acteur.Entree); }, 2300);
            d.Dire("Le gros bâton…", () => { mince.Aller(330, Sol, 700); gros.Aller(500, 360, 1000, 300, Acteur.Rebond); }, 1500);
            d.Dire("ne passe pas ! Il est trop gros.", () => gros.Secouer(0, 3, 5), 1300);
            d.Dire("Il lui faut le grand trou.", () => gros.Bond(890, 330, 70, 1000), 1300);
            d.Toi("À toi ! Regarde le gros bâton pour le faire passer.", gros, () => { gros.Aller(890, Sol, 1100, 0, Acteur.Entree); gros.Ecraser(1100); }, 1600);
            d.Dire("Il passe !", null, 700);
            d.Dire("Un gros bâton…", () => gros.Briller(), 1500);
            d.Dire("et un bâton mince.", () => mince.Briller(), 1500);
            return d;
        }

        // ==================================================================
        //  LOURD / LÉGER
        // ==================================================================
        /// <summary>Une balance à deux plateaux, dessinée : le fléau penche, les plateaux suivent.</summary>
        private sealed class Bal
        {
            public const double XG = 320, XD = 960, Pivot = 260, Plateau = 410;
            public readonly Acteur Fleau, G, D;
            public double Dy;

            public Bal(Demo d)
            {
                var pied = new Polygon { Points = new PointCollection { new Point(70, 0), new Point(140, 300), new Point(0, 300) }, Fill = new SolidColorBrush(Gris), Stroke = new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)), StrokeThickness = 3 };
                d.Chose(pied, 140, 300).En(640, Sol);
                G = d.Chose(Plat(), 220, 170).En(XG, Pivot + 170);
                D = d.Chose(Plat(), 220, 170).En(XD, Pivot + 170);
                Fleau = d.Rect(664, 18, Demo.Encre, 9, false).Origine(0.5, 0.5).Au(640, Pivot);
                d.Rond(36, Demo.Or).Au(640, Pivot);
            }

            private static FrameworkElement Plat()
            {
                var cv = new Canvas { Width = 220, Height = 170 };
                var encre = new SolidColorBrush(Demo.Encre);
                cv.Children.Add(new Line { X1 = 110, Y1 = 0, X2 = 16, Y2 = 152, Stroke = encre, StrokeThickness = 4 });
                cv.Children.Add(new Line { X1 = 110, Y1 = 0, X2 = 204, Y2 = 152, Stroke = encre, StrokeThickness = 4 });
                var p = new Rectangle { Width = 220, Height = 18, RadiusX = 9, RadiusY = 9, Fill = encre };
                Canvas.SetTop(p, 150); cv.Children.Add(p);
                return cv;
            }

            /// <summary>Penche : un angle négatif fait descendre le plateau de gauche. Les objets posés suivent.</summary>
            public void Pencher(double deg, int ms, Acteur surGauche = null, Acteur surDroite = null)
            {
                Dy = -320 * Math.Sin(deg * Math.PI / 180);
                Fleau.Tourner(deg, ms);
                G.Aller(XG, Pivot + 170 + Dy, ms); D.Aller(XD, Pivot + 170 - Dy, ms);
                surGauche?.Aller(XG, Plateau + Dy, ms); surDroite?.Aller(XD, Plateau - Dy, ms);
            }
            public double HautG => Plateau + Dy;
            public double HautD => Plateau - Dy;
        }

        /// <summary>La balance : ce qui est lourd fait descendre le plateau.</summary>
        private static Demo Balance()
        {
            var d = new Demo();
            var bal = new Bal(d);
            var el = d.Image("elephant", "🐘", 170).En(Bal.XG, -40);
            var fe = d.Image("feuille", "🍃", 110).En(Bal.XD, 170);
            d.Dire("Voici une balance, et une feuille.", () => fe.Secouer(0, 2, 8), 1400);
            d.Dire("Je pose l'éléphant.", () => el.Aller(Bal.XG, Bal.Plateau, 1000, 0, Acteur.Entree), 1100);
            d.Dire("Oh ! Ça descend.", () => bal.Pencher(-11, 1000, el, null), 1300);
            d.Dire("L'éléphant est lourd.", () => el.Briller(), 1500);
            d.Toi("À toi ! Regarde la feuille pour la poser sur la balance.", fe, () => { fe.Aller(Bal.XD, bal.HautD, 2200); fe.Secouer(0, 7, 10); }, 2500);
            d.Dire("La balance ne bouge pas.", null, 900);
            d.Dire("La feuille est légère.", () => fe.Briller(), 1500);
            d.Dire("Lourd…", () => el.Briller(), 1300);
            d.Dire("et léger.", () => fe.Briller(), 1300);
            return d;
        }

        /// <summary>Le vent : ce qui est léger s'envole, ce qui est lourd reste.</summary>
        private static Demo Vent()
        {
            var d = new Demo("ciel");
            var nuage = d.Image("nuage", "☁️", 150).En(170, 520);
            var feuille = d.Image("feuille", "🍃", 110).En(480, Sol).Opacite(0);
            var ballon = d.Image("ballon", "🎈", 170).En(680, Sol).Opacite(0);
            var pierre = d.Image("pierre", "🪨", 150).En(930, Sol).Opacite(0);
            var souffle = new Acteur[3];
            for (int i = 0; i < 3; i++) { souffle[i] = d.Rect(170, 12, Colors.White, 6, false).Au(370, 410 + i * 50).Opacite(0); souffle[i].Boite.IsHitTestVisible = false; }
            void Souffler()
            {
                nuage.Ecraser();
                for (int i = 0; i < 3; i++) { souffle[i].Au(370, 410 + i * 50); souffle[i].Montrer(i * 140, 900, 0.9); souffle[i].AllerAu(1150, 410 + i * 50, 1700, i * 140, Acteur.Sortie); }
            }
            d.Dire("Voici une feuille, un ballon et une pierre.", () => { feuille.Pop(); ballon.Pop(500); pierre.Pop(1100); }, 2200);
            d.Dire("Le vent souffle !", () => { Souffler(); feuille.Aller(1460, 110, 2300, 500, Acteur.Entree); feuille.Tourner(500, 2300, 500); }, 2900);
            d.Dire("La feuille s'envole : elle est légère.", null, 900);
            d.Dire("La pierre ne bouge pas : elle est lourde.", () => { pierre.Secouer(0, 1, 3); pierre.Briller(); }, 1600);
            d.Toi("À toi ! Regarde le nuage pour souffler encore.", nuage, () => { Souffler(); ballon.Aller(1480, 60, 2400, 500, Acteur.Entree); ballon.Tourner(40, 2400, 500); }, 3000);
            d.Dire("Le ballon s'envole aussi : il est léger.", null, 900);
            d.Dire("La pierre reste là : elle est lourde.", () => pierre.Briller(), 1600);
            return d;
        }

        /// <summary>Trois plateaux à ressort : plus c'est lourd, plus ça descend — puis on range.</summary>
        private static Demo Plateaux()
        {
            var d = new Demo("piece");
            double[] xs = { 300, 640, 980 };
            var ressort = new Acteur[3]; var plat = new Acteur[3];
            for (int i = 0; i < 3; i++)
            {
                d.Rect(210, 26, Gris, 10).En(xs[i], Sol);
                ressort[i] = d.Rect(64, 120, Color.FromRgb(0xC9, 0xD1, 0xDE), 14).En(xs[i], Sol - 26);
                plat[i] = d.Rect(240, 20, Demo.Encre, 10, false).En(xs[i], Sol - 146);
            }
            double haut = Sol - 166;
            var pomme = d.Image("pomme", "🍎", 120).En(xs[0], 110).Opacite(0);
            var pierre = d.Image("pierre", "🪨", 130).En(xs[1], 110).Opacite(0);
            var feuille = d.Image("feuille", "🍃", 110).En(xs[2], 110).Opacite(0);
            void Descendre(int i, Acteur a, double dy) { a.Aller(xs[i], haut + dy, 700); plat[i].Aller(xs[i], Sol - 146 + dy, 700); ressort[i].GrandirXY(1, (120 - dy) / 120, 700); }
            d.Dire("Trois plateaux à ressort. Plus c'est lourd, plus ça descend.", () => { pomme.Pop(); pierre.Pop(300); feuille.Pop(600); }, 1800);
            d.Faire(() => pomme.Aller(xs[0], haut, 700, 0, Acteur.Entree), 750);
            d.Dire("La pomme : ça descend un peu.", () => Descendre(0, pomme, 38), 1300);
            d.Faire(() => pierre.Aller(xs[1], haut, 700, 0, Acteur.Entree), 750);
            d.Dire("La pierre : ça descend beaucoup ! Elle est lourde.", () => Descendre(1, pierre, 86), 1500);
            d.Faire(() => { feuille.Aller(xs[2], haut, 1500); feuille.Secouer(0, 5, 9); }, 1600);
            d.Dire("La feuille : ça ne bouge presque pas. Elle est légère.", () => Descendre(2, feuille, 4), 1500);
            d.Dire("On range, du plus léger au plus lourd.", () =>
            {
                feuille.Bond(360, 200, 60, 1100); pomme.Bond(640, 200, 60, 1100, 250); pierre.Bond(920, 200, 60, 1100, 500);
                for (int i = 0; i < 3; i++) { plat[i].Aller(xs[i], Sol - 146, 600, 200); ressort[i].GrandirXY(1, 1, 600, 200); }
            }, 2000);
            d.Dire("La feuille : légère.", () => feuille.Briller(), 1400);
            d.Dire("La pomme : plus lourde.", () => pomme.Briller(), 1400);
            d.Toi("À toi ! Regarde la plus lourde.", pierre, () => pierre.Sauter(26, 460, 0, 2), 1100);
            d.Dire("La pierre : la plus lourde !", () => pierre.Briller(), 1400);
            return d;
        }

        // ==================================================================
        //  RANGER (tailles, longueurs)
        // ==================================================================
        /// <summary>Range les acteurs (donnés du plus petit au plus grand) : chacun rejoint sa place, en échangeant avec celui qui l'occupe.</summary>
        private static void Trier(Demo d, Acteur[] parRang, int[] depart, double[] places, Func<int, string> dit, bool enColonne = false)
        {
            int n = parRang.Length;
            var place = (int[])depart.Clone();
            for (int r = 0; r < n; r++)
            {
                int rr = r; var a = parRang[r];
                if (place[r] == r) { d.Dire(dit(r), () => a.Sauter(24), 1000); continue; }
                int autre = Array.IndexOf(place, r), libre = place[r];
                var b = parRang[autre];
                place[autre] = libre; place[r] = r;
                if (enColonne) d.Dire(dit(r), () => { a.Devant(); a.AllerAu(a.X, places[rr], 1100); b.AllerAu(b.X, places[libre], 1100); }, 1400);
                else d.Dire(dit(r), () => { a.Devant(); a.Bond(places[rr], Sol, 150, 1100); b.Bond(places[libre], Sol, 50, 1100); }, 1400);
            }
        }

        private static double[] Places(int n) => n == 3 ? new double[] { 380, 640, 900 } : new double[] { 250, 445, 640, 835, 1030 };
        private static int[] Depart(int n) => n == 3 ? new[] { 1, 2, 0 } : new[] { 2, 4, 0, 3, 1 };

        /// <summary>Le même objet en plusieurs tailles : rangé, ça monte comme un escalier.</summary>
        private static Demo Escalier(int n)
        {
            var d = new Demo();
            var so = Courant?.O; bool fem = so != null && so.Fem;      // l'objet de l'exercice, s'il y en a un
            var (nom, emoji) = so != null ? (so.Nom, so.Emoji) : Visuels.Un(new[] { ("arbre", "🌳"), ("ours", "🐻"), ("lapin", "🐰") });
            var xs = Places(n); var dep = Depart(n);
            var a = new Acteur[n]; var h = new double[n];
            // Un dessin large ne doit pas déborder sur son voisin : on réduit toutes les tailles ensemble.
            var essai = d.Image(nom, emoji, 100); double forme = essai.W / 100; d.Scene.Children.Remove(essai.Boite);
            double k = Math.Min(1, (xs[1] - xs[0] - 20) / (298 * forme));
            for (int r = 0; r < n; r++) { h[r] = (n == 3 ? 110 + 90 * r : 90 + 52 * r) * k; a[r] = d.Image(nom, emoji, h[r]).En(xs[dep[r]], Sol).Opacite(0); }
            d.Dire(fem ? "Regarde : elles n'ont pas toutes la même taille." : "Regarde : ils n'ont pas tous la même taille.", () => { for (int r = 0; r < n; r++) a[r].Pop(dep[r] * 180); }, 1800);
            d.Dire(fem ? "On les range, de la plus petite à la plus grande." : "On les range, du plus petit au plus grand.", null, 1200);
            Trier(d, a, dep, xs, r => r == 0 ? (fem ? "D'abord la plus petite." : "D'abord le plus petit.") : r == n - 1 ? (fem ? "Et la plus grande, au bout !" : "Et le plus grand, au bout !")
                : r == 1 ? (fem ? "Ensuite, un peu plus grande." : "Ensuite, un peu plus grand.") : r == 2 ? (fem ? "Puis celle-là." : "Puis celui-là.") : (fem ? "Et celle-là." : "Et celui-là."));
            // L'escalier : une marche au-dessus de chaque tête.
            var marches = new List<Line>();
            for (int r = 0; r < n; r++)
            {
                double g = r == 0 ? xs[0] - 90 : (xs[r - 1] + xs[r]) / 2, dr = r == n - 1 ? xs[r] + 90 : (xs[r] + xs[r + 1]) / 2, y = Sol - h[r] - 18;
                marches.Add(d.Trait(g, y, dr, y, Demo.Or, 8));
                if (r < n - 1) marches.Add(d.Trait(dr, y, dr, Sol - h[r + 1] - 18, Demo.Or, 8));
            }
            d.Dire("Ça monte, comme un escalier !", () => { for (int i = 0; i < marches.Count; i++) Demo.Voir(marches[i], 300, i * 260); for (int r = 0; r < n; r++) a[r].Sauter(20, 400, r * 520); }, 600 + n * 520);
            d.Toi(fem ? "À toi ! Regarde la plus grande." : "À toi ! Regarde le plus grand.", a[n - 1], () => a[n - 1].Sauter(30, 460, 0, 2), 1100);
            d.Dire(fem ? "Oui ! De la plus petite…" : "Oui ! Du plus petit…", () => a[0].Briller(), 1400);
            d.Dire(fem ? "à la plus grande." : "au plus grand.", () => a[n - 1].Briller(), 1400);
            return d;
        }

        private static readonly (string Nom, string Emoji, string Dit, bool Fem, double H)[] Betes =
        {
            ("coccinelle", "🐞", "la coccinelle", true, 70), ("poussin", "🐥", "le poussin", false, 125), ("chat", "🐱", "le chat", false, 185),
            ("mouton", "🐑", "le mouton", false, 245), ("girafe", "🦒", "la girafe", true, 320),
        };

        /// <summary>Des animaux différents, rangés par taille puis mesurés à la toise.</summary>
        private static Demo AnimauxRanges(int n)
        {
            var d = new Demo();
            var b = n == 3 ? new[] { Betes[0], Betes[2], Betes[4] } : Betes;
            var xs = Places(n); var dep = Depart(n);
            var a = new Acteur[n]; var toise = new Acteur[n];
            for (int r = 0; r < n; r++) a[r] = d.Image(b[r].Nom, b[r].Emoji, b[r].H).En(xs[dep[r]], Sol).Opacite(0);
            for (int r = 0; r < n; r++) toise[r] = d.Toise(b[r].H, Demo.Or).En(xs[r] + a[r].W / 2 + 26, Sol);
            string Maj(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);
            d.Dire("Voici des animaux. Ils n'ont pas la même taille.", () => { for (int r = 0; r < n; r++) a[r].Pop(dep[r] * 180); }, 1900);
            d.Dire("On les range, du plus petit au plus grand.", null, 1200);
            Trier(d, a, dep, xs, r => r == 0 ? Maj(b[r].Dit) + " : " + (b[r].Fem ? "la plus petite." : "le plus petit.")
                : r == n - 1 ? "Et " + b[r].Dit + " : " + (b[r].Fem ? "la plus grande !" : "le plus grand !")
                : "Ensuite, " + b[r].Dit + ".");
            d.Dire("On mesure : ça grandit, ça grandit !", () => { for (int r = 0; r < n; r++) { toise[r].Fondu(1, 200, r * 450); toise[r].GrandirXY(1, 1, 700, r * 450); } }, 700 + n * 450);
            d.Toi("À toi ! Regarde " + (b[n - 1].Fem ? "la plus grande." : "le plus grand."), a[n - 1], () => a[n - 1].Sauter(30, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(b[n - 1].Dit) + " !", () => a[n - 1].Briller(), 1400);
            return d;
        }

        /// <summary>Des barres en désordre : même départ, puis de la plus courte à la plus longue.</summary>
        private static Demo BarresRangees(int n)
        {
            var d = new Demo("table");
            const double X0 = 250;
            var col = Visuels.Un(Visuels.Couleurs).C;
            var dep = Depart(n);
            var ys = n == 3 ? new double[] { 200, 320, 440 } : new double[] { 130, 225, 320, 415, 510 };
            double[] px = { 860, 420, 700, 300, 980 }, py = { 180, 300, 470, 130, 400 }, ang = { -12, 16, -7, 22, 9 };
            var a = new Acteur[n]; var len = new double[n];
            for (int r = 0; r < n; r++)
            {
                len[r] = n == 3 ? 260 + 190 * r : 220 + 105 * r;
                a[r] = d.Rect(len[r], 48, col, 24).Origine(0.5, 0.5).Au(px[r], py[r]).Angle(ang[r]).Opacite(0);
            }
            var depart = d.Trait(X0, 70, X0, 560, Demo.Encre, 7, true);
            d.Dire("Des barres, toutes mélangées.", () => { for (int r = 0; r < n; r++) a[r].Pop(r * 160); }, 1600);
            d.Dire("On les met toutes au même départ.", () =>
            {
                Demo.Voir(depart);
                for (int r = 0; r < n; r++) { a[r].Tourner(0, 900, 300); a[r].AllerAu(X0 + len[r] / 2, ys[dep[r]], 1300, 300); }
            }, 1900);
            Trier(d, a, dep, ys, r => r == 0 ? "La plus courte, tout en haut." : r == n - 1 ? "Et la plus longue, tout en bas !" : r == 1 ? "Ensuite, un peu plus longue." : r == 2 ? "Puis celle-là." : "Et celle-là.", true);
            d.Dire("Ça descend, comme un escalier.", () => { for (int r = 0; r < n; r++) a[r].Briller(r * 380, 1200); }, 900 + n * 380);
            d.Toi("À toi ! Regarde la plus longue.", a[n - 1], () => a[n - 1].Sauter(20, 460, 0, 2), 1100);
            d.Dire("Oui ! De la plus courte…", () => a[0].Briller(), 1400);
            d.Dire("à la plus longue.", () => a[n - 1].Briller(), 1400);
            return d;
        }

        private static FrameworkElement Crayon(double h, Color c)
        {
            var cv = new Canvas { Width = 50, Height = h };
            var bord = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0));
            cv.Children.Add(new Polygon { Points = new PointCollection { new Point(25, 0), new Point(47, 44), new Point(3, 44) }, Fill = new SolidColorBrush(Color.FromRgb(0xF3, 0xD3, 0xA0)), Stroke = bord, StrokeThickness = 3, StrokeLineJoin = PenLineJoin.Round });
            cv.Children.Add(new Polygon { Points = new PointCollection { new Point(25, 0), new Point(33, 16), new Point(17, 16) }, Fill = new SolidColorBrush(c) });
            var corps = new Rectangle { Width = 44, Height = h - 44, Fill = new SolidColorBrush(c), Stroke = bord, StrokeThickness = 3, RadiusX = 5, RadiusY = 5 };
            Canvas.SetLeft(corps, 3); Canvas.SetTop(corps, 42); cv.Children.Add(corps);
            var reflet = new Rectangle { Width = 9, Height = h - 60, Fill = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), RadiusX = 4, RadiusY = 4 };
            Canvas.SetLeft(reflet, 11); Canvas.SetTop(reflet, 50); cv.Children.Add(reflet);
            return cv;
        }

        /// <summary>Des crayons debout, de toutes les couleurs : la longueur se voit aussi à la verticale.</summary>
        private static Demo Crayons(int n)
        {
            var d = new Demo("piece");
            var xs = Places(n); var dep = Depart(n);
            var cols = new[] { Rouge, Bleu, Vert, Violet, Orange };
            var a = new Acteur[n]; var h = new double[n];
            for (int r = 0; r < n; r++) { h[r] = n == 3 ? 170 + 120 * r : 150 + 65 * r; a[r] = d.Chose(Crayon(h[r], cols[(r * 2 + 1) % 5]), 50, h[r]).En(xs[dep[r]], Sol).Opacite(0); }
            var repere = d.Trait(xs[0] - 110, Sol - h[0], xs[n - 1] + 110, Sol - h[0], Demo.Or, 6, true);
            d.Dire("Des crayons de toutes les couleurs.", () => { for (int r = 0; r < n; r++) a[r].Pop(dep[r] * 180); }, 1700);
            d.Dire("Ils sont tous posés sur le sol : on voit bien lequel dépasse.", () => Demo.Voir(repere, 600), 1700);
            d.Dire("On les range, du plus court au plus long.", null, 1200);
            Trier(d, a, dep, xs, r => r == 0 ? "D'abord le plus court." : r == n - 1 ? "Et le plus long, au bout !" : r == 1 ? "Ensuite, un peu plus long." : r == 2 ? "Puis celui-là." : "Et celui-là.");
            d.Dire("La couleur ne change rien : c'est la longueur qui compte.", () => { for (int r = 0; r < n; r++) a[r].Sauter(18, 400, r * 260); }, 800 + n * 260);
            d.Toi("À toi ! Regarde le plus long.", a[n - 1], () => a[n - 1].Sauter(30, 460, 0, 2), 1100);
            d.Dire("Oui ! Du plus court…", () => a[0].Briller(), 1400);
            d.Dire("au plus long.", () => a[n - 1].Briller(), 1400);
            return d;
        }

        // ==================================================================
        //  BEAUCOUP / UN PEU · PLUS / MOINS · AUTANT
        // ==================================================================
        private static FrameworkElement Panier()
        {
            var cv = new Canvas { Width = 280, Height = 130 };
            var bord = new SolidColorBrush(Color.FromRgb(0x8A, 0x57, 0x2C));
            cv.Children.Add(new Polygon { Points = new PointCollection { new Point(0, 12), new Point(280, 12), new Point(246, 130), new Point(34, 130) }, Fill = new SolidColorBrush(Color.FromRgb(0xD9, 0xA4, 0x66)), Stroke = bord, StrokeThickness = 4, StrokeLineJoin = PenLineJoin.Round });
            for (int i = 1; i < 4; i++) cv.Children.Add(new Line { X1 = 10 + i * 6, Y1 = 12 + i * 29, X2 = 270 - i * 6, Y2 = 12 + i * 29, Stroke = bord, StrokeThickness = 3, Opacity = 0.5 });
            var haut = new Rectangle { Width = 292, Height = 22, RadiusX = 11, RadiusY = 11, Fill = new SolidColorBrush(Color.FromRgb(0xC4, 0x8A, 0x4C)), Stroke = bord, StrokeThickness = 4 };
            Canvas.SetLeft(haut, -6); cv.Children.Add(haut);
            return cv;
        }

        /// <summary>Deux paniers : un peu de pommes ici, beaucoup là.</summary>
        private static Demo Paniers()
        {
            var d = new Demo();
            double[] xs = { 360, 920 };
            Acteur[] Pommes(double x, int n)
            {
                var l = new Acteur[n];
                for (int i = 0; i < n; i++) l[i] = d.Image("pomme", "🍎", 66).En(x - 74 + (i % 3) * 74 + ((i / 3) % 2) * 20, -80);
                return l;
            }
            double Y(int i) => Sol - 108 - (i / 3) * 50;
            var peu = Pommes(xs[0], 2); var bcp = Pommes(xs[1], 11);
            var pg = d.Chose(Panier(), 280, 130).En(xs[0], Sol).Opacite(0);
            var pd = d.Chose(Panier(), 280, 130).En(xs[1], Sol).Opacite(0);
            void Tomber(Acteur[] l, int de, int a, int pas) { for (int i = de; i < a; i++) { l[i].Aller(l[i].X, Y(i), 650, (i - de) * pas, Acteur.Entree); } }
            d.Dire("Voici deux paniers.", () => { pg.Pop(); pd.Pop(300); }, 1200);
            d.Dire("Ici, je mets un peu de pommes.", () => Tomber(peu, 0, 2, 450), 1700);
            d.Dire("Là, je mets beaucoup de pommes !", () => Tomber(bcp, 0, 8, 240), 2800);
            d.Dire("Un peu…", () => pg.Briller(), 1400);
            d.Dire("et beaucoup !", () => pd.Briller(), 1400);
            d.Toi("À toi ! Regarde ce panier pour ajouter encore des pommes.", pd, () => Tomber(bcp, 8, 11, 260), 1500);
            d.Dire("Beaucoup, beaucoup de pommes !", () => pd.Sauter(14, 420), 1200);
            return d;
        }

        /// <summary>Face à face, un avec un : ce qui reste tout seul, c'est ce qu'il y a en plus.</summary>
        private static Demo Lignes(int haut, int bas)
        {
            var d = new Demo("table");
            double X(int i) => 300 + i * 130;
            const double YH = 290, YB = 500;
            double[] ox = { 0, 70, -60, 40, -30, 90, -80 }, oy = { 0, -50, -30, 30, 60, 20, -70 };
            var h = new Acteur[haut]; var b = new Acteur[bas];
            for (int i = 0; i < haut; i++) h[i] = d.Image("pomme", "🍎", 100).En(360 + ox[i], 280 + oy[i]).Opacite(0);
            for (int i = 0; i < bas; i++) b[i] = d.Image("pomme", "🍎", 100).En(900 + ox[i], 420 + oy[i]).Opacite(0);
            var liens = new Line[bas];
            for (int i = 0; i < bas; i++) liens[i] = d.Trait(X(i), YH + 8, X(i), YB - 108, Demo.Encre, 6, true);
            d.Dire("Deux tas de pommes. Où y en a-t-il le plus ?", () => { for (int i = 0; i < haut; i++) h[i].Pop(i * 90); for (int i = 0; i < bas; i++) b[i].Pop(500 + i * 90); }, 2200);
            d.Dire("On les met en ligne, face à face.", () =>
            {
                for (int i = 0; i < haut; i++) h[i].Aller(X(i), YH, 1200, i * 80);
                for (int i = 0; i < bas; i++) b[i].Aller(X(i), YB, 1200, i * 80);
            }, 1900);
            d.Dire("Une avec une, une avec une…", () => { for (int i = 0; i < bas; i++) Demo.Voir(liens[i], 300, i * 520); }, 400 + bas * 520);
            d.Dire("Celles-là sont toutes seules : elles sont en plus.", () => { for (int i = bas; i < haut; i++) { h[i].Sauter(22, 440, (i - bas) * 200, 2); h[i].Briller(); } }, 1900);
            d.Toi("À toi ! Regarde une pomme en plus.", h[haut - 1], () => h[haut - 1].Sauter(26, 460, 0, 2), 1100);
            d.Dire("En haut, il y en a plus.", () => { for (int i = 0; i < haut; i++) h[i].Sauter(14, 380, i * 70); }, 1500);
            d.Dire("En bas, il y en a moins.", () => { for (int i = 0; i < bas; i++) b[i].Sauter(14, 380, i * 70); }, 1500);
            return d;
        }

        /// <summary>Deux tours de cubes : la plus haute, c'est là qu'il y en a le plus.</summary>
        private static Demo Tours(int gauche, int droite)
        {
            var d = new Demo("piece");
            const double T = 66, XG = 440, XD = 840;
            double[] ox = { -150, -60, 40, -110, 90, -20, 130, -170 };
            var g = new Acteur[gauche]; var dr = new Acteur[droite];
            for (int i = 0; i < gauche; i++) g[i] = d.Bloc(T, Bleu).En(XG - 60 + ox[i] * 0.8, Sol).Opacite(0);
            for (int i = 0; i < droite; i++) dr[i] = d.Bloc(T, Orange).En(XD + 60 + ox[i] * 0.8, Sol).Opacite(0);
            int petit = Math.Min(gauche, droite); bool plusADroite = droite > gauche;
            var niveau = d.Trait(XG - 110, Sol - petit * T, XD + 110, Sol - petit * T, Demo.Encre, 6, true);
            var hauts = plusADroite ? dr : g; var bas = plusADroite ? g : dr;
            d.Dire("Des cubes bleus, des cubes orange. Où y en a-t-il le plus ?", () => { for (int i = 0; i < gauche; i++) g[i].Pop(i * 100); for (int i = 0; i < droite; i++) dr[i].Pop(400 + i * 100); }, 2200);
            d.Dire("On fait deux tours.", () =>
            {
                for (int i = 0; i < gauche; i++) g[i].Bond(XG, Sol - i * T, 90, 700, i * 380);
                for (int i = 0; i < droite; i++) dr[i].Bond(XD, Sol - i * T, 90, 700, 190 + i * 380);
            }, 900 + Math.Max(gauche, droite) * 380);
            d.Dire("Cette tour est plus haute.", () => { Demo.Voir(niveau); for (int i = petit; i < hauts.Length; i++) hauts[i].Briller(300); }, 1800);
            d.Dire("C'est là qu'il y en a le plus.", () => { for (int i = 0; i < hauts.Length; i++) hauts[i].Sauter(12, 380, i * 80); }, 1500);
            d.Dire("L'autre est plus petite : il y en a moins.", () => { for (int i = 0; i < bas.Length; i++) bas[i].Sauter(12, 380, i * 80); }, 1500);
            d.Toi("À toi ! Regarde le haut de la tour où il y en a le plus.", hauts[hauts.Length - 1], () => hauts[hauts.Length - 1].Sauter(22, 460, 0, 2), 1100);
            d.Dire("Oui ! La plus haute : le plus de cubes.", null, 1000);
            return d;
        }

        /// <summary>Une carotte pour chaque lapin : autant. Un lapin de plus : plus autant.</summary>
        private static Demo Paires()
        {
            var d = new Demo();
            double[] xs = { 250, 460, 670, 880, 1090 };
            double[] cx = { 520, 330, 760, 640, 1150 };
            var lapin = new Acteur[5]; var car = new Acteur[5];
            for (int i = 0; i < 5; i++) lapin[i] = d.Image("lapin", "🐰", 170).En(i < 4 ? xs[i] : 1450, Sol).Opacite(i < 4 ? 0 : 1);
            for (int i = 0; i < 5; i++) car[i] = d.Image("carotte", "🥕", 96).En(cx[i], 200).Opacite(0);
            d.Dire("Voici des lapins…", () => { for (int i = 0; i < 4; i++) lapin[i].Pop(i * 160); }, 1500);
            d.Dire("et des carottes.", () => { for (int i = 0; i < 4; i++) car[i].Pop(i * 160); }, 1500);
            d.Dire("Une carotte pour chaque lapin.", () => { for (int i = 0; i < 4; i++) car[i].Aller(xs[i] + 70, Sol - 6, 800, i * 620); }, 4 * 620 + 500);
            d.Dire("Chaque lapin a sa carotte, et il ne reste rien.", () => { for (int i = 0; i < 4; i++) lapin[i].Sauter(18, 400, i * 120); }, 1700);
            d.Dire("Il y a autant de carottes que de lapins.", null, 1400);
            d.Dire("Oh ! Un lapin de plus.", () => lapin[4].Bond(xs[4], Sol, 70, 1100), 1500);
            d.Dire("Pas de carotte pour lui… Ce n'est plus autant.", () => lapin[4].Secouer(0, 3, 6), 1700);
            d.Toi("À toi ! Regarde ce lapin pour lui donner une carotte.", lapin[4], () => { car[4].Pop(); car[4].Aller(xs[4] + 70, Sol - 6, 900, 350); }, 1500);
            d.Dire("Voilà ! Autant de carottes que de lapins.", () => { for (int i = 0; i < 5; i++) lapin[i].Sauter(18, 400, i * 110); }, 1600);
            return d;
        }

        /// <summary>Deux tours qui montent ensemble, un cube ici, un cube là : pareilles, donc autant.</summary>
        private static Demo ToursEgales()
        {
            var d = new Demo("piece");
            const double T = 66, XG = 440, XD = 840; const int N = 4;
            var g = new Acteur[N]; var dr = new Acteur[N + 1];
            for (int i = 0; i < N; i++) g[i] = d.Bloc(T, Bleu).En(150, Sol - 10 - i * 12).Opacite(0);
            for (int i = 0; i <= N; i++) dr[i] = d.Bloc(T, Orange).En(1130, Sol - 10 - i * 12).Opacite(0);
            var niveau = d.Trait(XG - 110, Sol - N * T, XD + 110, Sol - N * T, Demo.Or, 7, true);
            d.Dire("Des cubes bleus, des cubes orange.", () => { for (int i = 0; i < N; i++) { g[i].Pop(i * 90); dr[i].Pop(i * 90); } }, 1400);
            d.Dire("Un ici, un là. Un ici, un là…", () =>
            {
                for (int i = 0; i < N; i++) { g[N - 1 - i].Bond(XG, Sol - i * T, 90, 650, i * 800); dr[N - 1 - i].Bond(XD, Sol - i * T, 90, 650, 400 + i * 800); }
            }, N * 800 + 500);
            d.Dire("Les deux tours sont pareilles.", () => Demo.Voir(niveau, 600), 1500);
            d.Dire("Il y en a autant.", () => { for (int i = 0; i < N; i++) { g[i].Sauter(12, 380, i * 70); dr[i].Sauter(12, 380, i * 70); } }, 1500);
            d.Dire("J'ajoute un cube ici.", () => { dr[N].Pop(); dr[N].Bond(XD, Sol - N * T, 90, 800, 350); }, 1500);
            d.Dire("Ce n'est plus autant : il y en a un en trop.", () => dr[N].Briller(), 1700);
            d.Toi("À toi ! Regarde le cube en trop pour l'enlever.", dr[N], () => { dr[N].Bond(1130, Sol - 10, 110, 900); dr[N].Fondu(0.35, 600, 700); }, 1500);
            d.Dire("Autant !", () => { for (int i = 0; i < N; i++) { g[i].Sauter(12, 380, i * 70); dr[i].Sauter(12, 380, i * 70); } }, 1300);
            return d;
        }

        // ==================================================================
        //  LE PLUS GRAND NOMBRE
        // ==================================================================
        /// <summary>Un nombre, c'est une tour : la tour de six est plus haute que la tour de trois.</summary>
        private static Demo ToursNombres()
        {
            var d = new Demo("piece");
            const double T = 60, XG = 440, XD = 840;
            int a = Visuels.Entre(2, 4), b = Visuels.Entre(a + 2, 8);
            if (Visuels.Rng.Next(2) == 0) { int t = a; a = b; b = t; }
            int grand = Math.Max(a, b), petit = Math.Min(a, b); double xGrand = a > b ? XG : XD;
            var g = new Acteur[a]; var dr = new Acteur[b];
            for (int i = 0; i < a; i++) g[i] = d.Bloc(T, Bleu).En(XG, Sol - i * T).Opacite(0);
            for (int i = 0; i < b; i++) dr[i] = d.Bloc(T, Orange).En(XD, Sol - i * T).Opacite(0);
            var ca = d.Chose(Visuels.Chiffre(a, 96), 96, 96).Au(XG, Sol + 62).Opacite(0);
            var cb = d.Chose(Visuels.Chiffre(b, 96), 96, 96).Au(XD, Sol + 62).Opacite(0);
            var niveau = d.Trait(XG - 110, Sol - petit * T, XD + 110, Sol - petit * T, Demo.Encre, 6, true);
            var cGrand = a > b ? ca : cb; var tour = a > b ? g : dr;
            d.Dire(Maj(Mots[a]) + ".", () => { ca.Pop(); for (int i = 0; i < a; i++) g[i].Pop(400 + i * 300); }, 800 + a * 300);
            d.Dire(Maj(Mots[b]) + ".", () => { cb.Pop(); for (int i = 0; i < b; i++) dr[i].Pop(400 + i * 300); }, 800 + b * 300);
            d.Dire("La tour de " + Mots[grand] + " est plus haute.", () => { Demo.Voir(niveau); for (int i = petit; i < grand; i++) tour[i].Briller(300); }, 1900);
            d.Dire(Maj(Mots[grand]) + " est plus grand que " + Mots[petit] + ".", () => cGrand.Briller(), 1800);
            d.Toi("À toi ! Regarde le plus grand nombre.", cGrand, () => cGrand.Sauter(20, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Mots[grand]) + " !", () => { for (int i = 0; i < grand; i++) tour[i].Sauter(10, 360, i * 70); }, 1300);
            return d;
        }

        /// <summary>Sur la bande des nombres : plus on va loin, plus le nombre est grand.</summary>
        private static Demo Bande()
        {
            var d = new Demo();
            int a = Visuels.Entre(2, 4), b = Visuels.Entre(a + 2, 9);
            const double Cell = 96, Pas = 104;
            d.Chose(Visuels.Bande(1, 10, -1, Cell), 10 * Pas, Pas).En(640, Sol + 96);
            double X(int k) => 640 - 5 * Pas + (k - 0.5) * Pas;
            double y = Sol - 12;
            var gre = d.Image("grenouille", "🐸", 110).En(X(1), y).Opacite(0);
            var lap = d.Image("lapin", "🐰", 130).En(X(1), y).Opacite(0);
            d.Dire("Voici la bande des nombres.", () => gre.Pop(600), 1500);
            d.Dire("La grenouille saute jusqu'à " + Mots[a] + ".", null, 1000);
            for (int k = 2; k <= a; k++) { int kk = k; d.Faire(() => gre.Bond(X(kk), y, 60, 420), 470); }
            d.Dire("Le lapin saute jusqu'à " + Mots[b] + ".", () => lap.Pop(), 1200);
            for (int k = 2; k <= b; k++) { int kk = k; d.Faire(() => lap.Bond(X(kk), y, 60, 380), 420); }
            d.Dire("Le lapin est allé plus loin.", () => lap.Briller(), 1500);
            d.Dire(Maj(Mots[b]) + " est plus grand que " + Mots[a] + ".", null, 1500);
            d.Toi("À toi ! Regarde celui qui est allé le plus loin.", lap, () => lap.Sauter(30, 460, 0, 2), 1100);
            d.Dire("Oui ! Plus loin sur la bande, c'est plus grand.", null, 1200);
            return d;
        }

        private static string Maj(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ==================================================================
        //  « REGARDE, JE TE MONTRE » : après une erreur, avec les objets mêmes de l'exercice
        // ==================================================================
        /// <summary>Le grand et le petit de l'exercice, côte à côte, mesurés.</summary>
        public static Demo PourquoiTailles(ObjetDef o)
        {
            var d = new Demo();
            var g = d.Image(o.Nom, o.Emoji, 300).En(430, Sol).Opacite(0);
            var p = d.Image(o.Nom, o.Emoji, 300).En(860, Sol).Echelle(0.4).Opacite(0);
            var tg = d.Toise(300, Demo.Or).En(430 + g.W / 2 + 30, Sol);
            var tp = d.Toise(120, Demo.Or).En(860 + g.W * 0.2 + 30, Sol);
            d.Dire("Regarde bien.", () => { g.Pop(); p.Pop(300); }, 1100);
            d.Dire(o.Fem ? "Celle-ci est grande." : "Celui-ci est grand.", () => { tg.Opacite(1); tg.GrandirXY(1, 1, 1100); g.Briller(200); }, 1700);
            d.Dire(o.Fem ? "Celle-là est petite." : "Celui-là est petit.", () => { tp.Opacite(1); tp.GrandirXY(1, 1, 600); p.Briller(200); }, 1700);
            return d;
        }

        /// <summary>Les deux barres de l'exercice, au même départ.</summary>
        public static Demo PourquoiLongueurs(Color col) => DepartDeux(col, 520, 240, false);

        /// <summary>Le gros et le mince de l'exercice, avec leur largeur montrée dessous.</summary>
        public static Demo PourquoiGrosseurs(Color col)
        {
            var d = new Demo();
            var g = d.Rect(150, 280, col, 30).En(440, Sol).Opacite(0);
            var m = d.Rect(44, 280, col, 20).En(840, Sol).Opacite(0);
            var lg = d.Rect(150, 16, Demo.Or, 8, false).Origine(0.5, 0.5).Au(440, Sol + 50).EchelleXY(0.001, 1);
            var lm = d.Rect(44, 16, Demo.Or, 8, false).Origine(0.5, 0.5).Au(840, Sol + 50).EchelleXY(0.001, 1);
            d.Dire("Regarde bien.", () => { g.Pop(); m.Pop(300); }, 1100);
            d.Dire("Celui-ci est gros.", () => { lg.GrandirXY(1, 1, 900); g.Briller(200); }, 1700);
            d.Dire("Celui-là est mince.", () => { lm.GrandirXY(1, 1, 600); m.Briller(200); }, 1700);
            return d;
        }

        /// <summary>Le lourd et le léger de l'exercice, sur la balance.</summary>
        public static Demo PourquoiPoids(string emojiLourd, string nomLourd, string emojiLeger, string nomLeger)
        {
            var d = new Demo();
            var bal = new Bal(d);
            var l = d.Chose(Art.Visual("", emojiLourd, 150), 150, 150).En(Bal.XG, -40);
            var g = d.Chose(Art.Visual("", emojiLeger, 120), 120, 120).En(Bal.XD, -40);
            d.Dire("Regarde, on les pose sur la balance.", () => { l.Aller(Bal.XG, Bal.Plateau, 900, 0, Acteur.Entree); g.Aller(Bal.XD, Bal.Plateau, 900, 500, Acteur.Entree); }, 1700);
            d.Dire(Maj(nomLourd) + " descend : c'est lourd.", () => { bal.Pencher(-11, 1000, l, g); l.Briller(600); }, 2000);
            d.Dire(Maj(nomLeger) + " monte : c'est léger.", () => g.Briller(), 1700);
            return d;
        }

        /// <summary>Les collections de l'exercice, mises en lignes : la plus longue, c'est là qu'il y en a le plus.</summary>
        public static Demo PourquoiQuantites(ObjetDef o, IList<int> nombres)
        {
            var d = new Demo("table");
            int n = Math.Min(3, nombres.Count);
            double[] ys = n == 2 ? new double[] { 300, 500 } : new double[] { 230, 390, 550 };
            int max = 0, min = 0;
            for (int k = 1; k < n; k++) { if (nombres[k] > nombres[max]) max = k; if (nombres[k] < nombres[min]) min = k; }
            double t = nombres[max] > 7 ? 80 : 110, pas = Math.Min(130, 820.0 / Math.Max(1, nombres[max]));
            var lignes = new List<Acteur[]>();
            for (int k = 0; k < n; k++)
            {
                var l = new Acteur[nombres[k]];
                for (int i = 0; i < l.Length; i++) l[i] = d.Image(o.Nom, o.Emoji, t).En(240 + i * pas, ys[k]).Opacite(0);
                lignes.Add(l);
            }
            var depart = d.Trait(175, ys[0] - t - 20, 175, ys[n - 1] + 10, Demo.Encre, 6, true);
            d.Dire("Regarde, on les met en lignes.", () => { Demo.Voir(depart); for (int k = 0; k < n; k++) for (int i = 0; i < lignes[k].Length; i++) lignes[k][i].Pop(k * 500 + i * 110); }, 900 + n * 500 + nombres[max] * 110);
            d.Dire("La ligne la plus longue : c'est là qu'il y en a le plus.", () => { foreach (var a in lignes[max]) a.Briller(); }, 2100);
            d.Dire("La ligne la plus courte : c'est là qu'il y en a le moins.", () => { foreach (var a in lignes[min]) a.Briller(); }, 2100);
            return d;
        }
    }
}
