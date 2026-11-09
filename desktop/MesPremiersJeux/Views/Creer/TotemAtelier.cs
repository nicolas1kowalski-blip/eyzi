using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🪵 LE TOTEM : un grand tronc, trois étages. Pour chaque étage, l'enfant choisit
    /// un animal ; le regard taille le bois (les copeaux volent) et l'animal apparaît,
    /// sculpté dans le tronc. Au bout de trois, le totem est fini : on peut le peindre
    /// et le garder.
    /// </summary>
    public sealed class TotemAtelier : AtelierBase
    {
        private const int W = 900, H = 800, TW = 340, EH = 230, TX = (W - TW) / 2, TY = 50, Etages = 3;

        private static readonly (string Emoji, string Nom)[] Animaux =
        {
            ("🦉", "un hibou"), ("🐻", "un ours"), ("🦅", "un aigle"), ("🐺", "un loup"), ("🐸", "une grenouille"), ("🦊", "un renard"),
            ("🐢", "une tortue"), ("🐰", "un lapin"), ("🦌", "un cerf"), ("🐟", "un poisson"), ("🐦", "un oiseau"), ("🦫", "un castor"),
        };

        private readonly Canvas _scene = new Canvas { Width = W, Height = H, ClipToBounds = true };
        private readonly Canvas _eclats = new Canvas { Width = W, Height = H, IsHitTestVisible = false, ClipToBounds = true };
        private readonly Bloc[] _blocs = new Bloc[Etages];
        private readonly Image[] _blocImgs = new Image[Etages], _reliefs = new Image[Etages];
        private readonly ContentControl[] _couleurs = new ContentControl[Etages];
        private readonly string[] _noms = new string[Etages];
        private readonly Rectangle _cadreEtage = new Rectangle { Width = TW + 12, Height = EH + 12, Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)), StrokeThickness = 6, RadiusX = 14, RadiusY = 14, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
        private readonly UniformGrid _animaux = new UniformGrid { Columns = 2 };
        private readonly StackPanel _apres = new StackPanel { Visibility = Visibility.Collapsed };
        private readonly TextBlock _consigne;
        private readonly DispatcherTimer _taille = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        private int _etage = -1;      // l'étage en cours de taille (-1 : on choisit)
        private int _fait;            // nombre d'étages finis
        private Point? _regard;
        private bool _peint;
        private DateTime _dernierCoup = DateTime.MinValue;

        public TotemAtelier()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF6, 0xE8)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 190 };
            gauche.Children.Add(BoutonRetour("⬅  Ateliers"));
            gauche.Children.Add(Outil("🪵", "Un nouveau tronc", () => { Nouveau(); Speech.Say("Un nouveau tronc ! Choisis le premier animal."); }));
            _consigne = new TextBlock { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(4, 6, 0, 10), TextWrapping = TextWrapping.Wrap };
            gauche.Children.Add(_consigne);
            _apres.Children.Add(Outil("🎨", "Peindre mon totem", Peindre));
            _apres.Children.Add(Outil("💾", "Garder dans ma galerie", () => Garder(true)));
            gauche.Children.Add(_apres);
            root.Children.Add(gauche);

            // La scène : le ciel, l'herbe, le tronc, les trois étages.
            _scene.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(Color.FromRgb(0xBF, 0xE6, 0xFF), Color.FromRgb(0xEA, 0xF6, 0xFF), 90) });
            var herbe = new Ellipse { Width = W * 1.5, Height = 300, Fill = new SolidColorBrush(Color.FromRgb(0x7C, 0xC4, 0x6A)) };
            Canvas.SetLeft(herbe, -W * 0.25); Canvas.SetTop(herbe, H - 150); _scene.Children.Add(herbe);
            var ombre = new Ellipse { Width = TW + 120, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)) };
            Canvas.SetLeft(ombre, TX - 60); Canvas.SetTop(ombre, TY + Etages * EH - 22); _scene.Children.Add(ombre);
            // Le tronc sous les étages : un bois plus sombre, qui se voit là où l'on a taillé.
            var fond = new Image { Width = TW, Height = Etages * EH, Stretch = Stretch.Fill, Source = Matiere.Image(Sombre(Matiere.Texture(Matiere.Bois, TW, Etages * EH, 21), 0.62), TW, Etages * EH) };
            Canvas.SetLeft(fond, TX); Canvas.SetTop(fond, TY); _scene.Children.Add(fond);
            for (int i = 0; i < Etages; i++)
            {
                double y = TY + i * EH;
                _reliefs[i] = new Image { Width = EH - 16, Height = EH - 16, Stretch = Stretch.Uniform };
                Canvas.SetLeft(_reliefs[i], TX + (TW - (EH - 16)) / 2.0); Canvas.SetTop(_reliefs[i], y + 8); _scene.Children.Add(_reliefs[i]);
                _couleurs[i] = new ContentControl { Width = EH - 16, Height = EH - 16, Visibility = Visibility.Collapsed };
                Canvas.SetLeft(_couleurs[i], TX + (TW - (EH - 16)) / 2.0); Canvas.SetTop(_couleurs[i], y + 8); _scene.Children.Add(_couleurs[i]);
                _blocs[i] = new Bloc(TW, EH);
                _blocImgs[i] = new Image { Width = TW, Height = EH, Stretch = Stretch.Fill, Source = _blocs[i].Image };
                Canvas.SetLeft(_blocImgs[i], TX); Canvas.SetTop(_blocImgs[i], y); _scene.Children.Add(_blocImgs[i]);
            }
            // Les anneaux entre les étages, et le chapeau du tronc.
            for (int i = 1; i < Etages; i++)
            {
                var a = new Rectangle { Width = TW + 10, Height = 8, RadiusX = 4, RadiusY = 4, Fill = new SolidColorBrush(Color.FromRgb(0x3E, 0x24, 0x10)), IsHitTestVisible = false };
                Canvas.SetLeft(a, TX - 5); Canvas.SetTop(a, TY + i * EH - 4); _scene.Children.Add(a);
            }
            var chapeau = new Ellipse { Width = TW, Height = 44, Fill = new SolidColorBrush(Color.FromRgb(0xD9, 0xA8, 0x6C)), Stroke = new SolidColorBrush(Color.FromRgb(0x7A, 0x4E, 0x24)), StrokeThickness = 4, IsHitTestVisible = false };
            Canvas.SetLeft(chapeau, TX); Canvas.SetTop(chapeau, TY - 22); _scene.Children.Add(chapeau);
            _scene.Children.Add(_cadreEtage);
            _scene.Children.Add(_eclats);
            var cadre = new Border { CornerRadius = new CornerRadius(14), Margin = new Thickness(2), ClipToBounds = true, Child = new Viewbox { Stretch = Stretch.Uniform, Child = _scene } };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);

            // À droite : les animaux à choisir pour l'étage.
            var droite = new DockPanel { Margin = new Thickness(6, 8, 10, 8), Width = 300 };
            var titre = new TextBlock { Text = "Quel animal ?", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 6) };
            DockPanel.SetDock(titre, Dock.Top);
            droite.Children.Add(titre);
            droite.Children.Add(_animaux);
            Grid.SetColumn(droite, 2);
            root.Children.Add(droite);
            Grid.SetColumnSpan(Fx, 3);
            root.Children.Add(Fx);
            Content = root;

            _taille.Tick += (s, e) => Tick();
            _scene.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regard = e.GetPosition(_scene); };
            _scene.MouseLeftButtonUp += (s, e) => _regard = null;
            Nouveau();
        }

        private static byte[] Sombre(byte[] px, double k)
        {
            for (int i = 0; i < px.Length; i += 4) { px[i] = (byte)(px[i] * k); px[i + 1] = (byte)(px[i + 1] * k); px[i + 2] = (byte)(px[i + 2] * k); }
            return px;
        }

        protected override void Entrer() { if (_etage >= 0) _taille.Start(); }
        protected override void Sortir() { _taille.Stop(); }
        protected override void SurRegard(Point ecran, bool valide) { _regard = valide ? Dans(_scene, ecran, W, H) : null; }

        private void Nouveau()
        {
            _taille.Stop();
            _fait = 0; _etage = -1; _peint = false;
            _eclats.Children.Clear();
            for (int i = 0; i < Etages; i++)
            {
                _blocs[i].Remplir(Matiere.Texture(Matiere.Bois, TW, EH, 30 + i + Rng.Next(100)));
                _blocImgs[i].BeginAnimation(OpacityProperty, null);
                _blocImgs[i].Opacity = 1;
                _reliefs[i].Source = null; _reliefs[i].Visibility = Visibility.Visible;
                _couleurs[i].Content = null; _couleurs[i].Visibility = Visibility.Collapsed;
                _noms[i] = null;
            }
            _apres.Visibility = Visibility.Collapsed;
            ProposerAnimaux();
        }

        private void ProposerAnimaux()
        {
            _animaux.Children.Clear();
            _cadreEtage.Visibility = Visibility.Visible;
            Canvas.SetLeft(_cadreEtage, TX - 6); Canvas.SetTop(_cadreEtage, TY + _fait * EH - 6);
            _consigne.Text = _fait == 0 ? "Choisis l'animal du haut." : _fait == 1 ? "Et au milieu ?" : "Et tout en bas ?";
            foreach (var a in Animaux.OrderBy(x => Rng.Next()).Take(8))
            {
                var aa = a;
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = double.NaN, Height = double.NaN, MinHeight = 110, Margin = new Thickness(5), Background = Brushes.White, Content = new Viewbox { Margin = new Thickness(10), Child = Art.ForEmoji(a.Emoji, 80) }, ToolTip = a.Nom };
                b.Click += (s, e) => Choisir(aa);
                _animaux.Children.Add(b);
            }
        }

        private void Choisir((string Emoji, string Nom) a)
        {
            if (_fait >= Etages) return;
            int i = _fait;
            int taille = EH - 16;
            var relief = Matiere.Relief(Art.ForEmoji(a.Emoji, taille), taille, Matiere.Bois);
            _reliefs[i].Source = relief;
            _couleurs[i].Content = new Viewbox { Child = Art.ForEmoji(a.Emoji, taille) };
            _couleurs[i].Visibility = relief == null ? Visibility.Visible : Visibility.Collapsed;
            _noms[i] = a.Nom;
            _etage = i;
            _consigne.Text = "Regarde le bois pour tailler " + a.Nom + " !";
            _taille.Start();
            Speech.Say(Maj(a.Nom) + " ! Regarde le tronc pour le tailler.");
        }

        private static string Maj(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private void Tick()
        {
            if (_etage < 0 || !_regard.HasValue || Games.GameControl.Paused) return;
            var p = _regard.Value;
            double y0 = TY + _etage * EH;
            if (!_blocs[_etage].Tailler(p.X - TX, p.Y - y0, 70, Rng)) return;
            Eclats(_eclats, p, Matiere.Bois.Eclat, H);
            if ((DateTime.Now - _dernierCoup).TotalMilliseconds > 230) { _dernierCoup = DateTime.Now; Matiere.Toc(Matiere.Bois); }
            if (_blocs[_etage].Reste < 0.10) FinEtage();
        }

        private void FinEtage()
        {
            int i = _etage;
            _etage = -1; _taille.Stop();
            var fondu = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(450));
            var bloc = _blocs[i];
            fondu.Completed += (s, e) => bloc.Vider();
            _blocImgs[i].BeginAnimation(OpacityProperty, fondu);
            _fait++;
            SoundFx.PopSound();
            if (_fait < Etages)
            {
                Speech.Say("Voilà " + _noms[i] + " ! Choisis l'animal suivant.");
                ProposerAnimaux();
                return;
            }
            _cadreEtage.Visibility = Visibility.Collapsed;
            _animaux.Children.Clear();
            _consigne.Text = "Ton totem est fini !";
            _apres.Visibility = Visibility.Visible;
            Speech.Say("Bravo ! Ton totem est fini : " + _noms[0] + ", " + _noms[1] + " et " + _noms[2] + " ! Tu peux le peindre, si tu veux.");
            Fete();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += (s, e) => { t.Stop(); Garder(false); };
            t.Start();
        }

        private void Peindre()
        {
            if (_fait < Etages) return;
            _peint = !_peint;
            for (int i = 0; i < Etages; i++)
            {
                bool couleur = _peint || _reliefs[i].Source == null;
                _couleurs[i].Visibility = couleur ? Visibility.Visible : Visibility.Collapsed;
                _reliefs[i].Visibility = couleur ? Visibility.Collapsed : Visibility.Visible;
            }
            SoundFx.PopSound();
            Speech.Say(_peint ? "Et voilà les couleurs !" : "Comme avant, en bois.");
        }

        private void Garder(bool dire)
        {
            if (_fait < Etages) return;
            var path = Creations.Garder(_scene, W, H, "totem");
            if (!dire) return;
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder le totem."); return; }
            Speech.Say("Ton totem est gardé dans ta galerie !");
            Fete();
        }
    }
}
