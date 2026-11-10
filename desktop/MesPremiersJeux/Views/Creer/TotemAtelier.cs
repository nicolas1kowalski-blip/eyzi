using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views.Creer.Volume;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🪵 LE TOTEM, EN VRAI VOLUME : un tronc rond avec son écorce, trois étages. Pour
    /// chaque étage l'enfant choisit un animal ; là où il regarde, le ciseau fait
    /// sauter l'écorce puis creuse le bois clair, par facettes, et l'animal ressort
    /// du tronc en bas-relief. Au troisième étage le totem est fini : la lumière suit
    /// le regard, on peut le peindre (la couleur se pose sur le bois sculpté) et le garder.
    /// </summary>
    public sealed class TotemAtelier : AtelierBase
    {
        private const int W = 900, H = 800, TW = Totem.TW, EH = Totem.EH, Etages = Totem.Etages, TH = Totem.EH * Totem.Etages, TX = (W - TW) / 2, TY = 62;

        private static readonly (string Emoji, string Nom)[] Animaux =
        {
            ("🦉", "un hibou"), ("🐻", "un ours"), ("🦊", "un renard"), ("🐢", "une tortue"), ("🐰", "un lapin"), ("🐱", "un chat"),
            ("🐶", "un chien"), ("🦁", "un lion"), ("🐘", "un éléphant"), ("🐬", "un dauphin"), ("🦄", "une licorne"), ("🐴", "un cheval"),
        };

        private readonly Canvas _scene = new Canvas { Width = W, Height = H, ClipToBounds = true };
        private readonly WriteableBitmap _bmp = new WriteableBitmap(TW, TH, 96, 96, PixelFormats.Pbgra32, null);
        private readonly Canvas _eclats = new Canvas { Width = W, Height = H, IsHitTestVisible = false, ClipToBounds = true };
        private readonly Canvas _ciseau = Ciseau();
        private readonly Rectangle _cadreEtage = new Rectangle { Width = TW + 14, Height = EH + 8, Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)), StrokeThickness = 6, RadiusX = 14, RadiusY = 14, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
        private readonly UniformGrid _animaux = new UniformGrid { Columns = 2 };
        private readonly StackPanel _apres = new StackPanel { Visibility = Visibility.Collapsed };
        private readonly TextBlock _consigne;
        private readonly DispatcherTimer _taille = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        private readonly string[] _noms = new string[Etages];
        private Volume3D _vol;
        private int _etage = -1, _fait, _jeton;
        private bool _polit, _prepare;
        private Point? _regard;
        private DateTime _dernierCoup = DateTime.MinValue, _derniereLumiere = DateTime.MinValue;
        private double _lumX = -0.45, _lumY = -0.60;

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

            // La scène : le ciel, la colline, l'ombre du tronc, le tronc (une image calculée), son chapeau.
            _scene.Children.Add(new Rectangle { Width = W, Height = H, Fill = new LinearGradientBrush(Color.FromRgb(0xBF, 0xE6, 0xFF), Color.FromRgb(0xEA, 0xF6, 0xFF), 90) });
            var herbe = new Ellipse { Width = W * 1.5, Height = 300, Fill = new LinearGradientBrush(Color.FromRgb(0x8C, 0xD0, 0x78), Color.FromRgb(0x5E, 0xA8, 0x52), 90) };
            Canvas.SetLeft(herbe, -W * 0.25); Canvas.SetTop(herbe, H - 150); _scene.Children.Add(herbe);
            var ombre = new Ellipse { Width = TW + 200, Height = 60, Fill = new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0)) };
            Canvas.SetLeft(ombre, TX - 40); Canvas.SetTop(ombre, TY + TH - 30); _scene.Children.Add(ombre);
            var pied = new Ellipse { Width = TW, Height = 40, Fill = new SolidColorBrush(Color.FromRgb(0x3E, 0x26, 0x14)) };
            Canvas.SetLeft(pied, TX); Canvas.SetTop(pied, TY + TH - 20); _scene.Children.Add(pied);
            var img = new Image { Width = TW, Height = TH, Stretch = Stretch.Fill, Source = _bmp };
            Canvas.SetLeft(img, TX); Canvas.SetTop(img, TY); _scene.Children.Add(img);
            // Le dessus du tronc : le bois scié, avec ses cernes.
            var chapeau = new Ellipse { Width = TW, Height = 44, Fill = new RadialGradientBrush(new GradientStopCollection { new GradientStop(Color.FromRgb(0xE8, 0xBE, 0x84), 0), new GradientStop(Color.FromRgb(0xC9, 0x96, 0x58), 0.45), new GradientStop(Color.FromRgb(0xE2, 0xB4, 0x78), 0.55), new GradientStop(Color.FromRgb(0xB8, 0x84, 0x4A), 0.8), new GradientStop(Color.FromRgb(0x5A, 0x38, 0x1C), 1) }), IsHitTestVisible = false };
            Canvas.SetLeft(chapeau, TX); Canvas.SetTop(chapeau, TY - 22); _scene.Children.Add(chapeau);
            _scene.Children.Add(_cadreEtage);
            _scene.Children.Add(_eclats);
            _scene.Children.Add(_ciseau);
            var cadre = new Border { CornerRadius = new CornerRadius(14), Margin = new Thickness(2), ClipToBounds = true, Child = new Viewbox { Stretch = Stretch.Uniform, Child = _scene } };
            Grid.SetColumn(cadre, 1);
            root.Children.Add(cadre);

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

        protected override void Entrer() { if (_vol != null) _taille.Start(); }
        protected override void Sortir() { _taille.Stop(); }
        protected override void SurRegard(Point ecran, bool valide) { _regard = valide ? Dans(_scene, ecran, W, H) : null; }

        private void Ecrire(int x0, int y0, int x1, int y1)
        {
            if (_vol == null || x1 < x0 || y1 < y0) return;
            _bmp.WritePixels(new Int32Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), _vol.Px, TW * 4, x0, y0);
        }

        private void Nouveau()
        {
            _jeton++;
            _fait = 0; _etage = -1; _polit = false; _prepare = false;
            _eclats.Children.Clear();
            _ciseau.Visibility = Visibility.Collapsed;
            for (int i = 0; i < Etages; i++) _noms[i] = null;
            _vol = Totem.Tronc(Rng.Next(1000));
            _lumX = -0.45; _lumY = -0.60;
            _vol.ToutOmbrer();
            Ecrire(0, 0, TW - 1, TH - 1);
            _apres.Visibility = Visibility.Collapsed;
            _taille.Start();
            ProposerAnimaux();
        }

        private void ProposerAnimaux()
        {
            _animaux.Children.Clear();
            _cadreEtage.Visibility = Visibility.Visible;
            Canvas.SetLeft(_cadreEtage, TX - 7); Canvas.SetTop(_cadreEtage, TY + _fait * EH - 4);
            _consigne.Text = _fait == 0 ? "Choisis l'animal du haut." : _fait == 1 ? "Et au milieu ?" : "Et tout en bas ?";
            foreach (var a in Animaux.Where(x => !_noms.Contains(x.Nom)).OrderBy(x => Rng.Next()).Take(8))
            {
                var aa = a;
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = double.NaN, Height = double.NaN, MinHeight = 110, Margin = new Thickness(5), Background = Brushes.White, Content = new Viewbox { Margin = new Thickness(10), Child = Art.ForEmoji(a.Emoji, 80) }, ToolTip = a.Nom };
                b.Click += (s, e) => Choisir(aa);
                _animaux.Children.Add(b);
            }
        }

        private static string Maj(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private async void Choisir((string Emoji, string Nom) a)
        {
            if (_fait >= Etages || _prepare || _polit || _etage >= 0 || _vol == null) return; // un étage à la fois
            int jeton = _jeton, i = _fait;
            _prepare = true;
            _consigne.Text = "Je trace " + a.Nom + " sur le tronc…";
            const int S = Totem.FS * 2;
            byte[] src = null;
            try { var rtb = Matiere.Rendre(Art.ForEmoji(a.Emoji, S), S); src = new byte[S * S * 4]; rtb.CopyPixels(src, S * 4, 0); }
            catch (Exception ex) { Log.Write("creer", "Totem, rendu du dessin : " + ex.Message); }
            var vol = _vol; bool ok = false;
            await Task.Run(() =>
            {
                try
                {
                    var fig = src == null ? null : Relief.DepuisImage(src, S);
                    if (fig == null || fig.Vide) return;
                    Totem.Etage(vol, i, Relief.Reduire(fig));
                    ok = true;
                }
                catch (Exception ex) { Log.Write("creer", "Totem, volume : " + ex); }
            });
            _prepare = false;
            if (jeton != _jeton) return;
            if (!ok) { Speech.Say("Cet animal-là ne se laisse pas sculpter. Choisis-en un autre !"); _consigne.Text = "Choisis un autre animal."; return; }
            _noms[i] = a.Nom;
            _etage = i;
            _animaux.Children.Clear();
            _consigne.Text = "Regarde le tronc pour tailler " + a.Nom + " !";
            Speech.Say(Maj(a.Nom) + " ! Regarde le tronc : ton ciseau fait sauter l'écorce.");
        }

        private void Tick()
        {
            if (_vol == null || Games.GameControl.Paused || _prepare) return;
            if (_polit) { Polir(); return; }
            if (!_regard.HasValue) { if (_fait < Etages) _ciseau.Visibility = Visibility.Collapsed; return; }
            var p = _regard.Value;
            if (_fait >= Etages) { LumiereSuit(p); return; }
            if (_etage < 0) return;
            int y0 = _etage * EH, y1 = y0 + EH - 1;
            bool parti = _vol.Coup(p.X - TX, p.Y - TY, 70, 0.07, Rng, out int x0, out int ya, out int x1, out int yb, y0, y1);
            PoserCiseau(_ciseau, p, parti);
            if (!parti) return;
            Ecrire(x0, ya, x1, yb);
            Eclats(_eclats, p, Matiere.Bois.Eclat, H, 2);
            if ((DateTime.Now - _dernierCoup).TotalMilliseconds > 230) { _dernierCoup = DateTime.Now; Matiere.Toc(Matiere.Bois); }
            if (_vol.ResteBande(y0, y1) < 0.10) { _polit = true; _ciseau.Visibility = Visibility.Collapsed; }
        }

        private void Polir()
        {
            int i = _etage, y0 = i * EH, y1 = y0 + EH - 1;
            bool encore = _vol.Polir(0.55f, y0, y1);
            _vol.Ombrer(0, Math.Max(0, y0 - 34), TW - 1, Math.Min(TH - 1, y1 + 34));
            Ecrire(0, Math.Max(0, y0 - 34), TW - 1, Math.Min(TH - 1, y1 + 34));
            if (encore) return;
            _polit = false; _etage = -1; _fait++;
            SoundFx.PopSound();
            if (_fait < Etages)
            {
                Speech.Say("Voilà " + _noms[i] + " ! Choisis l'animal suivant.");
                ProposerAnimaux();
                return;
            }
            _cadreEtage.Visibility = Visibility.Collapsed;
            _consigne.Text = "Ton totem est fini !";
            _apres.Visibility = Visibility.Visible;
            Speech.Say("Bravo ! Ton totem est fini : " + _noms[0] + ", " + _noms[1] + " et " + _noms[2] + " ! Regarde autour : la lumière suit tes yeux. Tu peux aussi le peindre.");
            Fete();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += (s, e) => { t.Stop(); Garder(false); };
            t.Start();
        }

        private void LumiereSuit(Point p)
        {
            if ((DateTime.Now - _derniereLumiere).TotalMilliseconds < 160) return;
            double lx = Math.Max(-0.85, Math.Min(0.85, (p.X - W / 2.0) / 380.0)), ly = Math.Max(-0.85, Math.Min(0.85, (p.Y - H / 2.0) / 380.0));
            if (Math.Abs(lx - _lumX) < 0.06 && Math.Abs(ly - _lumY) < 0.06) return;
            _derniereLumiere = DateTime.Now;
            _lumX = lx; _lumY = ly;
            _vol.Lumiere(lx, ly, 0.62);
            _vol.ToutOmbrer();
            Ecrire(0, 0, TW - 1, TH - 1);
        }

        private void Peindre()
        {
            if (_fait < Etages || _vol == null) return;
            _vol.Peint = !_vol.Peint;
            _vol.ToutOmbrer();
            Ecrire(0, 0, TW - 1, TH - 1);
            SoundFx.PopSound();
            Speech.Say(_vol.Peint ? "Et voilà les couleurs, posées sur le bois !" : "Comme avant, en bois.");
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
