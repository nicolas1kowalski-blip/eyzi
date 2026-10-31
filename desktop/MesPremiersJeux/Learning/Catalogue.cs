using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using V = MesPremiersJeux.Learning.Visuels;

namespace MesPremiersJeux.Learning
{
    /// <summary>
    /// LE CATALOGUE : les compétences du référentiel, leurs paliers, leurs axes
    /// et les générateurs d'exercices. Aligné sur le programme de l'école
    /// maternelle (BO n° 41 du 31 octobre 2024, modifié par le BO n° 19 du
    /// 7 mai 2026), domaine « L'acquisition des premiers outils mathématiques » :
    /// P1 = « à aborder avant 4 ans » (PS), P2 = « à partir de 4 ans » (MS),
    /// P3 = « à partir de 5 ans » (GS). Tout est procédural : les nombres, les
    /// formes et les solides se dessinent.
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
            var cN = new[] { Denombrer(), Chiffres(), Comparer(), Composer(), Suite(), Rang(), Probleme() };
            var cF = new[] { FormesC(), Solides(), Grandeurs(), Suites(), Classer(), Puzzles() };
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
                A("a", "où est le chiffre ? (chiffres proches)", () => OuEstChiffre(1, 10, 4, true)),
                A("b", "le chiffre et la boîte de 10", () => ChiffreVersBoite(1, 10, 4)),
                A("c", "ranger trois chiffres", () => OrdonnerNombres(1, 10, 3))));

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
                A("b", "où y en a-t-il le moins ?", () => BeaucoupPeu(false)),
                A("c", "où y en a-t-il le plus ? (jusqu'à 4)", () => PlusMoins(true, 4))),
            P(2,
                A("a", "où y en a-t-il le plus ?", () => PlusMoins(true, 6)),
                A("b", "autant que le modèle", () => Autant(6)),
                A("c", "oui ou non : autant ?", () => AutantOuiNon(6))),
            P(3,
                A("a", "le plus grand nombre", () => PlusGrandNombre()),
                A("b", "ranger trois nombres", () => OrdonnerNombres(1, 10, 3)),
                A("c", "oui ou non : plus grand ?", () => PlusGrandOuiNon()),
                A("d", "autant que le modèle (jusqu'à 10)", () => Autant(10))));

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
            var nums = V.Nombres(V.Entre(1, 10), 3, 1, 10, true); int bon = nums.Max();
            var ex = new ExerciceChoix { Consigne = "Quel est le plus grand nombre ?" };
            foreach (var n in nums) { int k = n; ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(k), Bonne = k == bon, Dit = k.ToString() }); }
            return ex;
        }

        private static ExerciceChoix PlusGrandOuiNon()
        {
            int a = V.Entre(1, 10), b; do b = V.Entre(1, 10); while (b == a);
            return ChoixOuiNon(a + " est plus grand que " + b + " ?", () => V.CoteACote(V.Chiffre(a, 200), V.Signe("et"), V.Chiffre(b, 200)), a > b);
        }

        private static Competence Composer() => C("nombres.composer", "Composer et décomposer", "➕",
            P(1,
                A("a", "2, c'est 1 et combien ?", () => Decomposer(2, 3)),
                A("b", "j'ajoute un (jusqu'à 3)", () => AjouteUn(true, 3))),
            P(2,
                A("a", "4, c'est 2 et combien ?", () => Decomposer(3, 6)),
                A("b", "j'ajoute un", () => AjouteUn(true, 6)),
                A("c", "j'enlève un", () => AjouteUn(false, 6))),
            P(3,
                A("a", "décomposer jusqu'à 10", () => Decomposer(6, 10)),
                A("b", "décomposer 10 avec la boîte", () => Decomposer10()),
                A("c", "les doubles : 3 et 3", () => Doubles()),
                A("d", "j'enlève un", () => AjouteUn(false, 10))));

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

        private static ExerciceChoix Doubles()
        {
            var o = V.Objet(); int a = V.Entre(1, 5);
            var ex = ChoixChiffres(a + " et " + a + ", ça fait combien ?", () => V.DeuxGroupes(o, a, a, "+"), 2 * a, 3, 1, 10, true);
            ex.Bravo = a + " et " + a + ", ça fait " + (2 * a) + " !";
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
            P(1,
                A("a", "quel nombre vient après ? (jusqu'à 6)", () => Apres(1, 5, true)),
                A("b", "le nombre qui manque sur la bande (1 à 6)", () => Manquant(1, 6))),
            P(2,
                A("a", "quel nombre vient après ? (jusqu'à 12)", () => Apres(1, 11, true)),
                A("b", "quel nombre vient avant ?", () => Apres(2, 12, false)),
                A("c", "le nombre qui manque sur la bande (1 à 12)", () => Manquant(1, 12))),
            P(3,
                A("a", "quel nombre vient après ? (jusqu'à 30)", () => Apres(1, 29, true)),
                A("b", "quel nombre vient avant ?", () => Apres(2, 30, false)),
                A("c", "le nombre qui manque sur la bande (1 à 30)", () => Manquant(1, 30)),
                A("d", "compter à rebours", () => Rebours()),
                A("e", "de deux en deux", () => SuiteNombres(2))));

        private static ExerciceChoix Apres(int min, int max, bool apres)
        {
            int n = V.Entre(min, max); int bon = apres ? n + 1 : n - 1;
            var ex = ChoixChiffres(apres ? "Quel nombre vient juste après " + n + " ?" : "Quel nombre vient juste avant " + n + " ?", () => V.Chiffre(n, 220), bon, 3, Math.Max(1, bon - 3), bon + 3, true);
            return ex;
        }

        private static ExerciceChoix Rebours()
        {
            int s = V.Entre(4, 10); int bon = s - 3;
            var items = new[] { s, s - 1, s - 2, bon }.Select(n => new Func<FrameworkElement>(() => V.Chiffre(n, 100))).ToList();
            var ex = ChoixChiffres("On compte à rebours : " + s + ", " + (s - 1) + ", " + (s - 2) + "… et après ?", () => V.Rangee(items, 3, 100), bon, 3, Math.Max(1, bon - 3), bon + 3, true);
            ex.Bravo = "Oui ! " + s + ", " + (s - 1) + ", " + (s - 2) + ", " + bon + " !";
            return ex;
        }

        private static ExerciceChoix Manquant(int min, int max)
        {
            int span = 5; int a = V.Entre(min, Math.Max(min, max - span + 1)); int trou = V.Entre(a + 1, a + span - 2);
            var ex = ChoixChiffres("Quel nombre manque sur la bande ?", () => V.Bande(a, a + span - 1, trou), trou, 3, Math.Max(1, trou - 3), trou + 3, true);
            return ex;
        }

        private static Competence Rang() => C("nombres.rang", "Le rang et la bande", "🚂",
            P(2,
                A("a", "le premier, le dernier", () => RangFile(4, 6, false)),
                A("b", "le deuxième, le troisième", () => RangFile(4, 6, true)),
                A("c", "le dé sur la bande", () => DeSurBande(6)),
                A("d", "compléter la bande (1 à 6)", () => Manquant(1, 6))),
            P(3,
                A("a", "premier, dernier, avant-dernier", () => RangFile(6, 10, true)),
                A("b", "avancer sur la piste", () => Piste(10, true)),
                A("c", "reculer sur la piste", () => Piste(10, false)),
                A("d", "compléter la bande (1 à 10)", () => Manquant(1, 10))));

        private static readonly string[] Ordinaux = { "premier", "deuxième", "troisième", "quatrième", "cinquième", "sixième", "septième", "huitième", "neuvième", "dixième" };

        /// <summary>Une file d'objets différents, une flèche de départ : « qui est le troisième ? ».</summary>
        private static ExerciceChoix RangFile(int min, int max, bool milieu)
        {
            int n = V.Entre(min, max);
            var objets = V.Melange(V.Objets).Take(n).ToList();
            int idx; string nom;
            int tirage = V.Rng.Next(milieu ? 4 : 2);
            if (tirage == 0) { idx = 0; nom = "le premier"; }
            else if (tirage == 1) { idx = n - 1; nom = "le dernier"; }
            else if (tirage == 2 && n >= 4 && max >= 10) { idx = n - 2; nom = "l'avant-dernier"; }
            else { idx = V.Entre(1, Math.Min(n - 1, max >= 10 ? 9 : 5)); nom = "le " + Ordinaux[idx]; }
            var bon = objets[idx];
            var ex = new ExerciceChoix { Consigne = "Regarde la file, en partant de la flèche. Qui est " + nom + " ?", Scene = () => V.File(objets, n <= 6 ? 96 : 70), Bravo = "Oui ! " + bon.Sing.Replace("une ", "La ").Replace("un ", "Le ") + ", c'est " + nom + " !" };
            var choix = new List<ObjetDef> { bon };
            foreach (var o in V.Melange(objets.Where(o => o != bon)).Take(2)) choix.Add(o);
            foreach (var o in V.Melange(choix)) { var oo = o; ex.Reponses.Add(new Carte { Visuel = () => oo.Visuel(150), Bonne = oo == bon, Dit = oo.Sing }); }
            return ex;
        }

        private static ExerciceChoix DeSurBande(int max)
        {
            int n = V.Entre(1, Math.Min(6, max));
            var ex = new ExerciceChoix { Consigne = "Dans quelle case de la bande va ce dé ?", Scene = () => V.CoteACote(V.De(n, 160), V.Bande(1, max, -1, 80)), Taille = 230 };
            foreach (var k in V.Nombres(n, 3, 1, max, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(kk), Bonne = kk == n, Dit = "la case " + kk }); }
            ex.Bravo = "Oui ! Le dé qui montre " + n + " va dans la case " + n + " !";
            return ex;
        }

        /// <summary>Le pion est sur la case k ; il avance (ou recule) de d cases : où arrive-t-il ?</summary>
        private static ExerciceChoix Piste(int n, bool avance)
        {
            int d = V.Entre(1, 2); int k = avance ? V.Entre(1, n - d) : V.Entre(1 + d, n); int bon = avance ? k + d : k - d;
            var ex = ChoixChiffres("La grenouille est sur la case " + k + ". Elle " + (avance ? "avance" : "recule") + " de " + d + (d == 1 ? " case" : " cases") + ". Sur quelle case arrive-t-elle ?",
                () => V.Piste(n, k, n <= 6 ? 110 : 84), bon, 3, 1, n, true);
            ex.Bravo = "Oui ! Elle arrive sur la case " + bon + " !";
            return ex;
        }

        private static Competence Probleme() => C("nombres.probleme", "Petits problèmes", "🧺",
            P(1,
                A("a", "j'en ajoute (jusqu'à 3)", () => Ajout(3)),
                A("b", "j'en enlève (jusqu'à 3)", () => Retrait(3))),
            P(2,
                A("a", "j'en ajoute (jusqu'à 6)", () => Ajout(6)),
                A("b", "j'en enlève (jusqu'à 6)", () => Retrait(6)),
                A("c", "partager en deux", () => Partage(3))),
            P(3,
                A("a", "j'en ajoute (jusqu'à 10)", () => Ajout(10)),
                A("b", "j'en enlève (jusqu'à 10)", () => Retrait(10)),
                A("c", "partager en deux", () => Partage(5)),
                A("d", "combien en plus ?", () => Ecart()),
                A("e", "combien en manque-t-il ?", () => Manque())));

        private static ExerciceChoix Ajout(int max)
        {
            var o = V.Objet(); int a = V.Entre(1, max - 1), b = V.Entre(1, max - a);
            var ex = ChoixChiffres("Il y a " + o.N(a) + ". J'en ajoute " + b + ". Combien de " + o.Plur + " en tout ?", () => V.DeuxGroupes(o, a, b, "+"), a + b, 3, 1, max, true);
            return ex;
        }

        private static ExerciceChoix Retrait(int max)
        {
            var o = V.Objet(); int a = V.Entre(2, max), b = V.Entre(1, a - 1);
            var ex = ChoixChiffres("Il y a " + o.N(a) + ". J'en enlève " + b + ". Combien en reste-t-il ?", () => V.DeuxGroupes(o, a - b, b, " ", true), a - b, 3, 1, max, true);
            return ex;
        }

        private static ExerciceChoix Partage(int maxMoitie)
        {
            var o = V.Objet(); int moitie = V.Entre(1, maxMoitie), total = moitie * 2;
            var ex = ChoixChiffres("Il y a " + o.N(total) + " pour deux enfants. Combien chacun ?", () => V.Collection(o, total, 90), moitie, 3, 1, Math.Max(3, maxMoitie + 1), true);
            return ex;
        }

        private static ExerciceChoix Ecart()
        {
            var o = V.Objet(); var o2 = V.AutreObjet(o); int a = V.Entre(3, 10), b = V.Entre(1, a - 1);
            var ex = ChoixChiffres("Il y a " + o.N(a) + " et " + o2.N(b) + ". Combien de " + o.Plur + " en plus ?", () => V.CoteACote(V.Collection(o, a, 70), V.Collection(o2, b, 70)), a - b, 3, 1, 10, true);
            ex.Bravo = "Oui ! " + (a - b) + " de plus !";
            return ex;
        }

        private static ExerciceChoix Manque()
        {
            var o = V.Objet(); int total = V.Entre(3, 10), a = V.Entre(1, total - 1);
            var ex = ChoixChiffres("Il faut " + o.N(total) + ". Il y en a " + a + ". Combien en manque-t-il ?", () => V.CoteACote(V.Collection(o, a, 80), V.Signe("→"), V.Chiffre(total, 160)), total - a, 3, 1, 10, true);
            ex.Bravo = "Oui ! Il en manque " + (total - a) + " !";
            return ex;
        }

        // ==================================================================
        //  FORMES ET GRANDEURS
        // ==================================================================
        private static Competence FormesC() => C("formes.formes", "Les formes", "🔷",
            P(1,
                A("a", "où est le rond, le carré ?", () => OuEstForme(2, 2, false)),
                A("b", "la même forme que le modèle", () => MemeForme(3)),
                A("c", "trier ronds et carrés", () => TrierFormes(2, 4, 2))),
            P(2,
                A("a", "où est la forme ? (rond, carré, triangle)", () => OuEstForme(3, 3, false)),
                A("b", "trier trois formes", () => TrierFormes(3, 6, 3)),
                A("c", "la forme dans l'objet", () => FormeObjet(3)),
                A("d", "formes de toutes tailles et couleurs", () => OuEstForme(3, 3, true))),
            P(3,
                A("a", "où est la forme ? (avec le rectangle)", () => OuEstForme(4, 4, true)),
                A("b", "formes tournées : le carré sur la pointe", () => OuEstForme(4, 4, true, true)),
                A("c", "trier quatre formes", () => TrierFormes(4, 8, 4)),
                A("d", "la forme dans l'objet", () => FormeObjet(4)),
                A("e", "oui ou non : c'est un triangle ?", () => FormeOuiNon())));

        /// <summary>Un modèle, trois formes d'une autre couleur : « montre la même forme ».</summary>
        private static ExerciceChoix MemeForme(int nFormes)
        {
            var formes = V.Formes.Take(nFormes).ToList(); string bon = V.Un(formes);
            var c1 = V.Un(V.Couleurs); var c2 = V.Un(V.Couleurs.Where(c => c.Nom != c1.Nom).ToList());
            var ex = new ExerciceChoix { Consigne = "Regarde bien le modèle. Montre la même forme.", Scene = () => V.Forme(bon, c1.C, 220), Bravo = "Oui, c'est la même forme !" };
            foreach (var f in V.Melange(formes)) { string ff = f; ex.Reponses.Add(new Carte { Visuel = () => V.Forme(ff, c2.C, 150), Bonne = ff == bon, Dit = V.NomForme(ff) }); }
            return ex;
        }

        private static ExerciceChoix OuEstForme(int nFormes, int count, bool variees, bool tournees = false)
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
                double rot = tournees && ff != "rond" ? (ff == "carre" ? 45 : V.Rng.Next(15, 346)) : variees && (ff == "carre" || ff == "rectangle" || ff == "triangle") ? V.Rng.Next(-30, 31) : 0;
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
            var quatre = V.Formes.Take(4).ToList();
            string f = V.Un(quatre), q = V.Rng.Next(2) == 0 ? f : V.Un(quatre);
            var col = V.Un(V.Couleurs).C;
            return ChoixOuiNon("C'est " + V.NomForme(q).Replace("le ", "un ").Replace("l'", "un ") + " ?", () => V.Forme(f, col, 240, f == "rond" ? 0 : V.Rng.Next(0, 360)), f == q);
        }

        private static Competence Solides() => C("formes.solides", "Les solides", "🧊",
            P(2,
                A("a", "où est le cube, la boule ?", () => OuEstSolide(4)),
                A("b", "l'objet qui a cette forme", () => SolideObjet(4)),
                A("c", "trier les solides et les formes plates", () => TrierSolidesPlats())),
            P(3,
                A("a", "où est le solide ? (avec le cône et le pavé)", () => OuEstSolide(6)),
                A("b", "l'objet qui a cette forme", () => SolideObjet(6)),
                A("c", "oui ou non : c'est un cylindre ?", () => SolideOuiNon()),
                A("d", "l'empreinte du solide", () => EmpreinteSolide())));

        private static ExerciceChoix OuEstSolide(int nSolides)
        {
            var sol = V.Solides.Take(nSolides).Select(s => s.Cle).ToList(); string bon = V.Un(sol);
            var ex = new ExerciceChoix { Consigne = "Où est " + V.NomSolide(bon) + " ?", Bravo = "Oui, c'est " + V.NomSolide(bon) + " !" };
            foreach (var s in V.Melange(sol).Take(4)) { string ss = s; var col = V.Un(V.Couleurs).C; ex.Reponses.Add(new Carte { Visuel = () => V.Solide(ss, col, 160), Bonne = ss == bon, Dit = V.NomSolide(ss) }); }
            if (!ex.Reponses.Any(r => r.Bonne)) { var col = V.Un(V.Couleurs).C; ex.Reponses[0] = new Carte { Visuel = () => V.Solide(bon, col, 160), Bonne = true, Dit = V.NomSolide(bon) }; }
            return ex;
        }

        private static readonly (string Emoji, string Nom, string Solide)[] ObjetsSolides =
        {
            ("🎲", "le dé", "cube"), ("🎁", "le cadeau", "cube"), ("⚽", "le ballon", "boule"), ("🍊", "l'orange", "boule"), ("🌍", "le globe", "boule"),
            ("🥫", "la boîte de conserve", "cylindre"), ("🪵", "la bûche", "cylindre"), ("🥁", "le tambour", "cylindre"),
            ("🍦", "le cornet de glace", "cone"), ("🎉", "le chapeau de fête", "cone"), ("📦", "le carton", "pave"), ("🧱", "la brique", "pave"),
        };

        /// <summary>« Quelle forme a le dé ? » : un objet du quotidien, les solides en réponse.</summary>
        private static ExerciceChoix SolideObjet(int nSolides)
        {
            var sol = V.Solides.Take(nSolides).Select(s => s.Cle).ToList();
            var obj = V.Un(ObjetsSolides.Where(o => sol.Contains(o.Solide)).ToList());
            var ex = new ExerciceChoix { Consigne = "Quelle forme a " + obj.Nom + " ?", Scene = () => MesPremiersJeux.Lib.Art.Visual("", obj.Emoji, 260), Bravo = "Oui ! " + obj.Nom.Substring(0, 1).ToUpperInvariant() + obj.Nom.Substring(1) + ", c'est " + V.NomSolide(obj.Solide) + " !" };
            var col = V.Un(V.Couleurs).C;
            var choix = new List<string> { obj.Solide };
            foreach (var s in V.Melange(sol.Where(s => s != obj.Solide)).Take(2)) choix.Add(s);
            foreach (var s in V.Melange(choix)) { string ss = s; ex.Reponses.Add(new Carte { Visuel = () => V.Solide(ss, col, 160), Bonne = ss == obj.Solide, Dit = V.NomSolide(ss) }); }
            return ex;
        }

        private static ExerciceAttribution TrierSolidesPlats()
        {
            var ex = new ExerciceAttribution { Consigne = "Les solides vont dans la première boîte, les formes plates dans l'autre.", ConsigneObjet = "Où va {0} ?" };
            var gris = Color.FromRgb(0x8A, 0x7A, 0xA8);
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Solide("cube", gris, 100), "solides"), Dit = "la boîte des solides" });
            ex.Cibles.Add(new Carte { Visuel = () => V.Boite(V.Forme("carre", gris, 90), "formes plates"), Dit = "la boîte des formes plates" });
            foreach (var s in V.Melange(V.Solides.Take(4).Select(x => x.Cle).ToList()).Take(3)) { string ss = s; var col = V.Un(V.Couleurs).C; ex.Objets.Add(new Carte { Visuel = () => V.Solide(ss, col, 150), Cible = 0, Dit = V.NomSolide(ss) }); }
            foreach (var f in V.Melange(V.Formes.Take(4).ToList()).Take(3)) { string ff = f; var col = V.Un(V.Couleurs).C; ex.Objets.Add(new Carte { Visuel = () => V.Forme(ff, col, 150), Cible = 1, Dit = V.NomForme(ff) }); }
            ex.Objets = V.Melange(ex.Objets);
            return ex;
        }

        private static ExerciceChoix SolideOuiNon()
        {
            var sol = V.Solides.Select(s => s.Cle).ToList();
            string s = V.Un(sol), q = V.Rng.Next(2) == 0 ? s : V.Un(sol);
            var col = V.Un(V.Couleurs).C;
            return ChoixOuiNon("C'est " + V.NomSolide(q).Replace("le ", "un ").Replace("la ", "une ") + " ?", () => V.Solide(s, col, 240), s == q);
        }

        /// <summary>Le solide posé sur le sable : quelle forme laisse-t-il ?</summary>
        private static ExerciceChoix EmpreinteSolide()
        {
            string s = V.Un(V.Solides.Select(x => x.Cle).ToList()); string bon = V.Empreinte(s);
            var col = V.Un(V.Couleurs).C; var col2 = V.Un(V.Couleurs).C;
            var ex = new ExerciceChoix { Consigne = "On pose " + V.NomSolide(s) + " sur le sable. Quelle forme laisse-t-il ?", Scene = () => V.Solide(s, col, 240), Bravo = "Oui ! " + V.NomSolide(s).Substring(0, 1).ToUpperInvariant() + V.NomSolide(s).Substring(1) + " laisse " + V.NomForme(bon) + " !" };
            foreach (var f in V.Melange(V.Formes.Take(4).ToList()).Take(3).Union(new[] { bon }).Distinct().Take(3).ToList()) { string ff = f; ex.Reponses.Add(new Carte { Visuel = () => V.Forme(ff, col2, 150), Bonne = ff == bon, Dit = V.NomForme(ff) }); }
            if (!ex.Reponses.Any(r => r.Bonne)) ex.Reponses[0] = new Carte { Visuel = () => V.Forme(bon, col2, 150), Bonne = true, Dit = V.NomForme(bon) };
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        private static Competence Grandeurs() => C("formes.grandeurs", "Les grandeurs", "📏",
            P(1,
                A("a", "le grand ou le petit", () => GrandPetit(true)),
                A("b", "le plus petit", () => GrandPetit(false)),
                A("c", "long ou court", () => LongCourt())),
            P(2,
                A("a", "ranger trois longueurs", () => OrdonnerLongueurs(3)),
                A("b", "gros ou mince", () => GrosMince()),
                A("c", "ranger trois tailles", () => OrdonnerTailles(3)),
                A("d", "lourd ou léger", () => LourdLeger())),
            P(3,
                A("a", "ranger cinq tailles", () => OrdonnerTailles(5)),
                A("b", "ranger cinq longueurs", () => OrdonnerLongueurs(5)),
                A("c", "du plus léger au plus lourd", () => OrdonnerPoids())));

        private static ExerciceOrdre OrdonnerLongueurs(int n)
        {
            var col = V.Un(V.Couleurs).C;
            var ex = new ExerciceOrdre { Consigne = "Range les barres de la plus courte à la plus longue. Laquelle est la plus courte ?", ConsigneSuite = "Et après, la plus courte qui reste ?" };
            for (int i = 0; i < n; i++) { double len = 70 + i * (180.0 / Math.Max(1, n - 1)); ex.Cartes.Add(new Carte { Visuel = () => V.Barre(len, 40, col), Dit = "" }); }
            return ex;
        }

        private static readonly (string Emoji, string Nom, int Poids)[] Masses =
        {
            ("🪶", "la plume", 1), ("🦋", "le papillon", 1), ("🍃", "la feuille", 1), ("🎈", "le ballon", 2), ("🍎", "la pomme", 3), ("📕", "le livre", 4),
            ("🐱", "le chat", 5), ("🐕", "le chien", 6), ("🪨", "le rocher", 8), ("🚗", "la voiture", 9), ("🐘", "l'éléphant", 10),
        };

        private static ExerciceOrdre OrdonnerPoids()
        {
            var choix = new List<(string Emoji, string Nom, int Poids)>();
            foreach (var m in V.Melange(Masses)) if (!choix.Any(c => c.Poids == m.Poids)) { choix.Add(m); if (choix.Count == 3) break; }
            var ex = new ExerciceOrdre { Consigne = "Range du plus léger au plus lourd. Lequel est le plus léger ?", ConsigneSuite = "Et après, le plus léger qui reste ?" };
            foreach (var m in choix.OrderBy(c => c.Poids)) { var mm = m; ex.Cartes.Add(new Carte { Visuel = () => MesPremiersJeux.Lib.Art.Visual("", mm.Emoji, 150), Dit = mm.Nom }); }
            return ex;
        }

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

        private static Competence Suites() => C("formes.suites", "Les motifs", "📿",
            P(1,
                A("a", "alternance de deux couleurs", () => SuiteCouleurs(2, 2)),
                A("b", "alternance de deux formes", () => SuiteFormes(2)),
                A("c", "le collier bien rangé", () => BonCollier(2))),
            P(2,
                A("a", "motif de trois couleurs", () => SuiteCouleurs(3, 3)),
                A("b", "forme et couleur", () => SuiteFormeCouleur()),
                A("c", "motif AAB", () => SuiteMotif("AAB")),
                A("d", "le collier bien rangé (motif de trois)", () => BonCollier(3))),
            P(3,
                A("a", "motif ABC de formes", () => SuiteFormes(3)),
                A("b", "motif ABB ou AAB", () => SuiteMotif(V.Rng.Next(2) == 0 ? "ABB" : "AAB")),
                A("c", "le motif qui grandit", () => Evolutif()),
                A("d", "rangé comme le modèle", () => MemeStructure()),
                A("e", "suite de nombres", () => SuiteNombres())));

        /// <summary>Trois colliers : un seul suit le motif sans faute.</summary>
        private static ExerciceChoix BonCollier(int nCouleurs)
        {
            var cols = V.Melange(V.Couleurs).Take(nCouleurs).ToList(); var autre = V.Un(V.Couleurs.Where(c => cols.All(x => x.Nom != c.Nom)).ToList());
            int longueur = nCouleurs == 2 ? 6 : 6;
            List<Func<FrameworkElement>> Collier(int casse)
            {
                var items = new List<Func<FrameworkElement>>();
                for (int i = 0; i < longueur; i++) { var c = i == casse ? autre : cols[i % nCouleurs]; items.Add(() => V.Perle(c.C)); }
                return items;
            }
            string nomMotif = string.Join(", ", cols.Select(c => c.Nom));
            var ex = new ExerciceChoix { Consigne = "Quel collier est bien rangé : " + nomMotif + ", " + nomMotif + " ?", Taille = 360, Bravo = "Oui ! Celui-là suit le motif !" };
            ex.Reponses.Add(new Carte { Visuel = () => V.Collier(Collier(-1)), Bonne = true, Dit = "" });
            foreach (var k in V.Melange(Enumerable.Range(1, longueur - 1)).Take(2)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Collier(Collier(kk)), Bonne = false, Dit = "" }); }
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        /// <summary>Un motif évolutif : A B AA BB AAA BBB… que vient-il à la place du « ? »</summary>
        private static ExerciceChoix Evolutif()
        {
            var cols = V.Melange(V.Couleurs).Take(2).ToList();
            var seq = new List<int>();
            for (int k = 1; seq.Count < 12; k++) { for (int i = 0; i < k; i++) seq.Add(0); for (int i = 0; i < k; i++) seq.Add(1); }
            int longueur = V.Rng.Next(2) == 0 ? 9 : 10; seq = seq.Take(longueur).ToList(); int trou = longueur - 1;
            var items = seq.Select(s => new Func<FrameworkElement>(() => V.Perle(cols[s].C))).ToList();
            var ex = new ExerciceChoix { Consigne = "Le collier grandit : une, une, deux, deux, trois, trois… Quelle perle vient à la place du point d'interrogation ?", Scene = () => V.Rangee(items, trou, 64), Bravo = "Oui ! Le motif grandit à chaque fois !" };
            foreach (var c in V.Melange(V.Couleurs.Where(c => cols.Any(x => x.Nom == c.Nom) || V.Rng.Next(3) == 0).ToList()).Take(3).Union(cols).Distinct().Take(3).ToList())
            {
                var cc = c; ex.Reponses.Add(new Carte { Visuel = () => V.Perle(cc.C, 130), Bonne = cc.Nom == cols[seq[trou]].Nom, Dit = cc.Nom });
            }
            if (!ex.Reponses.Any(r => r.Bonne)) { var cc = cols[seq[trou]]; ex.Reponses[0] = new Carte { Visuel = () => V.Perle(cc.C, 130), Bonne = true, Dit = cc.Nom }; }
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        /// <summary>Le modèle est en perles de couleur ; les réponses sont des colliers de formes : lequel a la même structure ?</summary>
        private static ExerciceChoix MemeStructure()
        {
            var structures = new[] { "ABAB", "AABB", "ABB", "AAB", "ABC" };
            string bon = V.Un(structures);
            var cols = V.Melange(V.Couleurs).Take(3).ToList(); var fs = V.Melange(V.Formes.Take(4).ToList()).Take(3).ToList(); var col = V.Un(V.Couleurs).C;
            List<Func<FrameworkElement>> Perles(string s) { var l = new List<Func<FrameworkElement>>(); for (int i = 0; i < 8; i++) { int k = s[i % s.Length] - 'A'; l.Add(() => V.Perle(cols[k].C)); } return l; }
            List<Func<FrameworkElement>> FormesDe(string s) { var l = new List<Func<FrameworkElement>>(); for (int i = 0; i < 8; i++) { int k = s[i % s.Length] - 'A'; l.Add(() => V.Forme(fs[k], col, 60)); } return l; }
            var ex = new ExerciceChoix { Consigne = "Quel collier est rangé comme le modèle ?", Scene = () => V.Rangee(Perles(bon), -1, 80), Taille = 360, Bravo = "Oui ! C'est le même motif, avec des formes !" };
            ex.Reponses.Add(new Carte { Visuel = () => V.Collier(FormesDe(bon)), Bonne = true, Dit = "" });
            foreach (var s in V.Melange(structures.Where(s => s != bon).ToList()).Take(2)) { string ss = s; ex.Reponses.Add(new Carte { Visuel = () => V.Collier(FormesDe(ss)), Bonne = false, Dit = "" }); }
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

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

        private static ExerciceChoix SuiteNombres(int pasForce = 0)
        {
            int pas = pasForce > 0 ? pasForce : V.Rng.Next(2) == 0 ? 1 : 2; int start = V.Entre(1, 4);
            var items = Enumerable.Range(0, 5).Select(i => start + i * pas).ToList();
            int trou = V.Entre(2, 4);
            var ex = new ExerciceChoix { Consigne = "Quel nombre manque dans la suite ?", Scene = () => V.Rangee(items.Select(n => new Func<FrameworkElement>(() => V.Chiffre(n, 100))).ToList(), trou, 100) };
            int bon = items[trou];
            foreach (var k in V.Nombres(bon, 3, Math.Max(1, bon - 3), bon + 3, true)) { int kk = k; ex.Reponses.Add(new Carte { Visuel = () => V.Chiffre(kk), Bonne = kk == bon, Dit = kk.ToString() }); }
            return ex;
        }

        private static Competence Puzzles() => C("formes.puzzles", "Puzzles", "🧩",
            P(1,
                A("a", "deux pièces côte à côte", () => PieceManquante(2, 1, 2, "autre")),
                A("b", "deux pièces l'une sur l'autre", () => PieceManquante(1, 2, 2, "autre"))),
            P(2,
                A("a", "quatre pièces", () => PieceManquante(2, 2, 3, "meme")),
                A("b", "où va la pièce ?", () => OuVaLaPiece(2, 2, 2)),
                A("c", "six pièces", () => PieceManquante(3, 2, 3, "meme"))),
            P(3,
                A("a", "neuf pièces", () => PieceManquante(3, 3, 4, "meme")),
                A("b", "la pièce à l'endroit", () => PieceManquante(3, 3, 3, "tournee")),
                A("c", "où va la pièce ? (trois cases)", () => OuVaLaPiece(3, 3, 3))));

        /// <summary>La pièce qui manque, parmi des pièces d'une autre image, de la même image, ou la bonne pièce tournée.</summary>
        private static ExerciceChoix PieceManquante(int cols, int rows, int count, string distracteurs)
        {
            var src = V.Photo(); int n = cols * rows; int trou = V.Rng.Next(n);
            var ex = new ExerciceChoix { Consigne = "Quelle pièce manque au puzzle ?", Scene = () => V.Puzzle(src, cols, rows, new Dictionary<int, FrameworkElement> { [trou] = null }), Bravo = "Oui ! Le puzzle est fini !" };
            ex.Reponses.Add(new Carte { Visuel = () => V.Piece(src, cols, rows, trou), Bonne = true, Dit = "" });
            if (distracteurs == "autre")
            {
                var autre = V.Photo(src);
                foreach (var i in V.Melange(Enumerable.Range(0, n)).Take(count - 1)) { int k = i; ex.Reponses.Add(new Carte { Visuel = () => V.Piece(autre, cols, rows, k), Bonne = false }); }
            }
            else if (distracteurs == "tournee")
            {
                foreach (var rot in V.Melange(new[] { 90.0, 180.0, 270.0 }).Take(count - 1)) { double r = rot; ex.Reponses.Add(new Carte { Visuel = () => V.Piece(src, cols, rows, trou, 170, r), Bonne = false }); }
            }
            else
            {
                foreach (var i in V.Melange(Enumerable.Range(0, n).Where(i => i != trou)).Take(count - 1)) { int k = i; ex.Reponses.Add(new Carte { Visuel = () => V.Piece(src, cols, rows, k), Bonne = false }); }
            }
            ex.Reponses = V.Melange(ex.Reponses);
            return ex;
        }

        /// <summary>Où va la pièce ? Les trous portent une forme de couleur ; l'enfant regarde la forme de la bonne case.</summary>
        private static ExerciceChoix OuVaLaPiece(int cols, int rows, int nTrous)
        {
            var src = V.Photo(); int n = cols * rows;
            var trous = V.Melange(Enumerable.Range(0, n)).Take(nTrous).ToList(); int bon = trous[0];
            var marques = new[] { ("rond", V.Couleurs[0].C, "le rond rouge"), ("carre", V.Couleurs[1].C, "le carré bleu"), ("triangle", V.Couleurs[2].C, "le triangle jaune") };
            var attrib = new Dictionary<int, (string F, Color C, string Nom)>();
            for (int i = 0; i < trous.Count; i++) attrib[trous[i]] = marques[i];
            var ex = new ExerciceChoix
            {
                Consigne = "Où va cette pièce ? Regarde la forme de la bonne case.",
                Scene = () =>
                {
                    var dict = new Dictionary<int, FrameworkElement>();
                    foreach (var kv in attrib) dict[kv.Key] = V.Forme(kv.Value.F, kv.Value.C, 90);
                    return V.CoteACote(V.Piece(src, cols, rows, bon, 230), V.Puzzle(src, cols, rows, dict, 640));
                },
                Bravo = "Oui ! C'est là !",
            };
            foreach (var t in V.Melange(trous)) { var m = attrib[t]; ex.Reponses.Add(new Carte { Visuel = () => V.Forme(m.F, m.C, 150), Bonne = t == bon, Dit = m.Nom }); }
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
