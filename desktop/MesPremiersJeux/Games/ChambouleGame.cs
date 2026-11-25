using System;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎪 « Chamboule-tout » — on est devant le stand de la fête foraine : les boîtes de conserve sont empilées
    /// sur la table du fond. L'enfant en regarde une, le rond se remplit autour d'elle, et la balle part du
    /// comptoir. La boîte touchée s'envole, celles qu'elle portait dégringolent. Tout est tombé : on gagne une
    /// peluche, et de nouvelles boîtes arrivent, plus nombreuses et plus petites d'un niveau à l'autre (niveau
    /// mémorisé). Pas d'échec possible : la balle touche toujours la boîte regardée. À la souris ou au doigt,
    /// on vise avec le pointeur.
    ///
    /// Le jeu est dans ChambouleMoteur.cs, ses dessins dans ChambouleArt.cs (fabriqué par desktop/jeux-dessines) ;
    /// l'affichage, le regard, la voix et les bruits sont dans VecToile.cs.
    /// </summary>
    public sealed class ChambouleGame : UserControl
    {
        private readonly VecToile _toile;

        public ChambouleGame(Action celebrate)
        {
            var moteur = new ChambouleMoteur { Niveau = Adaptive.Level("chamboule") };
            _toile = new VecToile(this, moteur, celebrate, nameof(ChambouleGame));
            // tout est tombé : la manche suivante se joue au niveau que l'application a retenu
            _toile.ApresBravo = () => { Adaptive.Report("chamboule", moteur.MancheReussie); moteur.Niveau = Adaptive.Level("chamboule"); };
        }
    }
}
