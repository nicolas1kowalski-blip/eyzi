using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;
using MesPremiersJeux.Views.Creer.Volume;

namespace MesPremiersJeux.Views.Creer
{
    /// <summary>
    /// 🏺 SCULPTER AU REGARD, quatre ateliers, tous en vrai volume éclairé :
    ///  · Le tour de potier : une motte de terre humide tourne sur le plateau ; là où
    ///    l'enfant regarde, la terre se creuse ou s'élargit, et la pièce prend sa
    ///    forme. On choisit l'émail, on cuit (la pièce rougeoie au four), elle
    ///    ressort brillante.
    ///  · La statue : pierre, marbre, bois, glace ou bronze ; le ciseau enlève de
    ///    vrais éclats du bloc jusqu'à la forme.
    ///  · Le totem : un tronc avec son écorce, trois animaux taillés en bas-relief.
    ///  · La gravure : le regard creuse un sillon dans le bois, la pierre ou le sable.
    /// </summary>
    public sealed class SculptureView : UserControl
    {
        private const int TW = 600, TH = 560;
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public event Action BackRequested;
        public event Action<Canvas> Celebrate;

        private readonly Grid _menu, _potier;
        private readonly Grid _hote = new Grid { Visibility = Visibility.Collapsed };
        private StatueAtelier _statueA; private TotemAtelier _totemA; private GravureAtelier _gravureA;
        private readonly Canvas _fx = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        private string _mode = "menu";

        // --- le tour de potier
        private readonly TourRendu _tourR = new TourRendu(TW, TH);
        private readonly WriteableBitmap _bmp = new WriteableBitmap(TW, TH, 96, 96, PixelFormats.Bgr32, null);
        private readonly Image _img = new Image { Width = TW, Height = TH, Stretch = Stretch.Fill };
        private readonly DispatcherTimer _tour = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        private readonly TextBlock _aide;
        private bool _faconne = true, _cuisson;
        private Button _btnFaconne;
        private Point? _regardRoue;
        private DateTime _debutCuisson;

