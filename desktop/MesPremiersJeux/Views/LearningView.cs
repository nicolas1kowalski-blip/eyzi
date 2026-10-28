using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MesPremiersJeux.Games;
using MesPremiersJeux.Learning;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 🧭 MES LEÇONS : le chemin d'un domaine (ses nœuds gris, colorés, avec
    /// jauge, dorés), le bouton « Ma leçon », et la leçon elle-même.
    /// </summary>
    public sealed class LearningView : UserControl
    {
        private readonly Action _celebrate;
        private readonly Grid _root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xF7, 0xFF)) };
        private readonly Grid _pathRoot = new Grid();
        private readonly Grid _lessonHost = new Grid { Visibility = Visibility.Collapsed };
        private readonly StackPanel _tabs = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        private readonly UniformGrid _nodes = new UniformGrid { Margin = new Thickness(16, 4, 16, 16) };
        private readonly TextBlock _titre = new TextBlock { FontSize = 34, FontWeight = FontWeights.Bold, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) };
        private Chemin _chemin;
        private Noeud _choisi;

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x1F, 0x3A, 0x5A));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));

        public LearningView(Action celebrate)
        {
            _celebrate = celebrate;
            Content = _root;
            _root.Children.Add(_pathRoot);
            _root.Children.Add(_lessonHost);

            _pathRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _pathRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new DockPanel { Margin = new Thickness(150, 10, 16, 0), LastChildFill = true };
            var go = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 300, Height = 110, Margin = new Thickness(12, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xF5, 0xE0)),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", "▶️", 60), new TextBlock { Text = "Ma leçon", FontSize = 34, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
                },
            };
            go.Click += (s, e) => Demarrer(LessonBuilder.NoeudDuMoment(_chemin));
            DockPanel.SetDock(go, Dock.Right);
            header.Children.Add(go);
            foreach (var ch in Catalogue.Chemins)
            {
                var cap = ch;
                var tab = new Button
                {
                    Style = (Style)Application.Current.Resources["AnswerButton"],
                    Width = 250, Height = 100, Margin = new Thickness(0, 0, 12, 0), Tag = ch.Id,
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                        Children = { Art.Visual("", ch.Icone, 54), new TextBlock { Text = ch.Nom, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 150 } },
                    },
                };
                tab.Click += (s, e) => Montrer(cap);
                _tabs.Children.Add(tab);
            }
            header.Children.Add(_tabs);
            _pathRoot.Children.Add(header);
            Grid.SetRow(_nodes, 1);
            _pathRoot.Children.Add(_nodes);

            Acquisition.Changed += OnChanged;
            Unloaded += (s, e) => Acquisition.Changed -= OnChanged;
            Montrer(Catalogue.Chemins[0]);
        }

        private void OnChanged() { if (_pathRoot.Visibility == Visibility.Visible && _chemin != null) Remplir(); }

        private void Montrer(Chemin ch)
        {
            _chemin = ch;
            foreach (Button t in _tabs.Children)
                t.Background = (string)t.Tag == ch.Id ? new SolidColorBrush(Color.FromArgb(0x55, ch.Couleur.R, ch.Couleur.G, ch.Couleur.B)) : Brushes.White;
            Remplir();
            _lessonHost.Children.Clear();
            _lessonHost.Visibility = Visibility.Collapsed;
            _pathRoot.Visibility = Visibility.Visible;
            Speech.ScreenChanged();
            Speech.Say(ch.Nom + " !");
        }

        private void Remplir()
        {
            _nodes.Children.Clear();
            int n = _chemin.Noeuds.Count;
            _nodes.Columns = n <= 12 ? 4 : n <= 15 ? 5 : 6;
            var courant = LessonBuilder.NoeudDuMoment(_chemin);
            foreach (var nd in _chemin.Noeuds) _nodes.Children.Add(Tuile(nd, nd.Key == courant.Key));
        }

        private Button Tuile(Noeud nd, bool courant)
        {
            var etat = Acquisition.EtatDe(nd);
            bool ouvert = Acquisition.Ouvert(nd);
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var top = new Grid();
            top.Children.Add(Art.ForEmoji(nd.Competence.Icone, 86));
            string badge = etat == Etat.Acquis ? "⭐" : etat == Etat.AConsolider ? "🔧" : !ouvert ? "🔒" : null;
            if (badge != null)
            {
                var b = Art.Visual("", badge, 44);
                b.HorizontalAlignment = HorizontalAlignment.Right; b.VerticalAlignment = VerticalAlignment.Top; b.Margin = new Thickness(0, -6, -30, 0);
                top.Children.Add(b);
            }
            st.Children.Add(top);
            st.Children.Add(new TextBlock { Text = nd.Competence.Nom, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 230, Margin = new Thickness(0, 6, 0, 0) });
            st.Children.Add(new TextBlock { Text = nd.Def.Ecole + "  ·  " + LessonControl.Libelle(etat), FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x7A, 0x9A)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });

            // La jauge : la part des axes validés (en validation ou en cours).
            if (ouvert && (etat == Etat.EnCours || etat == Etat.EnValidation))
            {
                double p = Acquisition.Progression(nd);
                var bar = new Grid { Width = 200, Height = 14, Margin = new Thickness(0, 8, 0, 0) };
                bar.Children.Add(new Border { CornerRadius = new CornerRadius(7), Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE6, 0xF0)) });
                bar.Children.Add(new Border { CornerRadius = new CornerRadius(7), Background = new SolidColorBrush(_chemin.Couleur), Width = Math.Max(0, 200 * p), HorizontalAlignment = HorizontalAlignment.Left });
                st.Children.Add(bar);
                if (Acquisition.PretPourDefi(nd))
                    st.Children.Add(new TextBlock { Text = "⚡ défi !", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            }
            else if (Acquisition.RevisionDue(nd))
                st.Children.Add(new TextBlock { Text = "🔁 à revoir", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });

            Emoji.FixTree(st);
            var btn = new Button
            {
                Style = (Style)Application.Current.Resources["MenuTile"],
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(10, 8, 10, 8), Child = st },
                Width = double.NaN, Height = double.NaN,
                Margin = new Thickness(6),
            };
            if (etat == Etat.Acquis || etat == Etat.AConsolider) { btn.BorderBrush = Gold; btn.BorderThickness = new Thickness(5); btn.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xDC)); }
            else if (!ouvert) { btn.Opacity = 0.45; }
            else if (courant) { btn.BorderBrush = new SolidColorBrush(_chemin.Couleur); btn.BorderThickness = new Thickness(5); }
            if (ouvert) btn.Click += (s, e) => Demarrer(nd);
            else btn.Click += (s, e) => Speech.Say("D'abord " + nd.Competence.Nom + ", niveau " + nd.Competence.PalierN(nd.Palier - 1).Ecole + " !");
            return btn;
        }

        private void Demarrer(Noeud nd)
        {
            _choisi = nd;
            var lecon = LessonBuilder.Composer(_chemin, nd);
            _lessonHost.Children.Clear();
            var ctl = new LessonControl(_celebrate, lecon, choix =>
            {
                if (choix == "encore")
                {
                    var suivant = Acquisition.EstAcquis(_choisi) ? LessonBuilder.NoeudDuMoment(_chemin) : _choisi;
                    Demarrer(suivant);
                }
                else Montrer(_chemin);
            });
            _lessonHost.Children.Add(ctl);
            var back = new Button
            {
                Style = (Style)Application.Current.Resources["BackButton"],
                Content = "🧭  Chemin",
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16),
            };
            back.Click += (s, e) => Montrer(_chemin);
            Emoji.FixTree(back);
            _lessonHost.Children.Add(back);
            _pathRoot.Visibility = Visibility.Collapsed;
            _lessonHost.Visibility = Visibility.Visible;
            Speech.ScreenChanged();
        }
    }
}
