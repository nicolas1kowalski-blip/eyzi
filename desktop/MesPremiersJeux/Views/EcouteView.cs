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
    /// Grille (la question en grand, les cases), Rebond (ce qui a été dit, puis
    /// ce que l'enfant pourrait dire ensuite : préciser, demander, poser une
    /// question à l'adulte ; le micro réécoute l'adulte sans « Dis Eyzi »), et
    /// Thèmes (« Autre chose » ou « Pas ça » : des grilles très complètes par
    /// thème, pour aller tout à fait ailleurs). Les cases proposées sont un point
    /// de départ, jamais un périmètre. Tout ce qui sert à l'adulte tient sur la
    /// ligne grise du bas.
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
        private readonly Grid _root, _attente, _ecoute, _grilleP;
        private readonly TextBlock _parleMoi, _sousTitre, _partielText, _question, _fil, _wait, _status, _ia;
        private readonly UniformGrid _answers;
        private readonly Ellipse _anneau1, _anneau2;
        private readonly Border _bas, _photoBox, _ditBox, _flash;
        private readonly Button _btnScene, _btnEnfant;
        private string _photoB64, _photoVue;
        private Suggest.Echange _enCours, _speculatifEchange;
        private string _speculatif, _partiel, _etat = "attente";
        private readonly DispatcherTimer _partielTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        private readonly DispatcherTimer _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
        private readonly DispatcherTimer _ecouteAuto = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        private int _grilleRows = 1, _grilleCols = 3, _maxCases = 8, _chaine;
        private string _grilleType = "autre", _statusEcoute = "", _statusIa = "";
        private readonly List<Suggest.Reponse> _derniereGrille = new List<Suggest.Reponse>(); // les cases de la dernière grille IA, pour « Revoir les cases »
        private string _derniereType = "autre";

        public string Etat => _etat;
        /// <summary>La grille est à l'écran (cases, rebonds ou thèmes) : une phrase partielle ne doit pas la chasser.</summary>
        private bool EnGrille => _etat == "grille" || _etat == "rebond" || _etat == "themes";

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
            var dire = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"], Width = 380, Height = 100, Margin = new Thickness(24, 40, 0, 0), Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xE4, 0xF0)),
                Content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Children = { Art.Visual("", "💬", 48), new TextBlock { Text = "Je veux dire…", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Dark, Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center } } },
            };
            dire.Click += (s, e) => ShowSpontane();
            ap.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Children = { mots, dire } });
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
            _question = new TextBlock { FontSize = 46, FontWeight = FontWeights.ExtraBold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(60, 22, 60, 8), MaxWidth = 1500 };
            // 📷 La photo qui vient d'être prise, à gauche de la question (l'enfant voit ce que l'IA a vu).
            _photoBox = new Border { Width = 250, Height = 188, CornerRadius = new CornerRadius(18), Margin = new Thickness(30, 14, 0, 8), Background = Brushes.White, Padding = new Thickness(6), Visibility = Visibility.Collapsed, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 20, ShadowDepth = 4, Opacity = 0.18 } };
            // ✅ Ce que l'enfant vient de dire (état Rebond), à gauche, à la place de la photo.
            _ditBox = new Border { Width = 200, Height = 200, CornerRadius = new CornerRadius(30), Margin = new Thickness(30, 10, 0, 8), Background = Brushes.White, Padding = new Thickness(18), Visibility = Visibility.Collapsed, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 20, ShadowDepth = 4, Opacity = 0.18 } };
            var tete = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(_photoBox, Dock.Left);
            DockPanel.SetDock(_ditBox, Dock.Left);
            tete.Children.Add(_photoBox);
            tete.Children.Add(_ditBox);
            // Le fil : les deux derniers échanges, en petit sous la question (et la phrase que le micro entend, en direct).
            _fil = new TextBlock { FontSize = 20, Foreground = Gris, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(60, 0, 60, 6), MaxWidth = 1500, Visibility = Visibility.Collapsed };
            var qf = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _question, _fil } };
            tete.Children.Add(qf);
            _grilleP.Children.Add(tete);
            _answers = new UniformGrid { Margin = new Thickness(40, 0, 40, 10) };
            _wait = new TextBlock { FontSize = 34, Foreground = Dark, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
            var gh = new Grid { Children = { _answers, _wait } };
            Grid.SetRow(gh, 1);
            _grilleP.Children.Add(gh);
            _root.Children.Add(_grilleP);

            // ---------- La ligne du bas, pour l'adulte : maison · état · micro
            var basGrid = new DockPanel { LastChildFill = true };
            var maison = PetitBouton("🏠", "Revenir à ce qu'on faisait");
            maison.Click += (s, e) => Fermer();
            DockPanel.SetDock(maison, Dock.Left);
            basGrid.Children.Add(maison);
            _btnScene = PetitBouton("📷", "Regarder ce qu'on montre (caméra arrière) et proposer des réponses dessus");
            _btnScene.Margin = new Thickness(6, 0, 0, 0);
            _btnScene.Click += (s, e) => { _ = Photographier(false, null); };
            DockPanel.SetDock(_btnScene, Dock.Left);
            basGrid.Children.Add(_btnScene);
            _btnEnfant = PetitBouton("🙂", "Regarder l'enfant (caméra avant) et lui proposer ce qu'elle ressent peut-être");
            _btnEnfant.Margin = new Thickness(4, 0, 0, 0);
            _btnEnfant.Click += (s, e) => { _ = Photographier(true, null); };
            DockPanel.SetDock(_btnEnfant, Dock.Left);
            basGrid.Children.Add(_btnEnfant);
            var micro = PetitBouton("👂", "Écouter une phrase");
            micro.Click += async (s, e) => { if (Listening.Enabled) await Listening.ListenOnce(); else _statusEcoute = "l'écoute n'est pas réglée (⚙ → Parler & IA → Écoute)"; MontrerStatus(); };
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
            _flash = new Border { Background = Brushes.White, Opacity = 0, IsHitTestVisible = false };
            Grid.SetRowSpan(_flash, 2);
            _root.Children.Add(_flash);

            Content = _root;
            Emoji.FixTree(_root);

            Listening.StatusChanged += st => Dispatcher.BeginInvoke(new Action(() => SurEtatEcoute(st)));
            Listening.Partial += t => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible) return;
                _partiel = t;
                // Pendant un rebond ou un thème, la phrase de l'adulte s'écrit dans le fil, sans chasser les cases : la grille viendra avec la phrase entière.
                if (_etat == "rebond" || _etat == "themes") { _fil.Text = "👂 " + t; _fil.Visibility = Visibility.Visible; Emoji.Fix(_fil); return; }
                if (_etat == "ecoute" || _etat == "attente") { if (_etat != "ecoute") Montrer("ecoute"); _partielText.Text = t; }
                _partielTimer.Stop(); _partielTimer.Start();
            }));
            _partielTimer.Tick += (s, e) =>
            {
                _partielTimer.Stop();
                var t = (_partiel ?? "").Trim();
                if (t.Length < 8 || !IsVisible || _settings == null || !_settings.ListenAnswers || _etat == "rebond" || _etat == "themes") return;
                if (_speculatif != null && Pictos.Norm(_speculatif) == Pictos.Norm(t)) return;
                _speculatif = t;
                ShowHeard(t, speculatif: true);
            };
            _autoClose.Tick += (s, e) => { _autoClose.Stop(); if (_etat == "rebond") Fermer(); };
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
            _photoB64 = null; _photoBox.Visibility = Visibility.Collapsed; _ditBox.Visibility = Visibility.Collapsed;
            _chaine = 0;
            _btnScene.Visibility = _btnEnfant.Visibility = _settings.CameraParler ? Visibility.Visible : Visibility.Collapsed;
            Montrer(ecoute ? "ecoute" : "attente");
            SurEtatEcoute(Listening.Status);
            if ((_settings.ListenMode ?? "off") != "off") Suggest.Reveiller(_settings);
        }

        /// <summary>Le mot-clé a été entendu, ou le micro pressé : l'oreille rouge.</summary>
        public void Ecouter() { if (!EnGrille) Montrer("ecoute"); }

        public void Fermer() { Arreter(); CloseRequested?.Invoke(); }

        private void Arreter() { _autoClose.Stop(); _partielTimer.Stop(); _ecouteAuto.Stop(); _speculatif = null; _speculatifEchange = null; }

        private void Montrer(string etat)
        {
            _etat = etat;
            _attente.Visibility = etat == "attente" ? Visibility.Visible : Visibility.Collapsed;
            _ecoute.Visibility = etat == "ecoute" ? Visibility.Visible : Visibility.Collapsed;
            _grilleP.Visibility = EnGrille ? Visibility.Visible : Visibility.Collapsed;
            if (etat != "rebond") { _autoClose.Stop(); _ecouteAuto.Stop(); }
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
                case "ecoute":
                    _statusEcoute = "j'écoute";
                    if (IsVisible && !EnGrille) Montrer("ecoute");
                    else if (_etat == "rebond" || _etat == "themes") { _fil.Text = "👂 J'écoute la réponse…"; _fil.Visibility = Visibility.Visible; Emoji.Fix(_fil); }
                    break;
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
        public void ShowHeard(string text, bool speculatif = false)
        {
            if (!speculatif || _settings == null) _settings = Settings.Load();
            // « Dis Eyzi, regarde » / « regarde Laura » : une photo part avec la question.
            if (Suggest.EstRegard(text, _settings.ChildName, out bool avant))
            {
                if (speculatif) return;
                if (_settings.CameraParler) { _ = Photographier(avant, Suggest.QuestionDuRegard(text, avant)); return; }
                Ia("📷 La caméra n'est pas autorisée (⚙ → Parler & IA → Écoute) : la question part sans image.");
                text = Suggest.QuestionDuRegard(text, avant);
            }
            Lancer(text, speculatif, null);
        }

        /// <summary>📷 Prend la photo (flash, déclic), la montre, puis pose la question avec l'image.</summary>
        private async System.Threading.Tasks.Task Photographier(bool avant, string question)
        {
            if (_settings == null) _settings = Settings.Load();
            if (!_settings.CameraParler) { Ia("📷 La caméra n'est pas autorisée (⚙ → Parler & IA → Écoute)."); return; }
            _partielTimer.Stop(); _speculatif = null; _autoClose.Stop();
            question = question ?? (avant ? "Comment tu te sens ?" : "Qu'est-ce que tu vois ? Qu'est-ce que tu veux ?");
            Montrer("grille");
            _answers.Children.Clear();
            _photoBox.Visibility = Visibility.Collapsed; _ditBox.Visibility = Visibility.Collapsed; _question.Foreground = Dark;
            _question.Text = avant ? "🙂 Je regarde " + (_settings.ChildName ?? "l'enfant") + "…" : "📷 Je regarde…";
            Emoji.Fix(_question);
            _wait.Visibility = Visibility.Visible; _wait.Text = "Clic !";
            SoundFx.Shutter();
            _flash.BeginAnimation(OpacityProperty, new DoubleAnimation(0.9, 0, TimeSpan.FromMilliseconds(500)));
            var p = await Photo.Prendre(avant);
            if (p.Jpeg == null)
            {
                _wait.Visibility = Visibility.Collapsed;
                Ia("📷 " + (p.Erreur ?? "pas de photo") + " La question part sans image.");
                Lancer(question, false, null);
                return;
            }
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                using (var ms = new System.IO.MemoryStream(p.Jpeg))
                {
                    bmp.BeginInit(); bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.StreamSource = ms; bmp.EndInit();
                }
                bmp.Freeze();
                _photoBox.Child = new Image { Source = bmp, Stretch = Stretch.UniformToFill };
                _photoBox.Visibility = Visibility.Visible;
            }
            catch { }
            _photoB64 = Convert.ToBase64String(p.Jpeg);
            _photoVue = avant ? "enfant" : "scene";
            Lancer(question, false, new Suggest.Options { PhotoB64 = _photoB64, Vue = _photoVue });
        }

        /// <summary>😴 La veille fatigue : « Tu es fatiguée ? On arrête ? », sans IA.</summary>
        public void ShowFatigue()
        {
            var r = Suggest.DefaultsFatigue();
            ShowProposition(r.Simple, r.Reponses, r.Type);
            GazeStats.RecordComm("fatigue-question");
        }

        /// <summary>Une question et ses cases, données d'avance (sans IA).</summary>
        private void ShowProposition(string question, List<Suggest.Reponse> reponses, string type)
        {
            if (_settings == null) _settings = Settings.Load();
            _partielTimer.Stop(); _speculatif = null; _autoClose.Stop(); _chaine = 0;
            _photoBox.Visibility = Visibility.Collapsed; _ditBox.Visibility = Visibility.Collapsed; _question.Foreground = Dark;
            _enCours = new Suggest.Echange { Question = question, Type = type };
            Conversation.Ajouter(_enCours);
            Montrer("grille");
            _question.Text = Titre(question, null);
            MettreFil();
            Ia(null);
            _wait.Visibility = Visibility.Collapsed;
            PreparerGrille(type, reponses.Count + 1);
            foreach (var r in reponses) AjouterCase(r, type);
            AjouterPasCa();
            Conversation.Sauver();
            Speech.SayAlways(question);
        }

        /// <summary>💬 « Je veux dire… » : ce que l'enfant pourrait vouloir dire maintenant.</summary>
        public async void ShowSpontane()
        {
            if (_settings == null) _settings = Settings.Load();
            _partielTimer.Stop(); _speculatif = null; _autoClose.Stop(); _chaine = 0;
            _photoBox.Visibility = Visibility.Collapsed; _ditBox.Visibility = Visibility.Collapsed; _question.Foreground = Dark;
            _enCours = new Suggest.Echange { Question = "Je veux dire", Type = "spontane", Qui = "enfant" };
            Conversation.Ajouter(_enCours);
            var monEchange = _enCours;
            Montrer("grille");
            _question.Text = "💬 Je veux dire…";
            Emoji.Fix(_question);
            MettreFil();
            Ia(null);
            PreparerGrille("ouverte", 8);
            _wait.Visibility = Visibility.Visible; _wait.Text = "Je réfléchis…";
            Usage.Screen("je-veux-dire");
            var res = await Spontane.Pour(_settings, rep => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_enCours != monEchange) return;
                _wait.Visibility = Visibility.Collapsed;
                AjouterCase(rep, "spontane");
            })));
            if (_enCours != monEchange) return;
            _wait.Visibility = Visibility.Collapsed;
            if (res.EnLigne) Ia("IA · " + res.Info, ok: true); else Ia("⚠️ " + res.Info);
            if (_answers.Children.Count == 0) foreach (var r in Spontane.Defaults(_settings)) AjouterCase(r, "spontane");
            AjouterPasCa();
            Conversation.Sauver();
        }

        private async void Lancer(string text, bool speculatif, Suggest.Options opt)
        {
            if (!speculatif)
            {
                _partielTimer.Stop();
                if (_speculatif != null && _enCours != null && Pictos.Norm(_speculatif) == Pictos.Norm(text))
                {
                    // La phrase entière confirme la spéculation : la grille reste, l'échange devient définitif.
                    _speculatif = null; _speculatifEchange = null;
                    _enCours.Question = text;
                    Conversation.Sauver();
                    if (_etat == "grille" && !_question.Text.StartsWith("+")) _question.Text = Titre(text, _enCours);
                    return;
                }
                _speculatif = null;
            }
            _autoClose.Stop(); _ecouteAuto.Stop(); _chaine = 0;
            // Une spéculation qui n'a pas abouti (la phrase entière était autre) sort du fil.
            if (_speculatifEchange != null) { Conversation.Retirer(_speculatifEchange); _speculatifEchange = null; }
            _enCours = new Suggest.Echange { Question = text, Qui = "adulte" };
            if (speculatif) _speculatifEchange = _enCours;
            var histo = Conversation.Recents(_enCours);
            Conversation.Ajouter(_enCours);
            var monEchange = _enCours;

            // « ou du chocolat » : la suite de la dernière question de l'adulte, si elle date de moins de deux minutes.
            var precedent = histo.LastOrDefault(h => h.Qui != "enfant");
            bool suite = precedent != null && precedent == histo.LastOrDefault(h => h.Type != "rebond") && (DateTime.Now - precedent.At).TotalSeconds < 120 && Suggest.EstSuite(text);
            monEchange.Suite = suite;
            var secours = opt != null && opt.Vue == "enfant" ? Suggest.DefaultsHumeur(text) : suite ? Suggest.DefaultsSuite(text, precedent) : Suggest.Defaults(text);
            monEchange.Type = secours.Type;
            if (opt == null || string.IsNullOrEmpty(opt.PhotoB64)) _photoBox.Visibility = Visibility.Collapsed;
            _ditBox.Visibility = Visibility.Collapsed; _question.Foreground = Dark;
            Montrer("grille");
            _question.Text = suite ? Titre(precedent.Question, precedent) + "\n+ « " + text + " »" : Titre(text, null);
            MettreFil();
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey);
            if (!enLigne)
            {
                PreparerGrille(secours.Type, secours.Reponses.Count + 1);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                AjouterPasCa();
                Conversation.Sauver();
                Ia("⚠️ Réponses devinées, sans IA : " + (!_settings.ListenAnswers ? "la case « Proposer des réponses » est décochée (⚙ → Parler & IA → Écoute)." : "pas de licence en ligne."));
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
                    rep => Dispatcher.BeginInvoke(new Action(() => { if (_enCours == monEchange) AjouterCase(rep, _grilleType); })), suite, opt);
            }
            catch (Exception ex) { res = secours; res.Erreur = ex.Message; }
            _photoB64 = null; // la photo ne sert qu'à cette question, et n'est gardée nulle part
            tick.Stop();
            _wait.Visibility = Visibility.Collapsed;
            if (_enCours != monEchange) return;
            try { Usage.Count(res.EnLigne ? "ecoute:ia" : "ecoute:devine"); } catch { }
            if (res.EnLigne)
            {
                Ia("IA · " + (res.Chrono ?? ""), ok: true);
                if (!speculatif && res.Simple != null && res.Simple != text) Speech.SayAlways(res.Simple);
            }
            else
            {
                PreparerGrille(secours.Type, secours.Reponses.Count + 1);
                foreach (var r in secours.Reponses) AjouterCase(r, secours.Type);
                Ia("⚠️ Réponses devinées : l'IA n'a pas répondu" + (string.IsNullOrEmpty(res.Erreur) ? "." : " (" + res.Erreur + ")."));
            }
            if (_etat == "grille") AjouterPasCa();
            Conversation.Sauver();
        }

        /// <summary>Le fil sous la question : les deux derniers échanges finis (« Tu veux du jus ? → Du jus de pomme »).</summary>
        private void MettreFil()
        {
            try
            {
                var l = Conversation.Recents(_enCours, 2).Where(h => !string.IsNullOrEmpty(h.Reponse)).ToList();
                if (l.Count == 0) { _fil.Visibility = Visibility.Collapsed; return; }
                _fil.Text = string.Join("   ·   ", l.Select(h => (h.Qui == "enfant" && h.Type != "spontane" ? "" : Court(h.Question) + " → ") + "« " + Court(h.Reponse) + " »"));
                _fil.Visibility = Visibility.Visible;
                Emoji.Fix(_fil);
            }
            catch { _fil.Visibility = Visibility.Collapsed; }
        }

        private static string Court(string t) { t = (t ?? "").Trim(); return t.Length > 60 ? t.Substring(0, 58) + "…" : t; }

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
            _maxCases = _grilleType == "rebond" ? 6 : 8;
            if (_etat == "grille") { _derniereGrille.Clear(); _derniereType = _grilleType; }
            int attendu = nConnu > 0 ? nConnu : _grilleType == "ouverte" ? 8 : _grilleType == "choix" ? 4 : 3;
            _grilleRows = attendu <= 3 ? 1 : 2;
            _grilleCols = (int)Math.Ceiling(attendu / (double)_grilleRows);
            _answers.Rows = _grilleRows;
            _answers.Columns = _grilleCols;
        }

        /// <summary>La grille grandit d'une case : une ligne jusqu'à trois, deux ensuite.</summary>
        private void Agrandir()
        {
            int n = _answers.Children.Count + 1;
            if (n > _grilleRows * _grilleCols)
            {
                if (_grilleRows == 1 && n > 3) { _grilleRows = 2; _grilleCols = (int)Math.Ceiling(n / 2.0); }
                else _grilleCols = (int)Math.Ceiling(n / (double)_grilleRows);
                _answers.Rows = _grilleRows; _answers.Columns = _grilleCols;
            }
        }

        private Button Tuile(FrameworkElement visuel, string texte, Action action)
        {
            bool grand = _grilleRows == 1;
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            st.Children.Add(new Viewbox { Child = visuel, Height = grand ? 260 : 170, Stretch = Stretch.Uniform });
            st.Children.Add(new TextBlock { Text = texte, FontSize = grand ? 40 : 30, FontWeight = FontWeights.Bold, Foreground = Dark, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 380, Margin = new Thickness(0, 8, 0, 0) });
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["MenuTile"],
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(10, 8, 10, 8), Child = st },
                Width = double.NaN, Height = double.NaN, Margin = new Thickness(8), Opacity = 0,
            };
            b.Click += (s, e) => action();
            Emoji.FixTree(st);
            _answers.Children.Add(b);
            b.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
            return b;
        }

        /// <summary>Une case proposée (IA ou secours). <paramref name="noter"/> : elle compte parmi les propositions de l'échange.</summary>
        private void AjouterCase(Suggest.Reponse r, string type, bool noter = true)
        {
            if (_answers.Children.Count >= _maxCases) return;
            if (noter && _enCours != null && !string.IsNullOrEmpty(r.Texte)) _enCours.Propositions.Add(r.Texte);
            if (noter && _etat == "grille") _derniereGrille.Add(r);
            Agrandir();
            var cap = r;
            var mot = CommBoard.Trouver(r.Cle);
            Tuile(Visuel(r, mot, _enCours?.Question), r.Texte, () => Choisir(cap, mot, type));
        }

        /// <summary>Une case fixe (Pas ça, Autre chose, Fini, un thème, une page) : pas une proposition de l'IA.</summary>
        private void AjouterFixe(string emoji, string texte, Action action, string mot = null)
        {
            Agrandir();
            FrameworkElement vis = null;
            if (!string.IsNullOrEmpty(mot)) { try { vis = Arasaac.Picture(mot, 200, Art.Visual("", emoji, 200)); } catch { } }
            Tuile(vis ?? Art.Visual("", emoji, 200), texte, action);
        }

        /// <summary>🤔 « Pas ça » : aucune case ne convient, ou l'enfant veut réfléchir. Toujours là, sur toutes les grilles.</summary>
        private void AjouterPasCa()
        {
            if (_answers.Children.Count == 0) return;
            AjouterFixe("🤔", "Pas ça", () =>
            {
                Speech.SayAlways("Ce n'est pas ça… je réfléchis.");
                if (_enCours != null) { _enCours.Ecart = "pasca"; Conversation.Sauver(); }
                try { Usage.Count("ecoute:pasca"); } catch { }
                GazeStats.RecordComm("ecoute-pasca");
                MontrerThemes(0);
            });
        }

        private static bool EstAutre(string t)
        {
            var n = Pictos.Norm(t ?? "");
            return n == "autre chose" || n == "je veux autre chose" || n == "autre";
        }

        /// <summary>L'enfant a regardé une case : la tablette le dit, puis les rebonds (ou les thèmes pour « Autre chose »).</summary>
        private void Choisir(Suggest.Reponse r, CommBoard.Mot mot, string type)
        {
            if (EstAutre(r.Texte))
            {
                Speech.SayAlways("Autre chose !");
                if (_enCours != null) { _enCours.Ecart = "autre"; Conversation.Sauver(); }
                try { Usage.Count("ecoute:autre"); } catch { }
                GazeStats.RecordComm("ecoute-autre");
                MontrerThemes(0);
                return;
            }
            var dit = mot != null && !string.IsNullOrEmpty(mot.Dit) ? mot.Dit : r.Texte;
            if (mot == null || !VoiceBank.Play("comm-" + mot.Cle)) Speech.SayAlways(dit);
            ReponsesPref.Choisie(r.Texte);
            if (_etat == "rebond")
            {
                // Un rebond : c'est l'enfant qui parle, de lui-même, dans la suite de la question.
                var e = new Suggest.Echange { Question = _enCours?.Question, Type = "rebond", Qui = "enfant", Reponse = r.Texte };
                Conversation.Ajouter(e);
                _enCours = e;
                _chaine++;
                try { Usage.Count("ecoute:rebond"); } catch { }
            }
            else
            {
                if (_enCours != null) { _enCours.Reponse = r.Texte; Conversation.Sauver(); }
                try { Habitudes.Choisie(_enCours?.Question, r.Texte); Usage.Count("ecoute:reponse"); if (type == "theme") Usage.Count("ecoute:theme"); } catch { }
            }
            GazeStats.RecordComm("ecoute-" + type);
            MontrerRebonds(r, mot);
        }

        // ------------------------------------------------------------------
        //  Les rebonds : ce que l'enfant pourrait dire ensuite, et l'adulte réécouté
        // ------------------------------------------------------------------
        private async void MontrerRebonds(Suggest.Reponse r, CommBoard.Mot mot)
        {
            if (_settings == null) _settings = Settings.Load();
            _partielTimer.Stop(); _speculatif = null; _speculatifEchange = null;
            Montrer("rebond");
            _photoBox.Visibility = Visibility.Collapsed;
            _ditBox.Child = new Viewbox { Stretch = Stretch.Uniform, Child = Visuel(r, mot, _enCours?.Question) };
            _ditBox.Visibility = Visibility.Visible;
            _question.Foreground = Vert;
            _question.Text = (r.Texte ?? "").TrimEnd('!', '.') + " !";
            MettreFil();
            PreparerGrille("rebond", 8);
            var monEchange = _enCours;
            EcouterLaReponse(monEchange);
            _autoClose.Stop(); _autoClose.Start();

            var question = _enCours?.Question ?? "";
            bool enLigne = _settings.ListenAnswers && !string.IsNullOrWhiteSpace(_settings.LicenceKey) && _chaine < 4;
            if (!enLigne)
            {
                foreach (var x in Suggest.DefaultsRebond(question, r.Texte).Reponses) AjouterCase(x, "rebond");
                FinRebonds();
                return;
            }
            Ia(null);
            _wait.Visibility = Visibility.Visible; _wait.Text = "Je réfléchis…";
            Suggest.Resultat res;
            try
            {
                var vocab = CommBoard.Vocabulaire().Select(m => new Suggest.Mot { Cle = m.Cle, Label = m.Label }).ToList();
                res = await Suggest.PourFlux(_settings, question.Length > 0 ? question : r.Texte, vocab, Conversation.Recents(monEchange), Suggest.ContexteActuel(_settings),
                    tete => Dispatcher.BeginInvoke(new Action(() => { if (_enCours == monEchange && _etat == "rebond") _wait.Visibility = Visibility.Collapsed; })),
                    rep => Dispatcher.BeginInvoke(new Action(() => { if (_enCours == monEchange && _etat == "rebond") AjouterCase(rep, "rebond"); })),
                    false, new Suggest.Options { Rebond = true, Reponse = r.Texte });
            }
            catch (Exception ex) { res = Suggest.DefaultsRebond(question, r.Texte); res.Erreur = ex.Message; }
            if (_enCours != monEchange || _etat != "rebond") return;
            _wait.Visibility = Visibility.Collapsed;
            try { Usage.Count(res.EnLigne ? "ecoute:ia" : "ecoute:devine"); } catch { }
            if (res.EnLigne) Ia("IA · " + (res.Chrono ?? ""), ok: true);
            else { foreach (var x in Suggest.DefaultsRebond(question, r.Texte).Reponses) AjouterCase(x, "rebond"); Ia("⚠️ Rebonds devinés : l'IA n'a pas répondu" + (string.IsNullOrEmpty(res.Erreur) ? "." : " (" + res.Erreur + ").")); }
            FinRebonds();
        }

        /// <summary>Après les rebonds : « Autre chose » (les thèmes) et « Fini ». Le délai de fermeture repart.</summary>
        private void FinRebonds()
        {
            AjouterFixe("💭", "Autre chose", () =>
            {
                try { Usage.Count("ecoute:autre"); } catch { }
                if (_enCours != null) { _enCours.Ecart = "autre"; Conversation.Sauver(); }
                MontrerThemes(0);
            });
            FrameworkElement fini = null;
            try { var src = Art.Find("comm-fini"); if (src != null) fini = new Image { Source = src, Stretch = Stretch.Uniform }; } catch { }
            Agrandir();
            Tuile(fini ?? Art.Visual("", "🏁", 200), "Fini", () => { GazeStats.RecordComm("ecoute-fini"); Fermer(); });
            Conversation.Sauver();
            _autoClose.Stop(); _autoClose.Start();
        }

        /// <summary>
        /// Le micro réécoute l'adulte dès que la tablette a fini de dire la réponse de l'enfant (sans « Dis Eyzi »),
        /// si la conversation est activée. Une seule phrase, puis la veille reprend d'elle-même.
        /// </summary>
        private EventHandler _tickAuto;
        private void EcouterLaReponse(Suggest.Echange monEchange)
        {
            _ecouteAuto.Stop();
            if (_tickAuto != null) { _ecouteAuto.Tick -= _tickAuto; _tickAuto = null; }
            if (_settings == null || !_settings.EcouteConversation || !Listening.Enabled) return;
            var t0 = DateTime.Now; DateTime? fin = null;
            _tickAuto = (s, e) =>
            {
                if (_etat != "rebond" || _enCours != monEchange || !IsVisible) { _ecouteAuto.Stop(); return; }
                var depuis = (DateTime.Now - t0).TotalSeconds;
                if (depuis < 1.2) return;                               // la voix enregistrée de papa ou maman (VoiceBank) n'est pas vue par Speech.IsBusy
                if (Speech.IsBusy && depuis < 8) { fin = null; return; }
                if (fin == null) { fin = DateTime.Now; return; }
                if ((DateTime.Now - fin.Value).TotalMilliseconds < 700) return;
                _ecouteAuto.Stop();
                Log.Write("ecoute", "Conversation : le micro réécoute l'adulte");
                _ = Listening.ListenOnce();
            };
            _ecouteAuto.Tick += _tickAuto;
            _ecouteAuto.Start();
        }

        // ------------------------------------------------------------------
        //  « Autre chose » : les thèmes, puis les mots d'un thème, puis « Mes mots »
        // ------------------------------------------------------------------
        private const int ParPage = 8;

        private void TitreThemes(string sous)
        {
            _ditBox.Visibility = Visibility.Collapsed; _photoBox.Visibility = Visibility.Collapsed;
            _question.Foreground = Dark;
            var q = _enCours?.Question;
            bool spont = _enCours != null && (_enCours.Type == "spontane" || string.IsNullOrEmpty(q));
            _question.Text = (spont ? "💬 Je veux dire…" : Titre(q, null)) + (string.IsNullOrEmpty(sous) ? "" : "\n" + sous);
            Emoji.Fix(_question);
            _fil.Visibility = Visibility.Collapsed;
            _wait.Visibility = Visibility.Collapsed;
            Ia(null);
            _answers.Children.Clear();
            _grilleType = "theme"; _maxCases = 10;
            _grilleRows = 2; _grilleCols = 5; _answers.Rows = 2; _answers.Columns = 5;
        }

        /// <summary>Les thèmes, huit par page, puis « D'autres thèmes » / « Mes mots » et « Revoir les cases » / « Retour ».</summary>
        private void MontrerThemes(int page)
        {
            Montrer("themes");
            var l = Themes.Liste;
            int pages = (int)Math.Ceiling(l.Count / (double)ParPage);
            page = Math.Max(0, Math.Min(pages - 1, page));
            TitreThemes("💭 Autre chose : de quoi tu veux parler ?");
            foreach (var t in l.Skip(page * ParPage).Take(ParPage)) { var cap = t; AjouterFixe(t.Emoji, t.Nom, () => MontrerTheme(cap, 0), t.Mot); }
            if (page + 1 < pages) AjouterFixe("➡️", "D'autres thèmes", () => MontrerThemes(page + 1));
            else AjouterFixe("🧩", "Mes mots", () => { Arreter(); MotsRequested?.Invoke(); });
            if (page == 0) AjouterFixe("↩️", "Revoir les cases", RevoirCases);
            else AjouterFixe("⬅️", "Retour", () => MontrerThemes(page - 1));
            if (page == 0) Speech.SayAlways("De quoi tu veux parler ?");
        }

        /// <summary>Les mots d'un thème, huit par page ; la question reste en haut, le mot choisi y répond.</summary>
        private void MontrerTheme(Themes.Theme t, int page)
        {
            Montrer("themes");
            var mots = Themes.MotsDe(t);
            int pages = Math.Max(1, (int)Math.Ceiling(mots.Count / (double)ParPage));
            page = Math.Max(0, Math.Min(pages - 1, page));
            TitreThemes(t.Emoji + " " + t.Nom + (pages > 1 ? "  ·  " + (page + 1) + "/" + pages : ""));
            foreach (var r in mots.Skip(page * ParPage).Take(ParPage)) AjouterCase(r, "theme", noter: false);
            if (page + 1 < pages) AjouterFixe("➡️", "Plus de mots", () => MontrerTheme(t, page + 1));
            else if (page > 0) AjouterFixe("⬅️", "Début", () => MontrerTheme(t, 0));
            else AjouterFixe("🧩", "Mes mots", () => { Arreter(); MotsRequested?.Invoke(); });
            AjouterFixe("↩️", "Les thèmes", () => MontrerThemes(0));
            if (page == 0) Speech.SayAlways(t.Nom);
        }

        /// <summary>Retour à la dernière grille proposée (les mêmes cases), avec « Pas ça ».</summary>
        private void RevoirCases()
        {
            Montrer("grille");
            _ditBox.Visibility = Visibility.Collapsed; _question.Foreground = Dark;
            var q = _enCours?.Question;
            _question.Text = _enCours != null && _enCours.Type == "spontane" ? "💬 Je veux dire…" : Titre(q, null);
            Emoji.Fix(_question);
            MettreFil();
            var cases = _derniereGrille.ToList();
            PreparerGrille(_derniereType, cases.Count + 1);
            foreach (var r in cases) AjouterCase(r, _derniereType, noter: false);
            _derniereGrille.AddRange(cases); // pour pouvoir revenir encore
            if (cases.Count == 0) MontrerThemes(0); else AjouterPasCa();
        }

        // ------------------------------------------------------------------
        //  Visuels
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, (string Img, string Emoji)> Fixes = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["oui"] = ("comm-oui", "✅"), ["non"] = ("comm-non", "❌"), ["je ne sais pas"] = ("", "🤷"), ["les deux"] = ("", "🤲"), ["autre chose"] = ("", "💭"),
            ["d'accord"] = ("comm-oui", "👍"), ["ok"] = ("comm-oui", "👍"), ["attends"] = ("", "✋"), ["encore"] = ("comm-encore", "🔁"), ["plus tard"] = ("", "⏰"), ["un peu"] = ("", "🤏"),
            ["je t'aime"] = ("comm-jetaime", "❤️"), ["fini"] = ("comm-fini", "🏁"), ["aide-moi"] = ("comm-aide", "🙋"), ["stop"] = ("", "🛑"),
            ["pas ça"] = ("", "🤔"), ["et toi ?"] = ("", "👉"), ["pourquoi ?"] = ("", "❓"),
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
                    if (cle == "autre chose" || cle == "les deux" || cle == "je ne sais pas" || cle == "pas ça") return secours; // ceux-là restent des symboles, pas des dessins
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

        private static Button PetitBouton(string emoji, string tooltip)
        {
            var b = new Button { Width = 36, Height = 36, BorderThickness = new Thickness(0), Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xEC, 0xFF)), Content = Art.Visual("", emoji, 20), ToolTip = tooltip, Cursor = System.Windows.Input.Cursors.Hand };
            Gaze.GazeTargets.SetParentOnly(b, true);
            return b;
        }
    }
}