        public SculptureView()
        {
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xE4)) };

            // ----- le choix de l'atelier
            _menu = new Grid();
            var choix = new UniformGrid { Columns = 2, Margin = new Thickness(40, 64, 40, 24) };
            var t1 = MenuKit.ThemeTile("🏺", "Le tour de potier", Color.FromRgb(0xFF, 0xE3, 0xC9), "👀  🫙  🔥");
            t1.Click += (s, e) => Essayer(OuvrirPotier);
            var t2 = MenuKit.ThemeTile("🗿", "La statue", Color.FromRgb(0xE6, 0xE6, 0xEA), "🪨  🏛️  🧊");
            t2.Click += (s, e) => Essayer(() => OuvrirAtelier(_statueA ?? (_statueA = Brancher(new StatueAtelier())), "statue"));
            var t3 = MenuKit.ThemeTile("🪵", "Le totem en bois", Color.FromRgb(0xF2, 0xDF, 0xC4), "🦉  🐻  🦊");
            t3.Click += (s, e) => Essayer(() => OuvrirAtelier(_totemA ?? (_totemA = Brancher(new TotemAtelier())), "totem"));
            var t4 = MenuKit.ThemeTile("✍️", "La gravure", Color.FromRgb(0xF6, 0xEC, 0xD0), "🪵  🪨  🏖️");
            t4.Click += (s, e) => Essayer(() => OuvrirAtelier(_gravureA ?? (_gravureA = Brancher(new GravureAtelier())), "gravure"));
            choix.Children.Add(t1); choix.Children.Add(t2); choix.Children.Add(t3); choix.Children.Add(t4);
            var backM = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Créer", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16, 10, 0, 0) };
            backM.Click += (s, e) => BackRequested?.Invoke();
            _menu.Children.Add(choix);
            _menu.Children.Add(backM);
            root.Children.Add(_menu);

            // ----- le tour de potier
            _potier = new Grid { Visibility = Visibility.Collapsed };
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _potier.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var gauche = new StackPanel { Margin = new Thickness(10, 8, 6, 8), Width = 180 };
            var back1 = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = "⬅  Ateliers", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
            back1.Click += (s, e) => Menu();
            gauche.Children.Add(back1);
            _btnFaconne = Outil("👀", "Je façonne / je regarde", () =>
            {
                _faconne = !_faconne;
                _btnFaconne.Content = new Viewbox { Height = 40, Child = Art.ForEmoji(_faconne ? "👀" : "✋", 40) };
                Speech.Say(_faconne ? "La terre suit tes yeux !" : "Stop. La pièce ne bouge plus.");
            });
            gauche.Children.Add(_btnFaconne);
            gauche.Children.Add(Outil("🎲", "Une nouvelle motte", () => { _cuisson = false; _tourR.Motte(); Rendre(); Speech.Say("Une nouvelle motte de terre !"); }));
            gauche.Children.Add(Outil("🔥", "Cuire et garder", Cuire));
            _potier.Children.Add(gauche);
            _img.Source = _bmp;
            var cadre1 = new Border { CornerRadius = new CornerRadius(14), Margin = new Thickness(2), ClipToBounds = true, Child = new Viewbox { Stretch = Stretch.Uniform, Child = _img } };
            Grid.SetColumn(cadre1, 1);
            _potier.Children.Add(cadre1);
            _aide = new TextBlock { Text = "Regarde la terre : elle suit tes yeux.\nLoin du milieu, elle s'élargit ; près du milieu, elle se resserre.", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.6, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 0, 0), IsHitTestVisible = false };
            Grid.SetColumn(_aide, 1);
            _potier.Children.Add(_aide);
            // Les émaux
            var droite = new StackPanel { Margin = new Thickness(6, 8, 10, 8), Width = 150 };
            droite.Children.Add(new TextBlock { Text = "Émail", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Dark, Opacity = 0.7, Margin = new Thickness(4, 6, 0, 4) });
            var gl = new UniformGrid { Columns = 2 };
            foreach (var (hex, nom) in new[] { ("#2A6BB8", "bleu"), ("#2F9A5A", "vert"), ("#E0A82E", "jaune"), ("#C73A5E", "rose"), ("#6A3FB5", "violet"), ("#C8623A", "terre cuite"), ("#EDEDE6", "blanc"), ("#2B2B2B", "noir") })
            {
                var c = (Color)ColorConverter.ConvertFromString(hex); var n = nom;
                var b = new Button { Style = (Style)Application.Current.Resources["SwatchButton"], Width = double.NaN, Height = double.NaN, MinHeight = 62, Margin = new Thickness(5), Background = new SolidColorBrush(c), ToolTip = nom };
                b.Click += (s, e) =>
                {
                    _tourR.Gr = c.R / 255f; _tourR.Gg = c.G / 255f; _tourR.Gb = c.B / 255f;
                    if (_tourR.Cuit) Rendre();
                    Speech.Say(_tourR.Cuit ? "Émail " + n + " !" : "Émail " + n + " : on le verra après la cuisson.");
                };
                gl.Children.Add(b);
            }
            droite.Children.Add(gl);
            Grid.SetColumn(droite, 2);
            _potier.Children.Add(droite);
            root.Children.Add(_potier);

            root.Children.Add(_hote);
            root.Children.Add(_fx);
            Content = root;

            _tour.Tick += (s, e) => TickRoue();
            Loaded += (s, e) => { GazeFeed.Sample += SurRegard; if (_mode == "potier") _tour.Start(); };
            Unloaded += (s, e) => { GazeFeed.Sample -= SurRegard; _tour.Stop(); };
            _img.MouseMove += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) _regardRoue = e.GetPosition(_img); };
            _img.MouseLeftButtonUp += (s, e) => _regardRoue = null;
            Rendre();
        }

        private Button Outil(string emoji, string tip, Action action)
        {
            var b = new Button { Style = (Style)Application.Current.Resources["BackButton"], Content = new Viewbox { Height = 40, Child = Art.ForEmoji(emoji, 40) }, MinWidth = 96, Margin = new Thickness(0, 0, 0, 8), ToolTip = tip, HorizontalAlignment = HorizontalAlignment.Left };
            b.Click += (s, e) => action();
            return b;
        }

        private void Essayer(Action ouvrir)
        {
            try { ouvrir(); }
            catch (Exception ex) { Log.Write("creer", "Atelier de sculpture : " + ex); Menu(); Speech.Say("Oh, cet atelier ne veut pas s'ouvrir. On le dira à un grand."); }
        }

        private T Brancher<T>(T a) where T : AtelierBase
        {
            a.BackRequested += Menu;
            a.Celebrate += c => Celebrate?.Invoke(_fx);
            return a;
        }

        private void OuvrirAtelier(AtelierBase a, string nom)
        {
            _mode = nom; _tour.Stop();
            _hote.Children.Clear();
            _hote.Children.Add(a);
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Collapsed; _hote.Visibility = Visibility.Visible;
            Usage.Count("creer:" + nom + ":ouvert");
        }

        private void Menu()
        {
            _mode = "menu"; _tour.Stop();
            _hote.Children.Clear();
            _menu.Visibility = Visibility.Visible; _potier.Visibility = Visibility.Collapsed; _hote.Visibility = Visibility.Collapsed;
        }

        private void OuvrirPotier()
        {
            _mode = "potier";
            _menu.Visibility = Visibility.Collapsed; _potier.Visibility = Visibility.Visible; _hote.Visibility = Visibility.Collapsed;
            _tour.Start();
            Usage.Count("creer:potier:ouvert");
            Speech.Say("Le tour de potier ! Regarde la terre : elle suit tes yeux.");
        }

        private void SurRegard(Point ecran, bool valide)
        {
            if (!IsVisible || _mode != "potier") return;
            if (!valide) { _regardRoue = null; return; }
            try { var p = _img.PointFromScreen(ecran); _regardRoue = p.X >= 0 && p.Y >= 0 && p.X < TW && p.Y < TH ? p : (Point?)null; }
            catch { }
        }

        // ------------------------------------------------------------------ le tour de potier
        private void Rendre()
        {
            _tourR.Rendre();
            _bmp.WritePixels(new Int32Rect(0, 0, TW, TH), _tourR.Px, TW * 4, 0);
        }

        private void TickRoue()
        {
            if (Games.GameControl.Paused) return;
            if (_cuisson) { TickCuisson(); return; }
            if (_tourR.Cuit) return; // cuite : elle ne tourne plus, l'image ne change pas
            _tourR.Phase += 0.22;
            if (_faconne && _regardRoue.HasValue && _tourR.Faconner(_regardRoue.Value.X, _regardRoue.Value.Y) && _aide.Visibility == Visibility.Visible)
                _aide.Visibility = Visibility.Collapsed;
            Rendre();
        }

        /// <summary>Au four : la terre rougeoie (une seconde et demie), puis ressort émaillée et brillante.</summary>
        private void Cuire()
        {
            if (_cuisson) return;
            if (_tourR.Cuit) { Garder(); return; }
            _cuisson = true; _debutCuisson = DateTime.Now;
            Speech.Say("Au four ! Ça chauffe, ça chauffe…");
        }

        private void TickCuisson()
        {
            double t = (DateTime.Now - _debutCuisson).TotalSeconds;
            if (t < 1.5) { _tourR.Chauffe = (float)(t / 1.5); _tourR.Phase += 0.22 * (1 - t / 1.5); }
            else if (t < 2.8) { _tourR.Cuit = true; _tourR.Chauffe = (float)(1 - (t - 1.5) / 1.3); }
            else
            {
                _tourR.Cuit = true; _tourR.Chauffe = 0; _cuisson = false;
                Rendre();
                Garder();
                return;
            }
            Rendre();
        }

        private void Garder()
        {
            var path = Creations.Garder(_bmp, "sculpture");
            if (path == null) { Speech.Say("Je n'ai pas réussi à garder la pièce."); return; }
            Speech.Say("Quelle belle pièce, toute brillante ! Elle est gardée dans ta galerie.");
            Celebrate?.Invoke(_fx);
        }
    }
}
