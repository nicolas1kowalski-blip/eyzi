using System;
using System.Collections.Generic;
using System.Windows;

namespace MesPremiersJeux.Learning
{
    /// <summary>L'état d'une compétence à un palier (voir le référentiel, section 9).</summary>
    public enum Etat { NonCommence, EnCours, EnValidation, Acquis, AConsolider }

    /// <summary>
    /// Un AXE : une façon de prouver une compétence (un exercice différent qui
    /// vérifie la même chose). Le générateur fabrique un exercice neuf à chaque appel.
    /// </summary>
    public sealed class Axe
    {
        public string Id;          // « a », « b »…
        public string Nom;         // « compter et choisir le chiffre »
        public Func<Exercice> Make;
    }

    /// <summary>Un palier d'une compétence (≈ PS, MS, GS), avec ses axes.</summary>
    public sealed class Palier
    {
        public int Numero;         // 1, 2, 3
        public string Ecole;       // « PS », « MS », « GS »
        public List<Axe> Axes = new List<Axe>();
    }

    /// <summary>Une compétence du référentiel.</summary>
    public sealed class Competence
    {
        public string Id;          // « nombres.denombrer »
        public string Nom;         // « Dénombrer »
        public string Icone;       // emoji
        public List<Palier> Paliers = new List<Palier>();

        public Palier PalierN(int n) => Paliers.Find(p => p.Numero == n);
    }

    /// <summary>Un nœud du chemin : une compétence à un palier.</summary>
    public sealed class Noeud
    {
        public Competence Competence;
        public int Palier;
        public Palier Def => Competence.PalierN(Palier);
        public string Key => Competence.Id + "|" + Palier;
        public string Titre => Competence.Nom + " · " + Def.Ecole;
    }

    /// <summary>Un chemin : un domaine, ses nœuds dans l'ordre.</summary>
    public sealed class Chemin
    {
        public string Id;          // « nombres »
        public string Nom;         // « Les nombres »
        public string Icone;
        public System.Windows.Media.Color Couleur;
        public List<Noeud> Noeuds = new List<Noeud>();
    }

    // ----------------------------------------------------------------------
    //  LES EXERCICES : trois formes d'écran couvrent les sept moteurs.
    //   • Choix : une scène (facultative) + des réponses — choisir, oui/non,
    //     compter, compléter, apparier un à un.
    //   • Attribution : des objets présentés un par un + des cibles — trier,
    //     apparier (plusieurs paires).
    //   • Ordre : des cartes à remettre dans l'ordre — ordonner.
    // ----------------------------------------------------------------------

    /// <summary>Une réponse possible (ou un objet, ou une carte) : un visuel neuf à chaque appel.</summary>
    public sealed class Carte
    {
        public Func<FrameworkElement> Visuel;
        public string Dit;         // ce que la voix dit quand l'enfant la choisit (facultatif)
        public bool Bonne;         // pour le choix
        public int Cible;          // pour l'attribution : l'index de la bonne cible
    }

    public abstract class Exercice
    {
        /// <summary>La consigne dite à voix haute (et redite par 🔊).</summary>
        public string Consigne;
        /// <summary>Le modèle montré dans la bulle de consigne (facultatif).</summary>
        public Func<FrameworkElement> Modele;
        /// <summary>Ce que dit la voix quand c'est réussi (facultatif, sinon un bravo).</summary>
        public string Bravo;
    }

    /// <summary>Choisir : une scène facultative et 2 à 6 réponses dont une bonne.</summary>
    public sealed class ExerciceChoix : Exercice
    {
        public Func<FrameworkElement> Scene;
        public List<Carte> Reponses = new List<Carte>();
        /// <summary>Taille des boutons de réponse (défaut selon le nombre).</summary>
        public double Taille;
    }

    /// <summary>Attribuer : chaque objet, présenté un par un, va dans une cible (boîte, paire…).</summary>
    public sealed class ExerciceAttribution : Exercice
    {
        public List<Carte> Cibles = new List<Carte>();   // Dit = le nom de la boîte
        public List<Carte> Objets = new List<Carte>();   // Cible = index de la bonne cible
        /// <summary>Consigne par objet : « Où va {0} ? » ({0} = Dit de l'objet).</summary>
        public string ConsigneObjet = "Où va {0} ?";
    }

    /// <summary>Ordonner : les cartes, données dans le BON ordre, sont mélangées à l'écran.</summary>
    public sealed class ExerciceOrdre : Exercice
    {
        public List<Carte> Cartes = new List<Carte>();
        /// <summary>Ce que dit la voix pour demander la prochaine carte : « Et après ? »</summary>
        public string ConsigneSuite = "Et ensuite ?";
    }
}
