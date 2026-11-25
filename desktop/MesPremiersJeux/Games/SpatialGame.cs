using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🛏️ « Sur, sous, devant, derrière » (PS/MS — se repérer dans l'espace) : une
    /// peluche et un lit, dans une chambre. Quatre cartes montrent les quatre
    /// places en petit ; c'est elles que l'enfant choisit du regard.
    ///   1. ON DÉCOUVRE — un regard sur une carte, et la peluche saute à cette
    ///      place : « Le nounours est SUR le lit ! ». Les quatre places.
    ///   2. OÙ EST-IL ? — la peluche se place toute seule ; l'enfant retrouve la
    ///      carte qui montre la même chose, et la voix dit le mot.
    ///   3. METS-LE… — la voix seule donne le mot (« Mets le canard SOUS le
    ///      lit ! ») ; la peluche attend à côté du lit et y saute si c'est juste.
    /// L'erreur enseigne : la voix nomme la place choisie, puis redit celle qu'on
    /// cherche. Zéro lecture.
    ///
    /// L'activité pose les questions et parle ; la chambre qu'on voit est dans
    /// LitMoteur.cs, ses dessins dans ReperesArt.cs (fabriqué par
    /// desktop/jeux-dessines).
    /// </summary>
    public sealed class SpatialGame : GameControl
    {
        // Les places, dans l'ordre du moteur : 0 sur, 1 sous, 2 devant, 3 derrière.
        private static readonly string[] Mots = { "SUR", "SOUS", "DEVANT", "DERRIÈRE" };
        private static readonly string[] Phrases = { "sur le lit", "sous le lit", "devant le lit", "derrière le lit" };
        private static readonly string[] Peluches = { "le nounours", "le canard", "le pingouin" };

        private readonly Random _rng = new Random();
        private readonly LitMoteur _moteur = new LitMoteur();
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private int _act, _round, _peluche, _cible, _lastCible = -1;
        private int[] _ordre = { 0, 1, 2, 3 };
        private bool[] _seen;

        public SpatialGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(SpatialGame), null, incruste: true);
            _moteur.Demande = quoi =>
            {
                if (Closed || quoi == null || !quoi.StartsWith("choix:") || !int.TryParse(quoi.Substring(6), out int k) || k < 0 || k > 3) return;
                if (_act == 1) Discover(k); else Answer(k);
            };
        }

        protected override void NewRound()
        {
            _peluche = _rng.Next(Peluches.Length);
            StartDiscover();
        }

        private string Nom => Peluches[_peluche];

        // ==================================================================
        // ACTE 1 — On découvre les quatre places
        // ==================================================================
        private void StartDiscover()
        {
            _act = 1;
            Locked = false;
            _seen = new bool[4];
            _ordre = new[] { 0, 1, 2, 3 };
            Question.Text = "🛏️ Sur, sous, devant, derrière";
            _moteur.Prepare(_peluche, _ordre, 4);
            if (_scene.Parent == null) SetBodyFill(_scene);
            SetConsigne(Art.ForEmoji("🧸", 90), () => "Regarde une image : " + Nom + " ira à cette place ! Essaie les quatre !");
            Schedule(450, () => Speak("Regarde une image, et " + Nom + " ira là !"));
        }

        private void Discover(int k)
        {
            if (_act != 1 || Locked) return;
            int place = _ordre[k];
            _moteur.Va(place);
            _moteur.Fete(k);
            bool first = !_seen[place];
            _seen[place] = true;
            Speak(Cap(Nom) + " est " + Mots[place] + " le lit !");
            if (first) RewardStar();
            if (_seen.All(x => x))
            {
                Locked = true;
                _moteur.Actif(false);
                Schedule(3000, () =>
                {
                    Speak("Tu connais les quatre places ! Maintenant, à toi de les retrouver !");
                    Schedule(3600, () => { _act = 2; _round = 0; NextQuestion(); });
                });
            }
        }

        // ==================================================================
        // ACTES 2 et 3 — Où est-il ?  puis  Mets-le…
        // ==================================================================
        private void NextQuestion()
        {
            Locked = false;
            _round++;
            if (_act == 2 && _round > 4) { _act = 3; _round = 1; }
            if (_act == 3 && _round > 6) { Finish(); return; }
            if (_round > 1 || _act == 3) _peluche = (_peluche + 1) % Peluches.Length;

            // la place à trouver : jamais la même deux fois de suite ; les cartes sont mélangées
            int guard = 0;
            do { _cible = _rng.Next(4); } while (_cible == _lastCible && ++guard < 20);
            _lastCible = _cible;
            _ordre = GameKit.Shuffle(new[] { 0, 1, 2, 3 }).ToArray();

            if (_act == 2)
            {
                // la peluche est déjà à sa place : on cherche la carte qui montre la même chose
                Question.Text = "🔎 Où est " + Nom + " ?";
                _moteur.Prepare(_peluche, _ordre, 4);
                _moteur.Va(_cible);
                SetConsigne(Art.ForEmoji("🔎", 90), () => "Où est " + Nom + " ? Regarde l'image qui montre la même chose !");
                Schedule(900, () => Speak("Où est " + Nom + " ? Regarde la même image !"));
            }
            else
            {
                // la peluche attend à côté du lit : la voix seule dit où la mettre
                Question.Text = "👉 Mets " + Nom + " " + Mots[_cible] + " le lit !";
                _moteur.Prepare(_peluche, _ordre, 4);
                SetConsigne(Art.ForEmoji("🧸", 90), () => "Mets " + Nom + " " + Mots[_cible] + " le lit ! Regarde la bonne image !");
                Schedule(500, () => Speak("Mets " + Nom + " " + Mots[_cible] + " le lit !"));
            }
            _moteur.Attendu = _cible;      // sans réponse, la bonne carte finit par briller
            if (_scene.Parent == null) SetBodyFill(_scene);
        }

        private void Answer(int k)
        {
            if (_act < 2 || Locked) return;
            int place = _ordre[k];
            if (place != _cible)
            {
                _moteur.Refuse(k);
                Speak(_act == 2
                    ? "Non, là, " + Nom + " est " + Phrases[place] + ". Regarde bien où il est !"
                    : "Ça, c'est " + Phrases[place] + ". Cherche " + Mots[_cible] + " le lit !");
                return;
            }
            Locked = true;
            GameKit.Success();
            _moteur.Fete(k);
            _moteur.Actif(false);
            _moteur.Va(_cible);       // acte 3 : la peluche saute à sa place ; acte 2 : elle y est déjà, et saute de joie
            Speak("Oui ! " + Cap(Nom) + " est " + Mots[_cible] + " le lit ! " + GameKit.Praise());
            RewardStar();
            Celebrate();
            Schedule(3400, NextQuestion);
        }

        private void Finish()
        {
            Locked = true;
            Question.Text = "🎉 Sur, sous, devant, derrière : tu sais !";
            SetConsigne(null, null);
            Speak("Sur, sous, devant, derrière : tu sais tout ! " + GameKit.Praise());
            Celebrate();
            Schedule(4500, NewRound);
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
