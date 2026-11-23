using System;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// « Le jet d'eau » — cause à effet au regard pur, sans échec possible. L'enfant tient la lance :
    /// elle est au premier plan, en bas de l'écran, et l'eau part vers l'endroit qu'il regarde, tant
    /// qu'il regarde l'écran. À la souris ou au doigt, l'eau suit le pointeur.
    ///
    /// Le jeu est dans JetMoteur.cs et JetScenes.cs, ses dessins dans JetArt.cs (fabriqué par
    /// desktop/jeux-dessines) ; l'affichage, le regard, la voix et les bruits sont dans VecToile.cs.
    /// </summary>
    public sealed class WaterJetGame : UserControl
    {
        private readonly VecToile _toile;

        public WaterJetGame(Action celebrate)
        {
            _toile = new VecToile(this, new JetMoteur(), celebrate, nameof(WaterJetGame), SoundFx.WaterLoopFile);
        }
    }
}
