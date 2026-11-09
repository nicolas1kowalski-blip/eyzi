using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🖼️ MA GALERIE : tout ce que l'enfant a créé, le plus récent d'abord, avec le
    /// jour. Un regard ouvre l'œuvre en grand ; « C'est moi qui l'ai fait ! » la
    /// montre avec fierté ; une musique se rejoue dans la fabrique. Le parent
    /// (mode admin) peut en retirer une.
    /// </summary>
    public sealed class GalerieView : UserControl
    {
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<string> MusiqueRequested; // le texte d'une musique gardée
        public event Action<Canvas> Celebrate;

        private readonly Grid _menuRoot = new Grid(), _grandRoot = new Grid { Visibility = Visibility.Collapsed };
        private readonly PagedMenu _menu = new PagedMenu(4, 2);
        private readonly Border _cadre;
        private readonly TextBlock _titre, _vide;
        private readonly StackPanel _boutons;
        private readonly Canvas _fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private Creations.Oeuvre _courante;

        public GalerieView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xEE)) };
            // ----- la galerie
            _menuRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _menuRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var haut = new Grid { Margin = new Thickness(16, 8, 16, 0) };
            var back = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left };
            back.Click += (s, e) => BackRequested?.Invoke();
            haut.Children.Add(back);
            haut.Children.Add(new TextBlock { Text = "🖼️  Ma galerie", FontSize = 34, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            _menuRoot.Children.Add(haut);
            Grid.SetRow(_menu, 1);
            _menuRoot.Children.Add(_menu);
            _vide = new TextBlock { Text = "Ta galerie est vide pour l'instant.\nPeins, colle, sculpte ou fais une musique, puis garde-la avec 💾 !", FontSize = 28, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.6, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 900, Visibility = Visibility.Collapsed };
            Grid.SetRow(_vide, 1);
            _menuRoot.Children.Add(_vide);
            root.Children.Add(_menuRoot);

            // ----- l'œuvre en grand
            _grandRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _grandRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _grandRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _titre = new TextBlock { FontSize = 34, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 6) };
            _grandRoot.Children.Add(_titre);
            _cadre = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(18), Padding = new Thickness(18), Margin = new Thickness(60, 0, 60, 10), BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xC8, 0x9A)), BorderThickness = new Thickness(10), Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 30, ShadowDepth = 6, Opacity = 0.2 } };
            Grid.SetRow(_cadre, 1);
            _grandRoot.Children.Add(_cadre);
            _boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
            Grid.SetRow(_boutons, 2);
            _grandRoot.Children.Add(_boutons);
            root.Children.Add(_grandRoot);
            root.Children.Add(_fx);
            Content = root;

            Creations.Changed += () => Dispatcher.BeginInvoke(new Action(Rafraichir));
            AdminMode.Changed += () => Dispatcher.BeginInvoke(new Action(() => { if (_courante != null) MontrerBoutons(); }));
        }

        public void Ouvrir() { _grandRoot.Visibility = Visibility.Collapsed; _menuRoot.Visibility = Visibility.Visible; Rafraichir(); }

        private void Rafraichir()
        {
            var oeuvres = Creations.Toutes();
            _vide.Visibility = oeuvres.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            var tiles = new List<(UIElement Content, Action OnClick)>();
            foreach (var o in oeuvres)
            {
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Width = 300 };
                content.Children.Add(new Border { Width = 300, Height = 200, CornerRadius = new CornerRadius(18), Background = Brushes.White, ClipToBounds = true, Child = Vignette(o, 300) });
                content.Children.Add(new TextBlock { Text = o.Titre, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });
                content.Children.Add(new TextBlock { Text = Creations.Quand(o.Quand), FontSize = 17, Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x5B, 0x8A)), HorizontalAlignment = HorizontalAlignment.Center });
                var cap = o;
                tiles.Add((content, () => Montrer(cap)));
            }
            _menu.SetTiles(tiles);
        }

        private static FrameworkElement Vignette(Creations.Oeuvre o, double size)
        {
            if (o.EstMusique) return new Viewbox { Margin = new Thickness(30), Child = Art.ForEmoji("🎼", 120) };
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.DecodePixelWidth = (int)(size * 2); bmp.UriSource = new Uri(o.Path); bmp.EndInit(); bmp.Freeze();
                return new Image { Source = bmp, Stretch = Stretch.Uniform, Margin = new Thickness(6) };
            }
            catch { return new Viewbox { Margin = new Thickness(30), Child = Art.ForEmoji("🖼️", 120) }; }
        }

        private void Montrer(Creations.Oeuvre o)
        {
            _courante = o;
            _titre.Text = o.Titre + "  ·  " + Creations.Quand(o.Quand);
            Emoji.Fix(_titre);
            if (o.EstMusique)
            {
                _cadre.Child = new Viewbox { Child = Art.ForEmoji("🎼", 300) };
            }
            else
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.UriSource = new Uri(o.Path); bmp.EndInit(); bmp.Freeze();
                    _cadre.Child = new Image { Source = bmp, Stretch = Stretch.Uniform };
                }
                catch { _cadre.Child = new Viewbox { Child = Art.ForEmoji("🖼️", 300) }; }
            }
            MontrerBoutons();
            _menuRoot.Visibility = Visibility.Collapsed;
            _grandRoot.Visibility = Visibility.Visible;
            Speech.Say(o.Titre + ", " + Creations.Quand(o.Quand) + ".");
        }

        private void MontrerBoutons()
        {
            _boutons.Children.Clear();
            _boutons.Children.Add(Bouton("⬅", "Galerie", () => { _grandRoot.Visibility = Visibility.Collapsed; _menuRoot.Visibility = Visibility.Visible; }));
            _boutons.Children.Add(Bouton("🙋", "C'est moi qui l'ai fait !", () => { Speech.Say("C'est moi qui l'ai fait ! Regarde comme c'est beau !"); Celebrate?.Invoke(_fx); }));
            if (_courante != null && _courante.EstMusique)
                _boutons.Children.Add(Bouton("▶️", "Écouter", () => { try { MusiqueRequested?.Invoke(File.ReadAllText(_courante.Path)); } catch { } }));
            if (AdminMode.IsActive)
                _boutons.Children.Add(Bouton("🗑", "Retirer", () =>
                {
                    GazeGate.Push();
                    var r = MessageBox.Show(Window.GetWindow(this), "Retirer cette œuvre de la galerie ?", "Galerie", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    GazeGate.Pop();
                    if (r != MessageBoxResult.Yes) return;
                    Creations.Supprimer(_courante);
                    _grandRoot.Visibility = Visibility.Collapsed; _menuRoot.Visibility = Visibility.Visible;
                }));
        }

        private static Button Bouton(string emoji, string label, Action action)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["BackButton"], Height = 96, MinWidth = 180, Margin = new Thickness(8, 0, 8, 0),
                Content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Children = { new Viewbox { Height = 44, Child = Art.ForEmoji(emoji, 44) }, new TextBlock { Text = label, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } } },
            };
            b.Click += (s, e) => action();
            return b;
        }
    }
}
