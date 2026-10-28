using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🖼 La galerie de choix au regard, partagée (miroir magique, Patatou…) :
    /// par-dessus le jeu, des vignettes en grandes images, six par page avec
    /// des flèches, « Rien » pour enlever, « Retour » pour ne rien changer.
    /// </summary>
    public sealed class PropGallery : Grid
    {
        public sealed class Item
        {
            public string Name;      // dit à voix haute et écrit sous la vignette
            public string Image;     // nom d'image de la banque (Art.Find), ou null
            public UIElement Visual; // sinon un visuel prêt (dessin)
        }

        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xFF, 0xD2, 0x4D));
        private readonly PagedMenu _menu;
        private readonly TextBlock _title;

        public PropGallery()
        {
            Visibility = Visibility.Collapsed;
            Background = new SolidColorBrush(Color.FromArgb(0xB0, 0x20, 0x10, 0x40));
            var card = new Border
            {
                Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0xF8, 0xEE), Color.FromRgb(0xF3, 0xEA, 0xFF), 90),
                CornerRadius = new CornerRadius(32), Margin = new Thickness(30, 10, 30, 10), Padding = new Thickness(14),
            };
            var inner = new Grid();
            inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var head = new DockPanel { Margin = new Thickness(10, 0, 10, 6) };
            var back = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "↩  Retour", HorizontalAlignment = HorizontalAlignment.Right };
            back.Click += (s, e) => Close();
            DockPanel.SetDock(back, Dock.Right);
            head.Children.Add(back);
            _title = new TextBlock { FontSize = 32, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)), VerticalAlignment = VerticalAlignment.Center };
            head.Children.Add(_title);
            inner.Children.Add(head);
            _menu = new PagedMenu(3, 2);
            Grid.SetRow(_menu, 1);
            inner.Children.Add(_menu);
            card.Child = inner;
            Children.Add(card);
        }

        /// <summary>Ouvre la galerie ; « items[0] » peut être null = « Rien ». Le choix appelle « pick » puis referme.</summary>
        public void Open(string what, IList<Item> items, int current, Action<int> pick)
        {
            var tiles = new List<(UIElement Content, Action OnClick)>();
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                tiles.Add((Thumb(items[i], i == current), () =>
                {
                    Close();
                    pick(idx);
                    var it = items[idx];
                    Speech.Say(it == null ? "On enlève !" : it.Name + " !");
                }));
            }
            _menu.SetTiles(tiles);
            _title.Text = "Choisis " + what;
            Visibility = Visibility.Visible;
            Speech.Say("Choisis " + what + " !");
        }

        public void Close() => Visibility = Visibility.Collapsed;

        private static UIElement Thumb(Item it, bool current)
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            UIElement visual;
            if (it == null)
            {
                var c = new Canvas { Width = 150, Height = 150 };
                var ring = new Ellipse { Width = 110, Height = 110, Stroke = new SolidColorBrush(Color.FromRgb(0xC9, 0xB8, 0xE8)), StrokeThickness = 10 };
                Canvas.SetLeft(ring, 20); Canvas.SetTop(ring, 20); c.Children.Add(ring);
                c.Children.Add(new Line { X1 = 36, Y1 = 36, X2 = 114, Y2 = 114, Stroke = new SolidColorBrush(Color.FromRgb(0xC9, 0xB8, 0xE8)), StrokeThickness = 10 });
                visual = c;
            }
            else if (it.Image != null)
                visual = new Image { Source = Art.Find(it.Image), Width = 230, Height = 150, Stretch = Stretch.Uniform };
            else
                visual = new Viewbox { Width = 230, Height = 150, Child = it.Visual ?? new TextBlock { Text = "?" }, Stretch = Stretch.Uniform };
            sp.Children.Add(visual);
            sp.Children.Add(new TextBlock
            {
                Text = it == null ? "Rien" : it.Name, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Ink,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 240, Margin = new Thickness(0, 6, 0, 0),
            });
            return new Border
            {
                Child = sp, Padding = new Thickness(8), CornerRadius = new CornerRadius(20),
                BorderBrush = current ? Gold : Brushes.Transparent, BorderThickness = new Thickness(5),
            };
        }
    }
}
