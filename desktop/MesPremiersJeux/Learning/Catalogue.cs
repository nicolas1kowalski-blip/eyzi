using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using V = MesPremiersJeux.Learning.Visuels;

namespace MesPremiersJeux.Learning
{
    /// <summary>
    /// LE CATALOGUE : les compétences du référentiel (cycle 1, domaine 4 pour
    /// commencer : les nombres ; formes et grandeurs), leurs paliers, leurs axes
    /// et les générateurs d'exercices. Tout est procédural : aucune image à
    /// produire, les nombres et les formes se dessinent.
    /// </summary>
    public static class Catalogue
    {
        public static readonly List<Chemin> Chemins = new List<Chemin>();

        static Catalogue()
        {
            var nombres = new Chemin { Id = "nombres", Nom = "Les nombres", Icone = "🔢", Couleur = Color.FromRgb(0x3B, 0x9B, 0xFF) };
            var formes = new Chemin { Id = "formes", Nom = "Formes et grandeurs", Icone = "🔷", Couleur = Color.FromRgb(0x9B, 0x5D, 0xE5) };
            Chemins.Add(nombres);
            Chemins.Add(formes);

            // Les nombres : P1 de chaque compétence, puis P2, puis P3 (comme les unités d'un chemin).
            var cN = new[] { Denombrer(), Chiffres(), Comparer(), Composer(), Suite(), Probleme() };
            var cF = new[] { FormesC(), Grandeurs(), Suites(), Classer() };
            foreach (var chemin in new[] { (nombres, cN), (formes, cF) })
                for (int p = 1; p <= 3; p++)
                    foreach (var c in chemin.Item2)
                        if (c.PalierN(p) != null) chemin.Item1.Noeuds.Add(new Noeud { Competence = c, Palier = p });
        }

        public static IEnumerable<Competence> Competences => Chemins.SelectMany(c => c.Noeuds).Select(n => n.Competence).Distinct();

        // ------------------------------------------------------------------
        //  Petits constructeurs
        // ------------------------------------------------------------------
        private static Competence C(string id, string nom, string icone, params Palier[] paliers)
        {
            var c = new Competence { Id = id, Nom = nom, Icone = icone };
            c.Paliers.AddRange(paliers);
            return c;
        }

        private static Palier P(int n, params Axe[] axes)
        {
            var p = new Palier { Numero = n, Ecole = n == 1 ? "PS" : n == 2 ? "MS" : "GS" };
            p.Axes.AddRange(axes);
            return p;
        }

        private static Axe A(string id, string nom, Func<Exercice> make) => new Axe { Id = id, Nom = nom, Make = make };

