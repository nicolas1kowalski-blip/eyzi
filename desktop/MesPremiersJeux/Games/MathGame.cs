using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🔢 LES NOMBRES JUSQU'À 10 (GS · CP), trois entrées :
    ///   1. UN DE PLUS, UN DE MOINS — des oiseaux sur un fil, un de plus arrive
    ///      (ou le dernier s'apprête à partir) : combien maintenant ? Puis
    ///      l'oiseau se pose (ou s'envole) sous ses yeux.
    ///   2. PARTAGER EN DEUX — des gâteaux pour l'enfant et Nounours, autant
    ///      pour chacun : combien chacun ? Puis les gâteaux se répartissent,
    ///      un pour Nounours, un pour lui.
    ///   3. LA BANDE NUMÉRIQUE — le petit train des nombres : un wagon a perdu
    ///      le sien, lequel ?
    /// Les grands nombres n'arrivent qu'avec le niveau (5, 7, 10).
    ///
    /// L'activité choisit les nombres, pose les questions et parle ; ce qu'on
    /// voit est un décor animé (FilMoteur.cs, GouterMoteur.cs, TrainMoteur.cs),
    /// où l'enfant choisit une pancarte à chiffre. Les dessins sont dans
    /// NombresArt.cs (fabriqué par desktop/jeux-dessines).
    /// </summary>
    public sealed class MathGame : GameControl
    {
        private static readonly string[] Num = { "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", "onze" };

        private readonly int _startAct; // 1 un de plus/moins · 2 partager · 3 bande numérique
        private int _act, _round, _answer;

        // Le décor animé de l'entrée choisie (créé à sa première question).
        private FilMoteur _fil;
        private GouterMoteur _gouter;
        private TrainMoteur _train;
        private UserControl _scene;
        private VecToile _toile;

        public MathGame(Action celebrate, int startAct = 1) : base(celebrate) { _startAct = startAct; }

        protected override void NewRound()
        {
            _round = 0;
            if (_startAct >= 3) NextStrip();
            else if (_startAct == 2) NextShare();
            else NextPlusMinus();
        }

        /// <summary>Met le décor animé dans la page, et branche les pancartes choisies sur « reponse ».</summary>
        private void Installe(VecMoteur moteur, Action<int> reponse)
        {
            if (_scene == null)
            {
                _scene = new UserControl();
                _toile = new VecToile(_scene, moteur, null, nameof(MathGame), null, incruste: true);
                moteur.Demande = quoi => { if (!Closed && quoi != null && quoi.StartsWith("choix:") && int.TryParse(quoi.Substring(6), out int v)) reponse(v); };
            }
            if (_scene.Parent == null) SetBodyFill(_scene);
        }

        private List<int> Options(int answer, int min, int max)
        {
            var pool = Enumerable.Range(min, max - min + 1).ToList();
            return Level.Pick(pool, answer, 4).OrderBy(v => v).ToList();
        }

        // ==================================================================
        // 1 · Un de plus, un de moins
        // ==================================================================
        private void NextPlusMinus()
        {
            _act = 1;
            Locked = false;
            _round++;
            if (_round > 6) { Finish("Un de plus, un de moins : tu sais ! "); return; }
            bool plus = _round % 2 == 1;
            int max = Level.MaxNumber;
            int n = plus ? GameKit.NextNumber("math-plus", 1, max - 1) : GameKit.NextNumber("math-moins", 2, max);
            _answer = plus ? n + 1 : n - 1;
            Question.Text = plus ? "➕ Un de plus !" : "➖ Un de moins !";

            if (_fil == null) _fil = new FilMoteur();
            _fil.Prepare(n, plus, Options(_answer, Math.Max(1, _answer - 2), Math.Min(max, _answer + 2)));
            Installe(_fil, AnswerPlusMinus);

            string ask = Oiseaux(n) + "... " + (plus ? "un de plus" : "un de moins") + " : combien ?";
            SetConsigne(Art.ForEmoji("🐦", 80), () => "Il y a " + Oiseaux(n) + " sur le fil. " + (plus ? "Un oiseau arrive" : "Un oiseau s'en va") + " ! Combien ça fait ?");
            Schedule(400, () => Speak(ask));
        }

        private static string Oiseaux(int n) => n == 1 ? "un oiseau" : Num[n] + " oiseaux";

        private void AnswerPlusMinus(int v)
        {
            if (_act != 1 || Locked) return;
            if (v != _answer)
            {
                _fil.Refuse(v);       // la pancarte se secoue et se voile
                Speak("Non, pas " + Num[v] + ". Compte bien !");
                return;
            }
            Locked = true;
            GameKit.Success();
            _fil.Reponse(v);          // l'oiseau vient se poser (ou s'envole) : on voit le nombre changer
            Speak("Oui ! " + Cap(Oiseaux(_answer)) + " ! " + GameKit.Praise());
            RewardStar();
            Celebrate();
            Schedule(3400, NextPlusMinus);
        }

        // ==================================================================
        // 2 · Partager en deux
        // ==================================================================
        private void NextShare()
        {
            _act = 2;
            Locked = false;
            _round++;
            if (_round > 5) { Finish("Tu sais partager ! "); return; }
            int half = GameKit.NextNumber("math-partage", 1, Level.MaxNumber / 2);
            int total = half * 2;
            _answer = half;
            Question.Text = "🍰 Autant pour chacun !";

            // Les deux assiettes : celle de l'enfant (le carton porte son prénom) et celle de Nounours.
            string name = (Settings.Load().ChildName ?? "Laura").Trim();
            if (_gouter == null) _gouter = new GouterMoteur();
            _gouter.Prepare(total, name, Options(half, 1, Math.Max(3, half + 2)));
            Installe(_gouter, AnswerShare);

            SetConsigne(Art.ForEmoji("🧁", 80), () => Num[total] + " gâteaux pour " + name + " et Nounours, autant pour chacun. Combien chacun ?");
            Schedule(400, () => Speak(Num[total] + " gâteaux, pour deux. Combien chacun ?"));
        }

        private void AnswerShare(int v)
        {
            if (_act != 2 || Locked) return;
            if (v != _answer)
            {
                _gouter.Refuse(v);
                Speak("Non, pas " + Num[v] + ". Un pour chacun, un pour chacun... Combien ?");
                return;
            }
            Locked = true;
            GameKit.Success();
            _gouter.Reponse(v);       // les gâteaux se répartissent : un pour Nounours, un pour l'enfant, en alternance
            Speak("Oui ! " + Cap(Num[_answer]) + " chacun ! " + GameKit.Praise());
            RewardStar();
            Celebrate();
            Schedule(4200 + _answer * 2 * 340, NextShare);
        }

        // ==================================================================
        // 3 · La bande numérique
        // ==================================================================
        private void NextStrip()
        {
            _act = 3;
            Locked = false;
            _round++;
            if (_round > 6) { Finish("Tu connais la bande des nombres ! "); return; }
            int max = Level.MaxNumber;
            _answer = GameKit.NextNumber("math-bande", 1, max);
            Question.Text = "🔢 Quel nombre manque ?";

            if (_train == null) _train = new TrainMoteur();
            _train.Prepare(max, _answer, Options(_answer, Math.Max(1, _answer - 2), Math.Min(max, _answer + 2)));
            Installe(_train, AnswerStrip);

            string before = _answer > 1 ? Num[_answer - 1] : null, after = _answer < max ? Num[_answer + 1] : null;
            SetConsigne(new TextBlock { Text = "?", FontSize = 70, FontWeight = System.Windows.FontWeights.ExtraBold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xB3, 0x00)) },
                () => "Compte les wagons avec moi : " + string.Join(", ", Enumerable.Range(1, max).Select(v => v == _answer ? "..." : Num[v])) + ". Quel nombre manque ?");
            // le train met deux secondes et demie à arriver : la question vient quand il s'arrête
            Schedule(2400, () => Speak(before != null && after != null ? Num[_answer - 1] + "... ? ... " + Num[_answer + 1] + ". Quel nombre manque ?"
                : before != null ? Num[_answer - 1] + "... et après ?" : "Avant " + Num[_answer + 1] + ", c'est ?"));
        }

        private void AnswerStrip(int v)
        {
            if (_act != 3 || Locked) return;
            if (v != _answer)
            {
                _train.Refuse(v);
                Speak("Non, pas " + Num[v] + ". Compte : " + (_answer > 1 ? Num[_answer - 1] + ", et après ?" : "un ?"));
                return;
            }
            Locked = true;
            GameKit.Success();
            _train.Reponse(v);        // le nombre s'écrit sur le wagon, le train siffle et repart
            Speak("Oui ! " + Cap(Num[_answer]) + " ! " + GameKit.Praise());
            RewardStar();
            Celebrate();
            Schedule(3600, NextStrip);
        }

        // ------------------------------------------------------------------
        private void Finish(string bravo)
        {
            Locked = true;
            Speak(bravo + GameKit.Praise());
            Celebrate();
            Schedule(4000, NewRound);
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
