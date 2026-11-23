using System;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎣 « La pêche » — on est assis au bout du ponton, la canne à la main : le bouchon va sur l'eau là où
    /// l'enfant regarde. Quand il reste près d'un poisson, celui-ci s'approche, le rond autour du bouchon se
    /// remplit, et il mord : la canne plie, le poisson saute et retombe dans le seau. Cinq poissons : le seau
    /// est plein, on les remet à l'eau, et ils nagent plus vite (niveau mémorisé). Pas d'échec possible.
    /// À la souris ou au doigt, le bouchon suit le pointeur.
    ///
    /// Le jeu est dans PecheMoteur.cs, ses dessins dans PecheArt.cs (fabriqué par desktop/jeux-dessines) ;
    /// l'affichage, le regard, la voix et les bruits sont dans VecToile.cs.
    /// </summary>
    public sealed class FishingGame : UserControl
    {
        private readonly VecToile _toile;

        public FishingGame(Action celebrate)
        {
            var moteur = new PecheMoteur { Niveau = Adaptive.Level("peche") };
            _toile = new VecToile(this, moteur, celebrate, nameof(FishingGame));
            // le seau est plein : la manche suivante se joue au niveau que l'application a retenu
            _toile.ApresBravo = () => { Adaptive.Report("peche", true); moteur.Niveau = Adaptive.Level("peche"); };
        }
    }
}
