using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 😀 Les humeurs : le personnage d'Eyzi (le « i » jaune du logo) joue une petite scène animée
    /// pour dire comment l'enfant se sent ou ce qu'il fait — « Trop cool ! », « Beurk ! », « J'ai soif »…
    /// La page s'ouvre avec le logo, en haut à gauche de la fenêtre.
    ///
    /// Les scènes sont des vidéos fabriquées à l'avance (dossier Humeurs, à côté de l'exécutable :
    /// une vidéo, une vignette et une ligne dans humeurs.txt par humeur ; voir desktop/humeurs).
    /// Ajouter une humeur ne demande donc aucun changement ici.
    /// </summary>
    public sealed class HumeursView : UserControl
    {
        private sealed class Humeur { public string Cle = "", Page = "", Titre = "", Dit = ""; public double Duree = 5; }
        private sealed class PageDef { public string Cle, Nom, Dit; public Color Teinte; }

        private static readonly PageDef[] PagesDef =
        {
            new PageDef { Cle = "joie",    Nom = "Content",       Dit = "Je suis content !",   Teinte = Color.FromRgb(0xFF, 0xE9, 0xA8) },
            new PageDef { Cle = "tendre",  Nom = "Tendresse",     Dit = "Les mots doux !",     Teinte = Color.FromRgb(0xFF, 0xD0, 0xE0) },
            new PageDef { Cle = "rigolo",  Nom = "Rigolo",        Dit = "C'est rigolo !",      Teinte = Color.FromRgb(0xCD, 0xF2, 0xC0) },
            new PageDef { Cle = "pasbien", Nom = "Ça ne va pas",  Dit = "Ça ne va pas.",       Teinte = Color.FromRgb(0xC9, 0xE2, 0xFF) },
            new PageDef { Cle = "actions", Nom = "Je fais",       Dit = "Ce que je fais !",    Teinte = Color.FromRgb(0xFF, 0xD9, 0xB0) },
        };

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Violet = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2));
        private const int ParPage = 12;

        private static string Dossier => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Humeurs");

        private readonly List<Humeur> _toutes = new List<Humeur>();
        private readonly List<PageDef> _pages = new List<PageDef>();
        private readonly List<Button> _onglets = new List<Button>();
        private readonly UniformGrid _grille = new UniformGrid { Columns = 4, Rows = 3 };
        private readonly Button _suite;
        private readonly TextBlock _suiteInfo = new TextBlock { FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x65, 0x87)), HorizontalAlignment = HorizontalAlignment.Center };
        private Action _suivant;

        // La scène : par-dessus la grille, le temps de l'animation.
        private readonly Grid _scene;
        private readonly MediaElement _film;
        private readonly StackPanel _secours;      // si la vidéo ne peut pas être lue : la vignette, en grand
        private readonly Image _secoursImage = new Image { Width = 380, Height = 380 };
        private readonly TextBlock _secoursTitre = new TextBlock { FontSize = 58, FontWeight = FontWeights.Black, Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x1C, 0x4E)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        private readonly DispatcherTimer _garde = new DispatcherTimer();   // ferme la scène même si la vidéo ne signale pas sa fin
        private readonly Dictionary<string, ImageSource> _vignettes = new Dictionary<string, ImageSource>();

        public HumeursView()
        {
            Background = new LinearGradientBrush(Color.FromRgb(0xF6, 0xF0, 0xFF), Color.FromRgb(0xFF, 0xF6, 0xEC), 90);
            Charger();

            var root = new Grid();
            var page = new Grid { Margin = new Thickness(10) };
            page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            page.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.Children.Add(page);

            // --- À gauche : les pages d'humeurs, chacune avec la tête d'une de ses scènes ---
            var onglets = new UniformGrid { Columns = 1, Rows = Math.Max(1, _pages.Count) };
            for (int i = 0; i < _pages.Count; i++)
            {
                int idx = i;
                var p = _pages[i];
                var premiere = _toutes.First(h => h.Page == p.Cle);
                var b = new Button
                {
                    Style = (Style)Application.Current.Resources["AnswerButton"],
                    Margin = new Thickness(6, 0, 6, 10),
                    Background = new SolidColorBrush(p.Teinte),
                    Content = new Viewbox
                    {
                        Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
                        Child = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                new Image { Source = Vignette(premiere.Cle), Width = 86, Height = 86, VerticalAlignment = VerticalAlignment.Center },
                                new TextBlock { Text = p.Nom, FontSize = 28, FontWeight = FontWeights.Bold, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0), TextWrapping = TextWrapping.Wrap, MaxWidth = 150 },
                            },
                        },
                    },
                };
                b.Click += (s, e) => { Montrer(idx, 0); Speech.SayAlways(p.Dit); };
                onglets.Children.Add(b);
                _onglets.Add(b);
            }
            page.Children.Add(onglets);

            // --- Au centre : les douze cases ---
            Grid.SetColumn(_grille, 1);
            page.Children.Add(_grille);

            // --- À droite : « Suivant », quand la page a une suite (même place que dans Parler) ---
            _suite = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 104, Margin = new Thickness(12, 6, 0, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xF2, 0xFF)),
                BorderBrush = Violet, BorderThickness = new Thickness(3),
                Visibility = Visibility.Collapsed,
                Content = new Viewbox
                {
                    Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "➜", FontSize = 58, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Violet },
                            new TextBlock { Text = "Suivant", FontSize = 22, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Dark, Margin = new Thickness(0, 4, 0, 4) },
                            _suiteInfo,
                        },
                    },
                },
            };
            _suite.Click += (s, e) => _suivant?.Invoke();
            Grid.SetColumn(_suite, 2);
            page.Children.Add(_suite);

            // --- La scène : la vidéo dans un cadre arrondi, sur le même fond que l'animation ---
            _film = new MediaElement { LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Manual, Stretch = Stretch.Uniform, IsMuted = true };
            _film.MediaEnded += (s, e) => Fermer();
            _film.MediaFailed += (s, e) => { Log.Write("humeurs", "vidéo illisible : " + (e.ErrorException?.Message ?? "?")); Secours(); };
            _secours = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Visibility = Visibility.Collapsed, Children = { _secoursImage, _secoursTitre } };
            var cadre = new Grid
            {
                Width = 960, Height = 540,
                Background = new LinearGradientBrush(Color.FromRgb(0xF6, 0xF0, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE4), 90),
                Clip = new RectangleGeometry(new Rect(0, 0, 960, 540), 40, 40),
                Children = { _film, _secours },
            };
            _scene = new Grid
            {
                Background = new LinearGradientBrush(Color.FromRgb(0xE9, 0xDD, 0xFF), Color.FromRgb(0xFF, 0xE8, 0xD0), 90),
                Visibility = Visibility.Collapsed,
                Children = { new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(14), Child = cadre } },
            };
            _scene.MouseLeftButtonUp += (s, e) => Fermer();   // l'accompagnant peut l'arrêter d'un appui
            root.Children.Add(_scene);

            _garde.Tick += (s, e) => Fermer();
            IsVisibleChanged += (s, e) => { if (!IsVisible) Fermer(); };
            Content = root;

            if (_pages.Count == 0)
                _grille.Children.Add(new TextBlock { Text = "Les humeurs ne sont pas installées.", FontSize = 26, Foreground = Dark, Margin = new Thickness(30) });
            else Montrer(0, 0);
        }

        /// <summary>Vrai si le dossier des humeurs est là (sinon le logo reste un simple logo).</summary>
        public bool Disponible => _pages.Count > 0;

        /// <summary>Revient à la première page (à chaque ouverture par le logo).</summary>
        public void Debut() { if (_pages.Count > 0) Montrer(0, 0); }

        // ------------------------------------------------------------------
        //  Le catalogue : humeurs.txt (clé|page|titre|ce que la tablette dit|durée)
        // ------------------------------------------------------------------
        private void Charger()
        {
            try
            {
                string f = Path.Combine(Dossier, "humeurs.txt");
                if (!File.Exists(f)) return;
                foreach (var brute in File.ReadAllLines(f))
                {
                    var l = brute.Trim();
                    if (l.Length == 0 || l.StartsWith("#")) continue;
                    var p = l.Split('|');
                    if (p.Length < 4 || !File.Exists(Path.Combine(Dossier, p[0] + ".png"))) continue;
                    double d = 5;
                    if (p.Length > 4) double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out d);
                    _toutes.Add(new Humeur { Cle = p[0], Page = p[1], Titre = p[2], Dit = p[3], Duree = d > 0.5 && d < 30 ? d : 5 });
                }
            }
            catch (Exception ex) { Log.Write("humeurs", "catalogue illisible : " + ex.Message); }
            foreach (var p in PagesDef)
                if (_toutes.Any(h => h.Page == p.Cle)) _pages.Add(p);
            // Une page inconnue du code (ajoutée plus tard dans le catalogue) : ses humeurs vont dans la dernière page.
            if (_pages.Count > 0)
                foreach (var h in _toutes.Where(h => _pages.All(p => p.Cle != h.Page))) h.Page = _pages[_pages.Count - 1].Cle;
        }

        private ImageSource Vignette(string cle)
        {
            if (_vignettes.TryGetValue(cle, out var img)) return img;
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.UriSource = new Uri(Path.Combine(Dossier, cle + ".png"));
                b.EndInit();
                b.Freeze();
                img = b;
            }
            catch { img = null; }
            _vignettes[cle] = img;
            return img;
        }

        // ------------------------------------------------------------------
        //  Une page d'humeurs
        // ------------------------------------------------------------------
        private void Montrer(int ip, int page)
        {
            if (ip < 0 || ip >= _pages.Count) return;
            var def = _pages[ip];
            var items = _toutes.Where(h => h.Page == def.Cle).ToList();
            int n = Math.Max(1, (items.Count + ParPage - 1) / ParPage);
            page = Math.Max(0, Math.Min(n - 1, page));

            for (int i = 0; i < _onglets.Count; i++)
            {
                bool on = i == ip;
                _onglets[i].BorderBrush = Violet;
                _onglets[i].BorderThickness = new Thickness(on ? 5 : 0);
                _onglets[i].Opacity = on ? 1.0 : 0.8;
            }

            _grille.Children.Clear();
            foreach (var it in items.Skip(page * ParPage).Take(ParPage))
            {
                var h = it;
                var c = Case(h, def.Teinte);
                c.Click += (s, e) => Jouer(h);
                _grille.Children.Add(c);
            }

            bool suite = n > 1;
            _suite.Visibility = suite ? Visibility.Visible : Visibility.Collapsed;
            _suiteInfo.Text = suite ? (page + 1) + " / " + n : "";
            _suivant = !suite ? (Action)null : () =>
            {
                bool fin = page + 1 >= n;
                Speech.SayAlways(fin ? "Le début !" : "La suite !");
                Montrer(ip, fin ? 0 : page + 1);
            };
        }

        private Button Case(Humeur h, Color teinte)
        {
            var contenu = new StackPanel
            {
                Children =
                {
                    new Image { Source = Vignette(h.Cle), Width = 200, Height = 200, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock
                    {
                        Text = h.Titre, FontSize = 32, FontWeight = FontWeights.Bold, Foreground = Dark,
                        HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap, MaxWidth = 320, Margin = new Thickness(0, 2, 0, 0),
                    },
                },
            };
            return new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb((byte)(teinte.R * 0.8), (byte)(teinte.G * 0.8), (byte)(teinte.B * 0.8))),
                BorderThickness = new Thickness(5),
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Child = contenu },
            };
        }

        // ------------------------------------------------------------------
        //  La scène
        // ------------------------------------------------------------------
        private Humeur _enCours;

        private void Jouer(Humeur h)
        {
            if (_enCours != null) return;
            _enCours = h;
            _secours.Visibility = Visibility.Collapsed;
            _film.Visibility = Visibility.Visible;
            _scene.Visibility = Visibility.Visible;
            Gaze.GazeTargets.ModalRoot = _scene;      // pendant la scène, le regard ne déclenche rien
            Speech.SayAlways(h.Dit);
            GazeStats.RecordComm("humeur-" + h.Cle);
            // La garde : un peu plus que la durée, au cas où la vidéo ne dirait jamais qu'elle est finie.
            _garde.Stop();
            _garde.Interval = TimeSpan.FromSeconds(h.Duree + 3);
            _garde.Start();
            try
            {
                string f = Path.Combine(Dossier, h.Cle + ".mp4");
                if (!File.Exists(f)) { Secours(); return; }
                _film.Source = new Uri(f);
                _film.Position = TimeSpan.Zero;
                _film.Play();
            }
            catch (Exception ex) { Log.Write("humeurs", ex.Message); Secours(); }
        }

        /// <summary>La vidéo ne se lit pas sur cette tablette : la vignette en grand, le temps que la voix parle.</summary>
        private void Secours()
        {
            var h = _enCours;
            if (h == null) return;
            try { _film.Stop(); _film.Source = null; } catch { }
            _film.Visibility = Visibility.Collapsed;
            _secoursImage.Source = Vignette(h.Cle);
            _secoursTitre.Text = h.Titre;
            _secours.Visibility = Visibility.Visible;
            _garde.Stop();
            _garde.Interval = TimeSpan.FromSeconds(3.5);
            _garde.Start();
        }

        private void Fermer()
        {
            _garde.Stop();
            if (_enCours == null && _scene.Visibility != Visibility.Visible) return;
            _enCours = null;
            try { _film.Stop(); _film.Source = null; } catch { }
            _scene.Visibility = Visibility.Collapsed;
            if (ReferenceEquals(Gaze.GazeTargets.ModalRoot, _scene)) Gaze.GazeTargets.ModalRoot = null;
        }
    }
}
