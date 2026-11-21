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
    /// Suite de la deuxième série : COMPOSER ET DÉCOMPOSER, LA SUITE DES NOMBRES,
    /// LE RANG ET LA BANDE, LES PETITS PROBLÈMES.
    /// </summary>
    public static partial class Explications
    {
        public static Famille Decomposer(int min, int max) => new Famille("decomposer", ("separer", () => Decompose(min, max)), ("plateaux", () => DeuxPlateaux(min, max)));
        public static Famille AjouteUn(int max) => new Famille("ajoute-un", ("objets", () => UnDePlus(max)), ("escalier", () => EscalierPlusUn(max)));
        public static Famille EnleveUn(int max) => new Famille("enleve-un", ("objets", () => UnDeMoins(max)), ("escalier", () => EscalierMoinsUn(max)));
        public static readonly Famille Decomposer10 = new Famille("decomposer-10", ("boite", Complement10), ("separer", () => Decompose(10, 10)));
        public static readonly Famille Doubles = new Famille("doubles", ("miroir", DoublesMiroir), ("domino", DoublesDomino));
        public static Famille Apres(int max) => new Famille("apres", ("bande", () => VoisinBande(max, 1)), ("tour", () => VoisinTour(max, 1)));
        public static Famille Avant(int max) => new Famille("avant", ("bande", () => VoisinBande(max, -1)), ("tour", () => VoisinTour(max, -1)));
        public static Famille Manquant(int max) => new Famille("manquant", ("trou", () => BandeTrou(max)), ("bande", () => VoisinBande(max, 1)));
        public static readonly Famille Rebours = new Famille("rebours", ("fusee", Fusee), ("bande", BandeRecule));
        public static readonly Famille DeuxEnDeux = new Famille("deux-en-deux", ("bande", BandeSaut), ("paires", PairesChaussettes));
        public static Famille RangFile(int n) => new Famille("rang", ("file", () => FileRang(n)), ("course", Course));
        public static readonly Famille DeBande = new Famille("de-bande", ("jetons", DeSurBande), ("formes", () => Representations(6, "de")));
        public static readonly Famille Avancer = new Famille("piste-avancer", ("piste", () => Piste(true)), ("bande", () => VoisinBande(10, 1)));
        public static readonly Famille Reculer = new Famille("piste-reculer", ("piste", () => Piste(false)), ("bande", () => VoisinBande(10, -1)));
        public static Famille Ajout(int max) => new Famille("ajout", ("compter", () => AjoutCompte(max)), ("tour", () => AjoutTours(max)));
        public static Famille Retrait(int max) => new Famille("retrait", ("oiseaux", () => RetraitOiseaux(max)), ("tour", () => RetraitTours(max)));
        public static Famille Partage(int moitie) => new Famille("partage", ("lapins", () => PartageLapins(moitie)), ("tours", () => PartageTours(moitie)));
        public static readonly Famille Ecart = new Famille("ecart", ("lignes", EcartLignes), ("tours", () => Tours(3, 5)));
        public static readonly Famille Manque = new Famille("manque", ("assiettes", ManqueAssiettes), ("boite", Complement10));

        private static string Un(ObjetDef o) => o.Fem ? "une" : "un";

        // ==================================================================
        //  COMPOSER ET DÉCOMPOSER
        // ==================================================================
        /// <summary>Le même nombre se sépare de plusieurs façons, et reste le même.</summary>
        private static Demo Decompose(int min, int max)
        {
            var d = new Demo("table");
            var o = ObjetCourant(); int total = NombreCourant(2, 10, () => Visuels.Entre(Math.Max(2, min), Math.Max(2, max)));
            double pas = Math.Min(125, 880.0 / total), t = Math.Min(100, pas - 18);
            var xs = Rang(total, pas);
            var a = new Acteur[total];
            for (int i = 0; i < total; i++) a[i] = Obj(d, o.Nom, o.Emoji, t, pas - 12).En(xs[i], Sol - 60).Opacite(0);
            var c = Chiffre(d, total, 170).En(640, 230).Opacite(0);
            int s1 = Courant?.N2 is int part && part >= 1 && part < total ? part : Visuels.Entre(1, total - 1), s2 = total > 2 ? (s1 % (total - 1)) + 1 : 1;
            void Separer(int s) { for (int i = 0; i < total; i++) a[i].Aller(xs[i] + (i < s ? -70 : 70), Sol - 60, 900); }
            void Reunir() { for (int i = 0; i < total; i++) a[i].Aller(xs[i], Sol - 60, 800); }
            d.Dire(Maj(o.N(total)) + ".", () => { for (int i = 0; i < total; i++) a[i].Pop(i * 100); c.Pop(total * 100); }, 1600);
            d.Dire(Maj(Dit(s1)) + "… et " + Dit(total - s1) + ".", () => Separer(s1), 1900);
            d.Dire("Ensemble : " + Dit(total) + ".", Reunir, 1500);
            d.Toi("À toi ! Regarde le chiffre pour les séparer autrement.", c, () => Separer(s2), 1200);
            d.Dire(Maj(Dit(s2)) + "… et " + Dit(total - s2) + ".", null, 1500);
            d.Dire("Ensemble, c'est toujours " + Dit(total) + ".", () => { Reunir(); c.Briller(); }, 2000);
            return d;
        }

        /// <summary>Deux plateaux : on en déplace un, les deux parts changent, le tout ne change pas.</summary>
        private static Demo DeuxPlateaux(int min, int max)
        {
            var d = new Demo("piece");
            var o = ObjetCourant(); int total = NombreCourant(2, 8, () => Math.Min(8, Visuels.Entre(Math.Max(2, min), Math.Max(2, max))));
            d.Rect(420, 30, Brun, 12).En(330, Sol);
            d.Rect(420, 30, Brun, 12).En(950, Sol);
            (double x, double y) G(int j) => (330 - 135 + (j % 4) * 90, Sol - 30 - (j / 4) * 92);
            (double x, double y) D(int j) => (950 - 135 + (j % 4) * 90, Sol - 30 - (j / 4) * 92);
            int L = total - 1, R = 1;
            var a = new Acteur[total];
            for (int i = 0; i < total; i++) { var p = i < L ? G(i) : D(0); a[i] = Obj(d, o.Nom, o.Emoji, 80, 84).En(p.x, p.y).Opacite(0); }
            var c = Chiffre(d, total, 160).En(640, 220).Opacite(0);
            int sauts = Math.Min(2, L);
            void Passer(int k) { var p = D(R + k); a[L - 1 - k].Devant(); a[L - 1 - k].Bond(p.x, p.y, 150, 1000); }
            d.Dire(Maj(o.N(total)) + ", sur deux plateaux.", () => { for (int i = 0; i < total; i++) a[i].Pop(i * 100); c.Pop(total * 100); }, 1800);
            d.Dire(Maj(Dit(L)) + " ici… et " + Dit(R) + " là.", null, 1600);
            if (sauts == 2)
            {
                d.Dire("On en déplace " + Un(o) + ".", () => Passer(0), 1300);
                d.Dire(Maj(Dit(L - 1)) + " et " + Dit(R + 1) + ".", null, 1500);
            }
            d.Toi("À toi ! Regarde le chiffre pour en déplacer " + (sauts == 2 ? "encore " : "") + Un(o) + ".", c, () => Passer(sauts - 1), 1300);
            d.Dire(Maj(Dit(L - sauts)) + " et " + Dit(R + sauts) + ".", null, 1500);
            d.Dire("Ça change de côté, mais ensemble c'est toujours " + Dit(total) + ".", () => c.Briller(), 2300);
            return d;
        }

        /// <summary>Un de plus : le nombre d'après.</summary>
        private static Demo UnDePlus(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int n = NombreCourant(1, 8, () => Visuels.Entre(1, Math.Max(1, Math.Min(7, max - 1))));
            int ajouts = n + 2 <= Math.Min(10, Math.Max(3, max)) ? 2 : 1, tot = n + ajouts;
            double pas = Math.Min(135, 920.0 / tot), t = Math.Min(110, pas - 18);
            var xs = Rang(tot, pas);
            var a = new Acteur[tot];
            for (int i = 0; i < tot; i++) a[i] = Obj(d, o.Nom, o.Emoji, t, pas - 12).En(i < n ? xs[i] : 1420, Sol - 50).Opacite(i < n ? 0 : 1);
            var compteur = new Compteur(d, n, tot, 640, 240, 180);
            d.Dire(Maj(o.N(n)) + ".", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 100); compteur.Montrer(n); }, 1500);
            d.Dire("J'en ajoute " + Un(o) + ".", () => a[n].Bond(xs[n], Sol - 50, 90, 1000), 1300);
            d.Dire("Ça fait " + Dit(n + 1) + ".", () => compteur.Montrer(n + 1), 1500);
            if (ajouts == 2)
            {
                d.Toi("À toi ! Regarde le chiffre pour en ajouter encore " + Un(o) + ".", compteur.De(n + 1), () => { a[n + 1].Bond(xs[n + 1], Sol - 50, 90, 1000); compteur.Montrer(n + 2); }, 1300);
                d.Dire(Maj(Dit(n + 2)) + " !", null, 900);
            }
            else d.Toi("À toi ! Regarde le " + Dit(n + 1) + ".", compteur.De(n + 1), () => compteur.De(n + 1).Sauter(20, 460, 0, 2), 1100);
            d.Dire("Un de plus, c'est le nombre d'après.", () => { for (int i = 0; i < tot; i++) a[i].Sauter(14, 380, i * 70); }, 1900);
            return d;
        }

        /// <summary>L'escalier des nombres : chaque tour a un cube de plus que celle d'avant.</summary>
        private static Demo EscalierPlusUn(int max)
        {
            var d = new Demo("piece");
            int m = Math.Max(2, Math.Min(max, 4)); const double T = 58;
            double X(int k) => 640 + (k - (m + 2) / 2.0) * 160;
            var tours = new Acteur[m + 2][]; var ch = new Acteur[m + 2];
            for (int k = 1; k <= m + 1; k++) { tours[k] = Tour(d, X(k), k, T, Bleu, Demo.Or); ch[k] = Chiffre(d, k, 86).Au(X(k), Sol + 62).Opacite(0); }
            void Monter(int k) { for (int i = 0; i < k; i++) tours[k][i].Pop(i * 110); ch[k].Pop(k * 110); }
            d.Dire("Un.", () => Monter(1), 1000);
            for (int k = 2; k <= m; k++) { int kk = k; d.Dire(Maj(Dit(kk)) + " : un de plus.", () => Monter(kk), 1100 + kk * 110); }
            d.Toi("À toi ! Regarde le " + Dit(m) + " pour faire la tour d'après.", ch[m], () => Monter(m + 1), 900 + m * 110);
            d.Dire(Maj(Dit(m + 1)) + " : encore un de plus.", null, 1500);
            d.Dire("À chaque fois, un cube de plus que le nombre d'avant.", () => { for (int k = 1; k <= m + 1; k++) tours[k][k - 1].Briller((k - 1) * 300, 1200); }, 2300);
            return d;
        }

        /// <summary>Un de moins : le nombre d'avant.</summary>
        private static Demo UnDeMoins(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int n = NombreCourant(2, 10, () => Visuels.Entre(2, Math.Max(2, Math.Min(8, max))));
            int retraits = n >= 3 ? 2 : 1;
            double pas = Math.Min(135, 920.0 / n), t = Math.Min(110, pas - 18);
            var xs = Rang(n, pas);
            var a = new Acteur[n];
            for (int i = 0; i < n; i++) a[i] = Obj(d, o.Nom, o.Emoji, t, pas - 12).En(xs[i], Sol - 50).Opacite(0);
            var compteur = new Compteur(d, n - retraits, n, 640, 240, 180);
            void Partir(int i) { a[i].Aller(1420, 60, 1200, 0, Acteur.Entree); a[i].Fondu(0, 400, 800); }
            d.Dire(Maj(o.N(n)) + ".", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 100); compteur.Montrer(n); }, 1500);
            d.Dire("J'en enlève " + Un(o) + ".", () => Partir(n - 1), 1400);
            d.Dire("Il en reste " + Dit(n - 1) + ".", () => compteur.Montrer(n - 1), 1500);
            if (retraits == 2)
            {
                d.Toi("À toi ! Regarde le chiffre pour en enlever encore " + Un(o) + ".", compteur.De(n - 1), () => { Partir(n - 2); compteur.Montrer(n - 2); }, 1400);
                d.Dire("Il en reste " + Dit(n - 2) + ".", null, 1300);
            }
            else d.Toi("À toi ! Regarde le " + Dit(n - 1) + ".", compteur.De(n - 1), () => compteur.De(n - 1).Sauter(20, 460, 0, 2), 1100);
            d.Dire("Un de moins, c'est le nombre d'avant.", null, 1700);
            return d;
        }

        /// <summary>Sur l'escalier : enlever un cube, c'est redescendre d'une marche.</summary>
        private static Demo EscalierMoinsUn(int max)
        {
            var d = new Demo("piece");
            int m = Math.Max(3, Math.Min(max, 5)); const double T = 58;
            double X(int k) => 640 + (k - (m + 1) / 2.0) * 160;
            var tours = new Acteur[m + 1][]; var ch = new Acteur[m + 1];
            for (int k = 1; k <= m; k++) { tours[k] = Tour(d, X(k), k, T, Bleu, Demo.Or); ch[k] = Chiffre(d, k, 86).Au(X(k), Sol + 62).Opacite(0); }
            var n1 = d.Trait(X(m - 1) - 50, Sol - (m - 1) * T, X(m) + 50, Sol - (m - 1) * T, Demo.Encre, 6, true);
            var n2 = d.Trait(X(m - 2) - 50, Sol - (m - 2) * T, X(m - 1) + 50, Sol - (m - 2) * T, Demo.Encre, 6, true);
            void Oter(int k) { var b = tours[k][k - 1]; b.Devant(); b.Bond(X(k) + 40, 110, 80, 900); b.Fondu(0, 400, 700); }
            d.Dire("L'escalier des nombres.", () => { for (int k = 1; k <= m; k++) { for (int i = 0; i < k; i++) tours[k][i].Pop(k * 200 + i * 60); ch[k].Pop(k * 200); } }, 1300 + m * 200);
            d.Dire("Regarde la tour de " + Dit(m) + ".", () => ch[m].Briller(), 1500);
            d.Dire("J'enlève un cube.", () => Oter(m), 1300);
            d.Dire("Elle devient comme la tour de " + Dit(m - 1) + ".", () => Demo.Voir(n1), 1800);
            d.Dire(Maj(Dit(m)) + ", un de moins : " + Dit(m - 1) + ".", () => ch[m - 1].Briller(), 1900);
            d.Toi("À toi ! Regarde le " + Dit(m - 1) + " pour enlever un cube à sa tour.", ch[m - 1], () => { Oter(m - 1); Demo.Voir(n2, 400, 700); }, 1400);
            d.Dire(Maj(Dit(m - 1)) + ", un de moins : " + Dit(m - 2) + ".", () => ch[m - 2].Briller(), 1900);
            return d;
        }

        /// <summary>Le cadre d'une boîte de dix ; rend le milieu et le bas de chaque case.</summary>
        private static Acteur CadreDix(Demo d, double x0, double y0, double c, out Func<int, (double x, double y)> place)
        {
            var cv = new Canvas { Width = 5 * c, Height = 2 * c };
            for (int i = 0; i < 10; i++)
            {
                var r = new Rectangle { Width = c, Height = c, Fill = Brushes.White, Stroke = new SolidColorBrush(Demo.Encre), StrokeThickness = 4 };
                Canvas.SetLeft(r, (i % 5) * c); Canvas.SetTop(r, (i / 5) * c); cv.Children.Add(r);
            }
            place = i => (x0 + (i % 5 + 0.5) * c, y0 + (i / 5 + 1) * c - 14);
            return d.Chose(cv, 5 * c, 2 * c).En(x0 + 2.5 * c, y0 + 2 * c);
        }

        /// <summary>La boîte de dix : les jetons et les cases vides font dix ensemble.</summary>
        private static Demo Complement10()
        {
            var d = new Demo("table");
            const double C = 110;
            int a = NombreCourant(1, 9, () => Visuels.Entre(2, 9)), b = 10 - a;
            var cadre = CadreDix(d, 640 - 2.5 * C, 230, C, out var place);
            var j = new Acteur[10];
            for (int i = 0; i < 10; i++) j[i] = d.Rond(C - 28, i < a ? Bleu : Orange).En(place(i).x, place(i).y).Opacite(0);
            var compteur = new Compteur(d, 1, b, 1130, 400, 150);
            d.Dire(Maj(Dit(a)) + " jetons dans la boîte de dix.", () => { for (int i = 0; i < a; i++) j[i].Pop(i * 110); }, 1900);
            d.Dire("Il reste des cases vides.", null, 1300);
            d.Toi("À toi ! Regarde la boîte pour la remplir, en comptant.", cadre, () => { j[a].Pop(); compteur.Montrer(1); }, 700);
            d.Dire("Un.", null, 500);
            for (int i = 1; i < b; i++) { int k = i; d.Dire(Maj(Dit(k + 1)) + ".", () => { j[a + k].Pop(); compteur.Montrer(k + 1); }, 800); }
            d.Dire(Maj(Dit(a)) + " et " + Dit(b) + ", ça fait dix.", () => { for (int i = 0; i < 10; i++) j[i].Sauter(10, 360, i * 50); }, 1900);
            d.Dire("La boîte est pleine.", () => cadre.Briller(), 1500);
            return d;
        }

        /// <summary>Le miroir : la même chose des deux côtés, c'est un double.</summary>
        private static Demo DoublesMiroir()
        {
            var d = new Demo("piece");
            var o = ObjetCourant(); int a = NombreCourant(1, 5, () => Visuels.Entre(1, 5));
            var g = new Acteur[a]; var dr = new Acteur[a];
            for (int i = 0; i < a; i++) { g[i] = Obj(d, o.Nom, o.Emoji, 90, 92).En(545 - i * 100, Sol - 70).Opacite(0); dr[i] = Obj(d, o.Nom, o.Emoji, 90, 92).En(735 + i * 100, Sol - 70).EchelleXY(-1, 1).Opacite(0); }
            var mg = new Grid { Width = 46, Height = 390 };
            mg.Children.Add(new Rectangle { RadiusX = 14, RadiusY = 14, Fill = new LinearGradientBrush(Color.FromRgb(0xD8, 0xF1, 0xFF), Color.FromRgb(0xA9, 0xD9, 0xF7), 0), Stroke = new SolidColorBrush(Gris), StrokeThickness = 4 });
            var miroir = d.Chose(mg, 46, 390).En(640, Sol - 10);
            var c = Chiffre(d, 2 * a, 170).En(1100, 240).Opacite(0);
            d.Dire(Maj(o.N(a)) + ", devant un miroir.", () => { for (int i = 0; i < a; i++) g[i].Pop(i * 130); }, 1900);
            d.Toi("À toi ! Regarde le miroir pour voir de l'autre côté.", miroir, () => { for (int i = 0; i < a; i++) dr[i].Pop(i * 130); }, 900 + a * 130);
            d.Dire(Maj(Dit(a)) + " d'un côté, " + Dit(a) + " de l'autre : c'est pareil.", () => { for (int i = 0; i < a; i++) { g[i].Sauter(14, 380, i * 80); dr[i].Sauter(14, 380, i * 80); } }, 2300);
            d.Dire(Maj(Dit(a)) + " et " + Dit(a) + ", ça fait " + Dit(2 * a) + ".", () => c.Pop(), 2000);
            d.Dire("C'est un double.", () => c.Briller(), 1500);
            return d;
        }

        /// <summary>Le domino double : le même dé des deux côtés.</summary>
        private static Demo DoublesDomino()
        {
            var d = new Demo("table");
            int a = NombreCourant(1, 5, () => Visuels.Entre(1, 5));
            var cadre = d.Rect(570, 300, Color.FromRgb(0xFF, 0xF6, 0xE0), 34).En(640, Sol - 40).Opacite(0);
            var milieu = d.Trait(640, Sol - 320, 640, Sol - 60, Demo.Encre, 6);
            var g = d.Chose(Visuels.De(a, 220), 220, 220).En(495, Sol - 80).Opacite(0);
            var dr = d.Chose(Visuels.De(a, 220), 220, 220).En(785, Sol - 80).Opacite(0);
            var c = Chiffre(d, 2 * a, 150).En(640, 200).Opacite(0);
            d.Dire("Voici un domino.", () => { cadre.Pop(); Demo.Voir(milieu, 400, 300); }, 1300);
            d.Dire("D'un côté, " + Dit(a) + ".", () => g.Pop(), 1400);
            d.Dire("De l'autre côté, " + Dit(a) + " aussi.", () => dr.Pop(), 1600);
            d.Dire("C'est un double : " + Dit(a) + " et " + Dit(a) + ", ça fait " + Dit(2 * a) + ".", () => c.Pop(600), 2600);
            d.Toi("À toi ! Regarde le domino.", cadre, () => { g.Sauter(18, 420); dr.Sauter(18, 420, 200); }, 1100);
            d.Dire("Oui ! Le double de " + Dit(a) + ", c'est " + Dit(2 * a) + ".", () => c.Briller(), 2000);
            return d;
        }

        // ==================================================================
        //  LA SUITE DES NOMBRES
        // ==================================================================
        /// <summary>Sur la bande, le nombre d'après (ou d'avant) est la case d'à côté.</summary>
        private static Demo VoisinBande(int max, int sens)
        {
            var d = new Demo();
            int de = max <= 6 ? 1 : max <= 12 ? Visuels.Entre(1, 5) : Visuels.Entre(1, max - 8), fin = max <= 6 ? 6 : de + 7;
            int n = sens > 0 ? Visuels.Entre(de, fin - 2) : Visuels.Entre(de + 2, fin);
            // Le nombre de l'exercice, s'il y en a un : la bande se cale autour de lui.
            if (Courant?.N is int sujet && sujet >= (sens > 0 ? 1 : 3) && sujet <= 98)
            {
                n = sujet;
                if (max <= 6 && n + (sens > 0 ? 2 : 0) <= 7) { de = 1; fin = Math.Max(6, n + (sens > 0 ? 2 : 0)); }
                else { de = Math.Max(1, sens > 0 ? n - 2 : n - 5); fin = de + 7; }
            }
            var X = PoserBande(d, de, fin, fin - de + 1 <= 6 ? 130 : 108);
            double y = Sol - 12;
            var gre = d.Image("grenouille", "🐸", 115).En(X(n), y).Opacite(0);
            var fleche = d.Image("", sens > 0 ? "➡️" : "⬅️", 110).En(640, 260).Opacite(0);
            string mot = sens > 0 ? "Après " : "Avant ", verbe = sens > 0 ? "avance" : "recule";
            d.Dire("La grenouille est sur le " + Dit(n) + ".", () => gre.Pop(), 1500);
            d.Dire("Elle " + verbe + " d'une case.", () => gre.Bond(X(n + sens), y, 70, 600), 1300);
            d.Dire(mot + Dit(n) + ", il y a " + Dit(n + sens) + ".", () => gre.Sauter(14, 360), 1800);
            d.Toi("À toi ! Regarde la grenouille pour " + (sens > 0 ? "avancer" : "reculer") + " encore.", gre, () => gre.Bond(X(n + 2 * sens), y, 70, 600), 900);
            d.Dire(mot + Dit(n + sens) + ", il y a " + Dit(n + 2 * sens) + ".", () => gre.Sauter(14, 360), 1800);
            d.Dire("Le nombre " + (sens > 0 ? "d'après" : "d'avant") + " est juste à côté, de ce côté-là.", () => fleche.Pop(), 2200);
            return d;
        }

        /// <summary>Avec une tour : le nombre d'après a un cube de plus, celui d'avant un cube de moins.</summary>
        private static Demo VoisinTour(int max, int sens)
        {
            var d = new Demo("piece");
            const double T = 54;
            int n = NombreCourant(sens > 0 ? 1 : 3, sens > 0 ? 6 : 8, () => sens > 0 ? Visuels.Entre(1, Math.Max(1, Math.Min(6, max - 2))) : Visuels.Entre(3, Math.Max(3, Math.Min(8, max))));
            int haut = sens > 0 ? n + 2 : n;
            var b = new Acteur[haut];
            for (int i = 0; i < haut; i++) b[i] = d.Bloc(T, (sens > 0 ? i >= n : i >= n - 2) ? Demo.Or : Bleu).En(500, Sol - i * T).Opacite(0);
            var compteur = new Compteur(d, Math.Min(n, n + 2 * sens), Math.Max(n, n + 2 * sens), 880, Sol - 90, 210);
            void Changer(int k)        // k = 1 puis 2 : le cube qu'on ajoute ou qu'on enlève
            {
                if (sens > 0) b[n + k - 1].Pop();
                else { var c = b[n - k]; c.Devant(); c.Bond(250, Sol, 130, 900); c.Fondu(0.3, 400, 700); }
                compteur.Montrer(n + k * sens);
            }
            string mot = sens > 0 ? "Après " : "Avant ", quoi = sens > 0 ? "un de plus" : "un de moins";
            d.Dire("Une tour de " + Dit(n) + ".", () => { for (int i = 0; i < n; i++) b[i].Pop(i * 110); compteur.Montrer(n); }, 1300 + n * 110);
            d.Dire(sens > 0 ? "J'ajoute un cube." : "J'enlève un cube.", () => Changer(1), 1400);
            d.Dire(mot + Dit(n) + ", il y a " + Dit(n + sens) + " : " + quoi + ".", null, 2200);
            d.Toi("À toi ! Regarde le chiffre pour " + (sens > 0 ? "ajouter" : "enlever") + " encore un cube.", compteur.De(n + sens), () => Changer(2), 1400);
            d.Dire(mot + Dit(n + sens) + ", il y a " + Dit(n + 2 * sens) + " : " + quoi + ".", null, 2200);
            return d;
        }

        /// <summary>Le nombre qui manque : on compte le long de la bande, et on le trouve.</summary>
        private static Demo BandeTrou(int max)
        {
            var d = new Demo();
            int de = max <= 6 ? Visuels.Entre(1, 2) : max <= 12 ? Visuels.Entre(1, 8) : Visuels.Entre(1, max - 4);
            int trou = de + Visuels.Entre(1, 3);
            if (Courant?.N is int sTrou && Courant?.N2 is int sDe && sDe >= 1 && sTrou > sDe && sTrou < sDe + 4) { de = sDe; trou = sTrou; }   // la bande de l'exercice
            const double Cell = 150, Pas = 158;
            var X = PoserBande(d, de, de + 4, Cell, trou);
            double y = Sol - 12, milieu = Sol - 8 + Pas / 2;
            var cache = d.Rect(Cell - 8, Cell - 8, Colors.White, 12, false).Au(X(trou), milieu).Opacite(0);
            var carte = Chiffre(d, trou, 118).En(X(trou), 170).Opacite(0);
            var cible = d.Rect(Cell, Cell, Demo.Or, 14, false).Au(X(trou), milieu).Opacite(0);
            var gre = d.Image("grenouille", "🐸", 110).En(X(de), y).Opacite(0);
            d.Dire("Il manque un nombre sur la bande.", () => gre.Pop(600), 1800);
            d.Dire("On compte pour le trouver. " + Maj(Dit(de)) + ".", () => gre.Sauter(14, 360, 900), 2000);
            for (int k = de + 1; k < trou; k++) { int kk = k; d.Dire(Maj(Dit(kk)) + ".", () => gre.Bond(X(kk), y, 60, 480), 800); }
            d.Dire("Et ici ?", () => { gre.Bond(X(trou), y, 60, 480); cible.Fondu(0.5, 500, 300); }, 1300);
            d.Toi("À toi ! Regarde le trou pour y mettre le nombre.", cible, () =>
            {
                cible.Fondu(0, 300); cache.Fondu(1, 300);
                carte.Pop(); carte.Aller(X(trou), milieu + 59, 900, 250, Acteur.Rebond);
            }, 1400);
            d.Dire(Maj(Dit(trou)) + " !", null, 900);
            for (int k = trou + 1; k <= de + 4; k++) { int kk = k; d.Dire(Maj(Dit(kk)) + ".", () => gre.Bond(X(kk), y, 60, 480), 800); }
            d.Dire("La bande est complète.", () => carte.Briller(), 1500);
            return d;
        }

        /// <summary>Le compte à rebours de la fusée.</summary>
        private static Demo Fusee()
        {
            var d = new Demo("ciel");
            const int S = 5;
            var fusee = d.Image("fusee", "🚀", 250).En(520, Sol - 6);
            var compteur = new Compteur(d, 0, S, 950, 400, 240);
            d.Dire("On compte à l'envers, comme pour le départ d'une fusée.", null, 2200);
            for (int k = S; k >= 1; k--) { int kk = k; d.Dire(Maj(Dit(kk)) + ".", () => { compteur.Montrer(kk); if (kk == 1) fusee.Secouer(300, 3, 3); }, 950); }
            d.Toi("À toi ! Regarde la fusée pour la faire décoller.", fusee, () => { compteur.Montrer(0); fusee.Aller(1400, -280, 2000, 250, Acteur.Entree); }, 2300);
            d.Dire("Zéro… Décollage !", null, 1200);
            d.Dire("Cinq, quatre, trois, deux, un : à chaque fois, un de moins.", null, 2600);
            return d;
        }

        /// <summary>À rebours sur la bande : on recule d'une case à chaque nombre.</summary>
        private static Demo BandeRecule()
        {
            var d = new Demo();
            var X = PoserBande(d, 1, 8, 108);
            int s = Visuels.Entre(5, 8); double y = Sol - 12;
            var gre = d.Image("grenouille", "🐸", 115).En(X(s), y).Opacite(0);
            d.Dire("On compte à rebours : on recule sur la bande.", () => gre.Pop(600), 2100);
            d.Dire(Maj(Dit(s)) + ".", () => gre.Sauter(14, 360), 800);
            for (int k = s - 1; k >= s - 2; k--) { int kk = k; d.Dire(Maj(Dit(kk)) + ".", () => gre.Bond(X(kk), y, 60, 480), 850); }
            d.Toi("À toi ! Regarde la grenouille pour reculer encore.", gre, () => gre.Bond(X(s - 3), y, 60, 480), 700);
            d.Dire(Maj(Dit(s - 3)) + ".", null, 700);
            d.Dire(Maj(Dit(s - 4)) + ".", () => gre.Bond(X(s - 4), y, 60, 480), 850);
            d.Dire("À rebours, c'est le nombre d'avant, à chaque fois.", null, 2000);
            return d;
        }

        /// <summary>De deux en deux : la grenouille saute par-dessus un nombre.</summary>
        private static Demo BandeSaut()
        {
            var d = new Demo();
            const double Cell = 96, Pas = 104;
            var X = PoserBande(d, 1, 10, Cell);
            double y = Sol - 12, milieu = Sol - 8 + Pas / 2;
            var voile = new Acteur[11];
            for (int k = 1; k <= 9; k += 2) voile[k] = d.Rect(Cell, Cell, Color.FromRgb(0x7A, 0x84, 0x96), 14, false).Au(X(k), milieu).Opacite(0);
            var gre = d.Image("grenouille", "🐸", 105).En(X(2), y).Opacite(0);
            void Saut(int k) { gre.Bond(X(k), y, 110, 700); voile[k - 1].Fondu(0.6, 500, 300); }
            d.Dire("On compte de deux en deux : on saute par-dessus un nombre.", () => { gre.Pop(600); voile[1].Fondu(0.6, 500, 900); }, 2600);
            d.Dire("Deux.", () => gre.Sauter(14, 360), 800);
            d.Dire("Quatre.", () => Saut(4), 1000);
            d.Dire("Six.", () => Saut(6), 1000);
            d.Toi("À toi ! Regarde la grenouille pour sauter encore.", gre, () => Saut(8), 900);
            d.Dire("Huit.", null, 700);
            d.Dire("Dix.", () => Saut(10), 1000);
            d.Dire("Deux, quatre, six, huit, dix !", () => gre.Sauter(18, 400, 0, 2), 2200);
            return d;
        }

        /// <summary>Les chaussettes vont par deux : deux, quatre, six, huit.</summary>
        private static Demo PairesChaussettes()
        {
            var d = new Demo("piece");
            double[] xs = { 250, 510, 770, 1030 };
            var g = new Acteur[4]; var dr = new Acteur[4]; var p = new Acteur[4];
            for (int k = 0; k < 4; k++)
            {
                g[k] = d.Image("chaussette", "🧦", 140).En(xs[k] - 46, Sol - 60).Opacite(0);
                dr[k] = d.Image("chaussette", "🧦", 140).En(xs[k] + 46, Sol - 60).EchelleXY(-1, 1).Opacite(0);
                p[k] = Pastille(d, 2 * (k + 1), null, 70).En(xs[k], Sol - 230).Opacite(0);
            }
            void Paire(int k) { g[k].Pop(); dr[k].Pop(180); p[k].Pop(500); }
            d.Dire("Les chaussettes vont par deux.", () => { g[0].Pop(); dr[0].Pop(180); }, 1800);
            d.Dire("Deux.", () => p[0].Pop(), 900);
            d.Dire("Quatre.", () => Paire(1), 1200);
            d.Dire("Six.", () => Paire(2), 1200);
            d.Toi("À toi ! Regarde les chaussettes pour faire venir la dernière paire.", g[2], () => Paire(3), 1100);
            d.Dire("Huit.", null, 800);
            d.Dire("Deux, quatre, six, huit : de deux en deux.", () => { for (int k = 0; k < 4; k++) p[k].Sauter(16, 380, k * 300); }, 2500);
            return d;
        }

        // ==================================================================
        //  LE RANG ET LA BANDE
        // ==================================================================
        private static readonly (string Nom, string Emoji, string Dit, bool Fem)[] Coureurs =
        {
            ("lapin", "🐰", "le lapin", false), ("chat", "🐱", "le chat", false), ("poussin", "🐥", "le poussin", false),
            ("mouton", "🐑", "le mouton", false), ("grenouille", "🐸", "la grenouille", true), ("canard", "🦆", "le canard", false),
        };

        /// <summary>La file : le premier est juste après la flèche, le dernier tout au bout.</summary>
        private static Demo FileRang(int n)
        {
            var d = new Demo();
            n = Math.Max(4, Math.Min(5, n));
            var qui = Visuels.Melange(Coureurs).Take(n).ToList();
            var noms = n == 4 ? new[] { "Le premier.", "Le deuxième.", "Le troisième.", "Le dernier." } : new[] { "Le premier.", "Le deuxième.", "Le troisième.", "L'avant-dernier.", "Le dernier." };
            var fleche = d.Image("", "➡️", 100).En(150, Sol - 50);
            var a = new Acteur[n]; var p = new Acteur[n];
            for (int i = 0; i < n; i++)
            {
                a[i] = Obj(d, qui[i].Nom, qui[i].Emoji, 130, 150).En(330 + i * 175, Sol - 30).Opacite(0);
                p[i] = Pastille(d, i + 1).En(330 + i * 175, Sol - 30 - a[i].H - 14).Opacite(0);
            }
            d.Dire("Ils font la queue. On part de la flèche.", () => { for (int i = 0; i < n; i++) a[i].Pop(i * 150); fleche.Sauter(14, 380, n * 150); }, 2300);
            for (int i = 0; i < n; i++) { int k = i; d.Dire(noms[k], () => { a[k].Sauter(22, 420); p[k].Pop(); }, 1100); }
            d.Toi("À toi ! Regarde le premier.", a[0], () => a[0].Sauter(26, 460, 0, 2), 1100);
            d.Dire("Oui ! Le premier est juste après la flèche.", () => a[0].Briller(), 1900);
            d.Dire("Le dernier est tout au bout.", () => a[n - 1].Briller(), 1700);
            return d;
        }

        /// <summary>La course : premier, deuxième, troisième, dernier, c'est l'ordre d'arrivée.</summary>
        private static Demo Course()
        {
            var d = new Demo();
            var qui = new[] { ("lapin", "🐰", "Le lapin arrive le premier."), ("chat", "🐱", "Le chat, le deuxième."), ("poussin", "🐥", "Le poussin, le troisième."), ("tortue", "🐢", "La tortue arrive la dernière.") };
            int[] couloir = { 2, 0, 3, 1 }; int[] duree = { 1800, 2700, 3500, 4700 };
            double Y(int c) => 215 + c * 115;
            var arrivee = d.Trait(1010, 110, 1010, Sol, Demo.Encre, 7, true); arrivee.Opacity = 1;
            var drapeau = d.Image("", "🏁", 110).En(1010, 112);
            var a = new Acteur[4]; var p = new Acteur[4];
            for (int i = 0; i < 4; i++)
            {
                a[i] = Obj(d, qui[i].Item1, qui[i].Item2, 92, 120).En(150, Y(couloir[i])).Opacite(0);
                p[i] = Pastille(d, i + 1, null, 62).En(1140, Y(couloir[i]) - 14).Opacite(0);
            }
            d.Dire("Une course ! Qui va arriver le premier ?", () => { for (int i = 0; i < 4; i++) a[i].Pop(couloir[i] * 150); }, 2200);
            d.Toi("À toi ! Regarde le drapeau pour donner le départ.", drapeau, () =>
            {
                for (int i = 0; i < 4; i++) { a[i].Aller(990 - a[i].W / 2, Y(couloir[i]), duree[i], 0, Acteur.Lineaire); p[i].Pop(duree[i]); }
            }, 5100);
            for (int i = 0; i < 4; i++) { int k = i; d.Dire(qui[k].Item3, () => { a[k].Briller(0, 1200); p[k].Sauter(14, 380); }, 1500); }
            return d;
        }

        /// <summary>Un jeton par point du dé, case après case : le dé va dans la case du dernier jeton.</summary>
        private static Demo DeSurBande()
        {
            var d = new Demo();
            int n = NombreCourant(1, 6, () => Visuels.Entre(2, 6));
            const double Pas = 138;
            var X = PoserBande(d, 1, 6, 130);
            var de = d.Chose(Visuels.De(n, 180), 180, 180).En(640, 300).Opacite(0);
            var j = new Acteur[n];
            for (int i = 0; i < n; i++) j[i] = d.Rond(64, Demo.Or).En(640, 240).Opacite(0);
            d.Dire("Le dé montre " + Dit(n) + ".", () => de.Pop(), 1500);
            d.Dire("On pose un jeton par point, case après case.", null, 1900);
            for (int i = 0; i < n; i++) { int k = i; d.Dire(Maj(Dit(k + 1)) + ".", () => { j[k].Devant(); j[k].Pop(); j[k].Bond(X(k + 1), Sol - 14, 60, 700, 150); }, 1000); }
            d.Dire("Le dernier jeton est sur le " + Dit(n) + ".", () => j[n - 1].Briller(), 1800);
            d.Toi("À toi ! Regarde le dé pour le ranger dans sa case.", de, () =>
            {
                j[n - 1].Fondu(0, 400);
                de.Devant(); de.Grandir(0.68, 900); de.Aller(X(n), Sol - 8 + Pas - 8, 1100);
            }, 1400);
            d.Dire("Le dé qui montre " + Dit(n) + " va dans la case " + Dit(n) + ".", null, 2300);
            return d;
        }

        /// <summary>Avancer ou reculer sur la piste : on compte les sauts, pas les cases de départ.</summary>
        private static Demo Piste(bool avance)
        {
            var d = new Demo();
            var X = PoserBande(d, 1, 10, 96);
            int s = avance ? 1 : -1, k = avance ? Visuels.Entre(1, 7) : Visuels.Entre(4, 10), sauts = 2;
            // La case et le nombre de sauts de l'exercice, s'il y en a un.
            if (Courant?.N is int sk && Courant?.N2 is int sd && (sd == 1 || sd == 2) && sk + s * sd >= 1 && sk + s * sd <= 10 && sk >= 1 && sk <= 10) { k = sk; sauts = sd; }
            double y = Sol - 12;
            var gre = d.Image("grenouille", "🐸", 105).En(X(k), y).Opacite(0);
            var p1 = Pastille(d, 1).En(X(k + s), Sol - 190).Opacite(0);
            var p2 = Pastille(d, 2).En(X(Math.Max(1, Math.Min(10, k + 2 * s))), Sol - 190).Opacite(0);
            var grand = Chiffre(d, k + sauts * s, 170).En(640, 230).Opacite(0);
            d.Dire("La grenouille est sur le " + Dit(k) + ".", () => gre.Pop(), 1500);
            d.Dire("Elle " + (avance ? "avance" : "recule") + (sauts == 1 ? " d'une case." : " de deux cases."), null, 1500);
            if (sauts == 1)
            {
                d.Toi("À toi ! Regarde la grenouille pour la faire sauter.", gre, () => { gre.Bond(X(k + s), y, 70, 600); p1.Pop(500); }, 1000);
                d.Dire("Un saut.", null, 900);
            }
            else
            {
                d.Dire("Un saut.", () => { gre.Bond(X(k + s), y, 70, 600); p1.Pop(500); }, 1200);
                d.Toi("À toi ! Regarde la grenouille pour faire le deuxième saut.", gre, () => { gre.Bond(X(k + 2 * s), y, 70, 600); p2.Pop(500); }, 1000);
                d.Dire("Deux sauts.", null, 900);
            }
            d.Dire("Elle arrive sur le " + Dit(k + sauts * s) + ".", () => grand.Pop(), 1800);
            return d;
        }

        // ==================================================================
        //  LES PETITS PROBLÈMES
        // ==================================================================
        /// <summary>J'en ajoute : on recompte tout pour savoir combien il y en a.</summary>
        private static Demo AjoutCompte(int max)
        {
            var d = new Demo();
            var o = ObjetCourant(); int cap = Math.Max(2, Math.Min(max, 8));
            int a = Visuels.Entre(1, cap - 1), b = Visuels.Entre(1, Math.Min(3, cap - a)), tot = a + b;
            if (Courant?.N is int sa && Courant?.N2 is int sb && sa >= 1 && sb >= 1 && sa + sb <= 10) { a = sa; b = sb; tot = a + b; }
            double pas = Math.Min(140, 960.0 / tot), t = Math.Min(115, pas - 22);
            var xs = Rang(tot, pas);
            var obj = new Acteur[tot]; var p = new Acteur[tot];
            for (int i = 0; i < tot; i++)
            {
                obj[i] = Obj(d, o.Nom, o.Emoji, t, pas - 14).En(i < a ? xs[i] : 1420, Sol - 40).Opacite(i < a ? 0 : 1);
                p[i] = Pastille(d, i + 1).En(xs[i], Sol - 40 - obj[i].H - 14).Opacite(0);
            }
            var total = Chiffre(d, tot, 150).En(640, 190).Opacite(0);
            d.Dire(Maj(o.N(a)) + ".", () => { for (int i = 0; i < a; i++) obj[i].Pop(i * 110); }, 1500);
            d.Dire("J'en ajoute " + (b == 1 ? Un(o) : Dit(b)) + ".", () => { for (int i = a; i < tot; i++) obj[i].Bond(xs[i], Sol - 40, 80, 900, (i - a) * 350); }, 1300 + b * 350);
            d.Dire("Combien en tout ? On compte.", null, 1500);
            Compter(d, obj, p, 0, tot - 1);
            d.Toi("À toi ! Regarde " + (o.Fem ? "la dernière" : "le dernier") + " pour finir de compter.", obj[tot - 1], () => { obj[tot - 1].Sauter(22, 400); p[tot - 1].Pop(); }, 700);
            d.Dire(Maj(Dit(tot)) + " !", () => total.Pop(), 1100);
            d.Dire(Maj(Dit(a)) + " et " + Dit(b) + ", ça fait " + Dit(tot) + ".", null, 2000);
            return d;
        }

        /// <summary>J'en ajoute, avec une tour : elle monte.</summary>
        private static Demo AjoutTours(int max)
        {
            var d = new Demo("piece");
            const double T = 54;
            int cap = Math.Max(2, Math.Min(max, 8)), a = Visuels.Entre(1, cap - 1), b = Visuels.Entre(1, Math.Min(3, cap - a)), tot = a + b;
            if (Courant?.N is int sa && Courant?.N2 is int sb && sa >= 1 && sb >= 1 && sa + sb <= 10) { a = sa; b = sb; tot = a + b; }
            var bleu = Tour(d, 480, a, T, Bleu);
            var or = new Acteur[b];
            for (int i = 0; i < b; i++) or[i] = d.Bloc(T, Orange).En(760 + i * 80, Sol).Opacite(0);
            var compteur = new Compteur(d, a, tot, 1010, 300, 190);
            d.Dire("Une tour de " + Dit(a) + ".", () => { for (int i = 0; i < a; i++) bleu[i].Pop(i * 110); compteur.Montrer(a); }, 1300 + a * 110);
            d.Dire("Et " + (b == 1 ? "un cube" : Dit(b) + " cubes") + " à ajouter.", () => { for (int i = 0; i < b; i++) or[i].Pop(i * 150); }, 1600);
            d.Toi("À toi ! Regarde le chiffre pour poser les cubes sur la tour.", compteur.De(a), () =>
            {
                for (int i = 0; i < b; i++) { or[i].Devant(); or[i].Bond(480, Sol - (a + i) * T, 110, 800, i * 500); }
                compteur.Montrer(tot);
            }, 1000 + b * 500);
            d.Dire(Maj(Dit(a)) + ", et encore " + Dit(b) + " : ça fait " + Dit(tot) + ".", null, 2400);
            d.Dire("Quand on ajoute, la tour monte.", () => { for (int i = 0; i < b; i++) or[i].Briller(); }, 1900);
            return d;
        }

        /// <summary>J'en enlève : des oiseaux s'envolent du fil, on compte ceux qui restent.</summary>
        private static Demo RetraitOiseaux(int max)
        {
            var d = new Demo("ciel");
            int cap = Math.Max(2, Math.Min(max, 7)), a = Visuels.Entre(2, cap), b = Visuels.Entre(1, Math.Min(3, a - 1)), reste = a - b;
            if (Courant?.N is int sa && Courant?.N2 is int sb && sa >= 2 && sa <= 10 && sb >= 1 && sb < sa) { a = sa; b = sb; reste = a - b; }
            var fil = d.Trait(90, 352, 1190, 352, Demo.Encre, 5); fil.Opacity = 1;
            var xs = Rang(a, Math.Min(160, 980.0 / a));
            var o = new Acteur[a]; var p = new Acteur[a];
            for (int i = 0; i < a; i++)
            {
                o[i] = Obj(d, "oiseau-" + (i % 6 + 1), "🐦", 105, Math.Min(130, 980.0 / a - 8)).En(xs[i], 358).Opacite(0);
                p[i] = Pastille(d, i + 1).En(xs[i], 358 - o[i].H - 12).Opacite(0);
            }
            var total = Chiffre(d, reste, 150).En(640, Sol - 10).Opacite(0);
            d.Dire(Maj(Dit(a)) + " oiseaux sur le fil.", () => { for (int i = 0; i < a; i++) o[i].Pop(i * 120); }, 1900);
            d.Toi("À toi ! Regarde cet oiseau pour le faire s'envoler.", o[a - 1], () =>
            {
                for (int i = 0; i < b; i++) { var z = o[a - 1 - i]; z.Aller(1450, 10, 1500, i * 300, Acteur.Entree); z.Fondu(0, 400, 1100 + i * 300); }
            }, 1700 + b * 300);
            d.Dire(b == 1 ? "Un oiseau s'envole." : Maj(Dit(b)) + " oiseaux s'envolent.", null, 1300);
            d.Dire("Combien en reste-t-il ? On compte.", null, 1500);
            Compter(d, o, p, 0, reste);
            d.Dire("Il en reste " + Dit(reste) + ".", () => total.Pop(), 1700);
            return d;
        }

        /// <summary>J'en enlève, avec une tour : elle descend.</summary>
        private static Demo RetraitTours(int max)
        {
            var d = new Demo("piece");
            const double T = 54;
            int cap = Math.Max(2, Math.Min(max, 8)), a = Visuels.Entre(2, cap), b = Visuels.Entre(1, Math.Min(3, a - 1)), reste = a - b;
            if (Courant?.N is int sa && Courant?.N2 is int sb && sa >= 2 && sa <= 10 && sb >= 1 && sb < sa) { a = sa; b = sb; reste = a - b; }
            var t = new Acteur[a];
            for (int i = 0; i < a; i++) t[i] = d.Bloc(T, i >= reste ? Orange : Bleu).En(480, Sol - i * T).Opacite(0);
            var compteur = new Compteur(d, reste, a, 1010, 300, 190);
            d.Dire("Une tour de " + Dit(a) + ".", () => { for (int i = 0; i < a; i++) t[i].Pop(i * 110); compteur.Montrer(a); }, 1300 + a * 110);
            d.Toi("À toi ! Regarde le chiffre pour enlever " + (b == 1 ? "un cube." : Dit(b) + " cubes."), compteur.De(a), () =>
            {
                for (int i = 0; i < b; i++) { var c = t[a - 1 - i]; c.Devant(); c.Bond(780 + i * 80, Sol, 110, 800, i * 500); c.Fondu(0.4, 300, 700 + i * 500); }
                compteur.Montrer(reste);
            }, 1000 + b * 500);
            d.Dire(Maj(Dit(a)) + ", j'en enlève " + Dit(b) + " : il en reste " + Dit(reste) + ".", null, 2600);
            d.Dire("Quand on enlève, la tour descend.", null, 1700);
            return d;
        }

        /// <summary>Partager en deux : une pour toi, une pour toi, jusqu'au bout.</summary>
        private static Demo PartageLapins(int moitie)
        {
            var d = new Demo();
            int m = NombreCourant(1, 5, () => Visuels.Entre(1, Math.Max(1, Math.Min(4, moitie))));
            var lg = d.Image("lapin", "🐰", 200).En(230, Sol).Opacite(0);
            var ld = d.Image("lapin", "🐰", 200).En(1050, Sol).Opacite(0);
            double[] jx = { -70, 40, -20, 80, -100, 10, 60, -50, 110, -120 }, jy = { 0, -40, 50, 20, 40, -70, -30, 70, 60, -10 };
            var c = new Acteur[2 * m];
            for (int i = 0; i < 2 * m; i++) c[i] = d.Image("carotte", "🥕", 96).En(640 + jx[i], 300 + jy[i]).Opacite(0);
            (double x, double y) Tas(bool gauche, int j) => ((gauche ? 400 : 880) + (gauche ? 1 : -1) * (j % 2) * 66, Sol - 8 - (j / 2) * 104);
            void Tour(int j) { var g = Tas(true, j); var dr = Tas(false, j); c[2 * j].Devant(); c[2 * j].Bond(g.x, g.y, 90, 800); c[2 * j + 1].Devant(); c[2 * j + 1].Bond(dr.x, dr.y, 90, 800, 700); }
            d.Dire(Maj(Dit(2 * m)) + " carottes pour deux lapins.", () => { lg.Pop(); ld.Pop(200); for (int i = 0; i < 2 * m; i++) c[i].Pop(500 + i * 100); }, 2300);
            d.Dire("On partage : pareil pour chacun.", null, 1600);
            for (int j = 0; j < m - 1; j++) { int jj = j; d.Dire("Une pour toi, une pour toi.", () => Tour(jj), 1900); }
            d.Toi("À toi ! Regarde les carottes pour " + (m > 1 ? "finir le partage." : "les partager."), c[2 * m - 1], () => Tour(m - 1), 1700);
            d.Dire("Une pour toi, une pour toi.", null, 1300);
            d.Dire("Chacun en a " + Dit(m) + " : c'est pareil, c'est juste.", () => { lg.Sauter(20, 420, 0, 2); ld.Sauter(20, 420, 0, 2); }, 2400);
            return d;
        }

        /// <summary>Partager une tour en deux tours pareilles.</summary>
        private static Demo PartageTours(int moitie)
        {
            var d = new Demo("piece");
            const double T = 54;
            int m = NombreCourant(1, 5, () => Visuels.Entre(1, Math.Max(1, Math.Min(4, moitie)))), n = 2 * m;
            var t = Tour(d, 640, n, T, Violet);
            var total = Chiffre(d, n, 92).Au(640, Sol + 62).Opacite(0);
            var cg = Chiffre(d, m, 92).Au(360, Sol + 62).Opacite(0);
            var cd = Chiffre(d, m, 92).Au(920, Sol + 62).Opacite(0);
            var niveau = d.Trait(270, Sol - m * T, 1010, Sol - m * T, Demo.Or, 7, true);
            d.Dire("Une tour de " + Dit(n) + ".", () => { for (int i = 0; i < n; i++) t[i].Pop(i * 100); total.Pop(n * 100); }, 1400 + n * 100);
            d.Dire("On la partage en deux tours pareilles.", null, 1700);
            d.Toi("À toi ! Regarde le chiffre pour partager.", total, () =>
            {
                for (int j = 0; j < n; j++) { var b = t[n - 1 - j]; b.Devant(); b.Bond(j % 2 == 0 ? 360 : 920, Sol - (j / 2) * T, 120, 750, j * 480); }
            }, 900 + n * 480);
            d.Dire("Un ici, un là, jusqu'au bout.", null, 1500);
            d.Dire("Les deux tours sont pareilles.", () => Demo.Voir(niveau, 600), 1700);
            d.Dire(Maj(Dit(n)) + ", partagé en deux : " + Dit(m) + " et " + Dit(m) + ".", () => { cg.Pop(); cd.Pop(300); }, 2500);
            return d;
        }

        /// <summary>Combien en plus ? Face à face, on compte ceux qui n'ont personne en face.</summary>
        private static Demo EcartLignes()
        {
            var d = new Demo("table");
            var o = ObjetCourant(); var o2 = Courant?.O2 ?? Visuels.AutreObjet(o);
            int a = Visuels.Entre(3, 7), b = Visuels.Entre(Math.Max(1, a - 4), a - 1), plus = a - b;
            if (Courant?.N is int sa && Courant?.N2 is int sb && sa >= 2 && sa <= 10 && sb >= 1 && sb < sa) { a = sa; b = sb; plus = a - b; }
            double pasE = Math.Min(130, 880.0 / Math.Max(1, a - 1)), tE = Math.Min(100, pasE - 12);
            double X(int i) => 220 + i * pasE;
            const double YH = 300, YB = 510;
            var h = new Acteur[a]; var bas = new Acteur[b]; var p = new Acteur[plus]; var extra = new Acteur[plus];
            for (int i = 0; i < a; i++) h[i] = Obj(d, o.Nom, o.Emoji, tE, pasE - 8).En(X(i), YH).Opacite(0);
            for (int i = 0; i < b; i++) bas[i] = Obj(d, o2.Nom, o2.Emoji, tE, pasE - 8).En(X(i), YB).Opacite(0);
            var liens = new Line[b];
            for (int i = 0; i < b; i++) liens[i] = d.Trait(X(i), YH + 8, X(i), YB - 110, Demo.Encre, 6, true);
            for (int i = 0; i < plus; i++) { extra[i] = h[b + i]; p[i] = Pastille(d, i + 1).En(X(b + i), YH - h[b + i].H - 12).Opacite(0); }
            d.Dire(Maj(o.N(a)) + ", et " + o2.N(b) + ".", () => { for (int i = 0; i < a; i++) h[i].Pop(i * 100); for (int i = 0; i < b; i++) bas[i].Pop(700 + i * 100); }, 2500);
            d.Dire("Combien de " + o.Plur + " en plus ? On les met face à face.", () => { for (int i = 0; i < b; i++) Demo.Voir(liens[i], 300, i * 450); }, 1700 + b * 450);
            d.Dire((o.Fem ? "Celles-là" : "Ceux-là") + " n'ont personne en face : on les compte.", () => { for (int i = 0; i < plus; i++) extra[i].Briller(); }, 2300);
            Compter(d, extra, p, 0, plus - 1);
            d.Toi("À toi ! Regarde " + (o.Fem ? "la dernière" : "le dernier") + " pour finir de compter.", extra[plus - 1], () => { extra[plus - 1].Sauter(22, 400); p[plus - 1].Pop(); }, 700);
            d.Dire(Maj(Dit(plus)) + " !", null, 800);
            d.Dire(Maj(Dit(plus)) + " de plus.", () => { for (int i = 0; i < plus; i++) extra[i].Sauter(14, 380, i * 80); }, 1600);
            return d;
        }

        /// <summary>Combien en manque-t-il ? Une pomme par assiette : on compte les assiettes vides.</summary>
        private static Demo ManqueAssiettes()
        {
            var d = new Demo("piece");
            int total = Visuels.Entre(3, 6), a = Visuels.Entre(Math.Max(1, total - 4), total - 1), m = total - a;
            if (Courant?.N is int st && Courant?.N2 is int sa && st >= 2 && st <= 10 && sa >= 1 && sa < st) { total = st; a = sa; m = total - a; }
            double pasA = Math.Min(180, 1060.0 / total), tA = Math.Min(140, pasA - 14);
            var xs = Rang(total, pasA);
            var ass = new Acteur[total]; var pom = new Acteur[total]; var vides = new Acteur[m]; var p = new Acteur[m];
            for (int i = 0; i < total; i++) ass[i] = d.Image("assiette", "🍽️", tA).En(xs[i], Sol - 40).Opacite(0);
            for (int i = 0; i < total; i++) pom[i] = d.Image("pomme", "🍎", tA * 0.6).En(i < a ? xs[i] : 1420, Sol - 40 - tA * 0.2).Opacite(i < a ? 0 : 1);
            for (int i = 0; i < m; i++) { vides[i] = ass[a + i]; p[i] = Pastille(d, i + 1).En(xs[a + i], Sol - 200).Opacite(0); }
            d.Dire(Maj(Dit(total)) + " assiettes. Il faut une pomme par assiette.", () => { for (int i = 0; i < total; i++) ass[i].Pop(i * 110); }, 2500);
            d.Dire("Il y a " + (a == 1 ? "une pomme." : Dit(a) + " pommes."), () => { for (int i = 0; i < a; i++) pom[i].Pop(i * 150); }, 1700);
            d.Dire("Ces assiettes sont vides : on les compte.", () => { for (int i = 0; i < m; i++) vides[i].Briller(); }, 2100);
            Compter(d, vides, p, 0, m);
            d.Dire("Il en manque " + Dit(m) + ".", null, 1500);
            d.Toi("À toi ! Regarde une assiette vide pour apporter les pommes.", ass[total - 1], () =>
            {
                for (int i = a; i < total; i++) { pom[i].Devant(); pom[i].Bond(xs[i], Sol - 40 - tA * 0.2, 90, 900, (i - a) * 400); }
            }, 1100 + m * 400);
            d.Dire("Voilà : une pomme par assiette.", () => { for (int i = 0; i < total; i++) pom[i].Sauter(12, 360, i * 70); }, 1900);
            return d;
        }
    }
}
