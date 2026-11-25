using System;
using System.Linq;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🌱 « Dans l'ordre » (MS — se repérer dans le temps : avant, après) : une
    /// petite histoire en trois images à remettre dans l'ordre.
    ///   1. ON REGARDE — les trois images sont dans leurs cadres, 1, 2, 3 ; la
    ///      voix raconte : « D'abord… Ensuite… À la fin… ».
    ///   2. ON REMET DANS L'ORDRE — les images descendent, mélangées. « Qu'est-ce
    ///      qui vient en PREMIER ? » : l'image regardée monte dans son cadre si
    ///      c'est la bonne ; sinon elle se secoue et on réessaie. Puis « Et
    ///      ENSUITE ? », puis « Et À LA FIN ? ».
    ///   3. L'HISTOIRE EST COMPLÈTE — la voix la redit en entier.
    /// Quatre histoires par partie : la graine, l'œuf, la chenille, la pomme.
    /// Zéro lecture.
    ///
    /// L'activité choisit l'histoire, pose les questions et parle ; les cadres et
    /// les images qu'on voit sont dans OrdreMoteur.cs, ses dessins dans
    /// OrdreArt.cs (fabriqué par desktop/jeux-dessines).
    /// </summary>
    public sealed class SequenceGame : GameControl
    {
        private sealed class Story
        {
            public string[] Dessins;      // les trois images, dans l'ordre (clés d'OrdreArt)
            public string[] Noms;         // « la graine », « la pousse », « la fleur »
            public string[] Raconte;      // ce que la voix dit de chaque image
            public string Emoji;
        }

        private static readonly Story[] Stories =
        {
            new Story
            {
                Emoji = "🌱", Dessins = new[] { "s_graine", "s_pousse", "s_fleur" }, Noms = new[] { "la graine", "la pousse", "la fleur" },
                Raconte = new[] { "D'abord, la graine est dans la terre.", "Ensuite, la pousse sort de la terre.", "À la fin, la fleur s'ouvre !" },
            },
            new Story
            {
                Emoji = "🥚", Dessins = new[] { "s_oeuf", "s_poussin", "s_poule" }, Noms = new[] { "l'œuf", "le poussin", "la poule" },
                Raconte = new[] { "D'abord, il y a un œuf.", "Ensuite, le poussin sort de l'œuf.", "À la fin, le poussin devient une poule !" },
            },
            new Story
            {
                Emoji = "🐛", Dessins = new[] { "s_chenille", "s_cocon", "s_papillon" }, Noms = new[] { "la chenille", "le cocon", "le papillon" },
                Raconte = new[] { "D'abord, la chenille se promène.", "Ensuite, elle s'enferme dans son cocon.", "À la fin, elle devient un papillon !" },
            },
            new Story
            {
                Emoji = "🍎", Dessins = new[] { "s_pomme", "s_pomme_croquee", "s_trognon" }, Noms = new[] { "la pomme entière", "la pomme croquée", "le trognon" },
                Raconte = new[] { "D'abord, la pomme est entière.", "Ensuite, on la croque.", "À la fin, il ne reste que le trognon !" },
            },
        };

        private static readonly string[] Questions = { "Qu'est-ce qui vient en PREMIER ?", "Et ENSUITE ?", "Et À LA FIN ?" };

        private readonly Random _rng = new Random();
        private readonly OrdreMoteur _moteur = new OrdreMoteur();
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private Story[] _todo;
        private Story _story;
        private int _idx, _step;
        private int[] _ordre;      // l'image (0, 1, 2) que porte chaque carte

        public SequenceGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(SequenceGame), null, incruste: true);
            _moteur.Demande = quoi =>
            {
                if (!Closed && quoi != null && quoi.StartsWith("choix:") && int.TryParse(quoi.Substring(6), out int k) && k >= 0 && k < 3) Answer(k);
            };
        }

        protected override void NewRound()
        {
            _todo = GameKit.Shuffle(Stories).ToArray();
            _idx = -1;
            NextStory();
        }

        // ==================================================================
        // 1 — On regarde l'histoire, racontée dans l'ordre
        // ==================================================================
        private void NextStory()
        {
            _idx++;
            if (_idx >= _todo.Length) { Finish(); return; }
            _story = _todo[_idx];
            Locked = true;
            Question.Text = "👀 Regarde bien l'histoire !";
            _moteur.Prepare(_story.Dessins);
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.ForEmoji(_story.Emoji, 90), () => string.Join(" ", _story.Raconte));
            Schedule(500, () => Speak("Regarde bien l'histoire !"));
            for (int i = 0; i < 3; i++)
            {
                int etape = i;
                Schedule(2600 + i * 3200, () => { _moteur.Raconte(etape); Speak(_story.Raconte[etape]); });
            }
            Schedule(2600 + 3 * 3200 + 400, Shuffle);
        }

        // ==================================================================
        // 2 — On la remet dans l'ordre
        // ==================================================================
        private void Shuffle()
        {
            // un vrai mélange : jamais les trois images à leur place
            do { _ordre = GameKit.Shuffle(new[] { 0, 1, 2 }).ToArray(); } while (_ordre[0] == 0 && _ordre[1] == 1);
            _moteur.Melange(_ordre);
            _step = 0;
            Speak("Oh ! Tout est mélangé ! Remets l'histoire dans l'ordre.");
            Schedule(3600, Ask);
        }

        private void Ask()
        {
            Locked = false;
            Question.Text = "🔢 " + Questions[_step];
            SetConsigne(Art.ForEmoji(_story.Emoji, 90), () => Questions[_step] + " Regarde la bonne image !");
            Speak(Questions[_step]);
        }

        private void Answer(int k)
        {
            if (Locked || _ordre == null) return;
            int image = _ordre[k];
            if (image != _step)
            {
                _moteur.Refuse(k);    // l'image se secoue
                Speak(_step == 0
                    ? "Non, pas " + _story.Noms[image] + ". Qu'est-ce qui vient tout au début ?"
                    : "Non, pas encore " + _story.Noms[image] + ". Qu'est-ce qui vient juste après " + _story.Noms[_step - 1] + " ?");
                return;
            }
            Locked = true;
            GameKit.Success();
            _moteur.Place(k);         // l'image monte dans son cadre
            Speak("Oui ! " + _story.Raconte[_step]);
            RewardStar();
            _step++;
            if (_step < 3) { Schedule(2800, Ask); return; }

            // 3 — l'histoire est complète : la voix la redit en entier
            Schedule(2600, () =>
            {
                Question.Text = "🎉 L'histoire est dans l'ordre !";
                _moteur.Fin();
                Speak("Bravo ! " + string.Join(" ", _story.Raconte));
                Celebrate();
                Schedule(8500, NextStory);
            });
        }

        private void Finish()
        {
            Locked = true;
            Question.Text = "🎉 Tu sais remettre les histoires dans l'ordre !";
            SetConsigne(null, null);
            Speak("Tu sais remettre les histoires dans l'ordre ! " + GameKit.Praise());
            Celebrate();
            Schedule(4500, NewRound);
        }
    }
}
