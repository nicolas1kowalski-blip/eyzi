using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎵 « Le petit orchestre » (PS/MS) : apprendre la musique.
    /// TOUT JOUE DÈS QUE LE REGARD SE POSE (pas besoin de fixer) — jamais de
    /// silence : regarder un instrument, c'est l'entendre.
    ///   1. LES INSTRUMENTS — un regard : ça joue ; une fixation : son nom.
    ///   2. QUI FAIT CE BRUIT ? — on entend un instrument, on le retrouve parmi trois.
    ///   3. LE XYLOPHONE — cinq lames : do, ré, mi, fa, sol, qui chantent au regard.
    ///   4. LA CHANSON — « Au clair de la lune » : la lame à jouer brille.
    ///   5. JE JOUE ! — on choisit un instrument (xylophone, piano, guitare,
    ///      trompette, violon, batterie) et on en joue librement au regard.
    ///   7. LA GUITARE — six cordes qui s'allument et chantent au regard, puis
    ///      huit chansons à jouer : la corde qui brille est la prochaine note.
    /// Le parent peut déposer de vrais sons dans Contenu\Sons (tambour.wav,
    /// guitare.wav, xylophone.wav, piano.wav, trompette.wav, violon.wav,
    /// grosse-caisse.wav, caisse-claire.wav, tom.wav, cymbale.wav).
    /// </summary>
    public sealed class MusicSchoolGame : GameControl
    {
        private const double W = 1400, H = 680;

        private sealed class Instr
        {
            public string Key, Name, Article, Emoji, Says;
        }

        private static readonly Instr[] Instruments =
        {
            new Instr { Key = "tambour", Name = "tambour", Article = "le", Emoji = "🥁", Says = "Boum, boum, boum !" },
            new Instr { Key = "guitare", Name = "guitare", Article = "la", Emoji = "🎸", Says = "Elle a des cordes qu'on pince !" },
            new Instr { Key = "xylophone", Name = "xylophone", Article = "le", Emoji = "🎹", Says = "On tape sur ses lames de couleur !" },
            new Instr { Key = "piano", Name = "piano", Article = "le", Emoji = "🎹", Says = "Il a des touches blanches et noires !" },
            new Instr { Key = "trompette", Name = "trompette", Article = "la", Emoji = "🎺", Says = "On souffle dedans très fort ! Tut, tut !" },
            new Instr { Key = "violon", Name = "violon", Article = "le", Emoji = "🎻", Says = "On le frotte avec un archet !" },
        };

        // Les notes : nom et couleur (do … do aigu).
        private static readonly (string Name, Color Color)[] Notes =
        {
            ("do", Color.FromRgb(0xFF, 0x6B, 0x6B)), ("ré", Color.FromRgb(0xFF, 0xA9, 0x3C)), ("mi", Color.FromRgb(0xFF, 0xD9, 0x3C)),
            ("fa", Color.FromRgb(0x7B, 0xE0, 0x6C)), ("sol", Color.FromRgb(0x5D, 0xAD, 0xE2)), ("la", Color.FromRgb(0x7E, 0x8C, 0xF2)),
            ("si", Color.FromRgb(0xC0, 0x7E, 0xE6)), ("do", Color.FromRgb(0xFF, 0x8F, 0xC8)),
        };

        // Les chansons : notes en degrés (0 = do … 5 = la) et syllabes.
        private sealed class Chanson
        {
            public string Titre, Emoji, Art, Fin;
            public int[] Notes;
            public string[] Mots;
        }

        private static readonly Chanson[] Chansons =
        {
            new Chanson
            {
                Titre = "Au clair de la lune", Emoji = "🌙", Art = "lune", Fin = "Au clair de la lune, mon ami Pierrot !",
                Notes = new[] { 0, 0, 0, 1, 2, 1, 0, 2, 1, 1, 0 },
                Mots = new[] { "Au", "clair", "de", "la", "lu", "ne", "mon", "a", "mi", "Pier", "rot" },
            },
            new Chanson
            {
                Titre = "Frère Jacques", Emoji = "🔔", Art = "cloche", Fin = "Frère Jacques, dormez-vous ?",
                Notes = new[] { 0, 1, 2, 0, 0, 1, 2, 0, 2, 3, 4, 2, 3, 4 },
                Mots = new[] { "Frè", "re", "Jac", "ques", "Frè", "re", "Jac", "ques", "dor", "mez", "vous", "dor", "mez", "vous" },
            },
            new Chanson
            {
                Titre = "Sonnez les matines", Emoji = "⏰", Art = "reveil", Fin = "Sonnez les matines ! Ding, dang, dong !",
                Notes = new[] { 4, 5, 4, 3, 2, 0, 4, 5, 4, 3, 2, 0, 0, 4, 0, 0, 4, 0 },
                Mots = new[] { "Son", "nez", "les", "ma", "ti", "nes", "son", "nez", "les", "ma", "ti", "nes", "ding", "dang", "dong", "ding", "dang", "dong" },
            },
            new Chanson
            {
                Titre = "Ah ! vous dirai-je, maman", Emoji = "⭐", Art = "etoile", Fin = "Ah ! vous dirai-je, maman, ce qui cause mon tourment !",
                Notes = new[] { 0, 0, 4, 4, 5, 5, 4, 3, 3, 2, 2, 1, 1, 0 },
                Mots = new[] { "Ah", "vous", "di", "rai", "je", "ma", "man", "ce", "qui", "cau", "se", "mon", "tour", "ment" },
            },
            new Chanson
            {
                Titre = "La gamme : do, ré, mi…", Emoji = "🎼", Art = "note-musique", Fin = "Do, ré, mi, fa, sol, la ! Et on redescend !",
                Notes = new[] { 0, 1, 2, 3, 4, 5, 5, 4, 3, 2, 1, 0 },
                Mots = new[] { "do", "ré", "mi", "fa", "sol", "la", "la", "sol", "fa", "mi", "ré", "do" },
            },
            new Chanson
            {
                Titre = "Vive le vent", Emoji = "❄️", Art = "flocon", Fin = "Vive le vent, vive le vent, vive le vent d'hiver !",
                Notes = new[] { 2, 2, 2, 2, 2, 2, 2, 4, 0, 1, 2, 3, 3, 3, 3, 3, 2, 2, 2, 2, 1, 1, 2, 1, 4 },
                Mots = new[] { "Vi", "ve le", "vent", "vi", "ve le", "vent", "vi", "ve le", "vent", "d'hi", "ver", "qui", "s'en", "va", "sif", "flant", "souf", "flant", "dans", "les", "grands", "sa", "pins", "verts", "!" },
            },
            new Chanson
            {
                Titre = "Dans la ferme de Mathurin", Emoji = "🐄", Art = "vache", Fin = "Dans la ferme de Mathurin, i a i a o !",
                Notes = new[] { 3, 3, 3, 0, 1, 1, 0, 5, 5, 4, 4, 3 },
                Mots = new[] { "Dans", "la", "fer-me", "de", "Ma", "thu", "rin", "i", "a", "i", "a", "o" },
            },
            new Chanson
            {
                Titre = "L'Hymne à la joie", Emoji = "🎶", Art = "nuage-musique", Fin = "L'Hymne à la joie, de Beethoven !",
                Notes = new[] { 2, 2, 3, 4, 4, 3, 2, 1, 0, 0, 1, 2, 2, 1, 1 },
                Mots = new[] { "la", "la", "la", "la", "la", "la", "la", "la", "la", "la", "la", "la", "la", "la", "la" },
            },
        };

        private Chanson _chanson = Chansons[0];
        private string _timbre = "xylophone";
        private bool _guitare; // les « lames » sont les cordes horizontales de la guitare

        private readonly int _startAct; // 1 instruments · 3 xylophone et chanson · 5 je joue · 7 guitare
        private Canvas _canvas;
        private int _act;
        private bool[] _seen;
        private List<Instr> _session;
        private int _quizRound;
        private Instr _quizTarget;
        private int _played;
        private int _songPos;
        private List<Border> _bars;
        private List<Button> _barButtons;
        private TextBlock _lyrics;
        private FrameworkElement _marker; // l'étoile qui désigne la lame à jouer
        private readonly Dictionary<int, DateTime> _lastPlay = new Dictionary<int, DateTime>();

        // REGARD TENU = la note se RÉPÈTE (do, do, do…) : pas besoin de sortir
        // et de revenir sur la lame. Toutes les 750 ms tant qu'on reste dessus.
        private int _gazeBar = -1;
        private Action<int> _repeatAction;
        private readonly DispatcherTimer _repeat = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        private const int RepeatMs = 750;

        public MusicSchoolGame(Action celebrate, int startAct = 1) : base(celebrate)
        {
            _startAct = startAct;
            _repeat.Tick += (o, ev) =>
            {
                if (Closed || Locked || _gazeBar < 0 || _repeatAction == null) return;
                if (_lastPlay.TryGetValue(_gazeBar, out var t) && (DateTime.Now - t).TotalMilliseconds < RepeatMs) return;
                _repeatAction(_gazeBar);
            };
            Loaded += (o, ev) => _repeat.Start();
            Unloaded += (o, ev) => _repeat.Stop();
        }

        protected override void NewRound()
        {
            _session = GameKit.Shuffle(Instruments).ToList();
            if (_startAct >= 7) StartGuitar();
            else if (_startAct >= 5) StartFreePlay();
            else if (_startAct >= 3) StartXylophone();
            else StartInstruments();
        }

        private static FrameworkElement Pic(Instr i, double size) => Art.Visual(i.Key, i.Emoji, size);

        // Un pad ne rejoue pas s'il vient de jouer (le regard tremble un peu).
        private bool CanPlay(int pad, int cooldownMs)
        {
            var now = DateTime.Now;
            if (_lastPlay.TryGetValue(pad, out var t) && (now - t).TotalMilliseconds < cooldownMs) return false;
            _lastPlay[pad] = now;
            return true;
        }

        // ==================================================================
        // ACTE 1 — Les instruments : ça joue dès le regard, le nom au clic.
        // ==================================================================
        private void StartInstruments()
        {
            _act = 1;
            Locked = false;
            _seen = new bool[_session.Count];
            _lastPlay.Clear();
            Question.Text = "🎵 Le petit orchestre";
            _canvas = new Canvas { Width = W, Height = H };
            var pads = new List<FrameworkElement>();

            double size = 300, gapX = 60, gapY = 30;
            int cols = 3;
            double x0 = (W - cols * size - (cols - 1) * gapX) / 2, y0 = 20;
            for (int i = 0; i < _session.Count; i++)
            {
                var ins = _session[i];
                int idx = i;
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(Pic(ins, 180));
                content.Children.Add(new TextBlock
                {
                    Text = Cap(ins.Article) + " " + ins.Name, FontSize = 34, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 0),
                });
                var btn = AnswerButton(new Viewbox { Child = content, Margin = new Thickness(14) }, size);
                var captured = btn;
                btn.Click += (o, ev) => NameInstrument(captured, idx);
                Canvas.SetLeft(btn, x0 + (i % cols) * (size + gapX));
                Canvas.SetTop(btn, y0 + (i / cols) * (size + gapY));
                _canvas.Children.Add(btn);
                pads.Add(btn);
            }
            _repeatAction = null; // les instruments ne se répètent pas en boucle
            SetGazePads(pads, i =>
            {
                _gazeBar = -1;
                if (i < 0 || _act != 1 || Locked || !CanPlay(i, 1500)) return;
                SoundFx.Instrument(_session[i].Key);
                Pulse(pads[i]);
                Highlight(pads[i], false);
                if (!_seen[i]) { _seen[i] = true; RewardStar(); CheckAllHeard(); }
            });

            SetConsigne(Art.ForEmoji("🎵", 110), () => "Regarde un instrument : il joue tout de suite ! Fixe-le pour entendre son nom.");
            SetBody(_canvas);
            Schedule(450, () => Speak("Regarde un instrument : il joue !"));
        }

        private void NameInstrument(Button btn, int i)
        {
            if (_act != 1 || Locked) return;
            var ins = _session[i];
            SoundFx.Instrument(ins.Key);
            Pulse(btn);
            Speak(Cap(ins.Article) + " " + ins.Name + " ! " + ins.Says);
            if (!_seen[i]) { _seen[i] = true; RewardStar(); CheckAllHeard(); }
        }

        private void CheckAllHeard()
        {
            if (!_seen.All(x => x)) return;
            Locked = true;
            Schedule(3000, () =>
            {
                Speak("Bravo ! Maintenant : qui fait ce bruit ?");
                Schedule(3800, StartQuiz);
            });
        }

        // ==================================================================
        // ACTE 2 — Qui fait ce bruit ? (au clic : c'est un choix)
        // ==================================================================
        private void StartQuiz()
        {
            _act = 2;
            _quizRound = 0;
            SetGazePads(null, null);
            _repeatAction = null;
            _gazeBar = -1;
            NextQuiz();
        }

        private void NextQuiz()
        {
            Locked = false;
            _quizRound++;
            if (_quizRound > 4) { StartXylophone(); return; }

            _quizTarget = _session[(_quizRound - 1) % _session.Count];
            var choices = Level.Pick(_session, _quizTarget, 4);
            Question.Text = "👂 Qui fait ce bruit ?";
            _canvas = new Canvas { Width = W, Height = H };

            double gap = 60, size = Math.Min(330, (W - 100 - (choices.Count - 1) * gap) / choices.Count);
            double x0 = (W - choices.Count * size - (choices.Count - 1) * gap) / 2;
            for (int k = 0; k < choices.Count; k++)
            {
                var ins = choices[k];
                var btn = AnswerButton(new Viewbox { Child = Pic(ins, 200), Margin = new Thickness(26) }, size);
                var captured = btn;
                btn.Click += (o, ev) => AnswerQuiz(captured, ins);
                Canvas.SetLeft(btn, x0 + k * (size + gap));
                Canvas.SetTop(btn, 150);
                _canvas.Children.Add(btn);
            }

            var ear = AnswerButton(new TextBlock { Text = "👂", FontSize = 90, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, 150);
            ear.Click += (o, ev) => { if (!Locked) SoundFx.Instrument(_quizTarget.Key); };
            Canvas.SetLeft(ear, W / 2 - 75);
            Canvas.SetTop(ear, 10);
            _canvas.Children.Add(ear);

            SetConsigne(new TextBlock { Text = "👂", FontSize = 60 }, () => "Écoute... Quel instrument fait ce bruit ? Regarde-le ! L'oreille le rejoue.");
            SetBody(_canvas);
            Schedule(400, () =>
            {
                Speak(_quizRound == 1 ? "Écoute bien... Quel instrument fait ce bruit ?" : "Et celui-là ?");
                Schedule(2300, () => SoundFx.Instrument(_quizTarget.Key));
            });
            ArmHint(ear, () => "Regarde l'oreille pour réentendre, puis l'instrument qui fait ce bruit !");
        }

        private void AnswerQuiz(Button btn, Instr ins)
        {
            if (_act != 2 || Locked) return;
            if (ins == _quizTarget)
            {
                Locked = true;
                DisarmHint();
                GameKit.Success();
                Pulse(btn);
                SoundFx.Instrument(ins.Key);
                Speak("Oui ! C'est " + ins.Article + " " + ins.Name + " ! " + GameKit.Praise());
                Celebrate();
                Schedule(3400, NextQuiz);
            }
            else
            {
                Shake(btn);
                SoundFx.Instrument(ins.Key);
                Speak("Ça, c'est " + ins.Article + " " + ins.Name + ". Écoute encore !");
                Schedule(2600, () => { if (!Locked && _act == 2) SoundFx.Instrument(_quizTarget.Key); });
            }
        }

        // ==================================================================
        // ACTE 3 — Le xylophone (do ré mi fa sol), qui chante au regard
        // ==================================================================
        private void StartXylophone()
        {
            _act = 3;
            Locked = false;
            _played = 0;
            Question.Text = "🎹 Le xylophone";
            BuildBars(5, "xylophone", null);
            SetConsigne(Art.Visual("xylophone", "🎹", 110), () => "Regarde les lames du xylophone : chacune chante sa note ! Do, ré, mi, fa, sol !");
            SetBody(_canvas);
            Schedule(450, () => Speak("Le xylophone ! Regarde une lame, elle chante !"));
        }

        // Les lames (ou touches) : n notes, un timbre, et ce qui se passe quand
        // une note est jouée (regard ou clic).
        private void BuildBars(int n, string timbre, Action<int> onPlayed)
        {
            _canvas = new Canvas { Width = W, Height = H };
            _bars = new List<Border>();
            _lastPlay.Clear();
            _guitare = false;
            _timbre = timbre;
            var pads = new List<FrameworkElement>();
            double gap = n > 5 ? 18 : 34;
            double barW = Math.Min(190, (W - 80 - (n - 1) * gap) / n);
            double baseH = 520;
            double x0 = (W - n * barW - (n - 1) * gap) / 2;
            for (int i = 0; i < n; i++)
            {
                var (name, col) = Notes[i];
                double h = baseH - i * (n > 5 ? 40 : 62); // plus court vers l'aigu
                var label = new TextBlock
                {
                    Text = name, FontSize = n > 5 ? 44 : 56, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 22),
                };
                var bar = new Border
                {
                    Width = barW, Height = h, CornerRadius = new CornerRadius(Math.Min(40, barW / 3)),
                    Background = new LinearGradientBrush(Lighten(col), col, 90),
                    BorderBrush = Brushes.White, BorderThickness = new Thickness(5),
                    Child = label, RenderTransformOrigin = new Point(0.5, 1),
                };
                var btn = new Button
                {
                    Style = (Style)Application.Current.Resources["AnswerButton"],
                    Width = barW + 16, Height = h + 16, Padding = new Thickness(0),
                    Background = Brushes.Transparent, Content = bar,
                };
                int idx = i;
                btn.Click += (o, ev) => PlayBar(idx, timbre, onPlayed, 0);
                Canvas.SetLeft(btn, x0 + i * (barW + gap));
                Canvas.SetTop(btn, H - 40 - h - 16);
                _canvas.Children.Add(btn);
                _bars.Add(bar);
                pads.Add(btn);
            }
            _barButtons = pads.Cast<Button>().ToList();
            _marker = null;
            _gazeBar = -1;
            _repeatAction = i => PlayBar(i, timbre, onPlayed, 0);
            SetGazePads(pads, i => { _gazeBar = i; if (i >= 0) PlayBar(i, timbre, onPlayed, 350); });
        }

        private void PlayBar(int i, string timbre, Action<int> onPlayed, int cooldown)
        {
            if (Locked || !CanPlay(i, cooldown)) return;
            SoundFx.PlayNote(i, timbre);
            Pulse(_bars[i]);
            if (onPlayed != null) { onPlayed(i); return; }
            if (_act == 3)
            {
                Speak(Notes[i].Name + " !");
                if (++_played == 1) RewardStar();
                if (_played >= 8)
                {
                    Locked = true;
                    Schedule(1800, () =>
                    {
                        Speak("Bravo ! Maintenant, une chanson : regarde la lame qui brille !");
                        Schedule(5200, StartSong);
                    });
                }
            }
            else if (_act == 4) SongStep(i);
        }

        // ==================================================================
        // ACTE 4 — La chanson : « Au clair de la lune »
        // ==================================================================
        private void StartSong()
        {
            _act = 4;
            Locked = false;
            _songPos = 0;
            _chanson = Chansons[0];
            Question.Text = "🌙 Au clair de la lune";
            BuildBars(5, "xylophone", null);
            _lyrics = new TextBlock
            {
                FontSize = 44, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                Width = W, TextAlignment = TextAlignment.Center,
            };
            Canvas.SetTop(_lyrics, 18);
            _canvas.Children.Add(_lyrics);
            var moon = Art.Visual("lune", "🌙", 120);
            Canvas.SetLeft(moon, 40);
            Canvas.SetTop(moon, 10);
            _canvas.Children.Add(moon);
            SetConsigne(Art.Visual("lune", "🌙", 110), () => "Au clair de la lune ! Regarde la lame qui brille, note après note !");
            SetBody(_canvas);
            ShowLyrics();
            Glow(_chanson.Notes[0]);
        }

        private void ShowLyrics()
        {
            var parts = new List<string>();
            for (int k = 0; k < _chanson.Notes.Length; k++)
                parts.Add(k == _songPos ? "[" + _chanson.Mots[k] + "]" : _chanson.Mots[k]);
            _lyrics.Text = string.Join(" ", parts);
        }

        private void SongStep(int bar)
        {
            int want = _chanson.Notes[_songPos];
            if (bar != want) { Glow(want); return; } // on a entendu la note ; la bonne rebrille
            _songPos++;
            if (_songPos == 1) RewardStar();
            if (_songPos >= _chanson.Notes.Length)
            {
                Locked = true;
                ShowLyrics();
                ClearGlow();
                GameKit.Success();
                Speak("Tu as joué toute la chanson ! Écoute-la !");
                Schedule(2600, PlayBack);
                return;
            }
            ShowLyrics();
            Glow(_chanson.Notes[_songPos]);
        }

        private void PlayBack()
        {
            var notes = _chanson.Notes;
            for (int k = 0; k < notes.Length; k++)
            {
                int bar = notes[k];
                Schedule(60 + k * 520, () =>
                {
                    SoundFx.PlayNote(bar, _timbre, _guitare ? 0.9 : 0.55);
                    if (_guitare) Vibrate(_bars[bar]); else Pulse(_bars[bar]);
                });
            }
            Schedule(60 + notes.Length * 520 + 400, () =>
            {
                Speak(_chanson.Fin + " Bravo ! " + GameKit.Praise());
                Celebrate();
                Action suite = _guitare ? ChooseGuitarSong : _startAct >= 3 ? (Action)StartFreePlay : NewRound;
                Schedule(4500, suite);
            });
        }

        // La lame à jouer est ÉVIDENTE : les autres s'éteignent, elle garde un
        // gros halo doré, et une étoile sautille juste au-dessus.
        private void Glow(int bar)
        {
            for (int i = 0; i < _bars.Count; i++)
            {
                _bars[i].BeginAnimation(UIElement.OpacityProperty, null);
                _bars[i].Opacity = i == bar ? 1 : 0.35;
                _bars[i].Effect = i == bar
                    ? new DropShadowEffect { BlurRadius = 70, ShadowDepth = 0, Color = Color.FromRgb(0xFF, 0xB3, 0x00), Opacity = 1 }
                    : null;
            }
            if (_marker == null)
            {
                _marker = Art.Visual("etoile", "⭐", 110);
                _marker.IsHitTestVisible = false;
                var tr = new TranslateTransform();
                _marker.RenderTransform = tr;
                tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-18, 14, TimeSpan.FromMilliseconds(380))
                { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
                _canvas.Children.Add(_marker);
            }
            var btn = _barButtons[bar];
            if (_guitare)
            {
                // À droite du chevalet, en face de la corde.
                Canvas.SetLeft(_marker, GuitarStarX);
                Canvas.SetTop(_marker, Canvas.GetTop(btn) + btn.Height / 2 - 55);
                return;
            }
            // Dans le haut de la lame (au-dessus, elle cacherait les paroles).
            Canvas.SetLeft(_marker, Canvas.GetLeft(btn) + btn.Width / 2 - 55);
            Canvas.SetTop(_marker, Canvas.GetTop(btn) + 22);
        }

        private void ClearGlow()
        {
            foreach (var b in _bars) { b.BeginAnimation(UIElement.OpacityProperty, null); b.Opacity = 1; b.Effect = null; }
            if (_marker != null) { _canvas.Children.Remove(_marker); _marker = null; }
        }

        // ==================================================================
        // ACTE 5 — Je joue ! Choisir un instrument, puis en jouer au regard.
        // ==================================================================
        private static readonly (string Key, string Name, string Emoji)[] Playable =
        {
            ("xylophone", "Le xylophone", "🎹"), ("piano", "Le piano", "🎹"), ("guitare", "La guitare", "🎸"),
            ("trompette", "La trompette", "🎺"), ("violon", "Le violon", "🎻"), ("batterie", "La batterie", "🥁"),
        };

        private void StartFreePlay()
        {
            _act = 5;
            Locked = false;
            SetGazePads(null, null);
            _repeatAction = null;
            _gazeBar = -1;
            Question.Text = "🎸 Je joue !";
            _canvas = new Canvas { Width = W, Height = H };
            double size = 300, gapX = 60, gapY = 30;
            int cols = 3;
            double x0 = (W - cols * size - (cols - 1) * gapX) / 2, y0 = 20;
            for (int i = 0; i < Playable.Length; i++)
            {
                var (key, name, emoji) = Playable[i];
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(Art.Visual(key, emoji, 180));
                content.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 34, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0),
                });
                var btn = AnswerButton(new Viewbox { Child = content, Margin = new Thickness(14) }, size);
                var k = key; var n = name;
                btn.Click += (o, ev) => OpenInstrument(k, n);
                Canvas.SetLeft(btn, x0 + (i % cols) * (size + gapX));
                Canvas.SetTop(btn, y0 + (i / cols) * (size + gapY));
                _canvas.Children.Add(btn);
            }
            SetConsigne(Art.ForEmoji("🎵", 110), () => "Choisis un instrument, et joue avec tes yeux !");
            SetBody(_canvas);
            Schedule(450, () => Speak("Choisis un instrument !"));
        }

        private void OpenInstrument(string key, string name)
        {
            if (_act != 5 || Locked) return;
            _act = 6;
            Question.Text = "🎵 " + name;
            Action<int> compte = i => { if (_played++ == 0) RewardStar(); };
            if (key == "batterie") BuildDrums();
            else if (key == "guitare") BuildGuitar(compte);
            else BuildBars(8, key, compte);
            _played = 0;

            // Le bouton pour changer d'instrument (au clic, en haut à droite).
            var back = AnswerButton(new TextBlock { Text = "🔁", FontSize = 70, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, 130);
            back.Click += (o, ev) => { if (_act == 6) StartFreePlay(); };
            Canvas.SetLeft(back, W - 150);
            Canvas.SetTop(back, 16);
            _canvas.Children.Add(back);

            string quoi = key == "guitare" ? "une corde" : key == "batterie" ? "un tambour" : "une touche";
            SetConsigne(Art.Visual(key, "🎵", 110), () => name + " ! Regarde " + quoi + ", ça joue ! La flèche change d'instrument.");
            SetBody(_canvas);
            Schedule(300, () => Speak(name + " ! Regarde " + quoi + " pour jouer !"));
        }

        // ==================================================================
        // ACTE 7 — La guitare : six cordes qui s'allument au regard, puis des
        // chansons à jouer corde après corde (la corde à jouer brille).
        // ==================================================================
        private const double GuitarStarX = 1190;
        private static readonly double[] StringY = { 150, 234, 318, 402, 486, 570 };

        private void StartGuitar()
        {
            _act = 7;
            Locked = false;
            _played = 0;
            Question.Text = "🎸 La guitare";
            BuildGuitar(i =>
            {
                Speak(Notes[i].Name + " !");
                if (++_played == 1) RewardStar();
                if (_played >= 6)
                {
                    Locked = true;
                    Schedule(1600, () =>
                    {
                        Speak("Bravo ! Maintenant, on joue une chanson. Choisis-la !");
                        Schedule(4200, ChooseGuitarSong);
                    });
                }
            });
            SetConsigne(Art.Visual("guitare", "🎸", 110), () => "La guitare ! Regarde une corde : elle s'allume et elle chante !");
            SetBody(_canvas);
            Schedule(450, () => Speak("La guitare ! Regarde une corde, elle s'allume et elle chante !"));
        }

        private void ChooseGuitarSong()
        {
            _act = 8;
            Locked = false;
            SetGazePads(null, null);
            _repeatAction = null;
            _gazeBar = -1;
            Question.Text = "🎸 Quelle chanson ?";
            _canvas = new Canvas { Width = W, Height = H };
            int cols = 4;
            double size = 290, gap = 30;
            double x0 = (W - cols * size - (cols - 1) * gap) / 2;
            for (int i = 0; i < Chansons.Length; i++)
            {
                var c = Chansons[i];
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(Art.Visual(c.Art, c.Emoji, 150));
                content.Children.Add(new TextBlock
                {
                    Text = c.Titre, FontSize = 28, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                    MaxWidth = 250, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0),
                });
                var btn = AnswerButton(new Viewbox { Child = content, Margin = new Thickness(14) }, size);
                var ch = c;
                btn.Click += (o, ev) => PlayGuitarSong(ch);
                Canvas.SetLeft(btn, x0 + (i % cols) * (size + gap));
                Canvas.SetTop(btn, 30 + (i / cols) * (size + gap));
                _canvas.Children.Add(btn);
            }
            SetConsigne(Art.Visual("guitare", "🎸", 110), () => "Choisis une chanson à jouer sur la guitare !");
            SetBody(_canvas);
            Schedule(450, () => Speak("Choisis une chanson !"));
        }

        private void PlayGuitarSong(Chanson c)
        {
            if (_act != 8 || Locked) return;
            _act = 9;
            Locked = false;
            _chanson = c;
            _songPos = 0;
            Question.Text = c.Emoji + " " + c.Titre;
            BuildGuitar(SongStep);
            _lyrics = new TextBlock
            {
                FontSize = c.Notes.Length > 16 ? 30 : 40, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                Width = W - 180, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
            };
            Canvas.SetLeft(_lyrics, 20);
            Canvas.SetTop(_lyrics, 8);
            _canvas.Children.Add(_lyrics);

            // Changer de chanson (au clic, en haut à droite).
            var back = AnswerButton(new TextBlock { Text = "🔁", FontSize = 70, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, 130);
            back.Click += (o, ev) => { if (_act == 9) ChooseGuitarSong(); };
            Canvas.SetLeft(back, W - 150);
            Canvas.SetTop(back, 16);
            _canvas.Children.Add(back);

            SetConsigne(Art.Visual(c.Art, c.Emoji, 110), () => c.Titre + " ! Regarde la corde qui brille, note après note !");
            SetBody(_canvas);
            ShowLyrics();
            Glow(c.Notes[0]);
            Schedule(400, () => Speak(c.Titre + " ! Regarde la corde qui brille !"));
        }

        // La guitare couchée : manche à gauche, caisse à droite, six cordes de
        // couleur (do, ré, mi, fa, sol, la) qui traversent l'écran. Chaque corde
        // est une cible du regard : elle s'allume, vibre et sonne.
        private void BuildGuitar(Action<int> onPlayed)
        {
            _canvas = new Canvas { Width = W, Height = H };
            _bars = new List<Border>();
            _lastPlay.Clear();
            _guitare = true;
            _timbre = "guitare";
            var pads = new List<FrameworkElement>();

            var bois = new LinearGradientBrush(Color.FromRgb(0xF0, 0xB4, 0x6A), Color.FromRgb(0xC9, 0x7B, 0x2E), 90);
            var boisSombre = new SolidColorBrush(Color.FromRgb(0x5A, 0x36, 0x1C));
            var bord = new SolidColorBrush(Color.FromRgb(0x8A, 0x4B, 0x14));
            var creme = new SolidColorBrush(Color.FromRgb(0xF3, 0xD9, 0xA6));

            // La caisse : deux rondeurs (petite à gauche, grande à droite).
            void Rond(double left, double top, double w, double h, bool trait)
            {
                var e = new System.Windows.Shapes.Ellipse { Width = w, Height = h, Fill = bois, Stroke = trait ? bord : null, StrokeThickness = 7 };
                Canvas.SetLeft(e, left); Canvas.SetTop(e, top);
                _canvas.Children.Add(e);
            }
            Rond(560, 115, 470, 470, true);
            Rond(780, 70, 600, 560, true);
            Rond(560, 115, 470, 470, false); // recouvre les traits intérieurs
            Rond(780, 70, 600, 560, false);

            // Le manche et la tête.
            var manche = new System.Windows.Shapes.Rectangle { Width = 580, Height = 500, Fill = boisSombre, RadiusX = 14, RadiusY = 14 };
            Canvas.SetLeft(manche, 110); Canvas.SetTop(manche, 110);
            _canvas.Children.Add(manche);
            var tete = new System.Windows.Shapes.Rectangle { Width = 130, Height = 540, Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x23, 0x14)), RadiusX = 30, RadiusY = 30 };
            Canvas.SetLeft(tete, 24); Canvas.SetTop(tete, 90);
            _canvas.Children.Add(tete);
            for (int f = 0; f < 6; f++)
            {
                var frette = new System.Windows.Shapes.Rectangle { Width = 7, Height = 500, Fill = creme, Opacity = 0.8 };
                Canvas.SetLeft(frette, 200 + f * 82); Canvas.SetTop(frette, 110);
                _canvas.Children.Add(frette);
            }
            // La rosace et le chevalet.
            var rosace = new System.Windows.Shapes.Ellipse { Width = 180, Height = 180, Fill = new SolidColorBrush(Color.FromRgb(0x2B, 0x18, 0x0C)), Stroke = creme, StrokeThickness = 9 };
            Canvas.SetLeft(rosace, 800); Canvas.SetTop(rosace, 270);
            _canvas.Children.Add(rosace);
            var chevalet = new System.Windows.Shapes.Rectangle { Width = 36, Height = 470, Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x23, 0x14)), RadiusX = 12, RadiusY = 12 };
            Canvas.SetLeft(chevalet, 1125); Canvas.SetTop(chevalet, 125);
            _canvas.Children.Add(chevalet);

            for (int i = 0; i < StringY.Length; i++)
            {
                var (name, col) = Notes[i];
                double y = StringY[i];
                // La mécanique (le bouton d'accord) sur la tête.
                var meca = new System.Windows.Shapes.Ellipse { Width = 30, Height = 30, Fill = creme, Stroke = bord, StrokeThickness = 3 };
                Canvas.SetLeft(meca, 8); Canvas.SetTop(meca, y - 15);
                _canvas.Children.Add(meca);

                var grid = new Grid();
                var corde = new Border
                {
                    Height = 13 - i * 1.2, CornerRadius = new CornerRadius(7), VerticalAlignment = VerticalAlignment.Center,
                    Background = new LinearGradientBrush(Lighten(col), col, 0), Margin = new Thickness(60, 0, 20, 0),
                    BorderBrush = Brushes.White, BorderThickness = new Thickness(1.5),
                };
                grid.Children.Add(corde);
                grid.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 36, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(col),
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(44, 0, 0, 0),
                });
                var btn = new Button
                {
                    Style = (Style)Application.Current.Resources["AnswerButton"],
                    Width = 1125, Height = 80, Padding = new Thickness(0),
                    Background = Brushes.Transparent, Content = grid,
                };
                int idx = i;
                btn.Click += (o, ev) => Pluck(idx, onPlayed, 0);
                Canvas.SetLeft(btn, 40);
                Canvas.SetTop(btn, y - 40);
                _canvas.Children.Add(btn);
                _bars.Add(corde);
                pads.Add(btn);
            }
            _barButtons = pads.Cast<Button>().ToList();
            _marker = null;
            _gazeBar = -1;
            _repeatAction = i => Pluck(i, onPlayed, 0);
            SetGazePads(pads, i => { _gazeBar = i; if (i >= 0) Pluck(i, onPlayed, 350); });
        }

        private void Pluck(int i, Action<int> onPlayed, int cooldown)
        {
            if (Locked || !CanPlay(i, cooldown)) return;
            SoundFx.PlayNote(i, "guitare", 0.9);
            Vibrate(_bars[i]);
            onPlayed?.Invoke(i);
        }

        // La corde s'allume (halo blanc qui s'éteint) et vibre un instant.
        private static void Vibrate(Border corde)
        {
            var tr = new TranslateTransform();
            corde.RenderTransform = tr;
            tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-7, 7, TimeSpan.FromMilliseconds(45))
            { AutoReverse = true, RepeatBehavior = new RepeatBehavior(7), FillBehavior = FillBehavior.Stop });
            var halo = corde.Effect as DropShadowEffect;
            if (halo == null || halo.Color != Colors.White)
            {
                halo = new DropShadowEffect { BlurRadius = 45, ShadowDepth = 0, Color = Colors.White, Opacity = 1 };
                corde.Effect = halo;
                halo.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(900)) { BeginTime = TimeSpan.FromMilliseconds(250) });
            }
            else halo.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(900)) { BeginTime = TimeSpan.FromMilliseconds(250) });
        }

        // La batterie : quatre gros pads (grosse caisse, caisse claire, tom, cymbale).
        private void BuildDrums()
        {
            _canvas = new Canvas { Width = W, Height = H };
            _bars = new List<Border>();
            _lastPlay.Clear();
            var pads = new List<FrameworkElement>();
            (string Name, Color C)[] kit =
            {
                ("boum", Color.FromRgb(0xE0, 0x4A, 0x4A)), ("tac", Color.FromRgb(0xFF, 0xC1, 0x07)),
                ("tom", Color.FromRgb(0x4C, 0xC2, 0x6B)), ("tching", Color.FromRgb(0x5D, 0xAD, 0xE2)),
            };
            double size = 280, gap = 40;
            double x0 = (W - 4 * size - 3 * gap) / 2;
            for (int i = 0; i < 4; i++)
            {
                var pad = new Border
                {
                    Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
                    Background = new RadialGradientBrush(Lighten(kit[i].C), kit[i].C),
                    BorderBrush = Brushes.White, BorderThickness = new Thickness(8),
                    Child = new TextBlock { Text = kit[i].Name, FontSize = 48, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    RenderTransformOrigin = new Point(0.5, 0.5),
                };
                var btn = new Button
                {
                    Style = (Style)Application.Current.Resources["AnswerButton"],
                    Width = size + 16, Height = size + 16, Padding = new Thickness(0), Background = Brushes.Transparent, Content = pad,
                };
                int idx = i;
                btn.Click += (o, ev) => HitDrum(idx, 0);
                Canvas.SetLeft(btn, x0 + i * (size + gap));
                Canvas.SetTop(btn, 220);
                _canvas.Children.Add(btn);
                _bars.Add(pad);
                pads.Add(btn);
            }
            _gazeBar = -1;
            _repeatAction = i => HitDrum(i, 0);
            SetGazePads(pads, i => { _gazeBar = i; if (i >= 0) HitDrum(i, 250); });
        }

        private void HitDrum(int i, int cooldown)
        {
            if (Locked || !CanPlay(i, cooldown)) return;
            SoundFx.DrumKit(i);
            Pulse(_bars[i]);
            if (_played++ == 0) RewardStar();
        }

        // ------------------------------------------------------------------
        private static void Pulse(FrameworkElement el)
        {
            var sc = new ScaleTransform(1, 1);
            if (el.RenderTransformOrigin == new Point(0, 0)) el.RenderTransformOrigin = new Point(0.5, 0.5);
            el.RenderTransform = sc;
            var a = new DoubleAnimation(1, 1.12, TimeSpan.FromMilliseconds(140)) { AutoReverse = true, EasingFunction = new SineEase() };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }

        private static Color Lighten(Color c) => Color.FromRgb(
            (byte)(c.R + (255 - c.R) * 0.45), (byte)(c.G + (255 - c.G) * 0.45), (byte)(c.B + (255 - c.B) * 0.45));

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
