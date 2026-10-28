using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 💬 Le monde « Parler » : le tableau de communication en pleine page (le
    /// même que la tuile Parler, mais comme un lieu où l'on va, pas une fenêtre
    /// qui s'ouvre par-dessus). Il s'enrichira : messages des proches, photos
    /// de famille, tableaux par moment de la journée, prédiction.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        public event Action BackRequested;

        public ParlerView()
        {
            var root = new Grid();
            root.Background = new LinearGradientBrush(Color.FromRgb(0xF1, 0xE6, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90);
            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            root.Children.Add(_board);
            Content = root;
        }

        /// <summary>On entre dans le monde : la grille s'annonce.</summary>
        public void Enter() { try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); } }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }
    }
}
