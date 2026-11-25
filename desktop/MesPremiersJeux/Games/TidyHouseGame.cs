using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🏠 « Range la maison » (PS/MS) : chaque objet va dans sa pièce.
    ///   1. LES PIÈCES — la cuisine, la salle de bain, la chambre : un regard sur
    ///      une pièce l'allume, l'anime et dit à quoi elle sert.
    ///   2. ON RANGE — un objet attend devant la maison (« Où va la brosse à
    ///      dents ? ») : il s'envole dans la pièce regardée si c'est la bonne ;
    ///      sinon on explique et on réessaie. Huit objets par partie, tirés d'un
    ///      paquet mélangé (jamais les mêmes d'une partie à l'autre).
    ///   3. LA MAISON EST RANGÉE — la fête !
    /// Tous les objets sont des dessins de la banque d'images.
    ///
    /// L'activité choisit les objets, pose les questions et parle ; la maison de
    /// poupée qu'on voit est dans MaisonMoteur.cs, ses dessins dans MaisonArt.cs
    /// (fabriqué par desktop/jeux-dessines).
    /// </summary>
    public sealed class TidyHouseGame : GameControl
    {
        private sealed class Room
        {
            public string Key, Name, Says, Icon, Emoji;
            public Color Tint;
            public (string Img, string Name)[] Things;
        }

        // Dans l'ordre de la maison, de gauche à droite.
        private static readonly Room[] Rooms =
        {
            new Room
            {
                Key = "cuisine", Name = "la cuisine", Says = "C'est là qu'on prépare à manger ! Miam !", Icon = "frigo", Emoji = "🍳",
                Tint = Color.FromRgb(0xFF, 0xF1, 0xC8),
                Things = new[]
                {
                    ("casserole", "la casserole"), ("poele", "la poêle"), ("cuillere", "la cuillère"), ("fourchette", "la fourchette"),
                    ("assiette", "l'assiette"), ("verre", "le verre"), ("bol", "le bol"), ("tasse", "la tasse"),
                    ("grille-pain", "le grille-pain"), ("bouilloire", "la bouilloire"), ("theiere", "la théière"), ("louche", "la louche"),
                    ("marmite", "la marmite"), ("fouet", "le fouet"), ("micro-ondes", "le micro-ondes"), ("cafetiere", "la cafetière"),
                },
            },
            new Room
            {
                Key = "salle-de-bain", Name = "la salle de bain", Says = "C'est là qu'on se lave ! Splash !", Icon = "baignoire", Emoji = "🛁",
                Tint = Color.FromRgb(0xD6, 0xF0, 0xFF),
                Things = new[]
                {
                    ("brosse-a-dents", "la brosse à dents"), ("dentifrice", "le dentifrice"), ("savon", "le savon"), ("shampoing", "le shampoing"),
                    ("serviette", "la serviette"), ("gant-de-toilette", "le gant de toilette"), ("peignoir", "le peignoir"), ("eponge", "l'éponge"),
                    ("seche-cheveux", "le sèche-cheveux"), ("papier-toilette", "le papier toilette"), ("brosse-a-cheveux", "la brosse à cheveux"),
                    ("canard-de-bain", "le canard de bain"), ("tapis-de-bain", "le tapis de bain"), ("miroir", "le miroir"),
                },
            },
            new Room
            {
                Key = "chambre", Name = "la chambre", Says = "C'est là qu'on dort et qu'on joue ! Chut...", Icon = "ours-peluche", Emoji = "🧸",
                Tint = Color.FromRgb(0xEC, 0xE0, 0xFF),
                Things = new[]
                {
                    ("ours-peluche", "le nounours"), ("poupee", "la poupée"), ("cubes", "les cubes"), ("toupie", "la toupie"),
                    ("livre", "le livre"), ("puzzle", "le puzzle"), ("robot", "le robot"), ("train", "le petit train"),
                    ("pull", "le pull"), ("pantalon", "le pantalon"), ("chaussette", "la chaussette"),
                    ("bonnet", "le bonnet"), ("robe", "la robe"), ("pantoufles", "les pantoufles"), ("reveil", "le réveil"),
                },
            },
        };

        private const int PerGame = 8;
        private static readonly List<(Room Room, string Img, string Name)> Deck = new List<(Room, string, string)>();

        private static List<(Room Room, string Img, string Name)> Draw()
        {
            var res = new List<(Room, string, string)>();
            while (res.Count < PerGame)
            {
                if (Deck.Count == 0)
                    Deck.AddRange(GameKit.Shuffle(Rooms.SelectMany(r => r.Things.Select(t => (r, t.Img, t.Name)))));
                res.Add(Deck[0]);
                Deck.RemoveAt(0);
            }
            return res;
        }

        private readonly MaisonMoteur _moteur = new MaisonMoteur();
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private int _act;
        private bool[] _seen;
        private List<(Room Room, string Img, string Name)> _todo;
        private int _idx;

        public TidyHouseGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(TidyHouseGame), null, incruste: true);
            // la pièce regardée, de gauche à droite : 0, 1, 2
            _moteur.Demande = quoi =>
            {
                if (Closed || quoi == null || !quoi.StartsWith("piece:") || !int.TryParse(quoi.Substring(6), out int i) || i < 0 || i >= Rooms.Length) return;
                if (_act == 1) DiscoverRoom(i); else if (_act == 2) Put(i);
            };
        }

        protected override void NewRound()
        {
            _todo = Draw();
            StartRooms();
        }

        // ==================================================================
        // ACTE 1 — Les pièces
        // ==================================================================
        private void StartRooms()
        {
            _act = 1;
            Locked = false;
            _seen = new bool[Rooms.Length];
            Question.Text = "🏠 Range la maison";
            _moteur.Decouverte();
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.Visual("maison", "🏠", 90), () => "Voici la maison ! Regarde chaque pièce pour savoir à quoi elle sert.");
            Schedule(450, () => Speak("Regarde chaque pièce de la maison !"));
        }

        private void DiscoverRoom(int i)
        {
            if (_act != 1 || Locked) return;
            var r = Rooms[i];
            _moteur.Decouvre(i);      // la pièce s'allume et s'anime
            bool first = !_seen[i];
            _seen[i] = true;
            Speak(Cap(r.Name) + " ! " + r.Says);
            if (first) RewardStar();
            if (_seen.All(x => x))
            {
                Locked = true;
                Schedule(3600, () =>
                {
                    Speak("Tout est en désordre ! On range ?");
                    Schedule(3800, StartTidy);
                });
            }
        }

        // ==================================================================
        // ACTE 2 — On range
        // ==================================================================
        private void StartTidy()
        {
            _act = 2;
            _idx = -1;
            NextThing();
        }

        private void NextThing()
        {
            Locked = false;
            _idx++;
            if (_idx >= _todo.Count) { Finish(); return; }
            var item = _todo[_idx];
            Question.Text = "🧺 Où va " + item.Name + " ?   ✅ " + _idx + " / " + _todo.Count;
            // L'objet à ranger attend sur son tapis, devant la maison ; sans réponse, la bonne pièce finit par clignoter.
            _moteur.Montre(item.Img, Array.IndexOf(Rooms, item.Room));
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.Visual(item.Img, "📦", 90), () => "Où va " + item.Name + " ? Dans la cuisine, la salle de bain, ou la chambre ? Regarde la bonne pièce !");
            Schedule(400, () => Speak(_idx == 0 ? "Où va " + item.Name + " ? Regarde la bonne pièce !" : "Et " + item.Name + ", où est-ce que ça va ?"));
        }

        private void Put(int i)
        {
            if (_act != 2 || Locked) return;
            var item = _todo[_idx];
            var r = Rooms[i];
            if (r == item.Room)
            {
                Locked = true;
                GameKit.Success();
                _moteur.Range(i);     // l'objet s'envole dans la pièce et y prend sa place
                Speak("Oui ! " + Cap(item.Name) + " va dans " + r.Name + " ! " + GameKit.Praise());
                RewardStar();
                Schedule(2600, NextThing);
            }
            else
            {
                _moteur.Refuse(i);    // la pièce s'assombrit un instant, l'objet fait non
                Speak("Non, " + item.Name + " ne va pas dans " + r.Name + ". " + item.Room.Says + " Où est-ce ?");
            }
        }

        // ==================================================================
        // ACTE 3 — La maison est rangée !
        // ==================================================================
        private void Finish()
        {
            _act = 3;
            Locked = true;
            Question.Text = "🎉 La maison est rangée !";
            SetConsigne(null, null);
            _moteur.Fete();           // toutes les pièces s'allument, les étoiles pleuvent
            Speak("La maison est rangée ! " + GameKit.Praise());
            Celebrate();
            Schedule(4500, NewRound);
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
