using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Learning
{
    /// <summary>
    /// Troisième série d'explications animées : LES FORMES, LES SOLIDES, LES MOTIFS,
    /// LES PUZZLES et CLASSER. Avec elle, chaque leçon du chemin a son explication.
    /// </summary>
    public static partial class Explications
    {
        // ------------------------------------------------------------------
        //  Les familles
        // ------------------------------------------------------------------
        public static Famille Formes(int n) => new Famille("formes", ("presente", () => FormePresentee(n)), ("invariante", FormeInvariante), ("coins", CoinsComptes));
        public static readonly Famille MemeForme = new Famille("meme-forme", ("invariante", FormeInvariante), ("presente", () => FormePresentee(3)));
        public static Famille TrierFormes(int n) => new Famille("trier-formes", ("boites", () => TrierParForme(n)), ("deux-facons", Regrouper));
        public static Famille FormeObjet(int n) => new Famille("forme-objet", ("objets", FormeDansObjet), ("presente", () => FormePresentee(n)));
        public static readonly Famille FormesVariees = new Famille("formes-variees", ("invariante", FormeInvariante), ("coins", CoinsComptes));
        public static readonly Famille FormeOuiNon = new Famille("forme-oui-non", ("coins", CoinsComptes), ("presente", () => FormePresentee(4)));
        public static Famille Solides(int n) => new Famille("solides", ("presente", () => SolidePresente(n)), ("plat", PlatOuSolide));
        public static Famille SolideObjet(int n) => new Famille("solide-objet", ("objets", SolideDansObjet), ("presente", () => SolidePresente(n)));
        public static readonly Famille SolidesPlats = new Famille("solides-plats", ("plat", PlatOuSolide), ("presente", () => SolidePresente(4)));
        public static readonly Famille SolideOuiNon = new Famille("solide-oui-non", ("presente", () => SolidePresente(6)), ("objets", SolideDansObjet));
        public static readonly Famille Empreinte = new Famille("empreinte", ("sable", EmpreinteDemo), ("plat", PlatOuSolide));
        public static Famille Motif(string motif, string genre) => new Famille("motif-" + motif.ToLowerInvariant() + "-" + genre, ("regarde", () => MotifRepete(motif, genre)), ("ecoute", () => MotifEnMusique(motif)));
        public static Famille BonCollier(string motif) => new Famille("bon-collier", ("compare", () => ColliersCompares(motif)), ("regarde", () => MotifRepete(motif, "couleurs")));
        public static readonly Famille MotifGrandit = new Famille("motif-grandit", ("grandit", MotifQuiGrandit), ("regarde", () => MotifRepete("AB", "couleurs")));
        public static readonly Famille MemeStructure = new Famille("meme-structure", ("structure", MemeStructureDemo), ("ecoute", () => MotifEnMusique("AB")));
        public static readonly Famille SuiteDeNombres = new Famille("suite-de-nombres", ("saut", BandeSaut), ("bande", () => VoisinBande(10, 1)));
        public static Famille Puzzle(int cols, int rows) => new Famille("puzzle", ("assemble", () => PuzzleAssemble(cols, rows)), ("essai", OuVaLaPieceDemo));
        public static readonly Famille PuzzleTourne = new Famille("puzzle-tourne", ("tourne", PieceTournee), ("assemble", () => PuzzleAssemble(3, 3)));
        public static readonly Famille OuVaPiece = new Famille("ou-va-piece", ("essai", OuVaLaPieceDemo), ("assemble", () => PuzzleAssemble(2, 2)));
        public static Famille TrierCouleur(int n, bool variees) => new Famille("trier-couleur", ("boites", () => TrierParCouleur(n, variees)), ("deux-facons", Regrouper));
        public static readonly Famille TrierTaille = new Famille("trier-taille", ("boites", TrierParTaille), ("deux-facons", Regrouper));
        public static readonly Famille DeuxCriteres = new Famille("deux-criteres", ("deux", DeuxCriteresDemo), ("deux-facons", Regrouper));
        public static readonly Famille TrierNombre = new Famille("trier-nombre", ("boites", TrierParNombre), ("compter", () => CompterUnParUn(6)));

        // ------------------------------------------------------------------
        //  Petits outils
        // ------------------------------------------------------------------
        private static Acteur Forme(Demo d, string kind, Color c, double t) => d.Chose(Visuels.Forme(kind, c, t), t, t);
        private static Acteur Solide(Demo d, string kind, Color c, double t) => d.Chose(Visuels.Solide(kind, c, t), t, t);
        private static Acteur Perle(Demo d, Color c, double t) => d.Chose(Visuels.Perle(c, t), t, t);
        private static Acteur Emo(Demo d, string emoji, double t) => d.Chose(Art.Visual("", emoji, t), t, t);
        private static string Pluriel(string couleur) => couleur == "orange" ? "orange" : couleur + "s";
        private static string UnSolide(string k) => (k == "boule" || k == "pyramide" ? "une " : "un ") + Visuels.NomSolide(k, false);
        private static string UneForme(string f) => "un " + Visuels.NomForme(f, false);
        private static void Note(int degre) { try { SoundFx.Note(degre); } catch { } }

        /// <summary>Les coins d'une forme de côté <paramref name="s"/> posée en (centre, pied).</summary>
        private static List<(double x, double y)> Coins(string kind, double cx, double y, double s)
        {
            switch (kind)
            {
                case "carre": return new List<(double, double)> { (cx - s / 2, y - s), (cx + s / 2, y - s), (cx + s / 2, y), (cx - s / 2, y) };
                case "triangle": return new List<(double, double)> { (cx, y - s), (cx + s / 2, y), (cx - s / 2, y) };
                case "rectangle": return new List<(double, double)> { (cx - s / 2, y - s * 0.775), (cx + s / 2, y - s * 0.775), (cx + s / 2, y - s * 0.225), (cx - s / 2, y - s * 0.225) };
                default: return new List<(double, double)>();
            }
        }

        // ==================================================================
        //  LES FORMES
        // ==================================================================
        /// <summary>Chaque forme présentée par ce qui la fait reconnaître : ses côtés, ses coins.</summary>
        private static Demo FormePresentee(int n)
        {
            var d = new Demo("table");
            n = Math.Max(2, Math.Min(4, n));
            var kinds = Visuels.Formes.Take(n).ToList(); var cols = Visuels.Melange(Visuels.Couleurs).Take(n).ToList();
            const double S = 280, CX = 640; double y = Sol - 50;
            var xs = Rang(n, 210);
            var a = new Acteur[n]; var points = new List<Acteur>[n];
            for (int k = 0; k < n; k++)
            {
                a[k] = Forme(d, kinds[k], cols[k].C, S).En(CX, y).Opacite(0);
                points[k] = Coins(kinds[k], CX, y, S).Select(c => d.Rond(36, Demo.Or).Au(c.x, c.y).Opacite(0)).ToList();
            }
            for (int k = 0; k < n; k++)
            {
                int kk = k; string f = kinds[k];
                d.Dire("Voici " + Visuels.NomForme(f) + ".", () => a[kk].Pop(), 1400);
                if (f == "rond") d.Dire("Il est tout rond : pas de coin, il roule.", () => a[kk].Aller(CX + 230, y, 1100), 2100);
                else d.Dire(f == "carre" ? "Quatre côtés pareils, et quatre coins." : f == "triangle" ? "Trois côtés, et trois coins." : "Quatre coins aussi, mais il est allongé.",
                    () => { for (int i = 0; i < points[kk].Count; i++) points[kk][i].Pop(i * 380); }, 600 + points[kk].Count * 380);
                d.Faire(() => { foreach (var p in points[kk]) p.Fondu(0, 300); a[kk].Grandir(0.36, 800); a[kk].Aller(xs[kk], 200, 800); }, 950);
            }
            int c2 = Courant?.Forme != null && kinds.Contains(Courant.Forme) ? kinds.IndexOf(Courant.Forme) : Visuels.Rng.Next(n);   // la forme de l'exercice
            d.Toi("À toi ! Regarde " + Visuels.NomForme(kinds[c2]) + ".", a[c2], () => a[c2].Sauter(22, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Visuels.NomForme(kinds[c2])) + " !", () => a[c2].Briller(), 1400);
            return d;
        }

        /// <summary>La couleur change, la taille change, la forme tourne : c'est toujours la même forme.</summary>
        private static Demo FormeInvariante()
        {
            var d = new Demo("table");
            string f = Courant?.Forme == "carre" || Courant?.Forme == "triangle" || Courant?.Forme == "rectangle" ? Courant.Forme : Visuels.Un(new[] { "carre", "triangle", "rectangle" }); string un = UneForme(f);
            var cols = Visuels.Melange(Visuels.Couleurs).Take(2).ToList();
            var a = Forme(d, f, cols[0].C, 270).Origine(0.5, 0.5).En(640, Sol - 70).Opacite(0);
            var b = Forme(d, f, cols[1].C, 270).Origine(0.5, 0.5).En(640, Sol - 70).Opacite(0);
            double tour = f == "carre" ? 45 : 140;
            d.Dire("Voici " + un + ".", () => a.Pop(), 1400);
            d.Dire("Il change de couleur…", () => { a.Fondu(0, 1000); b.Fondu(1, 1000); }, 1500);
            d.Dire("c'est toujours " + un + ".", null, 1300);
            d.Dire("Il devient tout petit…", () => b.Grandir(0.4, 1300), 1600);
            d.Dire("c'est toujours " + un + ".", null, 1300);
            d.Dire("Il redevient grand, et il tourne…", () => { b.Grandir(1, 1000); b.Tourner(tour, 1700, 600); }, 2700);
            d.Dire("c'est toujours " + un + " !", null, 1400);
            d.Toi("À toi ! Regarde " + Visuels.NomForme(f) + " pour le faire tourner encore.", b, () => b.Tourner(tour + 180, 1600), 1800);
            d.Dire("La couleur, la taille, le sens : ça ne change pas la forme.", null, 2400);
            return d;
        }

        /// <summary>Pour être sûr, on compte les coins.</summary>
        private static Demo CoinsComptes()
        {
            var d = new Demo("table");
            const double S = 250; double y = Sol - 70;
            var cols = Visuels.Melange(Visuels.Couleurs).Take(2).ToList();
            var tri = Forme(d, "triangle", cols[0].C, S).En(400, y).Opacite(0);
            var car = Forme(d, "carre", cols[1].C, S).En(880, y).Opacite(0);
            var pt = Coins("triangle", 400, y, S).Select((c, i) => Pastille(d, i + 1, null, 54).Au(c.x, c.y).Opacite(0)).ToList();
            var pc = Coins("carre", 880, y, S).Select((c, i) => Pastille(d, i + 1, null, 54).Au(c.x, c.y).Opacite(0)).ToList();
            var oui = d.Chose(Visuels.OuiNon(true, 130), 130, 130).En(400, 190).Opacite(0);
            var non = d.Chose(Visuels.OuiNon(false, 130), 130, 130).En(880, 190).Opacite(0);
            d.Dire("Est-ce un triangle ? On compte les coins.", () => tri.Pop(), 2100);
            for (int i = 0; i < 3; i++) { int k = i; d.Dire(Maj(Dit(k + 1)) + ".", () => pt[k].Pop(), 800); }
            d.Dire("Trois coins : oui, c'est un triangle.", () => oui.Pop(), 2000);
            d.Dire("Et celui-là ?", () => car.Pop(), 1300);
            d.Toi("À toi ! Regarde la forme pour compter ses coins.", car, () => pc[0].Pop(), 600);
            d.Dire("Un.", null, 500);
            for (int i = 1; i < 4; i++) { int k = i; d.Dire(Maj(Dit(k + 1)) + ".", () => pc[k].Pop(), 800); }
            d.Dire("Quatre coins : ce n'est pas un triangle.", () => non.Pop(), 2000);
            d.Dire("C'est un carré.", () => car.Briller(), 1500);
            return d;
        }

        private static readonly (string Emoji, string Nom, string Forme)[] ChosesFormes =
        {
            ("🏀", "le ballon", "rond"), ("🎁", "le cadeau", "carre"), ("🍕", "la part de pizza", "triangle"), ("🚪", "la porte", "rectangle"), ("🕐", "l'horloge", "rond"), ("⛺", "la tente", "triangle"),
        };

        /// <summary>Les formes se cachent dans les objets de tous les jours.</summary>
        private static Demo FormeDansObjet()
        {
            var d = new Demo("piece");
            var trois = new List<(string Emoji, string Nom, string Forme)>();
            foreach (var c in Visuels.Melange(ChosesFormes)) if (!trois.Any(x => x.Forme == c.Forme)) { trois.Add(c); if (trois.Count == 3) break; }
            double[] xs = { 260, 640, 1020 };
            var obj = new Acteur[3]; var forme = new Acteur[3];
            for (int i = 0; i < 3; i++)
            {
                obj[i] = Emo(d, trois[i].Emoji, 210).En(xs[i], Sol - 40).Opacite(0);
                forme[i] = Forme(d, trois[i].Forme, Demo.Or, 190).En(xs[i], 250).Opacite(0);
            }
            void Poser(int i) { forme[i].Fondu(0.6, 500); forme[i].Aller(xs[i], Sol - 50, 1100, 0, Acteur.Sortie); }
            d.Dire("Les formes se cachent partout.", () => { for (int i = 0; i < 3; i++) obj[i].Pop(i * 250); }, 2000);
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                d.Dire("Regarde " + trois[k].Nom + ".", () => { obj[k].Sauter(18, 400); forme[k].Pop(500); }, 1700);
                d.Dire("On y voit " + UneForme(trois[k].Forme) + ".", () => Poser(k), 1900);
            }
            d.Dire("Et " + trois[2].Nom + " ?", () => { obj[2].Sauter(18, 400); forme[2].Pop(500); }, 1600);
            d.Toi("À toi ! Regarde " + trois[2].Nom + " pour voir sa forme.", obj[2], () => Poser(2), 1500);
            d.Dire(Maj(UneForme(trois[2].Forme)) + " !", null, 1200);
            return d;
        }

        // ==================================================================
        //  CLASSER : ranger dans des boîtes
        // ==================================================================
        /// <summary>Des boîtes étiquetées, des objets qui arrivent un par un et sautent dans la leur.</summary>
        private static Demo Boites(string intro, IList<(Func<FrameworkElement> Etiquette, string Label)> boites, IList<(Func<FrameworkElement> V, int Boite, string Dit)> objets, string morale)
        {
            var d = new Demo("piece");
            int nb = boites.Count, n = objets.Count;
            var xs = Rang(nb, nb == 2 ? 420 : 370);
            var b = new Acteur[nb]; var o = new Acteur[n];
            for (int i = 0; i < n; i++) o[i] = d.Chose(new Viewbox { Child = objets[i].V(), Width = 170, Height = 170, Stretch = Stretch.Uniform }, 170, 170).En(640, 290).Opacite(0);
            for (int i = 0; i < nb; i++) b[i] = d.Chose(new Viewbox { Child = Visuels.Boite(boites[i].Etiquette(), boites[i].Label, 190), Width = 250, Height = 210, Stretch = Stretch.Uniform }, 250, 210).En(xs[i], Sol + 40).Opacite(0);
            void Ranger(int i) { int bx = objets[i].Boite; o[i].Bond(xs[bx], Sol - 110, 130, 950); o[i].Grandir(0.4, 950); o[i].Fondu(0, 250, 850); b[bx].Ecraser(900); }
            d.Dire(intro, () => { for (int i = 0; i < nb; i++) b[i].Pop(i * 250); }, 2300);
            for (int i = 0; i < n - 1; i++)
            {
                int k = i;
                d.Dire(objets[k].Dit, () => o[k].Pop(), 1900);
                d.Faire(() => Ranger(k), 1150);
            }
            d.Dire("Et " + (char.ToLowerInvariant(objets[n - 1].Dit[0]) + objets[n - 1].Dit.Substring(1)).Split(':')[0].Trim() + " ?", () => o[n - 1].Pop(), 1700);
            d.Toi("À toi ! Regarde la bonne boîte pour ranger.", b[objets[n - 1].Boite], () => Ranger(n - 1), 1300);
            d.Dire(morale, () => { for (int i = 0; i < nb; i++) b[i].Sauter(14, 400, i * 200); }, 2400);
            return d;
        }

        private static Demo TrierParCouleur(int nb, bool variees)
        {
            nb = Math.Max(2, Math.Min(3, nb));
            var cols = Visuels.Melange(Visuels.Couleurs).Take(nb).ToList();
            var boites = cols.Select(c => (new Func<FrameworkElement>(() => Visuels.Pave(c.C)), c.Nom)).ToList();
            var objets = new List<(Func<FrameworkElement>, int, string)>();
            int n = nb == 2 ? 4 : 5; var ordre = Visuels.Melange(Enumerable.Range(0, n).Select(i => i % nb)).ToList();
            foreach (int ci in ordre)
            {
                var cc = cols[ci]; string f = variees ? Visuels.Un(Visuels.Formes.Take(4).ToList()) : "rond";
                objets.Add((() => Visuels.Forme(f, cc.C, 150), ci, "Un " + Visuels.NomForme(f, false) + " " + cc.Nom + " : avec les " + Pluriel(cc.Nom) + "."));
            }
            return Boites("Une boîte par couleur.", boites, objets, variees ? "La forme ne compte pas : on regarde la couleur." : "On met ensemble ce qui a la même couleur.");
        }

        private static Demo TrierParForme(int nb)
        {
            nb = Math.Max(2, Math.Min(3, nb));
            var fs = Visuels.Melange(Visuels.Formes.Take(Math.Max(nb, 3)).ToList()).Take(nb).ToList();
            var gris = Color.FromRgb(0x8A, 0x7A, 0xA8);
            var boites = fs.Select(f => (new Func<FrameworkElement>(() => Visuels.Forme(f, gris, 100)), Visuels.NomForme(f, false))).ToList();
            var objets = new List<(Func<FrameworkElement>, int, string)>();
            int n = nb == 2 ? 4 : 5; var ordre = Visuels.Melange(Enumerable.Range(0, n).Select(i => i % nb)).ToList();
            foreach (int fi in ordre)
            {
                string f = fs[fi]; var cc = Visuels.Un(Visuels.Couleurs); string nom = Visuels.NomForme(f, false);
                objets.Add((() => Visuels.Forme(f, cc.C, 150), fi, "Un " + nom + " : avec les " + nom + "s."));
            }
            return Boites("Une boîte par forme.", boites, objets, "La couleur ne compte pas : on regarde la forme.");
        }

        private static Demo TrierParTaille()
        {
            var ob = Visuels.Objet();
            var boites = new List<(Func<FrameworkElement>, string)> { (() => Visuels.Taille(ob, 100, 110), ob.Fem ? "grandes" : "grands"), (() => Visuels.Taille(ob, 45, 110), ob.Fem ? "petites" : "petits") };
            var objets = new List<(Func<FrameworkElement>, int, string)>();
            foreach (bool grand in new[] { true, false, false, true })
            {
                bool g = grand;
                objets.Add((() => Visuels.Taille(ob, g ? 165 : 70, 180), g ? 0 : 1, ob.Fem ? (g ? "Une grande : avec les grandes." : "Une petite : avec les petites.") : (g ? "Un grand : avec les grands." : "Un petit : avec les petits.")));
            }
            return Boites("Une boîte pour les " + boites[0].Item2 + ", une boîte pour les " + boites[1].Item2 + ".", boites, objets, "On met ensemble ce qui a la même taille.");
        }

        private static Demo TrierParNombre()
        {
            int a = Visuels.Entre(1, 3), b2 = a + Visuels.Entre(1, 2); var ob = Visuels.Objet(); var ob2 = Visuels.AutreObjet(ob);
            var boites = new List<(Func<FrameworkElement>, string)> { (() => Visuels.Chiffre(a, 100), a.ToString()), (() => Visuels.Chiffre(b2, 100), b2.ToString()) };
            var objets = new List<(Func<FrameworkElement>, int, string)>();
            foreach (var (premier, autre) in new[] { (true, false), (false, false), (false, true), (true, true) })
            {
                bool p = premier; var oo = autre ? ob2 : ob; int k = p ? a : b2;
                objets.Add((() => Visuels.Collection(oo, k, 60, autre), p ? 0 : 1, Maj(oo.N(k)) + " : dans la boîte du " + Dit(k) + "."));
            }
            return Boites("Une boîte pour " + Dit(a) + ", une boîte pour " + Dit(b2) + ".", boites, objets, "Peu importe l'objet : on regarde combien il y en a.");
        }

        /// <summary>Les mêmes objets, rangés de deux façons : par couleur, puis par forme.</summary>
        private static Demo Regrouper()
        {
            var d = new Demo("table");
            var cols = Visuels.Melange(Visuels.Couleurs).Take(2).ToList(); string[] fs = { "rond", "carre" };
            int[] couleur = { 0, 1, 0, 1, 1, 0 }, forme = { 0, 1, 1, 0, 1, 0 };
            (double x, double y) Centre(int i) => (500 + (i % 3) * 140, Sol - 80 - (i / 3) * 150);
            (double x, double y) Tas(bool gauche, int j) => ((gauche ? 190 : 930) + (j % 2) * 130, Sol - 80 - (j / 2) * 140);
            var a = new Acteur[6];
            for (int i = 0; i < 6; i++) a[i] = Forme(d, fs[forme[i]], cols[couleur[i]].C, 110).En(Centre(i).x, Centre(i).y).Opacite(0);
            void Grouper(int[] critere) { int g = 0, dr = 0; for (int i = 0; i < 6; i++) { var p = critere[i] == 0 ? Tas(true, g++) : Tas(false, dr++); a[i].Aller(p.x, p.y, 1300, i * 90); } }
            d.Dire("Des formes, toutes mélangées.", () => { for (int i = 0; i < 6; i++) a[i].Pop(i * 120); }, 2000);
            d.Dire("On met ensemble ce qui se ressemble.", null, 1700);
            d.Dire("Par couleur : les " + Pluriel(cols[0].Nom) + " ici, les " + Pluriel(cols[1].Nom) + " là.", () => Grouper(couleur), 2800);
            d.Dire("On peut aussi ranger autrement.", () => { for (int i = 0; i < 6; i++) a[i].Aller(Centre(i).x, Centre(i).y, 1100, i * 60); }, 2000);
            d.Toi("À toi ! Regarde une forme pour les ranger par forme.", a[1], () => Grouper(forme), 2000);
            d.Dire("Par forme : les ronds ici, les carrés là.", null, 2200);
            d.Dire("Les mêmes objets, deux façons de les ranger.", () => { for (int i = 0; i < 6; i++) a[i].Sauter(14, 380, i * 80); }, 2400);
            return d;
        }

        /// <summary>Deux conditions à la fois : grand ET rouge. On vérifie l'une, puis l'autre.</summary>
        private static Demo DeuxCriteresDemo()
        {
            var d = new Demo("piece");
            var rouge = Visuels.Couleurs[0]; var autre = Visuels.Un(Visuels.Couleurs.Skip(1).ToList()); string f = Visuels.Un(Visuels.Formes.Take(4).ToList());
            var gris = Color.FromRgb(0x8A, 0x7A, 0xA8);
            double[] xb = { 880, 400 };                                // la boîte des grands rouges à droite, l'autre à gauche
            var bOui = d.Chose(new Viewbox { Child = Visuels.Boite(Visuels.Forme(f, rouge.C, 110), "grands rouges", 190), Width = 250, Height = 210 }, 250, 210).En(xb[0], Sol + 40).Opacite(0);
            var bNon = d.Chose(new Viewbox { Child = Visuels.Boite(Visuels.Forme(f, gris, 60), "les autres", 190), Width = 250, Height = 210 }, 250, 210).En(xb[1], Sol + 40).Opacite(0);
            var cas = new[] { (true, true), (true, false), (false, true), (true, true) };
            var o = new Acteur[4]; var v1 = new Acteur[4]; var v2 = new Acteur[4];
            for (int i = 0; i < 4; i++)
            {
                o[i] = Forme(d, f, cas[i].Item2 ? rouge.C : autre.C, 190).En(640, 300).Echelle(cas[i].Item1 ? 1 : 0.45).Opacite(0);
                v1[i] = d.Chose(Visuels.OuiNon(cas[i].Item1, 105), 105, 105).En(430, 280).Opacite(0);
                v2[i] = d.Chose(Visuels.OuiNon(cas[i].Item2, 105), 105, 105).En(850, 280).Opacite(0);
            }
            void Ranger(int i) { bool ok = cas[i].Item1 && cas[i].Item2; double x = ok ? xb[0] : xb[1]; o[i].Bond(x, Sol - 110, 120, 950); o[i].Grandir(0.3, 950); o[i].Fondu(0, 250, 850); v1[i].Fondu(0, 300); v2[i].Fondu(0, 300); (ok ? bOui : bNon).Ecraser(900); }
            d.Dire("Dans cette boîte : seulement les grands rouges. Il faut les deux à la fois.", () => { bOui.Pop(); bNon.Pop(400); bOui.Briller(700); }, 3400);
            for (int i = 0; i < 3; i++)
            {
                int k = i; bool g = cas[i].Item1, r = cas[i].Item2;
                d.Dire("Grand ? " + (g ? "Oui." : "Non."), () => { o[k].Pop(); v1[k].Pop(700); }, 1900);
                if (g) d.Dire("Rouge ? " + (r ? "Oui." : "Non."), () => v2[k].Pop(300), 1600);
                d.Dire(g && r ? "Grand et rouge : dans la boîte." : g ? "Grand, mais pas rouge : avec les autres." : "Pas grand : avec les autres.", () => Ranger(k), 1900);
            }
            d.Dire("Et celui-là ? Grand ? Oui. Rouge ? Oui.", () => { o[3].Pop(); v1[3].Pop(900); v2[3].Pop(1800); }, 3200);
            d.Toi("À toi ! Regarde la boîte des grands rouges.", bOui, () => Ranger(3), 1300);
            d.Dire("Grand et rouge : il faut les deux.", () => bOui.Sauter(14, 400), 2000);
            return d;
        }

        // ==================================================================
        //  LES SOLIDES
        // ==================================================================
        private static readonly Dictionary<string, string> PhraseSolide = new Dictionary<string, string>
        {
            ["boule"] = "La boule : ronde de partout, elle roule.", ["cube"] = "Le cube : des faces carrées, toutes pareilles.",
            ["pyramide"] = "La pyramide : pointue en haut, large en bas.", ["cylindre"] = "Le cylindre : un rond dessus, un rond dessous.",
            ["cone"] = "Le cône : pointu comme un chapeau de fête.", ["pave"] = "Le pavé : comme une boîte à chaussures.",
        };

        /// <summary>Chaque solide présenté par ce qui le fait reconnaître.</summary>
        private static Demo SolidePresente(int n)
        {
            var d = new Demo("piece");
            n = n <= 4 ? 4 : 6;
            var kinds = Visuels.Solides.Take(n).Select(s => s.Cle).ToList();
            var xs = Rang(n, n == 4 ? 220 : 180);
            var a = new Acteur[n]; double y = Sol - 40;
            for (int k = 0; k < n; k++) a[k] = Solide(d, kinds[k], Visuels.Couleurs[k % Visuels.Couleurs.Length].C, 270).Origine(0.5, 0.5).En(640, y).Opacite(0);
            for (int k = 0; k < n; k++)
            {
                int kk = k; string s = kinds[k];
                d.Dire(PhraseSolide[s], () => { a[kk].Pop(); if (s == "boule") { a[kk].Aller(860, y, 1300, 600); a[kk].Tourner(300, 1300, 600); } else a[kk].Sauter(22, 460, 700); }, 2600);
                d.Faire(() => { a[kk].Grandir(0.38, 800); a[kk].Aller(xs[kk], 210, 800); }, 950);
            }
            int c = Courant?.Forme != null && kinds.Contains(Courant.Forme) ? kinds.IndexOf(Courant.Forme) : Visuels.Rng.Next(n);   // le solide de l'exercice
            d.Toi("À toi ! Regarde " + Visuels.NomSolide(kinds[c]) + ".", a[c], () => a[c].Sauter(22, 460, 0, 2), 1100);
            d.Dire("Oui ! " + Maj(Visuels.NomSolide(kinds[c])) + " !", () => a[c].Briller(), 1400);
            return d;
        }

        /// <summary>Plat ou solide : le carré se pose comme une feuille, le cube se prend et s'empile.</summary>
        private static Demo PlatOuSolide()
        {
            var d = new Demo("piece");
            var c1 = Visuels.Couleurs[1].C; var c2 = Visuels.Couleurs[4].C;
            var carre = Forme(d, "carre", c1, 200).En(360, Sol - 20).Opacite(0);
            var cubes = new Acteur[3];
            for (int i = 0; i < 3; i++) cubes[i] = Solide(d, "cube", c1, 170).En(i == 0 ? 880 : 1420, Sol - 20 - i * 118);
            cubes[0].Opacite(0);
            var rond = Forme(d, "rond", c2, 130).En(360, 230).Opacite(0);
            var boule = Solide(d, "boule", c2, 140).Origine(0.5, 0.5).En(620, 235).Opacite(0);
            d.Dire("Voici un carré, et un cube.", () => { carre.Pop(); cubes[0].Pop(700); }, 2200);
            d.Dire("Le carré est plat, comme une feuille.", () => carre.GrandirXY(1, 0.06, 1300), 2200);
            d.Dire("Posé sur la table, il ne dépasse pas.", null, 1700);
            d.Faire(() => carre.GrandirXY(1, 1, 900), 1000);
            d.Dire("Le cube est épais : on peut le prendre, et l'empiler.", () => { cubes[1].Aller(880, Sol - 138, 1000, 700, Acteur.Rebond); cubes[2].Aller(880, Sol - 256, 1000, 1500, Acteur.Rebond); }, 3000);
            d.Dire("C'est un solide.", () => { foreach (var c in cubes) c.Briller(); }, 1600);
            d.Dire("Pareil ici : le rond est plat, la boule est un solide.", () => { rond.Pop(); boule.Pop(900); }, 2800);
            d.Toi("À toi ! Regarde la boule.", boule, () => { boule.Aller(760, 235, 900); boule.Tourner(200, 900); }, 1200);
            d.Dire("Oui ! Un solide, on peut le tenir dans la main.", null, 2200);
            return d;
        }

        /// <summary>Le solide posé sur le sable laisse la forme de son dessous.</summary>
        private static Demo EmpreinteDemo()
        {
            var d = new Demo("piece");
            var trois = new List<string>();
            foreach (var s in Visuels.Melange(new[] { "cube", "cylindre", "pave", "pyramide", "cone" })) if (!trois.Any(x => Visuels.Empreinte(x) == Visuels.Empreinte(s))) { trois.Add(s); if (trois.Count == 3) break; }
            double[] xs = { 280, 640, 1000 };
            var sable = Color.FromRgb(0x9C, 0x6B, 0x3C);
            var trace = new Acteur[3]; var sol = new Acteur[3];
            for (int i = 0; i < 3; i++) trace[i] = Forme(d, Visuels.Empreinte(trois[i]), sable, 150).Origine(0.5, 0.5).Au(xs[i], Sol + 52).EchelleXY(1, 0.5).Opacite(0);
            for (int i = 0; i < 3; i++) sol[i] = Solide(d, trois[i], Visuels.Couleurs[(i * 2 + 1) % Visuels.Couleurs.Length].C, 200).En(xs[i], 260).Opacite(0);
            void Lever(int i) { sol[i].Aller(xs[i], 300, 1100); trace[i].Fondu(1, 500, 200); }
            d.Dire("On pose des solides sur le sable. Quelle trace laissent-ils ?", () => { for (int i = 0; i < 3; i++) sol[i].Pop(i * 250); }, 3000);
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                d.Dire("On pose " + Visuels.NomSolide(trois[k]) + ".", () => { sol[k].Aller(xs[k], Sol + 30, 900, 0, Acteur.Entree); sol[k].Ecraser(900); }, 1700);
                d.Dire("Il laisse " + UneForme(Visuels.Empreinte(trois[k])) + ".", () => Lever(k), 2000);
            }
            d.Dire("On pose " + Visuels.NomSolide(trois[2]) + ".", () => { sol[2].Aller(xs[2], Sol + 30, 900, 0, Acteur.Entree); sol[2].Ecraser(900); }, 1700);
            d.Toi("À toi ! Regarde " + Visuels.NomSolide(trois[2]) + " pour le soulever.", sol[2], () => Lever(2), 1400);
            d.Dire("Il laisse " + UneForme(Visuels.Empreinte(trois[2])) + ".", null, 1600);
            d.Dire("La trace, c'est la forme du dessous.", () => { for (int i = 0; i < 3; i++) trace[i].Briller(i * 300, 1200); }, 2400);
            return d;
        }

        private static readonly (string Emoji, string Nom, string Solide)[] ChosesSolides =
        {
            ("🎲", "le dé", "cube"), ("⚽", "le ballon", "boule"), ("🥫", "la boîte de conserve", "cylindre"), ("📦", "le carton", "pave"), ("🍦", "le cornet de glace", "cone"), ("🍊", "l'orange", "boule"),
        };

        /// <summary>Les solides sont partout : le dé est un cube, le ballon une boule.</summary>
        private static Demo SolideDansObjet()
        {
            var d = new Demo("piece");
            var trois = new List<(string Emoji, string Nom, string Solide)>();
            foreach (var c in Visuels.Melange(ChosesSolides)) if (!trois.Any(x => x.Solide == c.Solide)) { trois.Add(c); if (trois.Count == 3) break; }
            double[] xs = { 260, 640, 1020 };
            var obj = new Acteur[3]; var sol = new Acteur[3]; var lien = new Line[3];
            for (int i = 0; i < 3; i++)
            {
                obj[i] = Emo(d, trois[i].Emoji, 190).En(xs[i], Sol - 30).Opacite(0);
                sol[i] = Solide(d, trois[i].Solide, Visuels.Couleurs[(i * 2) % Visuels.Couleurs.Length].C, 160).En(xs[i], 250).Opacite(0);
                lien[i] = d.Trait(xs[i], 262, xs[i], Sol - 232, Demo.Or, 6, true);
            }
            void Montrer(int i) { sol[i].Pop(); Demo.Voir(lien[i], 400, 300); obj[i].Sauter(16, 400, 500); }
            d.Dire("Les solides sont partout autour de nous.", () => { for (int i = 0; i < 3; i++) obj[i].Pop(i * 250); }, 2400);
            for (int i = 0; i < 2; i++) { int k = i; d.Dire(Maj(trois[k].Nom) + " a la forme d'" + (UnSolide(trois[k].Solide).StartsWith("une") ? "une " : "un ") + Visuels.NomSolide(trois[k].Solide, false) + ".", () => Montrer(k), 2600); }
            d.Dire("Et " + trois[2].Nom + " ?", () => obj[2].Sauter(16, 400), 1500);
            d.Toi("À toi ! Regarde " + trois[2].Nom + " pour voir sa forme.", obj[2], () => Montrer(2), 1300);
            d.Dire(Maj(UnSolide(trois[2].Solide)) + " !", null, 1300);
            return d;
        }

        // ==================================================================
        //  LES MOTIFS
        // ==================================================================
        /// <summary>Les éléments d'un motif (A, B, C) : des perles de couleur, des formes, ou les deux.</summary>
        private sealed class Elements
        {
            public readonly Func<Demo, int, double, Acteur> Faire; public readonly string[] Nom;
            public Elements(string genre)
            {
                var cols = Visuels.Melange(Visuels.Couleurs).Take(3).ToList(); var fs = Visuels.Melange(Visuels.Formes.Take(4).ToList()).Take(3).ToList(); var col = Visuels.Un(Visuels.Couleurs).C;
                if (genre == "formes") { Faire = (d, k, t) => Forme(d, fs[k], col, t); Nom = fs.Select(f => Visuels.NomForme(f, false)).ToArray(); }
                else if (genre == "formecouleur") { Faire = (d, k, t) => Forme(d, fs[k], cols[k].C, t); Nom = Enumerable.Range(0, 3).Select(k => Visuels.NomForme(fs[k], false) + " " + cols[k].Nom).ToArray(); }
                else { Faire = (d, k, t) => Perle(d, cols[k].C, t); Nom = cols.Select(c => c.Nom).ToArray(); }
            }
        }

        private static Acteur Cadre(Demo d, double w, double h)
            => d.Chose(new Rectangle { Width = w, Height = h, RadiusX = 26, RadiusY = 26, Stroke = new SolidColorBrush(Demo.Or), StrokeThickness = 8, Fill = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xB3, 0x00)) }, w, h);

        /// <summary>Le motif : le petit bout qui se répète. Un cadre le suit de répétition en répétition.</summary>
        private static Demo MotifRepete(string motif, string genre)
        {
            var d = new Demo("table");
            var el = new Elements(genre);
            int L = motif.Length; const int N = 8; const double T = 96, Pas = 114; double y = Sol - 130;
            var xs = Rang(N, Pas);
            int[] k = Enumerable.Range(0, N).Select(i => motif[i % L] - 'A').ToArray();
            var fil = d.Trait(150, y - T / 2, 1130, y - T / 2, Gris, 5); fil.Opacity = 1;
            var a = new Acteur[N];
            for (int i = 0; i < N; i++) a[i] = el.Faire(d, k[i], T).En(xs[i], y).Opacite(0);
            var cadre = Cadre(d, L * Pas + 6, T + 44).Au((xs[0] + xs[L - 1]) / 2, y - T / 2).Opacite(0);
            string unite = string.Join(", ", motif.Select(ch => el.Nom[ch - 'A']));
            int reps = 6 / L;
            double Milieu(int r) => (xs[r * L] + xs[Math.Min(N - 1, r * L + L - 1)]) / 2 + (r * L + L - 1 > N - 1 ? Pas * (r * L + L - 1 - (N - 1)) / 2 : 0);
            d.Dire("Un collier. Regarde bien comment il est rangé.", null, 2200);
            for (int r = 0; r < reps; r++)
            {
                int rr = r;
                d.Dire(Maj(unite) + ".", () =>
                {
                    for (int j = 0; j < L; j++) a[rr * L + j].Pop(j * 380);
                    if (rr == 0) cadre.Fondu(1, 400); else cadre.AllerAu(Milieu(rr), y - T / 2, 500);
                }, 700 + L * 380);
            }
            d.Dire("Ça recommence toujours pareil : " + unite + ". C'est le motif.", () => cadre.Sauter(14, 400, 0, 2), 3300);
            d.Dire("Et après ?", () => cadre.AllerAu(Milieu(reps), y - T / 2, 600), 1400);
            d.Toi("À toi ! Regarde le cadre pour mettre la suite.", cadre, () => a[reps * L].Pop(), 900);
            d.Dire(Maj(el.Nom[k[reps * L]]) + " !", null, 1000);
            if (reps * L + 1 < N) d.Dire("Et puis " + el.Nom[k[reps * L + 1]] + ".", () => a[reps * L + 1].Pop(), 1500);
            return d;
        }

        /// <summary>Le motif s'entend : chaque couleur a son son, et la petite musique recommence.</summary>
        private static Demo MotifEnMusique(string motif)
        {
            var d = new Demo("table");
            var el = new Elements("couleurs");
            int L = motif.Length; const int N = 8; const double T = 96, Pas = 114; double y = Sol - 130;
            var xs = Rang(N, Pas);
            int[] k = Enumerable.Range(0, N).Select(i => motif[i % L] - 'A').ToArray();
            int[] son = { 0, 2, 4 };
            var fil = d.Trait(150, y - T / 2, 1130, y - T / 2, Gris, 5); fil.Opacity = 1;
            var a = new Acteur[N];
            for (int i = 0; i < N; i++) a[i] = el.Faire(d, k[i], T).En(xs[i], y).Opacite(0);
            var cadre = Cadre(d, L * Pas + 6, T + 44).Au((xs[0] + xs[L - 1]) / 2, y - T / 2).Opacite(0);
            int vus = 6 / L * L;
            d.Dire("Écoute le collier : chaque couleur a son son.", null, 2300);
            for (int i = 0; i < vus; i++) { int ii = i; d.Faire(() => { a[ii].Pop(); Note(son[k[ii]]); }, 560); }
            d.Dire("Tu entends ? Ça recommence toujours pareil.", () => cadre.Fondu(1, 400), 2300);
            for (int i = 0; i < L; i++) { int ii = i; d.Faire(() => { a[ii].Sauter(20, 380); Note(son[k[ii]]); }, 560); }
            d.Dire("C'est le motif.", () => cadre.Sauter(14, 400), 1500);
            d.Toi("À toi ! Regarde le cadre pour entendre la suite.", cadre, () => { cadre.Fondu(0, 300); a[vus].Pop(); Note(son[k[vus]]); }, 700);
            for (int i = vus + 1; i < N; i++) { int ii = i; d.Faire(() => { a[ii].Pop(); Note(son[k[ii]]); }, 560); }
            d.Dire("Un motif, on peut le voir, et on peut l'entendre.", null, 2400);
            return d;
        }

        /// <summary>Deux colliers : on vérifie le motif bout par bout, et on trouve la perle qui ne va pas.</summary>
        private static Demo ColliersCompares(string motif)
        {
            var d = new Demo("table");
            var cols = Visuels.Melange(Visuels.Couleurs).Take(4).ToList();
            int L = motif.Length; const int N = 6; const double T = 86, Pas = 104; double[] ys = { Sol - 300, Sol - 80 };
            var xs = Rang(N, Pas, 600);
            int[] k = Enumerable.Range(0, N).Select(i => motif[i % L] - 'A').ToArray();
            int intrus = N - 2, unites = N / L;
            var haut = new Acteur[N]; var bas = new Acteur[N];
            for (int i = 0; i < N; i++) { haut[i] = Perle(d, cols[k[i]].C, T).En(xs[i], ys[0]).Opacite(0); bas[i] = Perle(d, cols[i == intrus ? 3 : k[i]].C, T).En(xs[i], ys[1]).Opacite(0); }
            var bonne = Perle(d, cols[k[intrus]].C, T).En(xs[intrus], ys[1]).Opacite(0);
            var c1 = Cadre(d, L * Pas + 4, T + 40).Opacite(0); var c2 = Cadre(d, L * Pas + 4, T + 40).Opacite(0);
            double Milieu(int u) => (xs[u * L] + xs[u * L + L - 1]) / 2;
            c1.Au(Milieu(0), ys[0] - T / 2); c2.Au(Milieu(0), ys[1] - T / 2);
            var oui = d.Chose(Visuels.OuiNon(true, 110), 110, 110).En(1100, ys[0] + 10).Opacite(0);
            var non = d.Chose(Visuels.OuiNon(false, 110), 110, 110).En(1100, ys[1] + 10).Opacite(0);
            var oui2 = d.Chose(Visuels.OuiNon(true, 110), 110, 110).En(1100, ys[1] + 10).Opacite(0);
            string Unite(Func<int, int> coul, int u) => string.Join(", ", Enumerable.Range(u * L, L).Select(i => cols[coul(i)].Nom));
            d.Dire("Deux colliers. Lequel est bien rangé ?", () => { for (int i = 0; i < N; i++) { haut[i].Pop(i * 90); bas[i].Pop(600 + i * 90); } }, 2600);
            d.Dire("On vérifie le premier, bout par bout.", () => c1.Fondu(1, 400), 2000);
            for (int u = 0; u < unites; u++) { int uu = u; d.Dire(Maj(Unite(i => k[i], uu)) + ".", () => { if (uu > 0) c1.AllerAu(Milieu(uu), ys[0] - T / 2, 500); }, 1500); }
            d.Dire("Toujours pareil : il est bien rangé.", () => oui.Pop(), 2000);
            d.Dire("Et le deuxième ?", () => c2.Fondu(1, 400), 1400);
            int uIntrus = intrus / L;
            for (int u = 0; u < uIntrus; u++) { int uu = u; d.Dire(Maj(Unite(i => k[i], uu)) + ".", () => { if (uu > 0) c2.AllerAu(Milieu(uu), ys[1] - T / 2, 500); }, 1500); }
            d.Dire(Maj(Unite(i => i == intrus ? 3 : k[i], uIntrus)) + " ?", () => c2.AllerAu(Milieu(uIntrus), ys[1] - T / 2, 500), 1700);
            d.Dire("Ici, ça ne va pas : ce n'est plus le motif.", () => { bas[intrus].Briller(); non.Pop(500); }, 2400);
            d.Toi("À toi ! Regarde la perle qui ne va pas pour la changer.", bas[intrus], () => { bas[intrus].Fondu(0, 600); bonne.Fondu(1, 600); non.Fondu(0, 400); oui2.Pop(700); }, 1500);
            d.Dire("Voilà : maintenant, il est bien rangé aussi.", () => bonne.Sauter(18, 400), 2300);
            return d;
        }

        /// <summary>Le motif qui grandit : une, une, deux, deux, trois, trois…</summary>
        private static Demo MotifQuiGrandit()
        {
            var d = new Demo("table");
            var cols = Visuels.Melange(Visuels.Couleurs).Take(2).ToList();
            const double T = 58, Pas = 64; double y = Sol - 150;
            var seq = new List<(int Coul, int Groupe)>();
            for (int g = 1; g <= 4; g++) for (int c = 0; c < 2; c++) for (int i = 0; i < g; i++) if (g < 4 || c == 0) seq.Add((c, (g - 1) * 2 + c));
            int n = seq.Count;                                            // 1+1+2+2+3+3+4 = 16
            var xs = Rang(n, Pas);
            var a = new Acteur[n];
            for (int i = 0; i < n; i++) a[i] = Perle(d, cols[seq[i].Coul].C, T).En(xs[i], y).Opacite(0);
            var barres = new Acteur[7]; var nb = new Acteur[7];
            for (int g = 0; g < 7; g++)
            {
                var idx = Enumerable.Range(0, n).Where(i => seq[i].Groupe == g).ToList();
                double x = (xs[idx.First()] + xs[idx.Last()]) / 2, w = (idx.Count - 1) * Pas + T - 8;
                barres[g] = d.Rect(w, 10, Demo.Or, 5, false).Au(x, y + 22).Opacite(0);
                nb[g] = Pastille(d, idx.Count, null, 52).En(x, y + 96).Opacite(0);
            }
            void Groupes(int g1, int g2) { int delai = 0; for (int i = 0; i < n; i++) if (seq[i].Groupe == g1 || seq[i].Groupe == g2) { a[i].Pop(delai); delai += 170; } barres[g1].Fondu(1, 300, delai); nb[g1].Pop(delai); if (g2 >= 0) { barres[g2].Fondu(1, 300, delai + 200); nb[g2].Pop(delai + 200); } }
            d.Dire("Un collier qui grandit.", null, 1600);
            d.Dire("Une, une.", () => Groupes(0, 1), 1700);
            d.Dire("Deux, deux.", () => Groupes(2, 3), 2100);
            d.Dire("Trois, trois.", () => Groupes(4, 5), 2500);
            d.Dire("À chaque fois, il y en a une de plus.", () => { for (int g = 0; g < 6; g++) nb[g].Sauter(14, 380, g * 200); }, 2600);
            d.Toi("À toi ! Regarde le dernier nombre pour continuer le collier.", nb[5], () => Groupes(6, -1), 1700);
            d.Dire("Quatre !", null, 1100);
            return d;
        }

        /// <summary>Des perles en haut, des formes en bas : pas pareil à regarder, mais rangé pareil.</summary>
        private static Demo MemeStructureDemo()
        {
            var d = new Demo("table");
            var cols = Visuels.Melange(Visuels.Couleurs).Take(3).ToList(); string[] fs = { "rond", "carre" };
            const int N = 6; const double T = 92, Pas = 124; double[] ys = { Sol - 290, Sol - 70 };
            var xs = Rang(N, Pas);
            var haut = new Acteur[N]; var bas = new Acteur[N]; var liens = new Line[N];
            for (int i = 0; i < N; i++)
            {
                haut[i] = Perle(d, cols[i % 2].C, T).En(xs[i], ys[0]).Opacite(0);
                bas[i] = Forme(d, fs[i % 2], cols[2].C, T).En(xs[i], ys[1]).Opacite(0);
                liens[i] = d.Trait(xs[i], ys[0] + 10, xs[i], ys[1] - T - 10, i % 2 == 0 ? Demo.Or : Demo.Encre, 6, true);
            }
            void Battre(int parite, int delai) { for (int i = parite; i < N; i += 2) { haut[i].Sauter(18, 400, delai); bas[i].Sauter(18, 400, delai); } }
            d.Dire("En haut, des perles : " + cols[0].Nom + ", " + cols[1].Nom + ", " + cols[0].Nom + ", " + cols[1].Nom + "…", () => { for (int i = 0; i < N; i++) haut[i].Pop(i * 330); }, 3300);
            d.Dire("En bas, des formes : rond, carré, rond, carré…", () => { for (int i = 0; i < N; i++) bas[i].Pop(i * 330); }, 3300);
            d.Dire("Ce n'est pas pareil à regarder.", null, 1700);
            d.Dire("Mais c'est rangé pareil.", () => { for (int i = 0; i < N; i++) Demo.Voir(liens[i], 300, i * 220); }, 2300);
            d.Dire("Un, l'autre. Un, l'autre.", () => { Battre(0, 0); Battre(1, 700); }, 2300);
            d.Toi("À toi ! Regarde les formes pour voir encore.", bas[0], () => { Battre(0, 0); Battre(1, 700); }, 1700);
            d.Dire("C'est le même motif.", null, 1600);
            return d;
        }

        // ==================================================================
        //  LES PUZZLES
        // ==================================================================
        private sealed class Planche
        {
            public readonly Acteur[] P; public readonly double X0, Y0, Pw, Ph; public readonly int Cols, Rows;
            public (double x, double y) Place(int i) => (X0 + (i % Cols + 0.5) * Pw, Y0 + (i / Cols + 1) * Ph);

            /// <summary>L'image découpée en cols × rows morceaux, posés à leur place (invisibles).</summary>
            public Planche(Demo d, int cols, int rows, double largeur, double x0, double y0, bool fond = false)
            {
                var src = Courant?.Image ?? Visuels.Photo();        // l'image du puzzle de l'exercice
                if (src == null) throw new InvalidOperationException("pas d'image pour le puzzle");
                Cols = cols; Rows = rows; X0 = x0; Y0 = y0; Pw = largeur / cols; Ph = largeur * 0.75 / rows;
                if (fond) d.Rect(largeur + 16, largeur * 0.75 + 16, Color.FromRgb(0xFF, 0xF3, 0xC4), 10).En(x0 + largeur / 2, y0 + largeur * 0.75 + 8);
                int cw = src.PixelWidth / cols, ch = src.PixelHeight / rows;
                P = new Acteur[cols * rows];
                for (int i = 0; i < P.Length; i++)
                {
                    var img = new Image { Source = new CroppedBitmap(src, new Int32Rect((i % cols) * cw, (i / cols) * ch, cw, ch)), Stretch = Stretch.Fill };
                    var bord = new Border { Child = img, BorderBrush = Brushes.White, BorderThickness = new Thickness(3), Width = Pw, Height = Ph };
                    P[i] = d.Chose(bord, Pw, Ph).Origine(0.5, 0.5).En(Place(i).x, Place(i).y).Opacite(0);
                }
            }
        }

        private static readonly (double x, double y)[] Eparpille =
        {
            (100, 230), (1180, 230), (100, 410), (1180, 410), (100, 590), (1180, 590), (285, 320), (995, 320), (285, 500), (995, 500), (285, 660), (995, 660),
        };

        /// <summary>L'image est découpée, puis refaite morceau par morceau.</summary>
        private static Demo PuzzleAssemble(int cols, int rows)
        {
            var d = new Demo("table");
            var pl = new Planche(d, cols, rows, 520, 380, 140);
            int n = pl.P.Length; double k = Math.Min(0.8, Math.Min(170 / pl.Pw, 150 / pl.Ph));
            var ordre = Visuels.Melange(Enumerable.Range(0, n)).ToList();
            double[] angle = { -8, 7, 10, -6, 5, -9, 8, -5, 6 };
            void Remettre(int i) { var p = pl.P[i]; var c = pl.Place(i); p.Devant(); p.Bond(c.x, c.y, 60, 900); p.Grandir(1, 900); p.Tourner(0, 700); }
            d.Dire("Voici une image.", () => { foreach (var p in pl.P) p.Pop(); }, 1800);
            d.Dire("On la découpe en morceaux.", () =>
            {
                for (int j = 0; j < n; j++) { var p = pl.P[ordre[j]]; var e = Eparpille[j]; p.Aller(e.x, e.y, 1300, j * 80); p.Grandir(k, 1300, j * 80); p.Tourner(angle[j % angle.Length], 1300, j * 80); }
            }, 2300);
            d.Dire("Pour la refaire, on remet chaque morceau à sa place.", null, 2300);
            for (int j = 0; j < n - 1; j++)
            {
                int i = ordre[j];
                if (j == 0) d.Dire("Celui-ci va là.", () => Remettre(i), 1500);
                else if (j == 1) d.Dire("Et celui-là, ici.", () => Remettre(i), 1500);
                else d.Faire(() => Remettre(i), 1050);
            }
            int der = ordre[n - 1];
            d.Toi("À toi ! Regarde le dernier morceau pour finir le puzzle.", pl.P[der], () => Remettre(der), 1300);
            d.Dire("L'image continue d'un morceau à l'autre : c'est comme ça qu'on sait où il va.", () => { foreach (var p in pl.P) p.Briller(); }, 3300);
            return d;
        }

        /// <summary>Un morceau à l'envers : on le tourne, et l'image continue.</summary>
        private static Demo PieceTournee()
        {
            var d = new Demo("table");
            var pl = new Planche(d, 2, 2, 520, 440, 140, true);
            int trou = Visuels.Rng.Next(4); var p = pl.P[trou]; var c = pl.Place(trou);
            p.En(170, 430).Angle(180).Echelle(0.85);
            d.Dire("Il manque un morceau au puzzle.", () => { for (int i = 0; i < 4; i++) if (i != trou) pl.P[i].Pop(i * 120); p.Pop(700); }, 2500);
            d.Dire("On essaie celui-ci…", () => { p.Devant(); p.Aller(c.x, c.y, 1200); p.Grandir(1, 1200); }, 1700);
            d.Dire("Il est à l'envers : l'image ne continue pas.", () => p.Secouer(0, 3, 4), 2400);
            d.Toi("À toi ! Regarde le morceau pour le tourner.", p, () => p.Tourner(360, 1500), 1700);
            d.Dire("À l'endroit ! Maintenant, l'image continue.", () => { foreach (var q in pl.P) q.Briller(); }, 2600);
            return d;
        }

        /// <summary>Où va le morceau ? On essaie : là où l'image continue, c'est sa place.</summary>
        private static Demo OuVaLaPieceDemo()
        {
            var d = new Demo("table");
            var pl = new Planche(d, 2, 2, 500, 470, 150, true);
            var trous = Visuels.Melange(Enumerable.Range(0, 4)).Take(2).ToList(); int bon = trous[0], autre = trous[1];
            var cibleBon = d.Rect(pl.Pw - 10, pl.Ph - 10, Demo.Or, 10, false).En(pl.Place(bon).x, pl.Place(bon).y - 5).Opacite(0);
            var p = pl.P[bon]; var q = pl.P[autre];
            p.En(190, 400).Echelle(0.85); q.En(190, 620).Echelle(0.6);
            var c = pl.Place(bon); var faux = pl.Place(autre);
            d.Dire("Où va ce morceau ? Il y a deux trous.", () => { for (int i = 0; i < 4; i++) if (i != bon && i != autre) pl.P[i].Pop(i * 120); p.Pop(600); }, 2700);
            d.Dire("On essaie ici…", () => { p.Devant(); p.Bond(faux.x, faux.y, 60, 1100); p.Grandir(1, 1100); }, 1700);
            d.Dire("Non : l'image ne continue pas.", () => p.Secouer(0, 3, 4), 2200);
            d.Dire("Et dans l'autre trou ?", () => cibleBon.Fondu(0.45, 500), 1500);
            d.Toi("À toi ! Regarde l'autre trou pour essayer.", cibleBon, () => { cibleBon.Fondu(0, 300); p.Devant(); p.Bond(c.x, c.y, 80, 1100); }, 1400);
            d.Dire("Oui ! Là, l'image continue.", () => p.Briller(), 2000);
            d.Dire("Et l'autre morceau prend la place qui reste.", () => { q.Pop(); q.Bond(faux.x, faux.y, 80, 1100, 400); q.Grandir(1, 1100, 400); }, 2700);
            return d;
        }
    }
}
