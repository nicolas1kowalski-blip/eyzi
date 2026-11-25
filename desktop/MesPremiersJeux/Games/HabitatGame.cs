using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🐮 « Où habite-t-il ? » (PS/MS — explorer le monde du vivant) : chaque
    /// animal a son lieu de vie.
    ///   1. LES LIEUX — la ferme, la forêt, la mer : un regard sur un lieu
    ///      l'allume et dit qui y vit.
    ///   2. CHACUN CHEZ SOI — un animal attend (« Où habite le renard ? ») : il
    ///      s'en va dans le lieu regardé si c'est le bon ; sinon on explique et
    ///      on réessaie. Huit animaux par partie, tirés d'un paquet mélangé.
    ///   3. TOUT LE MONDE EST RENTRÉ — la fête !
    /// Tous les animaux sont des dessins de la banque d'images.
    ///
    /// L'activité choisit les animaux, pose les questions et parle ; les trois
    /// lieux qu'on voit sont animés par MaisonMoteur.cs (le même décor que
    /// « Range la maison », avec d'autres dessins), dans MaisonArt.cs.
    /// </summary>
    public sealed class HabitatGame : GameControl
    {
        private sealed class Place
        {
            public string Name, In, Says;                 // « la ferme », « à la ferme », ce que la voix en dit
            public (string Img, string Name)[] Animals;
        }

        // Dans l'ordre des cadres, de gauche à droite.
        private static readonly Place[] Places =
        {
            new Place
            {
                Name = "la ferme", In = "à la ferme", Says = "C'est là que vivent les animaux du fermier !",
                Animals = new[] { ("vache", "la vache"), ("cochon", "le cochon"), ("poule", "la poule"), ("mouton", "le mouton"), ("chevre", "la chèvre"), ("ane", "l'âne") },
            },
            new Place
            {
                Name = "la forêt", In = "dans la forêt", Says = "C'est là que vivent les animaux des bois, sous les grands arbres !",
                Animals = new[] { ("renard", "le renard"), ("hibou", "le hibou"), ("ecureuil", "l'écureuil"), ("cerf", "le cerf"), ("ours", "l'ours"), ("herisson", "le hérisson") },
            },
            new Place
            {
                Name = "la mer", In = "dans la mer", Says = "C'est là que vivent les animaux qui nagent, sous l'eau !",
                Animals = new[] { ("poisson", "le poisson"), ("baleine", "la baleine"), ("dauphin", "le dauphin"), ("requin", "le requin"), ("pieuvre", "la pieuvre"), ("crabe", "le crabe"), ("meduse", "la méduse") },
            },
        };

        private const int PerGame = 8;
        private static readonly List<(Place Place, string Img, string Name)> Deck = new List<(Place, string, string)>();

        private static List<(Place Place, string Img, string Name)> Draw()
        {
            var res = new List<(Place, string, string)>();
            while (res.Count < PerGame)
            {
                if (Deck.Count == 0)
                    Deck.AddRange(GameKit.Shuffle(Places.SelectMany(p => p.Animals.Select(a => (p, a.Img, a.Name)))));
                res.Add(Deck[0]);
                Deck.RemoveAt(0);
            }
            return res;
        }

        private readonly MaisonMoteur _moteur = new MaisonMoteur(0, habitats: true);
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private int _act;
        private bool[] _seen;
        private List<(Place Place, string Img, string Name)> _todo;
        private int _idx;

        public HabitatGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(HabitatGame), null, incruste: true);
            // le lieu regardé, de gauche à droite : 0, 1, 2
            _moteur.Demande = quoi =>
            {
                if (Closed || quoi == null || !quoi.StartsWith("piece:") || !int.TryParse(quoi.Substring(6), out int i) || i < 0 || i >= Places.Length) return;
                if (_act == 1) Discover(i); else if (_act == 2) Put(i);
            };
        }

        protected override void NewRound()
        {
            _todo = Draw();
            StartPlaces();
        }

        // ==================================================================
        // ACTE 1 — Les lieux
        // ==================================================================
        private void StartPlaces()
        {
            _act = 1;
            Locked = false;
            _seen = new bool[Places.Length];
            Question.Text = "🐮 Où habite-t-il ?";
            _moteur.Decouverte();
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.ForEmoji("🏡", 90), () => "Voici la ferme, la forêt et la mer ! Regarde chaque image pour savoir qui y habite.");
            Schedule(450, () => Speak("Regarde la ferme, la forêt, et la mer !"));
        }

        private void Discover(int i)
        {
            if (_act != 1 || Locked) return;
            var p = Places[i];
            _moteur.Decouvre(i);      // le lieu s'allume et s'anime
            bool first = !_seen[i];
            _seen[i] = true;
            Speak(Cap(p.Name) + " ! " + p.Says);
            if (first) RewardStar();
            if (_seen.All(x => x))
            {
                Locked = true;
                Schedule(4200, () =>
                {
                    Speak("Les animaux sont perdus ! On les ramène chez eux ?");
                    Schedule(3800, StartHomes);
                });
            }
        }

        // ==================================================================
        // ACTE 2 — Chacun chez soi
        // ==================================================================
        private void StartHomes()
        {
            _act = 2;
            _idx = -1;
            NextAnimal();
        }

        private void NextAnimal()
        {
            Locked = false;
            _idx++;
            if (_idx >= _todo.Count) { Finish(); return; }
            var item = _todo[_idx];
            Question.Text = "🏡 Où habite " + item.Name + " ?   ✅ " + _idx + " / " + _todo.Count;
            // L'animal attend sur son tapis, devant les trois lieux ; sans réponse, le bon lieu finit par clignoter.
            _moteur.Montre(item.Img, Array.IndexOf(Places, item.Place));
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.Visual(item.Img, "🐾", 90), () => "Où habite " + item.Name + " ? À la ferme, dans la forêt, ou dans la mer ? Regarde le bon endroit !");
            Schedule(400, () => Speak(_idx == 0 ? "Où habite " + item.Name + " ? Regarde le bon endroit !" : "Et " + item.Name + ", où habite-t-" + (Fem(item.Name) ? "elle" : "il") + " ?"));
        }

        private static bool Fem(string name) => name.StartsWith("la ");

        private void Put(int i)
        {
            if (_act != 2 || Locked) return;
            var item = _todo[_idx];
            var p = Places[i];
            if (p == item.Place)
            {
                Locked = true;
                GameKit.Success();
                _moteur.Range(i);     // l'animal s'en va chez lui et y prend sa place
                Speak("Oui ! " + Cap(item.Name) + " habite " + p.In + " ! " + GameKit.Praise());
                RewardStar();
                Schedule(2600, NextAnimal);
            }
            else
            {
                _moteur.Refuse(i);    // le lieu s'assombrit un instant, l'animal fait non
                Speak("Non, " + item.Name + " n'habite pas " + p.In + ". " + (Fem(item.Name) ? "Elle" : "Il") + " vit " + item.Place.In + " ! Où est-ce ?");
            }
        }

        // ==================================================================
        // ACTE 3 — Tout le monde est rentré !
        // ==================================================================
        private void Finish()
        {
            _act = 3;
            Locked = true;
            Question.Text = "🎉 Tout le monde est rentré chez soi !";
            SetConsigne(null, null);
            _moteur.Fete();
            Speak("Tout le monde est rentré chez soi ! " + GameKit.Praise());
            Celebrate();
            Schedule(4500, NewRound);
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