        /// <summary>Un choix : les réponses sont des nombres (chiffres) dont « bon ».</summary>
        private static ExerciceChoix ChoixChiffres(string consigne, Func<FrameworkElement> scene, int bon, int count, int min, int max, bool proches = false)
        {
            var ex = new ExerciceChoix { Consigne = consigne, Scene = scene };
            foreach (var n in V.Nombres(bon, count, min, max, proches))
            {
                int k = n;
                ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(k), Bonne = k == bon, Dit = k.ToString() });
            }
            return ex;
        }

        private static ExerciceChoix ChoixOuiNon(string consigne, Func<FrameworkElement> scene, bool oui)
        {
            var ex = new ExerciceChoix { Consigne = consigne, Scene = scene };
            ex.Reponses.Add(new Carte { Visuel = () => V.OuiNon(true), Bonne = oui, Dit = "oui" });
            ex.Reponses.Add(new Carte { Visuel = () => V.OuiNon(false), Bonne = !oui, Dit = "non" });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        // ==================================================================
        //  LES NOMBRES
        // ==================================================================
        private static Competence Denombrer() => C("nombres.denombrer", "Dénombrer", "🍎",
            P(1,
                A("a", "compter et choisir le chiffre", () => Compter(1, 3, 2, false)),
                A("b", "montrer la collection demandée", () => Montrer(1, 3, 2))),
            P(2,
                A("a", "compter et choisir le chiffre", () => Compter(1, 6, 3, false)),
                A("b", "montrer la collection demandée", () => Montrer(1, 6, 3)),
                A("c", "le dé qui montre autant", () => DeAutant(1, 6, 3)),
                A("d", "compter en désordre", () => Compter(1, 6, 3, true))),
            P(3,
                A("a", "compter et choisir le chiffre", () => Compter(1, 10, 4, false)),
                A("b", "montrer la collection demandée", () => Montrer(3, 10, 3)),
                A("c", "la boîte de 10 qui montre autant", () => Boite10Autant(1, 10, 3)),
                A("d", "compter en désordre, objets variés", () => Compter(4, 10, 4, true, true))));

        private static ExerciceChoix Compter(int min, int max, int count, bool desordre, bool varies = false)
        {
            var o = V.Objet(); int n = V.Entre(min, max);
            var v = varies ? V.Melange(V.Objets).Take(3).ToArray() : null;
            var ex = ChoixChiffres(varies ? "Combien d'objets ?" : "Combien de " + o.Plur + " ?", () => V.Collection(o, n, 110, desordre, v), n, count, Math.Max(0, min - 1), max, true);
            ex.Bravo = n <= 1 ? "Oui ! Un !" : "Oui ! " + n + " !";
            return ex;
        }

        private static ExerciceChoix Montrer(int min, int max, int count)
        {
            var o = V.Objet(); int n = V.Entre(min, max);
            var ex = new ExerciceChoix { Consigne = "Montre " + o.N(n) + ".", Modele = () => V.Chiffre(n, 70), Taille = 300 };
            foreach (var k in V.Nombres(n, count, Math.Max(1, min), max, true))
            {
                int kk = k;
                ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o, kk, 70), Bonne = kk == n, Dit = o.N(kk) });
            }
            return ex;
        }

        private static ExerciceChoix DeAutant(int min, int max, int count)
        {
            var o = V.Objet(); int n = V.Entre(min, Math.Min(6, max));
            var ex = new ExerciceChoix { Consigne = "Quel dé montre autant que les " + o.Plur + " ?", Scene = () => V.Collection(o, n, 100) };
            foreach (var k in V.Nombres(n, count, 1, 6, true))
            {
                int kk = k;
                ex.Reponses.Add(new Carte { Visuel = () => V.De(kk), Bonne = kk == n, Dit = kk.ToString() });
            }
            return ex;
        }

        private static ExerciceChoix Boite10Autant(int min, int max, int count)
        {
            var o = V.Objet(); int n = V.Entre(min, max);
            var ex = new ExerciceChoix { Consigne = "Quelle boîte montre autant que les " + o.Plur + " ?", Scene = () => V.Collection(o, n, 90, true), Taille = 280 };
            foreach (var k in V.Nombres(n, count, 1, 10, true))
            {
                int kk = k;
                ex.Reponses.Add(new Carte { Visuel = () => V.Boite10(kk, 220), Bonne = kk == n, Dit = kk.ToString() });
            }
            return ex;
        }

        private static Competence Chiffres() => C("nombres.chiffres", "Reconnaître les chiffres", "🔢",
            P(1,
                A("a", "où est le chiffre ?", () => OuEstChiffre(1, 3, 2, false)),
                A("b", "le chiffre et le dé", () => ChiffreVersDe(1, 3, 2))),
            P(2,
                A("a", "où est le chiffre ?", () => OuEstChiffre(1, 6, 4, false)),
                A("b", "le chiffre et le dé", () => ChiffreVersDe(1, 6, 3)),
                A("c", "le chiffre et la boîte de 10", () => ChiffreVersBoite(1, 6, 3))),
            P(3,
                A("a", "où est le chiffre ? (chiffres proches)", () => OuEstChiffre(0, 10, 4, true)),
                A("b", "le chiffre et la boîte de 10", () => ChiffreVersBoite(0, 10, 4)),
                A("c", "ranger trois chiffres", () => OrdonnerNombres(0, 10, 3))));

        private static ExerciceChoix OuEstChiffre(int min, int max, int count, bool proches)
        {
            int n = V.Entre(min, max);
            var ex = ChoixChiffres("Où est le " + n + " ?", null, n, count, min, max, proches);
            ex.Bravo = "Oui, c'est le " + n + " !";
            return ex;
        }

        private static ExerciceChoix ChiffreVersDe(int min, int max, int count)
        {
            int n = V.Entre(Math.Max(1, min), Math.Min(6, max));
            var ex = new ExerciceChoix { Consigne = "Quel dé montre " + n + " ?", Scene = () => V.Chiffre(n, 220) };
            foreach (var k in V.Nombres(n, count, 1, 6, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.De(kk), Bonne = kk == n, Dit = kk.ToString() }); }
            return ex;
        }

        private static ExerciceChoix ChiffreVersBoite(int min, int max, int count)
        {
            int n = V.Entre(min, max);
            var ex = new ExerciceChoix { Consigne = "Quelle boîte montre " + n + " ?", Scene = () => V.Chiffre(n, 220), Taille = 280 };
            foreach (var k in V.Nombres(n, count, 0, 10, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Boite10(kk, 220), Bonne = kk == n, Dit = kk.ToString() }); }
            return ex;
        }

        private static ExerciceOrdre OrdonnerNombres(int min, int max, int count)
        {
            var nums = V.Nombres(V.Entre(min, max), count, min, max).OrderBy(x => x).ToList();
            var ex = new ExerciceOrdre { Consigne = "Range les nombres du plus petit au plus grand. Lequel est le plus petit ?", ConsigneSuite = "Et après, le plus petit qui reste ?" };
            foreach (var n in nums) { int k = n; ex.Cartes.Add(new Carte { Visuel = () => V.Chiffre(k), Dit = k.ToString() }); }
            return ex;
        }

        private static Competence Comparer() => C("nombres.comparer", "Comparer", "⚖️",
            P(1,
                A("a", "où y en a-t-il beaucoup ?", () => BeaucoupPeu(true)),
                A("b", "où y en a-t-il le moins ?", () => BeaucoupPeu(false))),
            P(2,
                A("a", "où y en a-t-il le plus ?", () => PlusMoins(true, 6)),
                A("b", "autant que le modèle", () => Autant(6)),
                A("c", "oui ou non : autant ?", () => AutantOuiNon(6))),
            P(3,
                A("a", "le plus grand nombre", () => PlusGrandNombre()),
                A("b", "ranger trois nombres", () => OrdonnerNombres(0, 10, 3)),
                A("c", "oui ou non : plus grand ?", () => PlusGrandOuiNon())));

        private static ExerciceChoix BeaucoupPeu(bool beaucoup)
        {
            var o = V.Objet(); int peu = V.Entre(1, 2), bcp = V.Entre(5, 7);
            var ex = new ExerciceChoix { Consigne = beaucoup ? "Où y a-t-il beaucoup de " + o.Plur + " ?" : "Où y a-t-il le moins de " + o.Plur + " ?", Taille = 300 };
            ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o, bcp, 60, true), Bonne = beaucoup, Dit = "beaucoup" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o, peu, 60, true), Bonne = !beaucoup, Dit = "un peu" });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        private static ExerciceChoix PlusMoins(bool plus, int max)
        {
            var o = V.Objet(); var nums = V.Nombres(V.Entre(2, max), 3, 1, max, true);
            int bon = plus ? nums.Max() : nums.Min();
            var ex = new ExerciceChoix { Consigne = plus ? "Où y a-t-il le plus de " + o.Plur + " ?" : "Où y a-t-il le moins de " + o.Plur + " ?", Taille = 300 };
            foreach (var n in nums) { int k = n; ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o, k, 60), Bonne = k == bon, Dit = o.N(k) }); }
            return ex;
        }

        private static ExerciceChoix Autant(int max)
        {
            var o = V.Objet(); var o2 = V.AutreObjet(o); int n = V.Entre(2, max);
            var ex = new ExerciceChoix { Consigne = "Où y a-t-il autant de " + o2.Plur + " que de " + o.Plur + " ?", Scene = () => V.Collection(o, n, 90), Taille = 300 };
            foreach (var k in V.Nombres(n, 3, 1, max, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o2, kk, 60), Bonne = kk == n, Dit = o2.N(kk) }); }
            return ex;
        }

        private static ExerciceChoix AutantOuiNon(int max)
        {
            var o = V.Objet(); var o2 = V.AutreObjet(o); int n = V.Entre(1, max);
            bool oui = V.Rng.Next(2) == 0; int m = oui ? n : (n < max && V.Rng.Next(2) == 0 ? n + 1 : Math.Max(1, n - 1));
            if (!oui && m == n) m = n + 1;
            return ChoixOuiNon("Il y a autant de " + o.Plur + " que de " + o2.Plur + " ?", () => V.CoteACote(V.Collection(o, n, 90), V.Collection(o2, m, 90)), oui);
        }

        private static ExerciceChoix PlusGrandNombre()
        {
            var nums = V.Nombres(V.Entre(1, 10), 3, 0, 10, true); int bon = nums.Max();
            var ex = new ExerciceChoix { Consigne = "Quel est le plus grand nombre ?" };
            foreach (var n in nums) { int k = n; ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(k), Bonne = k == bon, Dit = k.ToString() }); }
            return ex;
        }

        private static ExerciceChoix PlusGrandOuiNon()
        {
            int a = V.Entre(0, 10), b; do b = V.Entre(0, 10); while (b == a);
            return ChoixOuiNon(a + " est plus grand que " + b + " ?", () => V.CoteACote(V.Chiffre(a, 200), V.Signe("et"), V.Chiffre(b, 200)), a > b);
        }

        private static Competence Composer() => C("nombres.composer", "Composer et décomposer", "➕",
            P(2,
                A("a", "3, c'est 2 et combien ?", () => Decomposer(3, 5)),
                A("b", "j'ajoute un", () => AjouteUn(true, 5))),
            P(3,
                A("a", "décomposer 5", () => Decomposer(5, 5)),
                A("b", "décomposer 10 avec la boîte", () => Decomposer10()),
                A("c", "j'enlève un", () => AjouteUn(false, 10))));

        private static ExerciceChoix Decomposer(int min, int max)
        {
            var o = V.Objet(); int total = V.Entre(min, max), a = V.Entre(1, total - 1), b = total - a;
            var ex = new ExerciceChoix
            {
                Consigne = total + ", c'est " + a + " et combien ?",
                Scene = () => V.CoteACote(V.Collection(o, a, 90), V.Signe("+"), V.Trou(110)),
                Modele = () => V.Chiffre(total, 70),
            };
            foreach (var k in V.Nombres(b, 3, 1, Math.Max(3, total), true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Collection(o, kk, 60), Bonne = kk == b, Dit = o.N(kk) }); }
            ex.Bravo = total + ", c'est " + a + " et " + b + " !";
            return ex;
        }

        private static ExerciceChoix Decomposer10()
        {
            int a = V.Entre(1, 9), b = 10 - a;
            var ex = ChoixChiffres("Il y a " + a + " jetons. Combien de cases vides pour faire 10 ?", () => V.Boite10(a, 420), b, 3, 1, 9, true);
            ex.Bravo = a + " et " + b + ", ça fait 10 !";
            return ex;
        }

        private static ExerciceChoix AjouteUn(bool plus, int max)
        {
            var o = V.Objet(); int n = plus ? V.Entre(1, max - 1) : V.Entre(2, max); int bon = plus ? n + 1 : n - 1;
            var ex = ChoixChiffres("Il y a " + o.N(n) + ". " + (plus ? "J'en ajoute une. " : "J'en enlève une. ") + "Combien maintenant ?",
                () => plus ? V.DeuxGroupes(o, n, 1, "+") : V.DeuxGroupes(o, n - 1, 1, " ", true), bon, 3, 0, max, true);
            return ex;
        }

        private static Competence Suite() => C("nombres.suite", "La suite des nombres", "🔢",
            P(2,
                A("a", "quel nombre vient après ?", () => Apres(1, 10, true)),
                A("b", "le nombre qui manque sur la bande", () => Manquant(1, 10))),
            P(3,
                A("a", "quel nombre vient après ?", () => Apres(1, 29, true)),
                A("b", "quel nombre vient avant ?", () => Apres(2, 30, false)),
                A("c", "le nombre qui manque sur la bande", () => Manquant(1, 30)),
                A("d", "ranger quatre nombres", () => OrdonnerNombres(0, 20, 4))));

        private static ExerciceChoix Apres(int min, int max, bool apres)
        {
            int n = V.Entre(min, max); int bon = apres ? n + 1 : n - 1;
            var ex = ChoixChiffres(apres ? "Quel nombre vient juste après " + n + " ?" : "Quel nombre vient juste avant " + n + " ?", () => V.Chiffre(n, 220), bon, 3, Math.Max(0, bon - 3), bon + 3, true);
            return ex;
        }

        private static ExerciceChoix Manquant(int min, int max)
        {
            int span = 5; int a = V.Entre(min, Math.Max(min, max - span + 1)); int trou = V.Entre(a + 1, a + span - 2);
            var ex = ChoixChiffres("Quel nombre manque sur la bande ?", () => V.Bande(a, a + span - 1, trou), trou, 3, Math.Max(0, trou - 3), trou + 3, true);
            return ex;
        }

        private static Competence Probleme() => C("nombres.probleme", "Petits problèmes", "🧺",
            P(2,
                A("a", "j'en ajoute", () => Ajout(5)),
                A("b", "j'en enlève", () => Retrait(5))),
            P(3,
                A("a", "j'en ajoute", () => Ajout(10)),
                A("b", "j'en enlève", () => Retrait(10)),
                A("c", "partager en deux", () => Partage())));

        private static ExerciceChoix Ajout(int max)
        {
            var o = V.Objet(); int a = V.Entre(1, max - 1), b = V.Entre(1, max - a);
            var ex = ChoixChiffres("Il y a " + o.N(a) + ". J'en ajoute " + b + ". Combien de " + o.Plur + " en tout ?", () => V.DeuxGroupes(o, a, b, "+"), a + b, 3, 1, max, true);
            return ex;
        }

        private static ExerciceChoix Retrait(int max)
        {
            var o = V.Objet(); int a = V.Entre(2, max), b = V.Entre(1, a - 1);
            var ex = ChoixChiffres("Il y a " + o.N(a) + ". J'en enlève " + b + ". Combien en reste-t-il ?", () => V.DeuxGroupes(o, a - b, b, " ", true), a - b, 3, 0, max, true);
            return ex;
        }

        private static ExerciceChoix Partage()
        {
            var o = V.Objet(); int moitie = V.Entre(1, 5), total = moitie * 2;
            var ex = ChoixChiffres("Il y a " + o.N(total) + " pour deux enfants. Combien chacun ?", () => V.Collection(o, total, 90), moitie, 3, 1, 6, true);
            return ex;
        }

        // ==================================================================
        //  FORMES ET GRANDEURS
        // ==================================================================
        private static Competence FormesC() => C("formes.formes", "Les formes", "🔷",
            P(1,
                A("a", "où est le rond, le carré ?", () => OuEstForme(2, 2, false)),
                A("b", "trier ronds et carrés", () => TrierFormes(2, 4, 2))),
            P(2,
                A("a", "où est la forme ?", () => OuEstForme(4, 4, false)),
                A("b", "trier trois formes", () => TrierFormes(3, 6, 4)),
                A("c", "la forme dans l'objet", () => FormeObjet(4)),
                A("d", "formes de toutes tailles et couleurs", () => OuEstForme(4, 4, true))),
            P(3,
                A("a", "où est la forme ? (six formes)", () => OuEstForme(6, 6, true)),
                A("b", "trier quatre formes", () => TrierFormes(4, 8, 6)),
                A("c", "la forme dans l'objet", () => FormeObjet(6)),
                A("d", "oui ou non : c'est un losange ?", () => FormeOuiNon())));

        private static ExerciceChoix OuEstForme(int nFormes, int count, bool variees)
        {
            var formes = V.Formes.Take(nFormes).ToList();
            var bon = V.Un(formes);
            var ex = new ExerciceChoix { Consigne = "Où est " + V.NomForme(bon) + " ?" };
            var baseColor = V.Un(V.Couleurs).C;
            foreach (var f in V.Melange(formes).Take(count))
            {
                string ff = f;
                Color col = variees ? V.Un(V.Couleurs).C : baseColor;
                double size = variees ? 110 + V.Rng.Next(60) : 150;
                double rot = variees && (ff == "carre" || ff == "rectangle" || ff == "triangle") ? V.Rng.Next(-30, 31) : 0;
                ex.Reponses.Add(new Carte { Visuel = () => V.Forme(ff, col, size, rot), Bonne = ff == bon, Dit = V.NomForme(ff) });
            }
            ex.Bravo = "Oui, c'est " + V.NomForme(bon) + " !";
            return ex;
        }

        private static ExerciceAttribution TrierFormes(int nBoites, int nObjets, int vivier)
        {
            var formes = V.Melange(V.Formes.Take(Math.Max(nBoites, vivier)).ToList()).Take(nBoites).ToList();
            var ex = new ExerciceAttribution { Consigne = "Range chaque forme dans sa boîte.", ConsigneObjet = "Où va {0} ?" };
            foreach (var f in formes) { string ff = f; ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Forme(ff, Color.FromRgb(0x8A, 0x7A, 0xA8), 100), V.NomForme(ff, false)), Dit = V.NomForme(ff) }); }
            for (int i = 0; i < nObjets; i++)
            {
                int ci = i % nBoites; string ff = formes[ci]; var col = V.Un(V.Couleurs);
                ex.Objets.Add(new Carte { Visuel = () => V.Forme(ff, col.C, 150), Cible = ci, Dit = V.NomForme(ff) + " " + col.Nom });
            }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static readonly (string Emoji, string Nom, string Forme)[] ObjetsFormes =
        {
            ("🕐", "l'horloge", "rond"), ("🏀", "le ballon", "rond"), ("🍕", "la part de pizza", "triangle"), ("🚪", "la porte", "rectangle"),
            ("🪟", "la fenêtre", "carre"), ("🎁", "le cadeau", "carre"), ("🥚", "l'œuf", "ovale"), ("🪁", "le cerf-volant", "losange"),
            ("⛺", "la tente", "triangle"), ("📱", "le téléphone", "rectangle"), ("🍪", "le biscuit", "rond"), ("🎲", "le dé", "carre"),
        };

        private static ExerciceChoix FormeObjet(int nFormes)
        {
            var formes = V.Formes.Take(nFormes).ToList();
            var pool = ObjetsFormes.Where(o => formes.Contains(o.Forme)).ToList();
            var obj = V.Un(pool);
            var ex = new ExerciceChoix { Consigne = "Quelle forme a " + obj.Nom + " ?", Scene = () => MesPremiersJeux.Lib.Art.Visual("", obj.Emoji, 260) };
            var col = V.Un(V.Couleurs).C;
            foreach (var f in V.Melange(formes).Take(Math.Min(4, nFormes))) { string ff = f; ex.Reponses.Add(new Carte { Visuel = () => V.Forme(ff, col), Bonne = ff == obj.Forme, Dit = V.NomForme(ff) }); }
            if (!ex.Reponses.Any(r => r.Bonne)) { string ff = obj.Forme; ex.Reponses[0] = new Carte { Visuel = () => V.Forme(ff, col), Bonne = true, Dit = V.NomForme(ff) }; }
            return ex;
        }

        private static ExerciceChoix FormeOuiNon()
        {
            string f = V.Un(V.Formes), q = V.Rng.Next(2) == 0 ? f : V.Un(V.Formes);
            var col = V.Un(V.Couleurs).C;
            return ChoixOuiNon("C'est " + V.NomForme(q).Replace("le ", "un ").Replace("l'", "un ") + " ?", () => V.Forme(f, col, 240, V.Rng.Next(-20, 21)), f == q);
        }

        private static Competence Grandeurs() => C("formes.grandeurs", "Les grandeurs", "📏",
            P(1,
                A("a", "le grand ou le petit", () => GrandPetit(true)),
                A("b", "le plus petit", () => GrandPetit(false))),
            P(2,
                A("a", "long ou court", () => LongCourt()),
                A("b", "gros ou mince", () => GrosMince()),
                A("c", "ranger trois tailles", () => OrdonnerTailles(3))),
            P(3,
                A("a", "ranger cinq tailles", () => OrdonnerTailles(5)),
                A("b", "lourd ou léger", () => LourdLeger()),
                A("c", "plein ou vide", () => PleinVide())));

        private static ExerciceChoix GrandPetit(bool grand)
        {
            var o = V.Objet();
            var ex = new ExerciceChoix { Consigne = grand ? "Où est le grand ? Montre " + o.Sing.Replace("une ", "la grande ").Replace("un ", "le grand ") + "." : "Montre " + o.Sing.Replace("une ", "la plus petite ").Replace("un ", "le plus petit ") + "." , Taille = 280 };
            ex.Reponses.Add(new Carte { Visuel = () => V.Taille(o, 170, 200), Bonne = grand, Dit = "grand" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Taille(o, 70, 200), Bonne = !grand, Dit = "petit" });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        private static ExerciceChoix LongCourt()
        {
            bool longQ = V.Rng.Next(2) == 0; var col = V.Un(V.Couleurs).C;
            var ex = new ExerciceChoix { Consigne = longQ ? "Montre la barre la plus longue." : "Montre la barre la plus courte.", Taille = 300 };
            ex.Reponses.Add(new Carte { Visuel = () => V.Barre(240, 44, col), Bonne = longQ, Dit = "la longue" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Barre(110, 44, col), Bonne = !longQ, Dit = "la courte" });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        private static ExerciceChoix GrosMince()
        {
            bool grosQ = V.Rng.Next(2) == 0; var col = V.Un(V.Couleurs).C;
            var ex = new ExerciceChoix { Consigne = grosQ ? "Montre le gros bâton." : "Montre le bâton mince.", Taille = 300 };
            ex.Reponses.Add(new Carte { Visuel = () => V.Barre(150, 90, col, true), Bonne = grosQ, Dit = "le gros" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Barre(150, 26, col, true), Bonne = !grosQ, Dit = "le mince" });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        private static ExerciceOrdre OrdonnerTailles(int n)
        {
            var o = V.Objet();
            var ex = new ExerciceOrdre { Consigne = "Range du plus petit au plus grand. Lequel est le plus petit ?", ConsigneSuite = "Et après, le plus petit qui reste ?" };
            for (int i = 0; i < n; i++) { double s = 60 + i * (120.0 / Math.Max(1, n - 1)); ex.Cartes.Add(new Carte { Visuel = () => V.Taille(o, s, 190), Dit = "" }); }
            return ex;
        }

        private static readonly (string Emoji, string Nom, bool Lourd)[] Poids =
        {
            ("🐘", "l'éléphant", true), ("🪨", "le rocher", true), ("🚗", "la voiture", true), ("🐋", "la baleine", true),
            ("🪶", "la plume", false), ("🎈", "le ballon", false), ("🦋", "le papillon", false), ("🍃", "la feuille", false),
        };

        private static ExerciceChoix LourdLeger()
        {
            bool lourdQ = V.Rng.Next(2) == 0;
            var l = V.Un(Poids.Where(p => p.Lourd).ToList()); var g = V.Un(Poids.Where(p => !p.Lourd).ToList());
            var ex = new ExerciceChoix { Consigne = lourdQ ? "Qu'est-ce qui est le plus lourd ?" : "Qu'est-ce qui est le plus léger ?", Taille = 280 };
            ex.Reponses.Add(new Carte { Visuel = () => MesPremiersJeux.Lib.Art.Visual("", l.Emoji, 170), Bonne = lourdQ, Dit = l.Nom });
            ex.Reponses.Add(new Carte { Visuel = () => MesPremiersJeux.Lib.Art.Visual("", g.Emoji, 170), Bonne = !lourdQ, Dit = g.Nom });
            if (V.Rng.Next(2) == 0) ex.Reponses.Reverse();
            return ex;
        }

        private static ExerciceChoix PleinVide()
        {
            int q = V.Rng.Next(3); // 0 plein, 1 vide, 2 à moitié
            var ex = new ExerciceChoix { Consigne = q == 0 ? "Montre le verre plein." : q == 1 ? "Montre le verre vide." : "Montre le verre à moitié plein." };
            ex.Reponses.Add(new Carte { Visuel = () => V.Verre(1), Bonne = q == 0, Dit = "plein" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Verre(0), Bonne = q == 1, Dit = "vide" });
            ex.Reponses.Add(new Carte { Visuel = () => V.Verre(0.5), Bonne = q == 2, Dit = "à moitié" });
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        private static Competence Suites() => C("formes.suites", "Les suites", "📿",
            P(1,
                A("a", "alternance de deux couleurs", () => SuiteCouleurs(2, 2)),
                A("b", "alternance de deux formes", () => SuiteFormes(2))),
            P(2,
                A("a", "motif de trois couleurs", () => SuiteCouleurs(3, 3)),
                A("b", "forme et couleur", () => SuiteFormeCouleur()),
                A("c", "motif AAB", () => SuiteMotif("AAB"))),
            P(3,
                A("a", "motif ABC de formes", () => SuiteFormes(3)),
                A("b", "motif ABB ou AAB", () => SuiteMotif(V.Rng.Next(2) == 0 ? "ABB" : "AAB")),
                A("c", "suite de nombres", () => SuiteNombres())));

        private static ExerciceChoix SuiteGenerique(List<Func<FrameworkElement>> motif, List<string> noms, int longueur, int nReponses, List<(Func<FrameworkElement> V, string Nom)> distracteurs)
        {
            var items = new List<Func<FrameworkElement>>(); var dits = new List<string>();
            for (int i = 0; i < longueur; i++) { items.Add(motif[i % motif.Count]); dits.Add(noms[i % motif.Count]); }
            int trou = longueur - 1 - V.Rng.Next(2);
            var ex = new ExerciceChoix { Consigne = "Qu'est-ce qui vient à la place du point d'interrogation ?", Scene = () => V.Rangee(items, trou, 100) };
            var bon = items[trou]; string bonNom = dits[trou];
            ex.Reponses.Add(new Carte { Visuel = bon, Bonne = true, Dit = bonNom });
            foreach (var d in V.Melange(distracteurs.Where(d => d.Nom != bonNom)).Take(nReponses - 1)) ex.Reponses.Add(new Carte { Visuel = d.V, Bonne = false, Dit = d.Nom });
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        private static ExerciceChoix SuiteCouleurs(int nCouleurs, int nReponses)
        {
            var cols = V.Melange(V.Couleurs).Take(nCouleurs).ToList();
            var motif = cols.Select(c => new Func<FrameworkElement>(() => V.Perle(c.C))).ToList();
            var tous = V.Couleurs.Select(c => (new Func<FrameworkElement>(() => V.Perle(c.C)), c.Nom)).ToList();
            return SuiteGenerique(motif, cols.Select(c => c.Nom).ToList(), nCouleurs == 2 ? 6 : 7, nReponses, tous);
        }

        private static ExerciceChoix SuiteFormes(int nFormes)
        {
            var fs = V.Melange(V.Formes.Take(4).ToList()).Take(nFormes).ToList(); var col = V.Un(V.Couleurs).C;
            var motif = fs.Select(f => new Func<FrameworkElement>(() => V.Forme(f, col, 100))).ToList();
            var tous = V.Formes.Take(4).Select(f => (new Func<FrameworkElement>(() => V.Forme(f, col, 100)), f)).ToList();
            return SuiteGenerique(motif, fs, nFormes == 2 ? 6 : 7, 3, tous);
        }

        private static ExerciceChoix SuiteFormeCouleur()
        {
            var fs = V.Melange(V.Formes.Take(4).ToList()).Take(2).ToList(); var cols = V.Melange(V.Couleurs).Take(2).ToList();
            var motif = new List<Func<FrameworkElement>> { () => V.Forme(fs[0], cols[0].C, 100), () => V.Forme(fs[1], cols[1].C, 100) };
            var noms = new List<string> { fs[0] + cols[0].Nom, fs[1] + cols[1].Nom };
            var tous = new List<(Func<FrameworkElement>, string)>
            {
                (() => V.Forme(fs[0], cols[0].C, 100), fs[0] + cols[0].Nom), (() => V.Forme(fs[1], cols[1].C, 100), fs[1] + cols[1].Nom),
                (() => V.Forme(fs[0], cols[1].C, 100), fs[0] + cols[1].Nom), (() => V.Forme(fs[1], cols[0].C, 100), fs[1] + cols[0].Nom),
            };
            return SuiteGenerique(motif, noms, 6, 3, tous);
        }

        private static ExerciceChoix SuiteMotif(string motifStr)
        {
            var cols = V.Melange(V.Couleurs).Take(2).ToList();
            var motif = motifStr.Select(ch => new Func<FrameworkElement>(() => V.Perle(cols[ch == 'A' ? 0 : 1].C))).ToList();
            var noms = motifStr.Select(ch => cols[ch == 'A' ? 0 : 1].Nom).ToList();
            var tous = V.Couleurs.Select(c => (new Func<FrameworkElement>(() => V.Perle(c.C)), c.Nom)).ToList();
            return SuiteGenerique(motif, noms, 8, 3, tous);
        }

        private static ExerciceChoix SuiteNombres()
        {
            int pas = V.Rng.Next(2) == 0 ? 1 : 2; int start = V.Entre(1, 4);
            var items = Enumerable.Range(0, 5).Select(i => start + i * pas).ToList();
            int trou = V.Entre(2, 4);
            var ex = new ExerciceChoix { Consigne = "Quel nombre manque dans la suite ?", Scene = () => V.Rangee(items.Select(n => new Func<FrameworkElement>(() => V.Chiffre(n, 100))).ToList(), trou, 100) };
            int bon = items[trou];
            foreach (var k in V.Nombres(bon, 3, Math.Max(0, bon - 3), bon + 3, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(kk), Bonne = kk == bon, Dit = kk.ToString() }); }
            return ex;
        }

        private static Competence Classer() => C("formes.classer", "Classer", "🧺",
            P(1,
                A("a", "trier par couleur", () => TrierCouleur(2, 4)),
                A("b", "trier par couleur, formes variées", () => TrierCouleur(2, 4, true))),
            P(2,
                A("a", "trier par forme", () => TrierFormes(2, 6, 4)),
                A("b", "trier grands et petits", () => TrierTaille()),
                A("c", "trier trois couleurs", () => TrierCouleur(3, 6, true))),
            P(3,
                A("a", "deux critères : grand et rouge", () => TrierDeuxCriteres()),
                A("b", "trier par nombre", () => TrierNombre()),
                A("c", "oui ou non : c'est grand et rouge ?", () => DeuxCriteresOuiNon())));

        private static ExerciceAttribution TrierCouleur(int nBoites, int nObjets, bool formesVariees = false)
        {
            var cols = V.Melange(V.Couleurs).Take(nBoites).ToList();
            var ex = new ExerciceAttribution { Consigne = "Range chaque objet dans la boîte de sa couleur." };
            foreach (var c in cols) { var cc = c; ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Pave(cc.C), cc.Nom), Dit = "la boîte " + cc.Nom }); }
            for (int i = 0; i < nObjets; i++)
            {
                int ci = i % nBoites; var cc = cols[ci]; string f = formesVariees ? V.Un(V.Formes.Take(4).ToList()) : "rond";
                ex.Objets.Add(new Carte { Visuel = () => V.Forme(f, cc.C, 150), Cible = ci, Dit = V.NomForme(f) + " " + cc.Nom });
            }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static ExerciceAttribution TrierTaille()
        {
            var o = V.Objet();
            var ex = new ExerciceAttribution { Consigne = "Range les grands avec les grands, et les petits avec les petits." };
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Taille(o, 100, 110), "grands"), Dit = "la boîte des grands" });
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Taille(o, 45, 110), "petits"), Dit = "la boîte des petits" });
            for (int i = 0; i < 6; i++) { bool grand = i % 2 == 0; ex.Objets.Add(new Carte { Visuel = () => V.Taille(o, grand ? 160 : 70, 180), Cible = grand ? 0 : 1, Dit = grand ? "le grand" : "le petit" }); }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static ExerciceAttribution TrierDeuxCriteres()
        {
            var rouge = V.Couleurs[0]; var autre = V.Un(V.Couleurs.Skip(1).ToList()); string f = V.Un(V.Formes.Take(4).ToList());
            var ex = new ExerciceAttribution { Consigne = "Dans la première boîte : seulement les grands rouges. Les autres vont dans l'autre boîte." };
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Forme(f, rouge.C, 110), "grands rouges"), Dit = "la boîte des grands rouges" });
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Forme(f, Color.FromRgb(0x8A, 0x7A, 0xA8), 60), "les autres"), Dit = "l'autre boîte" });
            var combos = new[] { (true, true), (true, false), (false, true), (false, false), (true, true), (false, true) };
            foreach (var (grand, isRouge) in combos)
            {
                bool g = grand, r = isRouge;
                ex.Objets.Add(new Carte { Visuel = () => V.Forme(f, r ? rouge.C : autre.C, g ? 170 : 80), Cible = g && r ? 0 : 1, Dit = (g ? "le grand " : "le petit ") + (r ? "rouge" : autre.Nom) });
            }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static ExerciceAttribution TrierNombre()
        {
            int a = V.Entre(1, 4), b = a + V.Entre(1, 2); var o = V.Objet(); var o2 = V.AutreObjet(o);
            var ex = new ExerciceAttribution { Consigne = "Range les collections : celles qui ont " + a + ", et celles qui ont " + b + "." };
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Chiffre(a, 100), a.ToString()), Dit = "la boîte " + a });
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Chiffre(b, 100), b.ToString()), Dit = "la boîte " + b });
            for (int i = 0; i < 4; i++) { bool first = i % 2 == 0, vrac = i >= 2; var oo = vrac ? o2 : o; ex.Objets.Add(new Carte { Visuel = () => V.Collection(oo, first ? a : b, 60, vrac), Cible = first ? 0 : 1, Dit = oo.N(first ? a : b) }); }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static ExerciceChoix DeuxCriteresOuiNon()
        {
            var rouge = V.Couleurs[0]; var autre = V.Un(V.Couleurs.Skip(1).ToList()); string f = V.Un(V.Formes.Take(4).ToList());
            bool grand = V.Rng.Next(2) == 0, r = V.Rng.Next(2) == 0;
            return ChoixOuiNon("C'est un grand " + V.NomForme(f, false) + " rouge ?", () => V.Forme(f, r ? rouge.C : autre.C, grand ? 240 : 100), grand && r);
        }
    }
}
