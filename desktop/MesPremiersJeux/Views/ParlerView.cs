using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 💬 Le monde « Parler » : le tableau de communication en pleine page, et
    /// l'ÉCOUTE : 👂 écoute la personne qui parle à l'enfant, affiche la phrase
    /// entendue et propose des réponses en PLEIN ÉCRAN, en grandes cases avec
    /// pictos (banque Mulberry, photos de la famille, mots du tableau). L'IA
    /// (eyzi.fr) adapte la grille au type de question ; sans IA, grille devinée.
    /// </summary>
    public sealed class ParlerView : UserControl
    {
        private readonly CommBoard _board;
        private readonly Border _panel;
        private readonly Border _lieuPrompt;
        private readonly TextBlock _lieuQuestion;
        private string _reseauInconnu = "";
        /// <summary>L'accompagnant ou l'enfant a changé le lieu à la main.</summary>
        public event Action<string> LieuChoisi;
        private readonly Grid _answersHost;
        private readonly UniformGrid _answers;
        private readonly TextBlock _heard, _state, _wait, _ia;
        private readonly Button _ear, _back, _lieu;
        private readonly Ellipse _led;
        private Settings _settings;
        private readonly List<Suggest.Echange> _historique = new List<Suggest.Echange>();
        private Suggest.Echange _enCours;
        private string _speculatif;            // la phrase partielle pour laquelle une grille est déjà en route
        private string _partiel;
        private readonly System.Windows.Threading.DispatcherTimer _partielTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        private int _grilleRows = 1, _grilleCols = 3;
        private string _grilleType = "autre";
        public event Action BackRequested;

        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));

        public ParlerView()
        {
            var root = new Grid();
            root.Background = new LinearGradientBrush(Color.FromRgb(0xF1, 0xE6, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- Le bandeau d'écoute : 👂, le voyant et l'état, la phrase entendue, retour au tableau ---
            var head = new DockPanel { LastChildFill = true };
            _ear = GrosBouton("👂", "J'écoute", Color.FromRgb(0xE2, 0xD4, 0xFF));
            _ear.Click += async (s, e) => { if (Listening.Enabled) await Listening.ListenOnce(); else Speech.SayAlways("L'écoute n'est pas réglée. C'est dans les réglages, page Nuage."); };
            DockPanel.SetDock(_ear, Dock.Left);
            head.Children.Add(_ear);
            _back = GrosBouton("🧩", "Tableau", Color.FromRgb(0xD9, 0xF5, 0xE0));
            _back.Click += (s, e) => MontrerTableau();
            _back.Visibility = Visibility.Collapsed;
            DockPanel.SetDock(_back, Dock.Right);
            head.Children.Add(_back);
            // Où est l'enfant : maison, école, dehors, voiture — un regard change le lieu, l'IA en tient compte.
            _lieu = GrosBouton("🏠", "Maison", Color.FromRgb(0xFF, 0xF0, 0xC8));
            Gaze.GazeTargets.SetParentOnly(_lieu, false);
            _lieu.Click += (s, e) => { CyclerLieu(); };
            DockPanel.SetDock(_lieu, Dock.Right);
            head.Children.Add(_lieu);
            _led = new Ellipse { Width = 22, Height = 22, Fill = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _state = new TextBlock { FontSize = 18, Foreground = Dark, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            _heard = new TextBlock { FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 14, 0) };
            texts.Children.Add(_heard);
            texts.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { _led, _state }, Margin = new Thickness(0, 4, 0, 0) });
            _ia = new TextBlock { FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0x5A, 0x00)), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
            texts.Children.Add(_ia);
            head.Children.Add(texts);
            _panel = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(24), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(12, 10, 12, 0),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.18 },
                Child = head, Visibility = Visibility.Collapsed,
            };
            // 📍 Un Wi-Fi inconnu : « on est où ? », pour l'accompagnant (souris ou doigt, pas le regard).
            _lieuQuestion = new TextBlock { FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Dark, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), TextWrapping = TextWrapping.Wrap };
            var choix = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var l in Lib.Lieux.Tous)
            {
                var cle = l.Cle;
                var b = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = 130, Height = 64, Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF0, 0xC8)), Content = new TextBlock { Text = l.Icone + " " + l.Nom, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Dark } };
                Gaze.GazeTargets.SetParentOnly(b, true);
                b.Click += (s, e) => RepondreLieu(cle);
                choix.Children.Add(b);
            }
            var jamais = new Button { Style = (Style)Application.Current.Resources["AnswerButton"], Width = 150, Height = 64, Background = Brushes.WhiteSmoke, Content = new TextBlock { Text = "Ne plus demander", FontSize = 16, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center } };
            Gaze.GazeTargets.SetParentOnly(jamais, true);
            jamais.Click += (s, e) => RepondreLieu(Lib.Lieux.Ignorer);
            choix.Children.Add(jamais);
            var lieuRow = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(choix, Dock.Right);
            lieuRow.Children.Add(choix);
            lieuRow.Children.Add(_lieuQuestion);
            _lieuPrompt = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(18), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(12, 8, 12, 0),
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 1, Opacity = 0.15 },
                Child = lieuRow, Visibility = Visibility.Collapsed,
            };
            Gaze.GazeTargets.SetParentOnly(_lieuPrompt, true);
            Emoji.FixTree(_lieuPrompt);
            var top = new StackPanel();
            top.Children.Add(_panel);
            top.Children.Add(_lieuPrompt);
            root.Children.Add(top);

            // --- Le tableau, ou la grille de réponses en plein écran ---
            _board = new CommBoard();
            _board.CloseRequested += () => BackRequested?.Invoke();
            Grid.SetRow(_board, 1);
            root.Children.Add(_board);

            _answers = new UniformGrid { Margin = new Thickness(10) };
            _wait = new TextBlock { FontSize = 34, Foreground = Dark, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
            _answersHost = new Grid { Visibility = Visibility.Collapsed, Children = { _answers, _wait } };
            Grid.SetRow(_answersHost, 1);
            root.Children.Add(_answersHost);
            Content = root;

            Listening.StatusChanged += st => Dispatcher.BeginInvoke(new Action(() => MontrerEtat(st)));
            // Pendant que la personne parle encore : dès que le texte partiel ne bouge plus 600 ms, la grille part.
            Listening.Partial += t => Dispatcher.BeginInvoke(new Action(() => { _partiel = t; _partielTimer.Stop(); _partielTimer.Start(); }));
            _partielTimer.Tick += (s, e) =>
            {
                _partielTimer.Stop();
                var t = (_partiel ?? "").Trim();
                if (t.Length < 8 || !IsVisible || _settings == null || !_settings.ListenAnswers) return;
                if (_speculatif != null && Pictos.Norm(_speculatif) == Pictos.Norm(t)) return;
                _speculatif = t;
                ShowHeard(t, speculatif: true);
            };
            Emoji.FixTree(head);
        }

        private static (string Cle, string Icone, string Nom)[] Lieux => Lib.Lieux.Tous;

        private void MontrerLieu()
        {
            var cle = _settings?.ParlerLieu ?? "maison";
            var l = Lieux.FirstOrDefault(x => x.Cle == cle); if (l.Cle == null) l = Lieux[0];
            _lieu.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Children = { Art.Visual("", l.Icone, 48), new TextBlock { Text = l.Nom, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
            };
        }

        private void CyclerLieu()
        {
            if (_settings == null) return;
            int i = Array.FindIndex(Lieux, x => x.Cle == (_settings.ParlerLieu ?? "maison"));
            var next = Lieux[(i + 1 + Lieux.Length) % Lieux.Length];
            _settings.ParlerLieu = next.Cle;
            _settings.Save();
            MontrerLieu();
            Speech.SayAlways(next.Nom + " !");
            try { LieuChoisi?.Invoke(next.Cle); } catch { }
        }

        /// <summary>Le Wi-Fi a dit où l'on est : on l'affiche (le réglage est déjà enregistré par la fenêtre principale).</summary>
        public void AfficherLieu(string lieu)
        {
            if (_settings == null || string.IsNullOrWhiteSpace(lieu)) return;
            _settings.ParlerLieu = lieu;
            MontrerLieu();
            if (_reseauInconnu.Length > 0 && Lib.Lieux.LieuDe(_reseauInconnu).Length > 0) { _reseauInconnu = ""; _lieuPrompt.Visibility = Visibility.Collapsed; }
        }

        /// <summary>Un réseau jamais vu : on demande une fois à l'accompagnant.</summary>
        public void DemanderLieu(string reseau)
        {
            if (string.IsNullOrWhiteSpace(reseau)) return;
            _reseauInconnu = reseau;
            _lieuQuestion.Text = "📍 Nouveau Wi-Fi « " + reseau + " » : on est où ?";
            Emoji.FixTree(_lieuQuestion);
            if (_settings != null && (_settings.ListenMode ?? "off") != "off" && _settings.LieuAuto) _lieuPrompt.Visibility = Visibility.Visible;
        }

        private void RepondreLieu(string cle)
        {
            var r = _reseauInconnu; _reseauInconnu = "";
            _lieuPrompt.Visibility = Visibility.Collapsed;
            if (r.Length > 0) Lib.Lieux.Associer(r, cle); // → Lieux.Changed → la fenêtre principale enregistre et nous rappelle
        }

        private Button GrosBouton(string icone, string label, Color fond)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 170, Height = 100, Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(fond),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", icone, 48), new TextBlock { Text = label, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } },
                },
            };
            return b;
        }

        /// <summary>On entre dans le monde : la grille s'annonce.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            MontrerTableau();
            try { _board.Open(); } catch (Exception ex) { Log.Write("parler", "Ouverture : " + ex.Message); }
            // Le bandeau d'écoute n'est là que si l'écoute est réglée.
            _panel.Visibility = (_settings.ListenMode ?? "off") != "off" ? Visibility.Visible : Visibility.Collapsed;
            MontrerEtat(Listening.Status);
            MontrerLieu();
            _lieuPrompt.Visibility = _reseauInconnu.Length > 0 && (_settings.ListenMode ?? "off") != "off" && _settings.LieuAuto ? Visibility.Visible : Visibility.Collapsed;
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings); // le site se réveille pendant qu'on parle
        }

        /// <summary>On quitte le monde.</summary>
        public void Leave() { _board.MarkClosed(); }

        private void MontrerTableau()
        {
            _answersHost.Visibility = Visibility.Collapsed;
            _board.Visibility = Visibility.Visible;
            _back.Visibility = Visibility.Collapsed;
            _heard.Text = "";
        }

        private void MontrerReponses()
        {
            _board.Visibility = Visibility.Collapsed;
            _answersHost.Visibility = Visibility.Visible;
            _back.Visibility = Visibility.Visible;
            _panel.Visibility = Visibility.Visible;
        }

        private void MontrerEtat(string st)
        {
            switch (st)
            {
                case "veille": _led.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0xB8, 0x8A)); _state.Text = "Je veille : dis « Dis Eyzi » puis ta question."; break;
                case "ecoute": _led.Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x3A, 0x3A)); _state.Text = "J'écoute…"; Pulse(); break;
                case "pret": _led.Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x9B, 0xFF)); _state.Text = "Appuie sur 👂 puis parle à " + (_settings?.ChildName ?? "l'enfant") + "."; break;
                case "erreur": _led.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x9F, 0x43)); _state.Text = "L'écoute ne marche pas : " + Listening.LastError; break;
                default: _led.Fill = Brushes.Gray; _state.Text = "Écoute éteinte (réglages → Nuage)."; break;
            }
            Emoji.Fix(_state);
        }

        private void Pulse()
        {
            var a = new DoubleAnimation(1, 0.3, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(6) };
            _led.BeginAnimation(OpacityProperty, a);
        }

        /// <summary>
        /// Une phrase entendue : la grille se remplit case par case à mesure que l'IA écrit.
        /// « speculatif » : la phrase n'est pas finie (texte partiel stable) ; si la phrase finale est la
        /// même, la grille déjà en route est gardée, sinon on recommence.
        /// </summary>
        public async void ShowHeard(string text, bool speculatif = false)
        {
            // Les réglages peuvent avoir changé dans l'administration pendant que Parler est ouvert : on relit le fichier.
            if (!speculatif || _settings == null) _settings = Settings.Load();
            if (!speculatif)
            {
                _partielTimer.Stop();
                if (_speculatif != null && _enCours != null && Pictos.Norm(_speculatif) == Pictos.Norm(text))
                {
                    // La phrase finale est celle qu'on avait anticipée : on garde tout, on corrige juste le texte.
                    _speculatif = null;
                    _enCours.Question = text;
                    _heard.Text = "« " + text + " »" + (_heard.Text.Contains("  ·  ") ? _heard.Text.Substring(_heard.Text.IndexOf("  ·  ", StringComparison.Ordinal)) : "");
                    return;
                }
                _speculatif = null;
            }
            MontrerReponses();
            _heard.Text = "« " + text + " »";
            _enCours = new Suggest.Echange { Question = text };
            _historique.Add(_enCours);
            while (_historique.Count > 3) _historique.RemoveAt(0);
            var histo = _historique.Take(_historique.Count - 1).ToList();
            var monEchange = _enCours;

            // « ou du chocolat » juste après « quel parfum de yaourt ? » : la phrase complète la question d'avant.
            var precedent = histo.LastOrDefault();
            bool suite = precedent != null && (DateTime.Now - precedent.At).TotalSeconds < 120 && Suggest.EstSuite(text);
            monEchange.Suite = suite;
            var secours = suite ? Suggest.DefaultsSuite(text, precedent) : Suggest.Defaults(text);
            monEchange.Type = secours.Type;
            if (suite) _heard.Text = "« " + precedent.Question + " »  +  « " + text + " »";
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey);
            if (!enLigne)
            {
                PreparerGrille(secours.Type, secours.Reponses.Count);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                Ia("⚠️ Réponses devinées, sans IA : " + (!_settings.ListenAnswers ? "la case « Proposer des réponses » est décochée (réglages → Nuage → Écoute)." : "pas de licence en ligne."));
                if (!speculatif) Speech.SayAlways(text);
                return;
            }

            _answers.Children.Clear();
            Ia(null);
            _wait.Visibility = Visibility.Visible;
            _wait.Text = "L'IA cherche des réponses…";
            var t0 = DateTime.Now;
            var tick = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            tick.Tick += (s, e) => _wait.Text = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "L'IA cherche des réponses… {0:0.0} s", (DateTime.Now - t0).TotalSeconds);
            tick.Start();
            if (!speculatif) Speech.SayAlways(text);

            Suggest.Resultat res;
            try
            {
                var vocab = CommBoard.Vocabulaire().Select(m => new Suggest.Mot { Cle = m.Cle, Label = m.Label }).ToList();
                res = await Suggest.PourFlux(_settings, text, vocab, histo, Suggest.ContexteActuel(_settings),
                    tete => Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_enCours != monEchange) return;
                        tick.Stop(); _wait.Visibility = Visibility.Collapsed;
                        monEchange.Type = tete.Type;
                        PreparerGrille(tete.Type, 0);
                        if (tete.Simple != null && tete.Simple != text) _heard.Text = "« " + text + " »  ·  " + tete.Simple;
                    })),
                    rep => Dispatcher.BeginInvoke(new Action(() => { if (_enCours == monEchange) AjouterCase(rep, _grilleType); })), suite);
            }
            catch (Exception ex) { res = secours; res.Erreur = ex.Message; }
            tick.Stop();
            _wait.Visibility = Visibility.Collapsed;
            if (_enCours != monEchange) return; // une autre phrase est arrivée entre-temps
            if (res.EnLigne)
            {
                Ia("🤖 Réponses de l'IA · " + (res.Chrono ?? ""), ok: true);
                if (!speculatif && res.Simple != null && res.Simple != text) Speech.SayAlways(res.Simple);
            }
            else
            {
                PreparerGrille(secours.Type, secours.Reponses.Count);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                Ia("⚠️ Réponses devinées : l'IA n'a pas répondu" + (string.IsNullOrEmpty(res.Erreur) ? "." : " (" + res.Erreur + ")."));
            }
        }

        /// <summary>La ligne qui dit d'où viennent les réponses : elle reste affichée, quoi que fasse le voyant d'écoute.</summary>
        private void Ia(string text, bool ok = false)
        {
            if (string.IsNullOrEmpty(text)) { _ia.Visibility = Visibility.Collapsed; return; }
            _ia.Text = text;
            _ia.Foreground = new SolidColorBrush(ok ? Color.FromRgb(0x1E, 0x7A, 0x4C) : Color.FromRgb(0xB0, 0x5A, 0x00));
            _ia.Visibility = Visibility.Visible;
            Emoji.FixTree(_ia);
        }

        /// <summary>La grille se dimensionne d'avance selon le type (3 grandes cases pour oui / non, 2 rangées pour une question ouverte).</summary>
        private void PreparerGrille(string type, int nConnu)
        {
            _answers.Children.Clear();
            _grilleType = type ?? "autre";
            int attendu = nConnu > 0 ? nConnu : _grilleType == "ouverte" ? 8 : _grilleType == "choix" ? 4 : 3;
            _grilleRows = attendu <= 3 ? 1 : 2;
            _grilleCols = (int)Math.Ceiling(attendu / (double)_grilleRows);
            _answers.Rows = _grilleRows;
            _answers.Columns = _grilleCols;
        }

        /// <summary>Une case de plus, tout de suite. Au-delà de la place prévue, la grille s'élargit.</summary>
        private void AjouterCase(Suggest.Reponse r, string type)
        {
            if (_answers.Children.Count >= 8) return;
            if (_enCours != null && !string.IsNullOrEmpty(r.Texte)) _enCours.Propositions.Add(r.Texte);
            int n = _answers.Children.Count + 1;
            if (n > _grilleRows * _grilleCols)
            {
                if (_grilleRows == 1 && n > 3) { _grilleRows = 2; _grilleCols = (int)Math.Ceiling(n / 2.0); }
                else _grilleCols = (int)Math.Ceiling(n / (double)_grilleRows);
                _answers.Rows = _grilleRows; _answers.Columns = _grilleCols;
            }
            var cap = r;
            var mot = CommBoard.Trouver(r.Cle);
            bool grand = _grilleRows == 1;
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            st.Children.Add(new Viewbox { Child = Visuel(r, mot), Height = grand ? 260 : 170, Stretch = Stretch.Uniform });
            st.Children.Add(new TextBlock { Text = r.Texte, FontSize = grand ? 40 : 30, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 380, Margin = new Thickness(0, 8, 0, 0) });
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["MenuTile"],
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(10, 8, 10, 8), Child = st },
                Width = double.NaN, Height = double.NaN, Margin = new Thickness(8), Opacity = 0,
            };
            b.Click += (s, e) =>
            {
                var dit = mot != null && !string.IsNullOrEmpty(mot.Dit) ? mot.Dit : cap.Texte;
                if (mot == null || !VoiceBank.Play("comm-" + mot.Cle)) Speech.SayAlways(dit);
                if (_enCours != null) _enCours.Reponse = cap.Texte;
                ReponsesPref.Choisie(cap.Texte);
                GazeStats.RecordComm("ecoute-" + type);
                var sc = new ScaleTransform(1, 1); b.RenderTransformOrigin = new Point(0.5, 0.5); b.RenderTransform = sc;
                var pop = new DoubleAnimation(1, 1.06, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop); sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            };
            Emoji.FixTree(st);
            _answers.Children.Add(b);
            b.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }

        // Les réponses de tous les jours ont TOUJOURS le même visuel (le même que le tableau), quoi que dise l'IA.
        private static readonly Dictionary<string, (string Img, string Emoji)> Fixes = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["oui"] = ("comm-oui", "✅"), ["non"] = ("comm-non", "❌"), ["je ne sais pas"] = ("", "🤷"), ["les deux"] = ("", "🤲"), ["autre chose"] = ("", "💭"),
            ["d'accord"] = ("comm-oui", "👍"), ["ok"] = ("comm-oui", "👍"), ["attends"] = ("", "✋"), ["encore"] = ("comm-encore", "🔁"), ["plus tard"] = ("", "⏰"), ["un peu"] = ("", "🤏"),
            ["je t'aime"] = ("comm-jetaime", "❤️"), ["fini"] = ("comm-fini", "🏁"), ["aide-moi"] = ("comm-aide", "🙋"), ["stop"] = ("", "🛑"),
        };

        /// <summary>Le visuel d'une réponse : fixe pour les mots de tous les jours, sinon la photo ou l'image du tableau, le picto Mulberry, l'emoji.</summary>
        private static FrameworkElement Visuel(Suggest.Reponse r, CommBoard.Mot mot)
        {
            try
            {
                var cle = (r.Texte ?? "").Trim().TrimEnd('!', '.').ToLowerInvariant();
                if (Fixes.TryGetValue(cle, out var fx))
                {
                    var src = string.IsNullOrEmpty(fx.Img) ? null : Art.Find(fx.Img);
                    return src != null ? new Image { Source = src, Stretch = Stretch.Uniform } : Art.Visual("", fx.Emoji, 200);
                }
                if (mot != null)
                {
                    if (mot.Photo != null) return mot.Photo();
                    var src = Art.Find(mot.Img);
                    if (src != null) return new Image { Source = src, Stretch = Stretch.Uniform };
                }
                var p = Pictos.Picture(r.Picto, 200) ?? Pictos.Picture(r.Texte, 200);
                if (p != null) return p;
                if (!string.IsNullOrWhiteSpace(r.Emoji)) return Art.Visual("", r.Emoji.Trim(), 200);
            }
            catch { }
            return Art.Visual("", "💬", 200);
        }

    }
}
