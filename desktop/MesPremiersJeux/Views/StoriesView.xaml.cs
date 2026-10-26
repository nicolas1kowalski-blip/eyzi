using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MesPremiersJeux.Games;
using MesPremiersJeux.Gaze;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// Onglet Histoires : bibliothèque de livres (histoire intégrée + livres
    /// ajoutés par le parent dans Documents\MesPremiersJeux\Histoires), puis
    /// lecture AU REGARD groupe de mots par groupe de mots : le groupe courant est
    /// surligné ; le fixer le lit à voix haute, le grise et allume le suivant.
    /// </summary>
    public sealed class StoriesView : UserControl
    {
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Grey = new SolidColorBrush(Color.FromRgb(0xC2, 0xBB, 0xD0));
        private static readonly Brush Violet = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2));
        private static readonly Brush Yellow = new SolidColorBrush(Color.FromRgb(0xFF, 0xF2, 0xA8));

        // Petits mots regroupés avec le mot suivant (articles, pronoms, liaisons…).
        private static readonly HashSet<string> SmallWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "le", "la", "les", "un", "une", "des", "de", "du",
            "il", "elle", "ils", "elles", "on", "je", "tu", "nous", "vous",
            "et", "ou", "à", "au", "aux", "en", "par", "sur", "dans", "avec", "pour",
            "ne", "se", "ce", "cette", "son", "sa", "ses", "qui", "que",
            "tout", "toute", "tous", "très",
        };

        // --- Modèle de livre (intégré ou personnalisé) ---
        private sealed class BookPage
        {
            public string Text;
            public Func<UIElement> Cartoon;   // dessin intégré
            public string Layout;             // "cote" | "dessus" | null = comme le livre
            public string ImagePath;          // photo personnalisée
            public List<UserZone> Zones = new List<UserZone>();
        }

        private sealed class Book
        {
            public double TextLeft;        // mise en page « surimage » : début du texte en % de la largeur de l'image
            public string BackgroundPath; // fond d'écran du lecteur (image), peut être null
            public string Layout; // mise en page par défaut du livre ("cote" si vide)
            public string Title;
            public string Dir; // dossier du livre personnalisé (null = livre intégré)
            public List<BookPage> Pages = new List<BookPage>();
            public List<UserQuestion> Questions = new List<UserQuestion>(); // quiz de fin
        }

        // Zones affichées sur la page courante (pour les faire réagir à la lecture).
        private readonly List<(UserZone Zone, Border Ui)> _zoneUis = new List<(UserZone, Border)>();

        private readonly List<Book> _books = new List<Book>();

        // --- Interface ---
        private readonly Grid _menuRoot;
        private readonly PagedMenu _menu; // bibliothèque paginée (pas d'ascenseur)
        private readonly Grid _readerRoot;
        private readonly TextBlock _title;
        private readonly ContentControl _illus;
        private readonly WrapPanel _wordHost;
        private readonly Grid _body;      // image + texte, en deux colonnes ou l'un au-dessus de l'autre
        private readonly Border _card;    // la carte blanche de l'illustration
        private readonly Viewbox _textBox; // le panneau de texte

        private string _layoutMode = StoryLayouts.Side; // mise en page de la page affichée (choisie par livre, puis par page)
        private double _imgAspect = 0.75; // largeur / hauteur de l'illustration affichée (pour poser le texte sur l'image)
        private Border _textCard;        // le cadre du texte
        private readonly Image _bg;      // fond d'écran du livre ouvert (derrière la page)
        private StackPanel _dots;        // points de progression (une pastille par page)
        private Button _listenBtn;       // 🔊 Écouter / ⏹ Stop
        private readonly List<Button> _navButtons = new List<Button>(); // inactifs pendant la narration
        private Grid _readerGate;        // ce que le regard ne doit pas sélectionner pendant la narration
        private Grid _textCol;           // colonne du texte
        private Button _nextBtn;         // ➡ Suite : mis en surbrillance quand le texte est fini
        private Grid _endRoot;           // l'écran de fin du livre
        private StackPanel _endButtons;

        // Mode lecture (🔊) : la voix raconte chaque page que l'on ouvre, jusqu'à ⏹.
        // Pendant qu'elle parle (_narrating), les boutons, les mots et les zones ne
        // répondent pas. Le jeton invalide la fin de lecture si l'on change de page,
        // revient aux livres ou quitte l'onglet entre-temps.
        private bool _listenMode;
        private bool _narrating;
        private int _narrToken;

        // Couleurs des bandeaux de la bibliothèque (un livre = une couleur).
        private static readonly Color[] Accents =
        {
            Color.FromRgb(0xFF, 0x9F, 0x43), Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0x2E, 0xB8, 0x8A),
            Color.FromRgb(0xF2, 0x5F, 0x8F), Color.FromRgb(0x3F, 0x8E, 0xF2), Color.FromRgb(0xE0, 0xA8, 0x00),
        };
        private readonly TextBlock _pageInfo;
        private readonly List<TextBlock> _groups = new List<TextBlock>();
        private readonly List<Border> _tiles = new List<Border>();   // mode jeune enfant : une tuile par morceau
        private BitmapImage _pageBitmap;                              // l'illustration de la page (pour découper les pictos)

        // 👶 Mode jeune enfant (réglage d'administration) : le texte par morceaux
        // (phrase ou bout de phrase), chacun sur une grande tuile avec un picto.
        public static bool LittleMode { get; private set; }
        private static event Action LittleModeChanged;
        public static void SetLittleMode(bool on)
        {
            if (LittleMode == on) return;
            LittleMode = on;
            LittleModeChanged?.Invoke();
        }
        private Book _book;
        private int _index;
        private int _readIndex;

        // --- Quiz de fin d'histoire ---
        private Grid _quizRoot;
        private TextBlock _quizInfo;
        private TextBlock _quizPrompt;
        private WrapPanel _quizAnswers;
        private Button _quizBtn;
        private int _quizIndex;
        private bool _quizLocked;

        // Histoire racontée : texte de la page et correspondance mot → groupe.
        private string _pageText = "";
        private readonly List<int> _wordToGroup = new List<int>();

        public StoriesView()
        {
            UserContent.EnsureFolders();
            BuildBooks();

            var root = new Grid();
            root.Background = new LinearGradientBrush(
                Color.FromRgb(0xFF, 0xF3, 0xFB), Color.FromRgb(0xE9, 0xF2, 0xFF), 90);
            // Décor de fond avec profondeur, derrière la bibliothèque et le lecteur.
            root.Children.Add(DecorBackground.ForStories());

            // --- Bibliothèque (choix du livre) ---
            _menuRoot = new Grid { Margin = new Thickness(12) };
            _menu = new PagedMenu(4, 2);
            _menuRoot.Children.Add(_menu);
            root.Children.Add(_menuRoot);

            // --- Lecteur : image à gauche (toute la hauteur), texte à droite ---
            // Les histoires livrées ont des pages longues : en colonne (image au-dessus
            // du texte) l'image finissait écrasée. En deux colonnes, l'illustration
            // (souvent en portrait) garde toute la hauteur et le texte a sa place.
            _readerRoot = new Grid { Visibility = Visibility.Collapsed };
            // Fond d'écran du livre (s'il en a un) : plein écran derrière la page.
            _bg = new Image { Stretch = Stretch.UniformToFill, Visibility = Visibility.Collapsed };
            _readerRoot.Children.Add(_bg);
            // Tout ce qui est bloqué pendant la narration (page + barre de boutons) ;
            // le bouton « Livres » reste en dehors : quitter est toujours possible.
            _readerGate = new Grid();
            _readerGate.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _readerGate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _readerRoot.Children.Add(_readerGate);

            var body = _body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition());
            body.ColumnDefinitions.Add(new ColumnDefinition());
            body.RowDefinitions.Add(new RowDefinition());
            body.RowDefinitions.Add(new RowDefinition());

            _title = new TextBlock
            {
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = Violet,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 0, 0),
            };
            var card = _card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(30),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0xA5)),
                BorderThickness = new Thickness(4),
                Margin = new Thickness(24, 52, 8, 8),
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 3, Opacity = 0.18 },
            };
            _illus = new ContentControl { Margin = new Thickness(20) };
            card.Child = _illus;
            Grid.SetColumn(card, 0);
            body.Children.Add(card);

            // Texte : panneau de 900 px de large ; s'il est trop haut pour l'écran,
            // il est réduit d'un bloc (Viewbox) plutôt que coupé.
            _wordHost = new WrapPanel { Width = 900, HorizontalAlignment = HorizontalAlignment.Center };
            var textBox = _textBox = new Viewbox
            {
                Child = _wordHost,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Margin = new Thickness(8, 56, 24, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _textCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xE4, 0xC4)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(30),
                Padding = new Thickness(18, 14, 18, 14),
                Child = textBox,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _textCol = new Grid();
            _textCol.Children.Add(_textCard);
            Grid.SetColumn(_textCol, 1);
            body.Children.Add(_textCol);
            ApplyLayout();
            body.SizeChanged += (s, e) => PositionOverlayTextSoon();

            Grid.SetColumnSpan(_title, 2);
            body.Children.Add(_title);

            // Bouton retour bibliothèque.
            var backBtn = new Button
            {
                Style = (Style)Application.Current.Resources["BackButton"],
                Content = "📚  Livres",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(16),
            };
            backBtn.Click += (s, e) => ShowMenu();

            Grid.SetRow(body, 0);
            _readerGate.Children.Add(body);

            var nav = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 18),
            };
            _navButtons.Clear();
            Button Nav(string g, string l, Action a) { var b = NavButton(g, l, a); _navButtons.Add(b); return b; }
            nav.Children.Add(Nav("⬅", "Avant", () => ShowPage(_index - 1)));
            nav.Children.Add(Nav("🔁", "Encore", () => ShowPage(_index)));
            _listenBtn = NavButton("🔊", "Écouter", ToggleListen); // reste actif : c'est lui qui arrête
            nav.Children.Add(_listenBtn);
            _quizBtn = Nav("🎯", "Quiz", StartQuiz);
            _quizBtn.Visibility = Visibility.Collapsed; // seulement si le livre a des questions
            nav.Children.Add(_quizBtn);
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 24, 0) };
            _pageInfo = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x5B, 0x8A)),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            info.Children.Add(_pageInfo);
            _dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            info.Children.Add(_dots);
            nav.Children.Add(info);
            _nextBtn = Nav("➡", "Suite", NextOrEnd);
            _nextBtn.RenderTransformOrigin = new Point(0.5, 0.5);
            _nextBtn.RenderTransform = new ScaleTransform(1, 1);
            nav.Children.Add(_nextBtn);
            Grid.SetRow(nav, 1);
            _readerGate.Children.Add(nav);
            _readerRoot.Children.Add(backBtn); // au-dessus, hors de la porte

            root.Children.Add(_readerRoot);

            // --- Fin du livre (par-dessus, masqué au départ) ---
            root.Children.Add(BuildEndRoot());

            // --- Quiz de fin (par-dessus, masqué au départ) ---
            root.Children.Add(BuildQuizRoot());

            Content = root;

            BuildMenu();
            // Le mode jeune enfant change : la page ouverte se refait.
            LittleModeChanged += () => Dispatcher.Invoke(() => { if (_book != null && _readerRoot.IsVisible) ShowPage(_index); });
            // Les outils parent apparaissent/disparaissent avec le mode admin.
            AdminMode.Changed += () => Dispatcher.Invoke(BuildMenu);
            // Après un import de contenu, la bibliothèque se recharge.
            UserContent.ContentChanged += () => Dispatcher.Invoke(ReloadBooks);
        }

        /// <summary>Place l'image et le texte selon la mise en page de la page (côte à côte ou l'un au-dessus de l'autre).</summary>
        private void ApplyLayout()
        {
            // Carte de l'illustration : blanche et encadrée, sauf quand le texte se pose sur l'image.
            bool onImage = _layoutMode == StoryLayouts.OnImage;
            _card.Background = onImage ? Brushes.Transparent : Brushes.White;
            _card.BorderThickness = new Thickness(onImage ? 0 : 4);
            _card.Effect = onImage ? null : new DropShadowEffect { BlurRadius = 22, ShadowDepth = 3, Opacity = 0.18 };
            _illus.Margin = new Thickness(onImage ? 0 : 20);
            _illus.HorizontalContentAlignment = onImage ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            _textCard.VerticalAlignment = onImage ? VerticalAlignment.Top : VerticalAlignment.Center;

            if (onImage)
            {
                // L'image remplit la hauteur, calée à gauche ; le texte vient par-dessus,
                // à partir de « TextLeft » % de sa largeur (PositionOverlayText).
                _body.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _body.ColumnDefinitions[1].Width = new GridLength(0);
                _body.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                _body.RowDefinitions[1].Height = new GridLength(0);
                Grid.SetRow(_card, 0); Grid.SetColumn(_card, 0); Grid.SetColumnSpan(_card, 2);
                Grid.SetRow(_textCol, 0); Grid.SetColumn(_textCol, 0); Grid.SetColumnSpan(_textCol, 2);
                _card.Margin = new Thickness(0, 48, 0, 0);
                _textCard.HorizontalAlignment = HorizontalAlignment.Stretch;
                PositionOverlayText();
                return;
            }
            if (_layoutMode == StoryLayouts.Side)
            {
                _body.ColumnDefinitions[0].Width = new GridLength(47, GridUnitType.Star);
                _body.ColumnDefinitions[1].Width = new GridLength(53, GridUnitType.Star);
                _body.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                _body.RowDefinitions[1].Height = new GridLength(0);
                Grid.SetRow(_card, 0); Grid.SetColumn(_card, 0); Grid.SetColumnSpan(_card, 1);
                Grid.SetRow(_textCol, 0); Grid.SetColumn(_textCol, 1); Grid.SetColumnSpan(_textCol, 1);
                _card.Margin = new Thickness(24, 52, 8, 8);
                _textCol.Margin = new Thickness(8, 56, 24, 8);
                _textCard.HorizontalAlignment = HorizontalAlignment.Stretch;
                _textBox.MaxHeight = double.PositiveInfinity;
                _wordHost.Width = 900;
            }
            else
            {
                _body.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _body.ColumnDefinitions[1].Width = new GridLength(0);
                _body.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                _body.RowDefinitions[1].Height = GridLength.Auto;
                Grid.SetRow(_card, 0); Grid.SetColumn(_card, 0); Grid.SetColumnSpan(_card, 2);
                Grid.SetRow(_textCol, 1); Grid.SetColumn(_textCol, 0); Grid.SetColumnSpan(_textCol, 2);
                _card.Margin = new Thickness(24, 44, 24, 8);
                _textCol.Margin = new Thickness(24, 4, 24, 4);
                _textCard.HorizontalAlignment = HorizontalAlignment.Center;
                // Le texte garde au plus un gros tiers de l'écran : l'image n'est jamais écrasée.
                _textBox.MaxHeight = 360;
                _wordHost.Width = 1300;
            }
        }

        /// <summary>Mise en page « texte sur l'image » : place le cadre du texte sur la partie droite de l'illustration.</summary>
        private void PositionOverlayText()
        {
            if (_layoutMode != StoryLayouts.OnImage || _book == null) return;
            double bw = _body.ActualWidth, bh = _body.ActualHeight;
            if (bw < 50 || bh < 50) return;
            double shownH = bh - 48;                           // hauteur de l'image affichée (marge du titre)
            double imgLeft = 0, imgW = Math.Min(bw, shownH * _imgAspect);
            // L'endroit RÉEL où l'image est dessinée (après la mise en page), plutôt
            // qu'une estimation : le texte doit tomber sur l'image, pas à côté.
            try
            {
                var host = (_illus.Content as Viewbox)?.Child as FrameworkElement;
                if (host != null && host.IsVisible && host.ActualWidth > 0)
                {
                    var t = host.TransformToAncestor(_body);
                    var p0 = t.Transform(new Point(0, 0));
                    var p1 = t.Transform(new Point(host.ActualWidth, host.ActualHeight));
                    if (p1.X - p0.X > 50) { imgLeft = p0.X; imgW = p1.X - p0.X; shownH = p1.Y - p0.Y; }
                }
            }
            catch { }
            double pct = _book.TextLeft > 0 ? _book.TextLeft : 60;
            double x = imgLeft + imgW * pct / 100;
            double width = Math.Max(320, bw - x - 24);
            _textCol.Margin = new Thickness(x + 8, 60, 16, 12);
            _wordHost.Width = Math.Max(240, width - 60);
            foreach (var t in _tiles) t.Width = Math.Max(300, _wordHost.Width - 12);
            _textBox.MaxHeight = Math.Max(200, shownH - 60);
        }

        private void PositionOverlayTextSoon() =>
            Dispatcher.BeginInvoke(new Action(PositionOverlayText), DispatcherPriority.Loaded);

        // --- Bibliothèque ---
        private void BuildBooks()
        {
            // Pages courtes et dessins carrés : image au-dessus du texte.
            var builtIn = new Book { Title = "Étincelle la licorne", Layout = StoryLayouts.Stacked };
            foreach (var (text, art) in new[]
            {
                ("Voici Étincelle, une petite licorne toute rose.", "licorne"),
                ("Un matin, elle saute par-dessus un grand arc-en-ciel !", "arcenciel"),
                ("Elle rencontre Minou, un chat très rigolo.", "chat"),
                ("Ensemble, ils cueillent de jolies fleurs.", "fleur"),
                ("Un papillon vient danser avec eux.", "papillon"),
                ("Le soir, ils font un vœu sur une étoile. Bonne nuit !", "etoile"),
            })
            {
                var a = art;
                builtIn.Pages.Add(new BookPage { Text = text, Cartoon = () => CartoonArt.Draw(a) });
            }
            // Petit quiz de fin (démontre les deux types de questions).
            builtIn.Questions.Add(new UserQuestion
            {
                Text = "Étincelle est une licorne ?",
                Kind = QuizKind.TrueFalse,
                Answer = true,
            });
            builtIn.Questions.Add(Pick("Montre le chat !", "chat", "poisson", "fleur"));
            builtIn.Questions.Add(Pick("Où est l'arc-en-ciel ?", "arcenciel", "etoile", "papillon"));
            _books.Add(builtIn);

            // Livres du parent (Documents\MesPremiersJeux\Histoires).
            foreach (var s in UserContent.LoadStories())
            {
                var book = new Book { Title = s.Title, Dir = s.Dir, Questions = s.Questions, Layout = s.Layout, TextLeft = s.TextLeft, BackgroundPath = s.BackgroundPath };
                foreach (var p in s.Pages)
                    book.Pages.Add(new BookPage { Text = p.Text, ImagePath = p.ImagePath, Zones = p.Zones, Layout = p.Layout });
                _books.Add(book);
            }
        }

        // Question « choisis la bonne image » à partir de dessins intégrés
        // (le 1er est la bonne réponse).
        private static UserQuestion Pick(string text, string correct, params string[] others)
        {
            var q = new UserQuestion { Text = text, Kind = QuizKind.Image };
            q.Choices.Add(new QuizChoice { Draw = correct, Correct = true });
            foreach (var o in others) q.Choices.Add(new QuizChoice { Draw = o });
            return q;
        }

        // Construit l'illustration d'une page : dessin intégré, ou photo avec ses
        // zones interactives (fixer une zone la nomme ; lire son mot l'illumine).
        private UIElement MakeIllustration(BookPage page)
        {
            _zoneUis.Clear();
            _pageBitmap = null;
            if (page.Cartoon != null) return page.Cartoon();
            if (page.ImagePath == null) return Missing("📖", "Pas d'image pour cette page");

            try
            {
                var bmp = UserContent.LoadBitmap(page.ImagePath);
                _pageBitmap = bmp;
                double w = bmp.PixelWidth, h = bmp.PixelHeight;
                _imgAspect = h > 0 ? w / h : 0.75;
                var host = new Grid { Width = w, Height = h };
                host.Children.Add(new Image { Source = bmp, Stretch = Stretch.Fill, Width = w, Height = h });

                var layer = new Canvas { Width = w, Height = h, Background = Brushes.Transparent };
                foreach (var z in page.Zones ?? new List<UserZone>())
                {
                    var ui = new Border
                    {
                        Width = z.Width / 100 * w,
                        Height = z.Height / 100 * h,
                        CornerRadius = new CornerRadius(10),
                        Background = Brushes.Transparent,
                        BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x7E, 0x3F, 0xF2)),
                        BorderThickness = new Thickness(3),
                    };
                    Canvas.SetLeft(ui, z.Left / 100 * w);
                    Canvas.SetTop(ui, z.Top / 100 * h);
                    var zone = z;
                    ui.MouseLeftButtonDown += (s, e) => { if (!_narrating) Speech.Say(zone.Label); }; // fixer la zone la nomme
                    GazeTargets.SetIsTarget(ui, true); // aimant du regard sur la zone
                    layer.Children.Add(ui);
                    _zoneUis.Add((zone, ui));
                }
                host.Children.Add(layer);
                return host;
            }
            catch (Exception ex)
            {
                Log.Write("histoires", "Image illisible " + page.ImagePath + " : " + ex);
                return Missing("🖼️", "Image illisible : " + ex.Message);
            }
        }

        // Quand une image manque ou ne se lit pas : le pictogramme ET la raison,
        // pour que le parent (et nous) sachions quoi corriger.
        private static UIElement Missing(string glyph, string why)
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Width = 600 };
            sp.Children.Add(new TextBlock { Text = glyph, FontSize = 120, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = why, FontSize = 22, Foreground = Grey, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
            return sp;
        }

        // --- Mise en évidence d'une zone quand son mot est lu (comme sur le web) ---
        private static string NormalizeWord(string s)
        {
            var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in d)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark
                    && char.IsLetterOrDigit(c))
                    sb.Append(c);
            return sb.ToString();
        }

        private void PulseMatchingZones(string groupText)
        {
            if (_zoneUis.Count == 0) return;
            var words = groupText.Split(' ').Select(NormalizeWord).Where(x => x.Length >= 3).ToList();
            if (words.Count == 0) return;

            foreach (var (zone, ui) in _zoneUis)
            {
                var labelParts = zone.Label.Split(' ').Select(NormalizeWord);
                if (!labelParts.Any(lp => words.Contains(lp))) continue;

                // Petit flash : fond jaune + bord épais qui s'estompent.
                var flash = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xE0, 0x5C));
                ui.Background = flash;
                ui.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                flash.BeginAnimation(SolidColorBrush.ColorProperty,
                    new System.Windows.Media.Animation.ColorAnimation(
                        Color.FromArgb(0x88, 0xFF, 0xE0, 0x5C),
                        Color.FromArgb(0x00, 0xFF, 0xE0, 0x5C),
                        TimeSpan.FromMilliseconds(1600)));
            }
        }

        private void BuildMenu()
        {
            var tiles = new List<(UIElement Content, Action OnClick)>();
            int bi = 0;
            foreach (var b in _books)
            {
                // Une « couverture » : l'image du livre dans un cadre teinté, le titre
                // sur un bandeau de couleur qui chevauche le bas de l'image, puis le
                // nombre de pages et la présence d'un quiz.
                var accent = Accents[bi++ % Accents.Length];
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Width = 300 };
                var cover = new Border
                {
                    Width = 300,
                    Height = 210,
                    CornerRadius = new CornerRadius(22),
                    Background = new SolidColorBrush(Color.FromArgb(0x38, accent.R, accent.G, accent.B)),
                    Clip = new RectangleGeometry(new Rect(0, 0, 300, 210), 22, 22),
                    Child = b.Pages.Count > 0 ? PreviewOf(b.Pages[0]) : null,
                };
                content.Children.Add(cover);
                var ribbon = new Border
                {
                    Background = new SolidColorBrush(accent),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(10, -20, 10, 0), // chevauche la couverture
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = b.Title,
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 250,
                    },
                };
                content.Children.Add(ribbon);
                content.Children.Add(new TextBlock
                {
                    Text = "📖 " + b.Pages.Count + (b.Pages.Count > 1 ? " pages" : " page") + (b.Questions.Count > 0 ? "   ·   🎯 quiz" : ""),
                    FontSize = 17,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x5B, 0x8A)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0),
                });

                // Livres personnalisés : boutons Modifier / Supprimer (mode admin).
                if (b.Dir != null && AdminMode.IsActive)
                {
                    var admin = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0),
                    };
                    var bookRef = b;
                    var edit = new Button { Content = "✏️", FontSize = 18, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 4, 0) };
                    edit.Click += (s, e) => { e.Handled = true; EditBook(bookRef); };
                    var del = new Button { Content = "🗑", FontSize = 18, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 4, 0) };
                    del.Click += (s, e) => { e.Handled = true; DeleteBook(bookRef); };
                    admin.Children.Add(edit);
                    admin.Children.Add(del);
                    content.Children.Add(admin);
                }

                var book = b;
                tiles.Add((content, () => OpenBook(book)));
            }

            // Tuile « nouveau livre » (mode admin uniquement).
            if (!AdminMode.IsActive)
            {
                _menu.SetTiles(tiles);
                return;
            }
            var addContent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Width = 300 };
            addContent.Children.Add(new Border
            {
                Width = 300, Height = 210, CornerRadius = new CornerRadius(22),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xC9, 0xB8, 0xE8)), BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
                Child = new TextBlock { Text = "➕", FontSize = 90, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            });
            addContent.Children.Add(new TextBlock
            {
                Text = "Nouveau livre",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Dark,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
            });
            tiles.Add((addContent, AddBook));
            _menu.SetTiles(tiles);
        }

        private void AddBook()
        {
            var editor = new BookEditorWindow { Owner = Window.GetWindow(this) };
            if (editor.ShowDialog() == true)
            {
                ReloadBooks();
                Speech.Say("Le livre est ajouté !");
            }
        }

        private void EditBook(Book book)
        {
            var drafts = book.Pages.Select(p => new PageDraft
            {
                Text = p.Text ?? "",
                ImagePath = p.ImagePath,
                Layout = p.Layout,
                Zones = p.Zones?.Select(z => new UserZone
                {
                    Left = z.Left, Top = z.Top, Width = z.Width, Height = z.Height, Label = z.Label,
                }).ToList() ?? new List<UserZone>(),
            }).ToList();

            // Copie des questions (pour ne pas modifier le modèle chargé).
            var qDrafts = book.Questions.Select(q => new UserQuestion
            {
                Text = q.Text,
                Kind = q.Kind,
                Answer = q.Answer,
                Choices = q.Choices.Select(c => new QuizChoice
                {
                    ImagePath = c.ImagePath, Draw = c.Draw, Correct = c.Correct,
                }).ToList(),
            }).ToList();

            var editor = new BookEditorWindow(book.Title, drafts, qDrafts, book.Dir, book.Layout, book.BackgroundPath) { Owner = Window.GetWindow(this) };
            if (editor.ShowDialog() == true)
            {
                ReloadBooks();
                Speech.Say("Le livre est modifié !");
            }
        }

        private void DeleteBook(Book book)
        {
            // Pause du regard pendant la confirmation (action parent, souris).
            GazeGate.Push();
            var res = MessageBox.Show(
                Window.GetWindow(this),
                $"Supprimer définitivement « {book.Title} » ?",
                "Supprimer le livre",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            GazeGate.Pop();

            if (res == MessageBoxResult.Yes && UserContent.DeleteStory(book.Dir))
                ReloadBooks();
        }

        private void ReloadBooks()
        {
            _books.Clear();
            BuildBooks();
            BuildMenu();
        }

        // Aperçu léger d'une page pour la tuile de la bibliothèque.
        private static UIElement PreviewOf(BookPage page)
        {
            if (page.Cartoon != null) return new Viewbox { Child = page.Cartoon(), Stretch = Stretch.Uniform, Margin = new Thickness(10) };
            if (page.ImagePath != null)
            {
                try { return new Image { Source = UserContent.LoadBitmap(page.ImagePath, 400), Stretch = Stretch.Uniform, Margin = new Thickness(6) }; }
                catch (Exception ex) { Log.Write("histoires", "Couverture illisible " + page.ImagePath + " : " + ex.Message); }
            }
            return new TextBlock { Text = "📖", FontSize = 90, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }

        private void ShowMenu()
        {
            _narrToken++;
            _listenMode = false;
            SetNarrating(false);
            UnhighlightNext();
            InputGate.Clear(_readerGate);
            _endRoot.Visibility = Visibility.Collapsed;
            _readerRoot.Visibility = Visibility.Collapsed;
            _menuRoot.Visibility = Visibility.Visible;
            Chrome.Immersive = false;
        }

        private void OpenBook(Book book)
        {
            _book = book;
            _title.Text = "✨ " + book.Title + " ✨";
            _bg.Source = null;
            _bg.Visibility = Visibility.Collapsed;
            if (book.BackgroundPath != null)
            {
                try { _bg.Source = UserContent.LoadBitmap(book.BackgroundPath, 1920); _bg.Visibility = Visibility.Visible; }
                catch { }
            }
            _quizBtn.Visibility = book.Questions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            _menuRoot.Visibility = Visibility.Collapsed;
            _readerRoot.Visibility = Visibility.Visible;
            Chrome.Immersive = true; // plein écran pour lire
            InputGate.Set(_readerGate, () => _narrating); // pendant la narration, le regard ne sélectionne rien ici
            ShowPage(0);
        }

        /// <summary>➡ : la page suivante, ou l'écran de fin après la dernière page.</summary>
        private void NextOrEnd()
        {
            if (_book == null) return;
            if (_index >= _book.Pages.Count - 1) ShowEnd();
            else ShowPage(_index + 1);
        }

        private Button NavButton(string glyph, string label, Action onClick)
        {
            var btn = new Button
            {
                Style = (Style)Application.Current.Resources["BackButton"],
                Height = 100,
                MinWidth = 150,
                Margin = new Thickness(10, 0, 10, 0),
            };
            SetNavContent(btn, glyph, label);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static void SetNavContent(Button btn, string glyph, string label)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = glyph, FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = label, FontSize = 16, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Dark, Margin = new Thickness(0, -2, 0, 0) });
            btn.Content = sp;
        }

        // --- Mode lecture / narration ---
        private void SetNarrating(bool on)
        {
            _narrating = on;
            if (_listenBtn != null) SetNavContent(_listenBtn, _listenMode ? "⏹" : "🔊", _listenMode ? "Stop" : "Écouter");
            foreach (var b in _navButtons) { b.IsEnabled = !on; b.Opacity = on ? 0.4 : 1.0; }
        }

        /// <summary>
        /// 🔊 : le mode lecture s'allume et la voix raconte la page ; il reste allumé
        /// de page en page (chaque page ouverte est racontée) jusqu'à ⏹.
        /// </summary>
        private void ToggleListen()
        {
            if (_listenMode)
            {
                _listenMode = false;
                _narrToken++;
                SetNarrating(false);
                Speech.Stop();
                _readIndex = 0;
                Refresh();
                return;
            }
            _listenMode = true;
            StartNarration();
        }

        private void StartNarration()
        {
            UnhighlightNext();
            SetNarrating(true);
            ListenPage();
        }

        /// <summary>Le texte de la page est fini (raconté ou lu) : le bouton ➡ s'illumine.</summary>
        private void OnTextFinished()
        {
            if (_book == null) return;
            bool last = _index >= _book.Pages.Count - 1;
            HighlightNext(last ? "La fin !" : "Suite");
        }

        private void HighlightNext(string label)
        {
            SetNavContent(_nextBtn, "➡", label);
            _nextBtn.Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0xB8, 0x6B), Color.FromRgb(0xFF, 0x8A, 0x3D), 90);
            var anim = new DoubleAnimation(1.0, 1.1, TimeSpan.FromMilliseconds(650)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
            var st = (ScaleTransform)_nextBtn.RenderTransform;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void UnhighlightNext()
        {
            if (_nextBtn == null) return;
            SetNavContent(_nextBtn, "➡", "Suite");
            _nextBtn.ClearValue(Button.BackgroundProperty); // retour au style
            var st = (ScaleTransform)_nextBtn.RenderTransform;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        // ------------------------------------------------------------------
        // Fin du livre : « Fin », bravo, et le choix : quiz, relire, un autre livre.
        // Tout est dessiné (étoiles, confettis, icônes) : les pictogrammes du
        // système sortent en noir et blanc sur certaines tablettes.
        // ------------------------------------------------------------------
        private Image _endBg;
        private Canvas _endDeco;
        private Border _endCover;

        private UIElement BuildEndRoot()
        {
            _endRoot = new Grid { Visibility = Visibility.Collapsed, ClipToBounds = true };
            _endRoot.Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0xE9, 0xD0), Color.FromRgb(0xE6, 0xD6, 0xFF), 90);
            _endBg = new Image { Stretch = Stretch.UniformToFill, Visibility = Visibility.Collapsed };
            _endRoot.Children.Add(_endBg);
            _endRoot.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(0x8C, 0xFF, 0xFF, 0xFF)) }); // voile clair
            _endDeco = new Canvas();
            _endRoot.Children.Add(_endDeco);

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0xA5)),
                BorderThickness = new Thickness(4),
                CornerRadius = new CornerRadius(44),
                Padding = new Thickness(56, 30, 56, 36),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var col = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            _endCover = new Border
            {
                Width = 210, Height = 210,
                CornerRadius = new CornerRadius(26),
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF2, 0xE2)),
                Clip = new RectangleGeometry(new Rect(0, 0, 210, 210), 26, 26),
                Margin = new Thickness(0, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            col.Children.Add(_endCover);
            col.Children.Add(new TextBlock
            {
                Text = "Fin",
                FontSize = 120,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new LinearGradientBrush(Color.FromRgb(0xFF, 0x8A, 0x3D), Color.FromRgb(0x7E, 0x3F, 0xF2), 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -10, 0, -16),
            });
            col.Children.Add(new TextBlock
            {
                Text = "Bravo, tu as lu toute l'histoire !",
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                Foreground = Dark,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30),
            });
            _endButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            col.Children.Add(_endButtons);
            card.Child = col;
            _endRoot.Children.Add(card);
            return _endRoot;
        }

        // Un gros bouton coloré de l'écran de fin : icône dessinée + libellé.
        private Border EndButton(string label, Color color, UIElement icon, Action onClick)
        {
            var sp = new StackPanel();
            icon.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            sp.Children.Add(icon);
            sp.Children.Add(new TextBlock
            {
                Text = label, FontSize = 30, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0),
            });
            var b = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(34),
                Padding = new Thickness(26, 24, 26, 20),
                Margin = new Thickness(14, 0, 14, 0),
                Width = 270,
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = sp,
            };
            b.MouseLeftButtonDown += (s, e) => { e.Handled = true; onClick(); };
            GazeTargets.SetIsTarget(b, true);
            return b;
        }

        private static readonly Geometry StarGeometry = Geometry.Parse("M 0,-10 L 2.9,-3.1 10,-3.1 4.3,1.2 6.2,8.1 0,4 -6.2,8.1 -4.3,1.2 -10,-3.1 -2.9,-3.1 Z");

        // Icônes vectorielles (blanches) : cible, flèche qui tourne, pile de livres.
        private static UIElement IconTarget()
        {
            var g = new Grid { Width = 72, Height = 72 };
            foreach (var (d, fill) in new[] { (72.0, false), (46.0, false), (20.0, true) })
                g.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = d, Height = d, Stroke = Brushes.White, StrokeThickness = 6,
                    Fill = fill ? Brushes.White : null, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                });
            return g;
        }

        private static UIElement IconRepeat()
        {
            var g = new Grid { Width = 72, Height = 72 };
            g.Children.Add(new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 60,36 A 24,24 0 1 1 50,16.5"),
                Stroke = Brushes.White, StrokeThickness = 7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            });
            g.Children.Add(new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 42,4 L 60,16 L 44,28 Z"), Fill = Brushes.White,
            });
            return g;
        }

        private static UIElement IconBooks()
        {
            var g = new Grid { Width = 72, Height = 72 };
            foreach (var (x, y, w) in new[] { (12.0, 10.0, 48.0), (6.0, 30.0, 56.0), (14.0, 50.0, 46.0) })
                g.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Width = w, Height = 14, RadiusX = 5, RadiusY = 5, Fill = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(x, y, 0, 0),
                });
            return g;
        }

        // Étoiles qui flottent et confettis qui tombent, à la taille réelle de l'écran.
        private void BuildEndDeco()
        {
            _endDeco.Children.Clear();
            double W = Math.Max(800, _endRoot.ActualWidth), H = Math.Max(600, _endRoot.ActualHeight);
            var rnd = new Random();
            var golds = new[] { Color.FromRgb(0xFF, 0xD2, 0x4D), Color.FromRgb(0xFF, 0xB3, 0x47), Color.FromRgb(0xFF, 0xE0, 0x8A) };
            for (int i = 0; i < 16; i++)
            {
                double size = 1.6 + rnd.NextDouble() * 2.6;
                var star = new System.Windows.Shapes.Path
                {
                    Data = StarGeometry, Fill = new SolidColorBrush(golds[i % golds.Length]),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)), StrokeThickness = 0.6,
                    Opacity = 0.9, RenderTransformOrigin = new Point(0.5, 0.5),
                };
                var tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(size, size));
                var rot = new RotateTransform(rnd.Next(360));
                tg.Children.Add(rot);
                var up = new TranslateTransform();
                tg.Children.Add(up);
                star.RenderTransform = tg;
                Canvas.SetLeft(star, 30 + rnd.NextDouble() * (W - 60));
                Canvas.SetTop(star, 20 + rnd.NextDouble() * (H - 40));
                up.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -14 - rnd.Next(18), TimeSpan.FromMilliseconds(1500 + rnd.Next(1500))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(rot.Angle, rot.Angle + (rnd.Next(2) == 0 ? 20 : -20), TimeSpan.FromMilliseconds(2200 + rnd.Next(1800))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
                _endDeco.Children.Add(star);
            }
            var confetti = new[] { Color.FromRgb(0xFF, 0x6B, 0x6B), Color.FromRgb(0x7E, 0x3F, 0xF2), Color.FromRgb(0x2E, 0xB8, 0x8A), Color.FromRgb(0x3F, 0x8E, 0xF2), Color.FromRgb(0xFF, 0x9F, 0x43), Color.FromRgb(0xF2, 0x5F, 0x8F) };
            for (int i = 0; i < 40; i++)
            {
                var c = new System.Windows.Shapes.Rectangle
                {
                    Width = 10 + rnd.Next(8), Height = 16 + rnd.Next(10), RadiusX = 3, RadiusY = 3,
                    Fill = new SolidColorBrush(confetti[i % confetti.Length]), Opacity = 0.85, RenderTransformOrigin = new Point(0.5, 0.5),
                };
                var rot = new RotateTransform(rnd.Next(360));
                c.RenderTransform = rot;
                Canvas.SetLeft(c, rnd.NextDouble() * W);
                double dur = 5000 + rnd.Next(5000);
                c.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(-40, H + 40, TimeSpan.FromMilliseconds(dur)) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromMilliseconds(rnd.Next((int)dur)) });
                rot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(1800 + rnd.Next(2200))) { RepeatBehavior = RepeatBehavior.Forever });
                _endDeco.Children.Add(c);
            }
        }

        private void ShowEnd()
        {
            if (_book == null) return;
            _narrToken++;
            SetNarrating(false);
            UnhighlightNext();
            Speech.ScreenChanged();

            // Le décor du livre (fond d'écran) derrière un voile, la couverture sur la carte.
            _endBg.Source = _bg.Source;
            _endBg.Visibility = _bg.Source != null ? Visibility.Visible : Visibility.Collapsed;
            _endCover.Child = _book.Pages.Count > 0 ? PreviewOf(_book.Pages[0]) : null;

            _endButtons.Children.Clear();
            if (_book.Questions.Count > 0)
                _endButtons.Children.Add(EndButton("Le quiz", Color.FromRgb(0xFF, 0x9F, 0x43), IconTarget(), () => { _endRoot.Visibility = Visibility.Collapsed; StartQuiz(); }));
            _endButtons.Children.Add(EndButton("Relire", Color.FromRgb(0x7E, 0x3F, 0xF2), IconRepeat(), () => { _endRoot.Visibility = Visibility.Collapsed; ShowPage(0); }));
            _endButtons.Children.Add(EndButton("Un autre livre", Color.FromRgb(0x2E, 0xB8, 0x8A), IconBooks(), () => { _endRoot.Visibility = Visibility.Collapsed; ShowMenu(); }));
            _endRoot.Visibility = Visibility.Visible;
            _endRoot.Dispatcher.BeginInvoke(new Action(BuildEndDeco), DispatcherPriority.Loaded);
            Speech.Say("C'est la fin de l'histoire. Bravo !");
        }

        private void UpdateDots(int n)
        {
            _dots.Children.Clear();
            _dots.Visibility = n <= 16 ? Visibility.Visible : Visibility.Collapsed;
            if (n > 16) return;
            for (int k = 0; k < n; k++)
            {
                bool cur = k == _index;
                _dots.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = cur ? 16 : 11,
                    Height = cur ? 16 : 11,
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Fill = cur ? new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)) : k < _index ? Violet : new SolidColorBrush(Color.FromRgb(0xD9, 0xD0, 0xE8)),
                });
            }
        }

        // --- Mode jeune enfant : le texte par morceaux, avec des pictos ---
        private void BuildTiles(BookPage page)
        {
            double width = Math.Max(300, _wordHost.Width - 12);
            int gi = 0;
            foreach (var chunk in SplitChunks(page.Text))
            {
                foreach (var tok in chunk.Split(' '))
                    if (tok.Any(char.IsLetterOrDigit)) _wordToGroup.Add(gi);
                int idx = gi++;
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var picto = PictoFor(chunk, page);
                if (picto != null)
                {
                    var pic = new Border
                    {
                        Width = 104, Height = 104, CornerRadius = new CornerRadius(18),
                        Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF6, 0xEC)),
                        Clip = new RectangleGeometry(new Rect(0, 0, 104, 104), 18, 18),
                        Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center,
                        Child = new Image { Source = picto, Stretch = Stretch.UniformToFill },
                    };
                    row.Children.Add(pic);
                }
                var tb = new TextBlock
                {
                    Text = chunk, FontSize = 36, FontWeight = FontWeights.SemiBold, Foreground = Dark,
                    TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(tb, 1);
                row.Children.Add(tb);
                var tile = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xDE, 0xF5)),
                    BorderThickness = new Thickness(3),
                    CornerRadius = new CornerRadius(24),
                    Padding = new Thickness(14, 10, 18, 10),
                    Margin = new Thickness(6),
                    Width = width,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Child = row,
                };
                tile.MouseLeftButtonDown += (s, e) => OnGroupClicked(idx);
                GazeTargets.SetIsTarget(tile, true); // la tuile entière est l'aimant du regard
                _groups.Add(tb);
                _tiles.Add(tile);
                _wordHost.Children.Add(tile);
            }
        }

        /// <summary>Les morceaux : une phrase, ou un bout de phrase (coupé aux virgules) de 9 mots au plus.</summary>
        private static List<string> SplitChunks(string text)
        {
            var chunks = new List<string>();
            foreach (var sentence in PiperTts.SplitSentences(text))
            {
                int start = chunks.Count;
                if (CountWords(sentence) <= 9) { chunks.Add(sentence); continue; }
                var cur = new List<string>();
                foreach (var w in sentence.Split(' ').Where(x => x.Length > 0))
                {
                    cur.Add(w);
                    int n = CountWords(string.Join(" ", cur));
                    bool boundary = w.EndsWith(",") || w.EndsWith(":") || w.EndsWith(";") || w == "—" || w == "-";
                    if ((boundary && n >= 3) || n >= 9) { chunks.Add(string.Join(" ", cur)); cur.Clear(); }
                }
                if (cur.Count > 0)
                {
                    var rest = string.Join(" ", cur);
                    if (CountWords(rest) < 3 && chunks.Count > start) chunks[chunks.Count - 1] += " " + rest; // trop court : collé au précédent
                    else chunks.Add(rest);
                }
            }
            return chunks;
        }

        private static int CountWords(string s) => s.Split(' ').Count(t => t.Any(char.IsLetterOrDigit));

        /// <summary>
        /// Le picto d'un morceau : d'abord une zone de l'illustration dont le nom
        /// apparaît dans le morceau (on découpe l'image : c'est LE personnage de
        /// l'histoire), sinon une image de la banque livrée (chat, maison, pomme…).
        /// </summary>
        private ImageSource PictoFor(string chunk, BookPage page)
        {
            var words = chunk.Split(' ').Select(NormalizeWord).Where(w => w.Length >= 3).ToList();
            if (words.Count == 0) return null;
            if (_pageBitmap != null && page.Zones != null)
            {
                foreach (var z in page.Zones)
                {
                    var parts = z.Label.Split(' ').Select(NormalizeWord).Where(p => p.Length >= 3);
                    if (!parts.Any(p => words.Contains(p))) continue;
                    try
                    {
                        int W = _pageBitmap.PixelWidth, H = _pageBitmap.PixelHeight;
                        int x = Math.Max(0, Math.Min(W - 2, (int)(z.Left / 100 * W)));
                        int y = Math.Max(0, Math.Min(H - 2, (int)(z.Top / 100 * H)));
                        int w = Math.Max(2, Math.Min(W - x, (int)(z.Width / 100 * W)));
                        int h = Math.Max(2, Math.Min(H - y, (int)(z.Height / 100 * H)));
                        var crop = new CroppedBitmap(_pageBitmap, new Int32Rect(x, y, w, h));
                        crop.Freeze();
                        return crop;
                    }
                    catch { }
                }
            }
            foreach (var w in words)
            {
                var src = Art.Find(w) ?? (w.EndsWith("s") && w.Length > 3 ? Art.Find(w.Substring(0, w.Length - 1)) : null);
                if (src != null) return src;
            }
            return null;
        }

        // --- Lecture au regard ---
        private static List<string> SplitGroups(string text)
        {
            var groups = new List<string>();
            var buffer = "";
            foreach (var token in text.Split(' '))
            {
                bool punctOnly = token.Trim().Length > 0 && !char.IsLetterOrDigit(token.Trim()[0]);
                if (punctOnly && groups.Count > 0 && buffer.Length == 0)
                {
                    groups[groups.Count - 1] += " " + token; // « ! » collé au groupe précédent
                    continue;
                }
                buffer = buffer.Length == 0 ? token : buffer + " " + token;
                var bare = token.Trim().TrimEnd('.', ',', '!', '?', ';', ':').ToLowerInvariant();
                if (!SmallWords.Contains(bare))
                {
                    groups.Add(buffer); // mot « plein » : le groupe se termine ici
                    buffer = "";
                }
            }
            if (buffer.Length > 0) groups.Add(buffer);
            return groups;
        }

        private void ShowPage(int i)
        {
            if (_book == null || _book.Pages.Count == 0) return;
            _narrToken++;          // la fin de lecture de l'ancienne page ne compte plus
            SetNarrating(false);
            UnhighlightNext();
            int n = _book.Pages.Count;
            _index = ((i % n) + n) % n;
            var page = _book.Pages[_index];
            UpdateDots(n);
            var mode = StoryLayouts.Resolve(page.Layout, _book.Layout);
            if (mode != _layoutMode) { _layoutMode = mode; ApplyLayout(); }
            bool onImage = _layoutMode == StoryLayouts.OnImage;
            _illus.Content = new Viewbox
            {
                Child = MakeIllustration(page),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = onImage ? HorizontalAlignment.Left : HorizontalAlignment.Center, // calée à gauche : le texte vient sur sa partie droite
            };
            _textCard.Background = new SolidColorBrush(Color.FromArgb(onImage ? (byte)0xEC : (byte)0xD9, 0xFF, 0xFF, 0xFF));
            PositionOverlayText();
            PositionOverlayTextSoon();
            _pageInfo.Text = $"{_index + 1} / {n}";

            _wordHost.Children.Clear();
            _groups.Clear();
            _tiles.Clear();
            _readIndex = 0;
            _pageText = page.Text;
            _wordToGroup.Clear();

            if (LittleMode)
            {
                BuildTiles(page);
                Refresh();
                if (_listenMode) StartNarration();
                else Speech.Say("À toi de lire !");
                return;
            }

            // Pages courtes en grandes lettres ; pages longues un peu plus petites
            // (le panneau se réduit encore tout seul si besoin).
            double fontSize = page.Text.Length <= 140 ? 52 : page.Text.Length <= 260 ? 46 : 40;
            int gi = 0;
            foreach (var g in SplitGroups(page.Text))
            {
                // Nombre de mots réellement prononcés dans ce groupe (pour la
                // synchronisation de l'histoire racontée).
                foreach (var tok in g.Split(' '))
                    if (tok.Any(char.IsLetterOrDigit)) _wordToGroup.Add(gi);
                var tb = new TextBlock
                {
                    Text = g,
                    FontSize = fontSize,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Dark,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 1300,
                    Padding = new Thickness(14, 8, 14, 8),
                    Margin = new Thickness(5, 6, 5, 6),
                    Background = Brushes.Transparent,
                };
                int idx = gi++;
                // Le dwell injecte un vrai clic : on lit le groupe si c'est le courant.
                tb.MouseLeftButtonDown += (s, e) => OnGroupClicked(idx);
                GazeTargets.SetIsTarget(tb, true); // aimant du regard sur chaque groupe de mots
                _groups.Add(tb);
                _wordHost.Children.Add(tb);
            }

            Refresh();
            if (_listenMode) StartNarration();   // mode lecture : la page est racontée tout de suite
            else Speech.Say("À toi de lire !");
        }

        // Histoire racontée : la voix lit toute la page ; les groupes se
        // surlignent au fil des mots, et les zones s'illuminent.
        private void ListenPage()
        {
            if (_groups.Count == 0) return;
            _readIndex = 0;
            Refresh();
            PulseMatchingZones(_groups[0].Text);
            int tok = _narrToken;

            Speech.SayPage(
                _pageText,
                wordIdx => Dispatcher.Invoke(() =>
                {
                    if (_wordToGroup.Count == 0) return;
                    int g = _wordToGroup[Math.Min(wordIdx, _wordToGroup.Count - 1)];
                    if (g != _readIndex && g < _groups.Count)
                    {
                        _readIndex = g;
                        Refresh();
                        PulseMatchingZones(_groups[g].Text);
                    }
                }),
                () => Dispatcher.Invoke(() =>
                {
                    _readIndex = 0; // la page se remet au début pour la lecture au regard
                    Refresh();
                    if (tok != _narrToken) return;
                    SetNarrating(false);
                    OnTextFinished();
                }));
        }

        private void OnGroupClicked(int idx)
        {
            if (_narrating) return; // pendant la narration, les mots ne répondent pas
            if (idx != _readIndex || _readIndex >= _groups.Count) return;
            Speech.Say(_groups[idx].Text);
            PulseMatchingZones(_groups[idx].Text); // la zone correspondante s'illumine
            _readIndex++;
            Refresh();
            if (_readIndex >= _groups.Count)
            {
                Speech.Say("Bravo !");
                OnTextFinished(); // le grand bouton « page suivante » (ou « c'est la fin ! »)
            }
        }

        private void Delay(int ms, Action action)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            t.Tick += (s, e) => { t.Stop(); action(); };
            t.Start();
        }

        // Met à jour les couleurs : lus = gris, courant = surligné, à venir = foncé.
        private void Refresh()
        {
            if (_tiles.Count > 0)
            {
                var soft = new SolidColorBrush(Color.FromRgb(0xE6, 0xDE, 0xF5));
                for (int k = 0; k < _tiles.Count && k < _groups.Count; k++)
                {
                    var t = _tiles[k]; var tb = _groups[k];
                    if (k < _readIndex)
                    {
                        t.Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF0, 0xF8)); t.BorderBrush = soft; t.Opacity = 0.8;
                        tb.Foreground = Grey; tb.FontWeight = FontWeights.SemiBold;
                    }
                    else if (k == _readIndex)
                    {
                        t.Background = Yellow; t.BorderBrush = Violet; t.Opacity = 1;
                        tb.Foreground = Violet; tb.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        t.Background = Brushes.White; t.BorderBrush = soft; t.Opacity = 1;
                        tb.Foreground = Dark; tb.FontWeight = FontWeights.SemiBold;
                    }
                }
                return;
            }
            for (int k = 0; k < _groups.Count; k++)
            {
                if (k < _readIndex)
                {
                    _groups[k].Foreground = Grey;
                    _groups[k].Background = Brushes.Transparent;
                    _groups[k].FontWeight = FontWeights.SemiBold;
                }
                else if (k == _readIndex)
                {
                    _groups[k].Foreground = Violet;
                    _groups[k].Background = Yellow;
                    _groups[k].FontWeight = FontWeights.Bold;
                }
                else
                {
                    _groups[k].Foreground = Dark;
                    _groups[k].Background = Brushes.Transparent;
                    _groups[k].FontWeight = FontWeights.SemiBold;
                }
            }
        }

        // ------------------------------------------------------------------
        // Quiz de fin d'histoire (Vrai/Faux ou choix d'image, joué au regard)
        // ------------------------------------------------------------------
        private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(0x7B, 0xC9, 0x6F));
        private static readonly Brush Red = new SolidColorBrush(Color.FromRgb(0xF2, 0x6D, 0x6D));

        private UIElement BuildQuizRoot()
        {
            _quizRoot = new Grid { Visibility = Visibility.Collapsed };
            _quizRoot.Background = new LinearGradientBrush(
                Color.FromRgb(0xFF, 0xF8, 0xE6), Color.FromRgb(0xE9, 0xF7, 0xFF), 90);
            _quizRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _quizRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _quizRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _quizInfo = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Violet,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
            };
            Grid.SetRow(_quizInfo, 0);
            _quizRoot.Children.Add(_quizInfo);

            _quizPrompt = new TextBlock
            {
                FontSize = 46,
                FontWeight = FontWeights.Bold,
                Foreground = Dark,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 1200,
                Margin = new Thickness(24, 12, 24, 8),
            };
            Grid.SetRow(_quizPrompt, 1);
            _quizRoot.Children.Add(_quizPrompt);

            _quizAnswers = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(_quizAnswers, 2);
            _quizRoot.Children.Add(_quizAnswers);

            var back = new Button
            {
                Style = (Style)Application.Current.Resources["BackButton"],
                Content = "📚  Livres",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(16),
            };
            back.Click += (s, e) => { _quizRoot.Visibility = Visibility.Collapsed; ShowMenu(); };
            _quizRoot.Children.Add(back);
            return _quizRoot;
        }

        private void StartQuiz()
        {
            if (_book == null || _book.Questions.Count == 0) return;
            _narrToken++;
            SetNarrating(false);
            UnhighlightNext();
            _endRoot.Visibility = Visibility.Collapsed;
            _quizIndex = 0;
            _menuRoot.Visibility = Visibility.Collapsed;
            _readerRoot.Visibility = Visibility.Collapsed;
            _quizRoot.Visibility = Visibility.Visible;
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            if (_book == null || _quizIndex >= _book.Questions.Count) { FinishQuiz(); return; }
            var q = _book.Questions[_quizIndex];
            _quizLocked = false;
            _quizInfo.Text = $"Question {_quizIndex + 1} / {_book.Questions.Count}";
            _quizPrompt.Text = q.Text;
            _quizAnswers.Children.Clear();
            Speech.Say(q.Text);

            if (q.Kind == QuizKind.TrueFalse)
            {
                _quizAnswers.Children.Add(VfCard("✅", "Vrai", Green, q.Answer == true));
                _quizAnswers.Children.Add(VfCard("❌", "Faux", Red, q.Answer == false));
            }
            else
            {
                foreach (var c in q.Choices)
                {
                    var choice = c;
                    var card = QuizCard(new Viewbox { Child = ChoiceVisual(choice), Stretch = Stretch.Uniform }, Brushes.White);
                    card.Click += (s, e) => AnswerQuestion(choice.Correct, card);
                    _quizAnswers.Children.Add(card);
                }
            }
        }

        private Button VfCard(string glyph, string label, Brush bg, bool correct)
        {
            var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            inner.Children.Add(new TextBlock { Text = glyph, FontSize = 84, HorizontalAlignment = HorizontalAlignment.Center });
            inner.Children.Add(new TextBlock { Text = label, FontSize = 32, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            var card = QuizCard(inner, bg);
            card.Click += (s, e) => AnswerQuestion(correct, card);
            return card;
        }

        private void AnswerQuestion(bool correct, FrameworkElement card)
        {
            if (_quizLocked) return;
            if (correct)
            {
                _quizLocked = true;
                RewardStore.Add();               // une étoile ⭐
                Speech.Say("Bravo !");
                PopEl(card);
                _quizIndex++;
                Delay(1500, ShowQuestion);
            }
            else
            {
                ShakeEl(card);
                Speech.Say("Essaie encore !");
            }
        }

        private void FinishQuiz()
        {
            _quizLocked = true;
            _quizInfo.Text = "";
            _quizPrompt.Text = "🌟 Bravo, tu as tout trouvé ! 🌟";
            _quizAnswers.Children.Clear();
            Speech.Say("Bravo ! Tu as tout trouvé !");

            var again = QuizCard(BigGlyph("🔁", "Encore"), Brushes.White);
            again.Click += (s, e) => StartQuiz();
            var lib = QuizCard(BigGlyph("📚", "Livres"), Brushes.White);
            lib.Click += (s, e) => { _quizRoot.Visibility = Visibility.Collapsed; ShowMenu(); };
            _quizAnswers.Children.Add(again);
            _quizAnswers.Children.Add(lib);
        }

        private static UIElement BigGlyph(string glyph, string label)
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = glyph, FontSize = 76, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = label, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center });
            return sp;
        }

        private static UIElement ChoiceVisual(QuizChoice c)
        {
            if (!string.IsNullOrEmpty(c.Draw))
            {
                try { return CartoonArt.Draw(c.Draw); } catch { }
            }
            if (!string.IsNullOrEmpty(c.ImagePath))
            {
                try { return new Image { Source = UserContent.LoadBitmap(c.ImagePath, 500), Stretch = Stretch.Uniform }; }
                catch { }
            }
            return new TextBlock { Text = "🖼", FontSize = 90, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }

        // Carte-réponse (grande cible cliquable au regard), apparence entièrement
        // maîtrisée pour ne pas dépendre d'un style de bouton particulier.
        private static Button QuizCard(UIElement inner, Brush bg)
        {
            var card = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(12),
                Child = inner,
                Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.2 },
            };
            return new Button
            {
                Template = CardTemplate(),
                Content = card,
                Background = Brushes.Transparent,
                Width = 230,
                Height = 230,
                Margin = new Thickness(18),
            };
        }

        private static ControlTemplate CardTemplate()
        {
            var tpl = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            border.AppendChild(cp);
            tpl.VisualTree = border;
            return tpl;
        }

        private static void PopEl(FrameworkElement el)
        {
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(1, 1);
            el.RenderTransform = st;
            var a = new DoubleAnimation(1, 1.16, TimeSpan.FromMilliseconds(190)) { AutoReverse = true };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }

        private static void ShakeEl(FrameworkElement el)
        {
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            var tt = new TranslateTransform();
            el.RenderTransform = tt;
            var a = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(450) };
            foreach (var (p, v) in new[] { (0.0, 0.0), (0.2, -10.0), (0.4, 9.0), (0.6, -7.0), (0.8, 5.0), (1.0, 0.0) })
                a.KeyFrames.Add(new LinearDoubleKeyFrame(v, KeyTime.FromPercent(p)));
            tt.BeginAnimation(TranslateTransform.XProperty, a);
        }
    }
}
