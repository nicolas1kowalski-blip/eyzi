using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MesPremiersJeux.Games;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views.Creer;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 🎨 LE MONDE CRÉER : l'endroit où rien n'est faux. Peindre au regard, coller
    /// des tampons, sculpter (tour de potier, statue cachée), fabriquer une musique,
    /// colorier, jouer d'un instrument, écouter ses musiques, et la galerie de tout
    /// ce que l'enfant a fait. Une activité à la fois, plein écran, retour par ⬅.
    /// </summary>
    public sealed class CreerView : UserControl
    {
        public event Action<string> Navigate; // coloring · music

        private readonly Grid _root = new Grid();
        private readonly Grid _menuRoot = new Grid(), _listRoot = new Grid { Visibility = Visibility.Collapsed }, _playRoot = new Grid { Visibility = Visibility.Collapsed };
        private readonly UniformGrid _cats = new UniformGrid { Columns = 4, Margin = new Thickness(16) };
        private readonly UniformGrid _list = new UniformGrid { Columns = 3, Margin = new Thickness(16, 4, 16, 16) };
        private readonly Grid _host = new Grid();
        private readonly Button _backBtn;
        private readonly Canvas _confetti = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private PeintureView _peinture; private CollageView _collage; private SculptureView _sculpture; private FabriqueView _fabrique; private GalerieView _galerie;

        public CreerView()
        {
            _root.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xE6));
            _root.Children.Add(DecorBackground.ForCreer());

            // Niveau 1 : les ateliers
            _menuRoot.Children.Add(_cats);
            Tuile("🖌️", "Peindre", Color.FromRgb(0xFF, 0xE0, 0xE6), "🌈  ✨  🫧", () => Ouvrir(_peinture ?? (_peinture = Brancher(new PeintureView(), v => v.BackRequested += Menu, v => v.Celebrate += Fete)), "peinture"));
            Tuile("🧩", "Collage", Color.FromRgb(0xE6, 0xF0, 0xFF), "🐱  🌳  ⭐", () => Ouvrir(_collage ?? (_collage = Brancher(new CollageView(), v => v.BackRequested += Menu, v => v.Celebrate += Fete)), "collage"));
            Tuile("🏺", "Sculpture", Color.FromRgb(0xFF, 0xEC, 0xD2), "🫙  🗿  🔥", () => Ouvrir(_sculpture ?? (_sculpture = Brancher(new SculptureView(), v => v.BackRequested += Menu, v => v.Celebrate += Fete)), "sculpture"));
            Tuile("🎼", "Fabrique de musique", Color.FromRgb(0xEE, 0xE4, 0xFF), "🥁  🎹  🎸", () => Ouvrir(Fabrique(), "fabrique"));
            Tuile("🎨", "Coloriage", Color.FromRgb(0xFF, 0xE6, 0xF4), "🖍️  🎨  🖼️", () => Navigate?.Invoke("coloring"));
            Tuile("🎸", "Instruments", Color.FromRgb(0xFF, 0xF3, 0xD6), "🥁  🎹  🎸", OuvrirInstruments);
            Tuile("🎧", "Mes musiques", Color.FromRgb(0xDE, 0xF7, 0xEA), "🎶  🎧  🎤", () => Navigate?.Invoke("music"));
            Tuile("🖼️", "Ma galerie", Color.FromRgb(0xFF, 0xF8, 0xE0), "🖌️  🏺  🎼", () => { var g = Galerie(); g.Ouvrir(); Ouvrir(g, "galerie"); });
            _root.Children.Add(_menuRoot);

            // Niveau 2 : les instruments (l'école de musique)
            _listRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _listRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var haut = new Grid();
            var themes = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 10, 0, 2) };
            themes.Click += (s, e) => Menu();
            haut.Children.Add(themes);
            haut.Children.Add(new TextBlock { Text = "🎸  Les instruments", FontSize = 36, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x2E, 0x1F)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 12, 0, 2) });
            _listRoot.Children.Add(haut);
            Grid.SetRow(_list, 1);
            _listRoot.Children.Add(_list);
            foreach (var (icon, label, act) in new[] { ("🥁", "Le petit orchestre", 1), ("🎹", "Le xylophone et la chanson", 3), ("🎸", "Je joue d'un instrument", 5), ("🎸", "La guitare et ses chansons", 7), ("🎹", "Le piano et ses chansons", 10) })
            {
                var tile = MenuKit.GameTile(icon, label);
                int a = act;
                tile.Click += (s, e) => Jouer(c => new MusicSchoolGame(c, a));
                _list.Children.Add(tile);
            }
            _root.Children.Add(_listRoot);

            // Niveau 3 : l'atelier ou le jeu en plein écran
            _playRoot.Children.Add(_host);
            _backBtn = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Menu", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
            _backBtn.Click += (s, e) => { _host.Children.Clear(); _playRoot.Visibility = Visibility.Collapsed; _listRoot.Visibility = Visibility.Visible; Chrome.Immersive = false; };
            _playRoot.Children.Add(_backBtn);
            _root.Children.Add(_playRoot);
            _root.Children.Add(_confetti);
            Content = _root;
        }

        private static T Brancher<T>(T v, params Action<T>[] liens) { foreach (var l in liens) l(v); return v; }

        private FabriqueView Fabrique() => _fabrique ?? (_fabrique = Brancher(new FabriqueView(), v => v.BackRequested += Menu, v => v.Celebrate += Fete));
        private GalerieView Galerie() => _galerie ?? (_galerie = Brancher(new GalerieView(), v => v.BackRequested += Menu, v => v.Celebrate += Fete,
            v => v.MusiqueRequested += texte => { var f = Fabrique(); Ouvrir(f, "fabrique"); f.Charger(texte); }));

        private void Tuile(string icon, string name, Color tint, string minis, Action action)
        {
            var t = MenuKit.ThemeTile(icon, name, tint, minis);
            t.Click += (s, e) =>
            {
                // Un atelier qui ne s'ouvre pas le DIT (et l'écrit au journal) au lieu de rester muet.
                try { action(); }
                catch (Exception ex) { Log.Write("creer", "« " + name + " » ne s'ouvre pas : " + ex); Menu(); Speech.Say("Oh, cet atelier ne veut pas s'ouvrir. On le dira à un grand."); }
            };
            _cats.Children.Add(t);
        }

        /// <summary>Revient à la porte du monde : les ateliers.</summary>
        public void ShowRoot() { Menu(); }

        private void Menu()
        {
            if (_playRoot.Visibility == Visibility.Visible) _host.Children.Clear();
            _playRoot.Visibility = Visibility.Collapsed;
            _listRoot.Visibility = Visibility.Collapsed;
            _menuRoot.Visibility = Visibility.Visible;
            Chrome.Immersive = false;
            Speech.ScreenChanged();
        }

        private void OuvrirInstruments()
        {
            _menuRoot.Visibility = Visibility.Collapsed;
            _listRoot.Visibility = Visibility.Visible;
            Speech.ScreenChanged();
            Speech.Say("Les instruments !");
        }

        /// <summary>Un atelier (sa propre flèche de retour) en plein écran.</summary>
        private void Ouvrir(UserControl atelier, string nom)
        {
            _host.Children.Clear();
            _host.Children.Add(atelier);
            _backBtn.Visibility = Visibility.Collapsed;
            _menuRoot.Visibility = Visibility.Collapsed;
            _listRoot.Visibility = Visibility.Collapsed;
            _playRoot.Visibility = Visibility.Visible;
            Chrome.Immersive = true;
            Speech.ScreenChanged();
            Usage.Count("creer:" + nom + ":ouvert");
        }

        /// <summary>Un jeu de l'école de musique, avec la flèche « Menu » du monde.</summary>
        private void Jouer(Func<Action, UserControl> make)
        {
            try
            {
                _host.Children.Clear();
                _host.Children.Add(make(() => CelebrationKit.Play(_confetti)));
                _backBtn.Visibility = Visibility.Visible;
                _menuRoot.Visibility = Visibility.Collapsed;
                _listRoot.Visibility = Visibility.Collapsed;
                _playRoot.Visibility = Visibility.Visible;
                Chrome.Immersive = true;
            }
            catch (Exception ex) { MessageBox.Show("Impossible d'ouvrir cette activité :\n" + ex.Message, "Eyzi"); }
        }

        private void Fete(Canvas local)
        {
            // La fête sur la couche du monde (toujours de la bonne taille), pas sur la toile de l'atelier.
            CelebrationKit.Play(_confetti);
        }
    }
}
