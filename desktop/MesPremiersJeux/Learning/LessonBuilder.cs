using System;
using System.Collections.Generic;
using System.Linq;

namespace MesPremiersJeux.Learning
{
    public enum Genre { Noeud, Revision, Facile, Defi }

    /// <summary>Un pas de leçon : un exercice, d'où il vient, et pourquoi.</summary>
    public sealed class Pas
    {
        public Exercice Exercice;
        public Noeud Noeud;
        public string Axe;
        public Genre Genre;
        public bool Reussi;
    }

    /// <summary>Une leçon : 6 à 10 pas, ou un défi de 6.</summary>
    public sealed class Lecon
    {
        public Noeud Noeud;
        public bool EstDefi;
        public List<Pas> Pas = new List<Pas>();
    }

    /// <summary>
    /// COMPOSE UNE LEÇON à la Duolingo : ~60 % sur le nœud en cours (axes
    /// alternés, les axes jamais vus d'abord), ~20 % de révisions dues,
    /// ~20 % de choses déjà réussies pour finir sur une réussite.
    /// Si le nœud est prêt (tous les axes validés), la leçon est le DÉFI :
    /// 6 exercices mélangés, sans aide.
    /// </summary>
    public static class LessonBuilder
    {
        private static readonly Random Rng = new Random();

        public static Lecon Composer(Chemin chemin, Noeud noeud, int taille = 8)
        {
            var lecon = new Lecon { Noeud = noeud };
            var axes = noeud.Def.Axes;

            if (Acquisition.PretPourDefi(noeud))
            {
                lecon.EstDefi = true;
                var ordre = new List<Axe>();
                while (ordre.Count < Acquisition.DefiExercices) ordre.AddRange(Melange(axes));
                foreach (var a in ordre.Take(Acquisition.DefiExercices))
                    lecon.Pas.Add(new Pas { Exercice = a.Make(), Noeud = noeud, Axe = a.Id, Genre = Genre.Defi });
                return lecon;
            }

            int nNoeud = Math.Max(3, (int)Math.Round(taille * 0.6));
            int nRev = Math.Max(0, (int)Math.Round(taille * 0.2));
            int nFacile = Math.Max(1, taille - nNoeud - nRev);

            // Le nœud : les axes jamais vus en premier, puis en alternance.
            var jamaisVus = axes.Where(a => !Acquisition.AxeVu(noeud, a.Id)).ToList();
            var file = new List<Axe>(Melange(jamaisVus));
            while (file.Count < nNoeud) file.AddRange(Melange(axes));
            var pasNoeud = file.Take(nNoeud).Select(a => new Pas { Exercice = a.Make(), Noeud = noeud, Axe = a.Id, Genre = Genre.Noeud }).ToList();

            // Les révisions dues, dans tous les chemins.
            var dues = Catalogue.Chemins.SelectMany(c => c.Noeuds).Where(n => n.Key != noeud.Key && Acquisition.RevisionDue(n)).ToList();
            var pasRev = Melange(dues).Take(nRev).Select(n =>
            {
                var a = Aleatoire(n.Def.Axes);
                return new Pas { Exercice = a.Make(), Noeud = n, Axe = a.Id, Genre = Genre.Revision };
            }).ToList();

            // Les réussites faciles : des nœuds acquis (sinon le premier palier de la compétence, ou le nœud lui-même).
            var acquis = chemin.Noeuds.Where(n => n.Key != noeud.Key && Acquisition.EstAcquis(n)).ToList();
            var pasFacile = new List<Pas>();
            for (int i = 0; i < nFacile + (nRev - pasRev.Count); i++)
            {
                Noeud src = acquis.Count > 0 ? Aleatoire(acquis)
                    : noeud.Palier > 1 ? new Noeud { Competence = noeud.Competence, Palier = 1 } : noeud;
                var a = Aleatoire(src.Def.Axes);
                pasFacile.Add(new Pas { Exercice = a.Make(), Noeud = src, Axe = a.Id, Genre = src.Key == noeud.Key ? Genre.Noeud : Genre.Facile });
            }

            // L'ordre : on commence par le nœud, on glisse les révisions au milieu, on finit facile.
            var corps = new List<Pas>(pasNoeud);
            foreach (var r in pasRev) corps.Insert(Math.Min(corps.Count, 2 + Rng.Next(Math.Max(1, corps.Count - 1))), r);
            if (pasFacile.Count > 1) corps.Insert(Math.Min(corps.Count, corps.Count / 2), pasFacile[0]);
            lecon.Pas.AddRange(corps);
            lecon.Pas.AddRange(pasFacile.Skip(pasFacile.Count > 1 ? 1 : 0));
            return lecon;
        }

        /// <summary>Le nœud « du moment » d'un chemin : le premier ouvert qui n'est pas acquis
        /// (une révision due passe avant), sinon le dernier.</summary>
        public static Noeud NoeudDuMoment(Chemin chemin)
        {
            var due = chemin.Noeuds.FirstOrDefault(Acquisition.RevisionDue);
            if (due != null) return due;
            return chemin.Noeuds.FirstOrDefault(n => Acquisition.Ouvert(n) && !Acquisition.EstAcquis(n)) ?? chemin.Noeuds.Last();
        }

        private static T Aleatoire<T>(IList<T> l) => l[Rng.Next(l.Count)];
        private static List<T> Melange<T>(IEnumerable<T> src) => src.OrderBy(_ => Rng.Next()).ToList();
    }
}
