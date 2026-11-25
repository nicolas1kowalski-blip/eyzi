using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// « La valise du voyageur » (PS/MS) : on part en voyage ! Selon la MÉTÉO
    /// du pays (neige qui tombe ❄, pluie qui ruisselle 💧, grand soleil ☀ —
    /// animées à l'écran), l'enfant choisit les bons habits et les met dans la
    /// valise (ils y volent en cloche, la valise rebondit). Les mauvais choix
    /// font rire : « le maillot de bain en Russie ? On va geler ! » Zéro
    /// lecture, tout est parlé.
    ///
    /// L'activité choisit le pays et les habits, et parle ; ce qu'on voit (le
    /// pays et son temps, la valise, les habits) est dans ValiseMoteur.cs, ses
    /// dessins dans ValiseArt.cs (fabriqué par desktop/jeux-dessines).
    /// </summary>
    public sealed class SuitcaseGame : GameControl
    {
        private sealed class Trip
        {
            public string Country;    // « la Russie »
            public int Flag;          // FlagArt
            public string Weather;    // neige | pluie | soleil
            public string Why;        // « il fait très froid »
            public (string Dessin, string Name)[] Good;              // Dessin : la clé de l'habit dans ValiseArt
            public (string Dessin, string Name, string Joke)[] Bad;
        }

        private static readonly Trip[] Trips =
        {
            new Trip
            {
                Country = "la Russie", Flag = 4, Weather = "neige", Why = "il fait très très froid, il neige",
                Good = new[] { ("echarpe", "l'écharpe"), ("moufles", "les moufles"), ("manteau", "le gros manteau") },
                Bad = new[]
                {
                    ("maillot", "le maillot de bain", "Le maillot de bain dans la neige ? Brrr, on va geler ! Hi hi !"),
                    ("lunettes", "les lunettes de soleil", "Des lunettes de soleil ? Mais il neige ! Hi hi !"),
                    ("tongs", "les tongs", "Des tongs dans la neige ? Les pieds vont devenir tout bleus ! Hi hi !"),
                },
            },
            new Trip
            {
                Country = "l'Égypte", Flag = 3, Weather = "soleil", Why = "il fait très très chaud, le soleil tape fort",
                Good = new[] { ("casquette", "la casquette"), ("lunettes", "les lunettes de soleil"), ("teeshirt", "le tee-shirt") },
                Bad = new[]
                {
                    ("echarpe", "l'écharpe", "Une écharpe en plein désert ? On va cuire ! Hi hi !"),
                    ("moufles", "les moufles", "Des moufles au soleil ? Les mains vont fondre ! Hi hi !"),
                    ("manteau", "le gros manteau", "Le gros manteau sous le grand soleil ? Quelle idée ! Hi hi !"),
                },
            },
            new Trip
            {
                Country = "l'Angleterre", Flag = 1, Weather = "pluie", Why = "il pleut beaucoup",
                Good = new[] { ("parapluie", "le parapluie"), ("bottes", "les bottes"), ("impermeable", "l'imperméable") },
                Bad = new[]
                {
                    ("maillot", "le maillot de bain", "Le maillot sous la pluie ? Brrr ! Hi hi !"),
                    ("lunettes", "les lunettes de soleil", "Des lunettes de soleil sous les nuages gris ? Hi hi !"),
                    ("chapeau", "le chapeau de paille", "Le chapeau de paille va être tout mouillé ! Hi hi !"),
                },
            },
        };

        private readonly Random _rng = new Random();
        private readonly ValiseMoteur _moteur = new ValiseMoteur();
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private List<(string Dessin, string Name, string Joke, bool Good)> _items;
        private int _tripIdx;
        private int _packed;

        public SuitcaseGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(SuitcaseGame), null, incruste: true);
            _moteur.Demande = quoi =>
            {
                if (Closed || quoi == null) return;
                if (quoi == "range") Packed();
                else if (quoi.StartsWith("choix:") && int.TryParse(quoi.Substring(6), out int i)) PickItem(i);
            };
        }

        protected override void NewRound()
        {
            _tripIdx = 0;
            BuildTrip();
        }

        private void BuildTrip()
        {
            Locked = false;
            _packed = 0;
            var t = Trips[_tripIdx % Trips.Length];

            Question.Text = "🧳 La valise du voyageur";
            SetConsigne(FlagArt.Make(t.Flag, 140),
                () => "On part " + Dest(t) + " ! Là-bas, " + t.Why + " ! Mets les BONS habits dans la valise !");

            // Les 6 habits autour de la valise, mélangés ; derrière, le pays et le temps qu'il y fait.
            _items = new List<(string Dessin, string Name, string Joke, bool Good)>();
            foreach (var (d, n) in t.Good) _items.Add((d, n, null, true));
            foreach (var (d, n, j) in t.Bad) _items.Add((d, n, j, false));
            _items = _items.OrderBy(i => _rng.Next()).ToList();
            _moteur.Prepare(t.Weather, _items.Select(i => i.Dessin).ToList(), _items.Select(i => i.Good).ToList());
            if (_scene.Parent == null) SetBodyFill(_scene);

            Speak("La valise du voyageur ! On part " + Dest(t) + " ! Là-bas, " + t.Why +
                  " ! Regarde les bons habits pour les mettre dans la valise !");
        }

        private static string Dest(Trip t) => "en " + t.Country.Replace("la ", "").Replace("l'", "");

        private void PickItem(int i)
        {
            if (Locked || _items == null || i < 0 || i >= _items.Count) return;
            var it = _items[i];
            if (!it.Good)
            {
                _moteur.Refuse(i);    // l'habit se secoue
                Speak(it.Joke);
                return;
            }
            Speak(Cap(it.Name) + " ! Oui, dans la valise !");
            _moteur.Range(i);         // l'habit VOLE en cloche jusqu'à la valise ; « range » revient quand il y est
        }

        /// <summary>Un bon habit vient de tomber dans la valise.</summary>
        private void Packed()
        {
            var t = Trips[_tripIdx % Trips.Length];
            _packed++;
            if (_packed < t.Good.Length || Locked) return;
            // Valise pleine : le couvercle se FERME, on colle l'étiquette, et on décolle !
            Locked = true;
            Schedule(500, () =>
            {
                _moteur.Ferme();
                Schedule(560, () =>
                {
                    GameKit.Success();
                    Speak("La valise est prête ! En route ! Bon voyage !");
                    Celebrate();
                    _tripIdx++;
                    if (_tripIdx >= Trips.Length)
                    {
                        Schedule(2600, () =>
                        {
                            Speak("Bravo, petit voyageur !");
                            Schedule(3200, NewRound);
                        });
                    }
                    else Schedule(2800, BuildTrip);
                });
            });
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
