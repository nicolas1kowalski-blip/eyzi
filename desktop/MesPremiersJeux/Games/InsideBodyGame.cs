using System;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🧠 « Dans mon corps » — un bonhomme dont on voit l'intérieur attend ses organes : le cerveau, les
    /// poumons, le cœur, l'estomac, le foie, les intestins, la vessie. La voix en nomme un, l'enfant le remet
    /// à sa place ; une fois posé, l'organe se met à vivre et la voix dit à quoi il sert. Trois niveaux,
    /// mémorisés : un seul endroit brille ; trois endroits au choix ; reconnaître l'organe parmi trois.
    ///
    /// Le jeu est dans CorpsMoteur.cs, ses dessins dans CorpsArt.cs (fabriqué par desktop/jeux-dessines) ;
    /// l'affichage, le regard, la voix et les bruits sont dans VecToile.cs.
    /// </summary>
    public sealed class InsideBodyGame : UserControl
    {
        private readonly VecToile _toile;

        public InsideBodyGame(Action celebrate)
        {
            var moteur = new CorpsMoteur { Niveau = Adaptive.Level("organes") };
            _toile = new VecToile(this, moteur, celebrate, nameof(InsideBodyGame));
            // le corps est complet : la manche suivante se joue au niveau que l'application a retenu
            _toile.ApresBravo = () => { Adaptive.Report("organes", moteur.MancheReussie); moteur.Niveau = Adaptive.Level("organes"); };
        }
    }
}
