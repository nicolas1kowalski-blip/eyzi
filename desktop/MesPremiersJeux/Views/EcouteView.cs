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

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 👂 L'ÉCOUTE, en surcouche : elle se pose sur n'importe quel écran quand
    /// quelqu'un dit « Dis Eyzi » (ou appuie sur le micro), et l'activité du
    /// dessous reprend quand elle se referme. Quatre états, pensés pour
    /// l'enfant seul : Attente (« Parle-moi »), Écoute (l'oreille rouge),
    /// Grille (la question en grand, les cases), Confirmation (ce qui a été
    /// dit, puis Encore · Autre chose · Fini). Tout ce qui sert à l'adulte
    /// tient sur la ligne grise du bas.
    /// </summary>
    public sealed class EcouteView : UserControl
    {
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Gris = new SolidColorBrush(Color.FromRgb(0x5F, 0x56, 0x78));
        private static readonly Brush Vert = new SolidColorBrush(Color.FromRgb(0x1E, 0x7A, 0x4C));
        private static readonly Brush Rouge = new SolidColorBrush(Color.FromRgb(0xE8, 0x35, 0x2F));

        public event Action CloseRequested;   // la maison, « Fini », ou le délai après une réponse
        public event Action MotsRequested;    // « Mes mots » : le tableau de communication (monde Parler)

        private Settings _settings;
        private readonly Grid _root, _attente, _ecoute, _grilleP, _confirm;
        private readonly TextBlock _parleMoi, _sousTitre, _partielText, _question, _wait, _confirmText, _status, _ia;
        private readonly UniformGrid _answers;
        private readonly Border _confirmVisual;
        private readonly Ellipse _anneau1, _anneau2;
        private readonly Border _bas;
        private readonly List<Suggest.Echange> _historique = new List<Suggest.Echange>();
        private Suggest.Echange _enCours;
        private string _speculatif, _partiel, _etat = "attente";
        private readonly DispatcherTimer _partielTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        private readonly DispatcherTimer _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        private int _grilleRows = 1, _grilleCols = 3;
        private string _grilleType = "autre", _statusEcoute = "", _statusIa = "";

        public string Etat => _etat;

        public EcouteView()
        {
            _root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xEC, 0xFF)) };
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ---------- Attente : « Parle-moi »
            _attente = new Grid();
            var ap = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            ap.Children.Add(Rond(Oreille(170), 260, Brushes.White));
            _parleMoi = new TextBlock { Text = "Parle-moi", FontSize = 64, FontWeight = FontWeights.ExtraBold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 22, 0, 6) };
            ap.Children.Add(_parleMoi);
            _sousTitre = new TextBlock { FontSize = 26, Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x65, 0x87)), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 760 };
            ap.Children.Add(_sousTitre);
            var mots = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"], Width = 380, Height = 100, Margin = new Thickness(0, 40, 0, 0), Background = Brushes.White,
                Content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Children = { Art.Visual("", "🧩", 48), new TextBlock { Text = "Mes mots", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } } },
            };
            mots.Click += (s, e) => { Arreter(); MotsRequested?.Invoke(); };
            ap.Children.Add(mots);
            _attente.Children.Add(ap);
            _root.Children.Add(_attente);

            // ---------- Écoute : l'oreille rouge qui pulse, la phrase qui s'écrit
            _ecoute = new Grid { Visibility = Visibility.Collapsed };
            var ep = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var cercle = new Grid { Width = 300, Height = 300 };
            _anneau1 = new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xD9, 0xD9)) };
            _anneau2 = new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0xB8)), Margin = new Thickness(26) };
            cercle.Children.Add(_anneau1); cercle.Children.Add(_anneau2);
            cercle.Children.Add(Rond(Oreille(150), 196, Brushes.White));
            ep.Children.Add(cercle);
            ep.Children.Add(new TextBlock { Text = "J'écoute…", FontSize = 64, FontWeight = FontWeights.ExtraBold, Foreground = Rouge, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 18, 0, 6) });
            _partielText = new TextBlock { FontSize = 30, Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x65, 0x87)), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 900, MinHeight = 40 };
            ep.Children.Add(_partielText);
            _ecoute.Children.Add(ep);
            _root.Children.Add(_ecoute);

            // ---------- Grille : la question en grand, les cases
            _grilleP = new Grid { Visibility = Visibility.Collapsed };
            _grilleP.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _grilleP.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _question = new TextBlock { FontSize = 46, FontWeight = FontWeights.ExtraBold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(60, 22, 60, 8), MaxWidth = 1500 };
            _grilleP.Children.Add(_question);
            _answers = new UniformGrid { Margin = new Thickness(40, 0, 40, 10) };
            _wait = new TextBlock { FontSize = 34, Foreground = Dark, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
            var gh = new Grid { Children = { _answers, _wait } };
            Grid.SetRow(gh, 1);
            _grilleP.Children.Add(gh);
            _root.Children.Add(_grilleP);

            // ---------- Confirmation : ce qui a été dit, puis la suite
            _confirm = new Grid { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromRgb(0xDF, 0xF5, 0xE7)) };
            _confirm.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _confirm.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var cp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _confirmVisual = new Border { Width = 300, Height = 300, CornerRadius = new CornerRadius(40), Background = Brushes.White, Padding = new Thickness(40) };
            cp.Children.Add(_confirmVisual);
            _confirmText = new TextBlock { FontSize = 72, FontWeight = FontWeights.ExtraBold, Foreground = Vert, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 1100, Margin = new Thickness(0, 16, 0, 0) };
            cp.Children.Add(_confirmText);
            _confirm.Children.Add(cp);
            var row = new UniformGrid { Columns = 3, Rows = 1, Height = 190, Margin = new Thickness(40, 0, 40, 20) };
            row.Children.Add(Suite("encore", "🔁", "Encore", () => { Speech.SayAlways("Encore !"); GazeStats.RecordComm("ecoute-encore"); Fermer(); }));
            row.Children.Add(Suite("", "💭", "Autre chose", () => { _autoClose.Stop(); Montrer("grille"); }));
            row.Children.Add(Suite("comm-fini", "🏁", "Fini", () => { GazeStats.RecordComm("ecoute-fini"); Fermer(); }));
            Grid.SetRow(row, 1);
            _confirm.Children.Add(row);
            _root.Children.Add(_confirm);

            // ---------- La ligne du bas, pour l'adulte : maison · état · micro
            var basGrid = new DockPanel { LastChildFill = true };
            var maison = PetitBouton("🏠", "Revenir à ce qu'on faisait");
            maison.Click += (s, e) => Fermer();
            DockPanel.SetDock(maison, Dock.Left);
            basGrid.Children.Add(maison);
            var micro = PetitBouton("👂", "Écouter une phrase");
            micro.Click += async (s, e) => { if (Listening.Enabled) await Listening.ListenOnce(); else _statusEcoute = "l'écoute n'est pas réglée (⚙ → Licence & nuage → Écoute)"; MontrerStatus(); };
            DockPanel.SetDock(micro, Dock.Right);
            basGrid.Children.Add(micro);
            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0) };
            _status = new TextBlock { FontSize = 15, Foreground = Gris, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center };
            _ia = new TextBlock { FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0x5A, 0x00)), TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, Visibility = Visibility.Collapsed };
            textes.Children.Add(_status); textes.Children.Add(_ia);
            basGrid.Children.Add(textes);
            _bas = new Border { Background = new SolidColorBrush(Color.FromRgb(0xE9, 0xE1, 0xF6)), Padding = new Thickness(10, 5, 10, 5), Child = basGrid };
            Gaze.GazeTargets.SetParentOnly(_bas, true);
            Grid.SetRow(_bas, 1);
            _root.Children.Add(_bas);

            Content = _root;
            Emoji.FixTree(_root);

            Listening.StatusChanged += st => Dispatcher.BeginInvoke(new Action(() => SurEtatEcoute(st)));
            Listening.Partial += t => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible) return;
                _partiel = t;
                if (_etat == "ecoute" || _etat == "attente" || _etat == "confirm") { if (_etat != "ecoute") Montrer("ecoute"); _partielText.Text = t; }
                _partielTimer.Stop(); _partielTimer.Start();
            }));
            _partielTimer.Tick += (s, e) =>
            {
                _partielTimer.Stop();
                var t = (_partiel ?? "").Trim();
                if (t.Length < 8 || !IsVisible || _settings == null || !_settings.ListenAnswers) return;
                if (_speculatif != null && Pictos.Norm(_speculatif) == Pictos.Norm(t)) return;
                _speculatif = t;
                ShowHeard(t, speculatif: true);
            };
            _autoClose.Tick += (s, e) => { _autoClose.Stop(); if (_etat == "confirm") Fermer(); };
        }

        // ------------------------------------------------------------------
        //  Ouvrir, fermer, les états
        // ------------------------------------------------------------------
        /// <summary>La surcouche s'ouvre : en attente, ou directement en écoute.</summary>
        public void Open(bool ecoute = false)
        {
            _settings = Settings.Load();
            _sousTitre.Text = "Pose une question à " + (string.IsNullOrWhiteSpace(_settings.ChildName) ? "l'enfant" : _settings.ChildName) + ", elle répondra avec ses yeux.";
            _statusIa = "";
            _ia.Visibility = Visibility.Collapsed;
            _partielText.Text = "";
            Montrer(ecoute ? "ecoute" : "attente");
            SurEtatEcoute(Listening.Status);
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings);
        }

        /// <summary>Le mot-clé a été entendu, ou le micro pressé : l'oreille rouge.</summary>
        public void Ecouter() { if (_etat != "grille") Montrer("ecoute"); }

        public void Fermer() { Arreter(); CloseRequested?.Invoke(); }

        private void Arreter() { _autoClose.Stop(); _partielTimer.Stop(); _speculatif = null; }

        private void Montrer(string etat)
        {
            _etat = etat;
            _attente.Visibility = etat == "attente" ? Visibility.Visible : Visibility.Collapsed;
            _ecoute.Visibility = etat == "ecoute" ? Visibility.Visible : Visibility.Collapsed;
            _grilleP.Visibility = etat == "grille" ? Visibility.Visible : Visibility.Collapsed;
            _confirm.Visibility = etat == "confirm" ? Visibility.Visible : Visibility.Collapsed;
            if (etat == "ecoute") Pulser(); else { _anneau1.BeginAnimation(OpacityProperty, null); _anneau2.BeginAnimation(OpacityProperty, null); }
        }

        private void Pulser()
        {
            var a = new DoubleAnimation(1, 0.35, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            _anneau1.BeginAnimation(OpacityProperty, a);
            var b = new DoubleAnimation(1, 0.5, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            _anneau2.BeginAnimation(OpacityProperty, b);
        }

        private void SurEtatEcoute(string st)
        {
            switch (st)
            {
                case "veille": _statusEcoute = "je veille sur « Dis Eyzi »"; break;
                case "ecoute": _statusEcoute = "j'écoute"; if (IsVisible && _etat != "grille") Montrer("ecoute"); break;
                case "pret": _statusEcoute = "écoute prête (micro en bas à droite)"; break;
                case "erreur": _statusEcoute = "écoute en panne : " + Listening.LastError; break;
                default: _statusEcoute = "écoute éteinte"; break;
            }
            MontrerStatus();
        }

        private void MontrerStatus()
        {
            var lieu = Lib.Lieux.Nom(_settings?.ParlerLieu ?? "maison");
            _status.Text = lieu + " · " + _statusEcoute + (string.IsNullOrEmpty(_statusIa) ? "" : " · " + _statusIa);
            Emoji.Fix(_status);
        }

        private void Ia(string text, bool ok = false)
        {
            if (string.IsNullOrEmpty(text)) { _ia.Visibility = Visibility.Collapsed; _statusIa = ""; MontrerStatus(); return; }
            if (ok) { _statusIa = text; _ia.Visibility = Visibility.Collapsed; }
            else { _ia.Text = text; _ia.Visibility = Visibility.Visible; Emoji.Fix(_ia); }
            MontrerStatus();
        }

        // ------------------------------------------------------------------
        //  Une phrase entendue → la grille
        // ------------------------------------------------------------------
        public async void ShowHeard(string text, bool speculatif = false)
        {
            if (!speculatif || _settings == null) _settings = Settings.Load();
            if (!speculatif)
            {
                _partielTimer.Stop();
                if (_speculatif != null && _enCours != null && Pictos.Norm(_speculatif) == Pictos.Norm(text))
                {
                    _speculatif = null;
                    _enCours.Question = text;
                    if (_etat == "grille" && !_question.Text.StartsWith("+")) _question.Text = Titre(text, _enCours);
                    return;
                }
                _speculatif = null;
            }
            _autoClose.Stop();
            _enCours = new Suggest.Echange { Question = text };
            _historique.Add(_enCours);
            while (_historique.Count > 3) _historique.RemoveAt(0);
            var histo = _historique.Take(_historique.Count - 1).ToList();
            var monEchange = _enCours;

            var precedent = histo.LastOrDefault();
            bool suite = precedent != null && (DateTime.Now - precedent.At).TotalSeconds < 120 && Suggest.EstSuite(text);
            monEchange.Suite = suite;
            var secours = suite ? Suggest.DefaultsSuite(text, precedent) : Suggest.Defaults(text);
            monEchange.Type = secours.Type;
            Montrer("grille");
            _question.Text = suite ? Titre(precedent.Question, precedent) + "\n+ « " + text + " »" : Titre(text, null);
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey);
            if (!enLigne)
            {
                PreparerGrille(secours.Type, secours.Reponses.Count);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                Ia("⚠️ Réponses devinées, sans IA : " + (!_settings.ListenAnswers ? "la case « Proposer des réponses » est décochée (⚙ → Licence & nuage → Écoute)." : "pas de licence en ligne."));
                if (!speculatif) Speech.SayAlways(text);
                return;
            }

            _answers.Children.Clear();
            Ia(null);
            _wait.Visibility = Visibility.Visible;
            _wait.Text = "Je réfléchis…";
            var t0 = DateTime.Now;
            var tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            tick.Tick += (s, e) => _wait.Text = string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "Je réfléchis… {0:0.0} s", (DateTime.Now - t0).TotalSeconds);
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
                        if (!suite && tete.Simple != null && tete.Simple != text) _question.Text = Titre(tete.Simple, null);
                    })),
                    rep => Dispatcher.BeginInvoke(new Action(() => { if (_enCours == monEchange) AjouterCase(rep, _grilleType); })), suite);
            }
            catch (Exception ex) { res = secours; res.Erreur = ex.Message; }
            tick.Stop();
            _wait.Visibility = Visibility.Collapsed;
            if (_enCours != monEchange) return;
            if (res.EnLigne)
            {
                Ia("IA · " + (res.Chrono ?? ""), ok: true);
                if (!speculatif && res.Simple != null && res.Simple != text) Speech.SayAlways(res.Simple);
            }
            else
            {
                PreparerGrille(secours.Type, secours.Reponses.Count);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                Ia("⚠️ Réponses devinées : l'IA n'a pas répondu" + (string.IsNullOrEmpty(res.Erreur) ? "." : " (" + res.Erreur + ")."));
            }
        }

        private static string Titre(string text, Suggest.Echange e)
        {
            var t = (text ?? "").Trim();
            if (t.Length > 0) t = char.ToUpperInvariant(t[0]) + t.Substring(1);
            return t;
        }

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
            st.Children.Add(new Viewbox { Child = Visuel(r, mot, _enCours?.Question), Height = grand ? 260 : 170, Stretch = Stretch.Uniform });
            st.Children.Add(new TextBlock { Text = r.Texte, FontSize = grand ? 40 : 30, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 380, Margin = new Thickness(0, 8, 0, 0) });
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["MenuTile"],
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(10, 8, 10, 8), Child = st },
                Width = double.NaN, Height = double.NaN, Margin = new Thickness(8), Opacity = 0,
            };
            b.Click += (s, e) => Choisir(cap, mot, type);
            Emoji.FixTree(st);
            _answers.Children.Add(b);
            b.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }

        /// <summary>L'enfant a regardé une case : la tablette le dit, l'écran le montre en grand, puis Encore · Autre chose · Fini.</summary>
        private void Choisir(Suggest.Reponse r, CommBoard.Mot mot, string type)
        {
            var dit = mot != null && !string.IsNullOrEmpty(mot.Dit) ? mot.Dit : r.Texte;
            if (mot == null || !VoiceBank.Play("comm-" + mot.Cle)) Speech.SayAlways(dit);
            if (_enCours != null) _enCours.Reponse = r.Texte;
            ReponsesPref.Choisie(r.Texte);
            GazeStats.RecordComm("ecoute-" + type);
            _confirmVisual.Child = new Viewbox { Stretch = Stretch.Uniform, Child = Visuel(r, mot, _enCours?.Question) };
            _confirmText.Text = (r.Texte ?? "").TrimEnd('!', '.') + " !";
            Emoji.FixTree(_confirm);
            Montrer("confirm");
            _autoClose.Stop(); _autoClose.Start();
        }

        // ------------------------------------------------------------------
        //  Visuels
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, (string Img, string Emoji)> Fixes = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["oui"] = ("comm-oui", "✅"), ["non"] = ("comm-non", "❌"), ["je ne sais pas"] = ("", "🤷"), ["les deux"] = ("", "🤲"), ["autre chose"] = ("", "💭"),
            ["d'accord"] = ("comm-oui", "👍"), ["ok"] = ("comm-oui", "👍"), ["attends"] = ("", "✋"), ["encore"] = ("comm-encore", "🔁"), ["plus tard"] = ("", "⏰"), ["un peu"] = ("", "🤏"),
            ["je t'aime"] = ("comm-jetaime", "❤️"), ["fini"] = ("comm-fini", "🏁"), ["aide-moi"] = ("comm-aide", "🙋"), ["stop"] = ("", "🛑"),
        };

        /// <summary>Le visuel d'une réponse : la photo d'un proche d'abord, puis le picto ARASAAC du mot français, puis le secours (image du tableau, Mulberry, emoji).</summary>
        public static FrameworkElement Visuel(Suggest.Reponse r, CommBoard.Mot mot, string question = null)
        {
            try
            {
                if (mot != null && mot.Photo != null) return mot.Photo();
                var cle = (r.Texte ?? "").Trim().TrimEnd('!', '.').ToLowerInvariant();
                FrameworkElement secours = null;
                if (Fixes.TryGetValue(cle, out var fx))
                {
                    var src = string.IsNullOrEmpty(fx.Img) ? null : Art.Find(fx.Img);
                    secours = src != null ? new Image { Source = src, Stretch = Stretch.Uniform } : Art.Visual("", fx.Emoji, 200);
                    if (cle == "autre chose" || cle == "les deux" || cle == "je ne sais pas") return secours; // ceux-là restent des symboles, pas des dessins
                }
                if (secours == null && mot != null) { var src = Art.Find(mot.Img); if (src != null) secours = new Image { Source = src, Stretch = Stretch.Uniform }; }
                if (secours == null) secours = Pictos.Picture(r.Picto, 200) ?? Pictos.Picture(r.Texte, 200);
                if (secours == null && !string.IsNullOrWhiteSpace(r.Emoji)) secours = Art.Visual("", r.Emoji.Trim(), 200);
                // Le mot français complet donné par l'IA (« yaourt nature ») d'abord, sinon la réponse lue dans le contexte de la question.
                var a = string.IsNullOrWhiteSpace(r.Mot) ? null : Arasaac.Picture(r.Mot, 200, secours);
                if (a == null) a = Arasaac.Picture(r.Texte, 200, secours, question);
                if (a != null) return a;
                if (secours != null) return secours;
            }
            catch { }
            return Art.Visual("", "💬", 200);
        }

        private static FrameworkElement Oreille(double size)
        {
            FrameworkElement v = null;
            try { v = Pictos.Picture("ear", size); } catch { }
            return v ?? Art.Visual("", "👂", size);
        }

        private static FrameworkElement Rond(FrameworkElement inner, double size, Brush fond)
            => new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = fond, Child = inner, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(size * 0.17), Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 30, ShadowDepth = 8, Opacity = 0.14 } };

        private Button Suite(string img, string emoji, string label, Action action)
        {
            FrameworkElement vis = null;
            try { var src = string.IsNullOrEmpty(img) ? null : Art.Find(img); if (src != null) vis = new Image { Source = src, Stretch = Stretch.Uniform }; } catch { }
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"], Margin = new Thickness(10), Background = Brushes.White,
                Content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Children = { new Viewbox { Height = 90, Child = vis ?? Art.Visual("", emoji, 90) }, new TextBlock { Text = label, FontSize = 32, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } } },
            };
            b.Click += (s, e) => action();
            return b;
        }

        private static Button PetitBouton(string emoji, string tooltip)
        {
            var b = new Button { Width = 36, Height = 36, BorderThickness = new Thickness(0), Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xEC, 0xFF)), Content = Art.Visual("", emoji, 20), ToolTip = tooltip, Cursor = System.Windows.Input.Cursors.Hand };
            Gaze.GazeTargets.SetParentOnly(b, true);
            return b;
        }
    }
}
