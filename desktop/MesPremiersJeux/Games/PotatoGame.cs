using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🥔 « Patatou » — un bonhomme patate à habiller au regard. Il est au milieu, sur son estrade ; les
    /// familles de pièces sont à gauche (yeux, nez, bouche, chapeau, cou, main, pieds) et, sur la même page,
    /// les pièces de la famille ouverte sont à droite. Une pièce regardée s'envole de sa carte et vient se
    /// poser sur lui. Avec des yeux, un nez et une bouche, il prend vie : il suit l'enfant des yeux, parle,
    /// salue, danse. En bas : la surprise, la photo (rangée dans le dossier Patatou de la famille), tout
    /// enlever. Schéma corporel, choix et création, sans échec.
    ///
    /// Le jeu est dans PatateMoteur.cs, ses dessins dans PatateArt.cs (fabriqué par desktop/jeux-dessines) ;
    /// l'affichage, le regard, la voix et les bruits sont dans VecToile.cs.
    /// </summary>
    public sealed class PotatoGame : UserControl
    {
        private readonly VecToile _toile;

        public PotatoGame(Action celebrate)
        {
            string prenom = null;
            try { prenom = Settings.Load().ChildName; } catch { }
            var moteur = new PatateMoteur { Prenom = string.IsNullOrWhiteSpace(prenom) ? "toi" : prenom.Trim() };
            _toile = new VecToile(this, moteur, celebrate, nameof(PotatoGame));
            moteur.Demande = quoi => { if (quoi == "photo") GardeLaPhoto(); };
        }

        /// <summary>La photo de Patatou (lui et son jardin, sans les panneaux) va dans le dossier Patatou de la famille.</summary>
        private void GardeLaPhoto()
        {
            try
            {
                var image = _toile.Capture(190, 0, 846, 800);
                var dossier = Path.Combine(UserContent.RootDir, "Patatou");
                Directory.CreateDirectory(dossier);
                using (var f = File.Create(Path.Combine(dossier, "patatou-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png")))
                {
                    var png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(image));
                    png.Save(f);
                }
            }
            catch (Exception ex) { Log.Write("patatou", "Photo : " + ex.Message); }
        }
    }
}
